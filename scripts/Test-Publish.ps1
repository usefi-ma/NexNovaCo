#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$PublishPath,
    [string]$BaseUrl = 'http://localhost:5158'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PublishPath).Path
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Get-Response([string]$Path) {
    Invoke-WebRequest -Uri ([Uri]::new($baseUri, $Path)) -UseBasicParsing -SkipHttpErrorCheck
}

$excludedScripts = @('script.js', 'inner-project.js', 'member.js', 'contact.js', 'bootstrap.min.js', 'aos.js', 'jquery-3.1.0.js')
$forbiddenFiles = @('appsettings.Development.json', 'NexNovaCo.Web.pdb', 'packages.lock.json', 'launchSettings.json')
$files = Get-ChildItem -LiteralPath $root -Recurse -File
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
    Assert-True ($file.Name -notin $forbiddenFiles) "Unnecessary development file published: $relative"
    Assert-True ($relative -notmatch '(^|/)(\.git|\.vs|bin|obj|tests|docs|scripts|node_modules)/') "Source/build directory published: $relative"
    Assert-True ($file.Extension -notin @('.html', '.cs', '.csproj', '.razor', '.sln', '.ps1', '.pfx', '.p12', '.pem', '.key', '.log', '.user', '.suo')) "Unexpected source/private file published: $relative"
    Assert-True ($file.Name -notmatch '^\.env($|\.)') "Local environment file published: $relative"
}
foreach ($script in $excludedScripts) {
    foreach ($suffix in @('', '.br', '.gz')) {
        Assert-True (-not (Test-Path -LiteralPath (Join-Path $root "wwwroot/js/$script$suffix"))) "Excluded script published: $script$suffix"
    }
}
foreach ($required in @('NexNovaCo.Web.dll', 'MudBlazor.dll', 'NexNovaCo.Web.runtimeconfig.json',
    'NexNovaCo.Web.staticwebassets.endpoints.json', 'wwwroot/_framework/blazor.web.js',
    'wwwroot/_content/MudBlazor/MudBlazor.min.css', 'wwwroot/_content/MudBlazor/MudBlazor.min.js',
    'wwwroot/data/projects.json', 'wwwroot/data/member.json', 'wwwroot/js/countUp.LICENSE.md')) {
    Assert-True (Test-Path -LiteralPath (Join-Path $root $required)) "Missing runtime/content file: $required"
}
$manifest = Get-Content -LiteralPath (Join-Path $root 'NexNovaCo.Web.staticwebassets.endpoints.json') -Raw | ConvertFrom-Json
foreach ($script in $excludedScripts) {
    Assert-True (@($manifest.Endpoints | Where-Object { $_.AssetFile -eq "js/$script" -or $_.Route -eq "js/$script" }).Count -eq 0) "Excluded asset remains in endpoint manifest: $script"
}

$routes = [ordered]@{
    '' = 'NexNovaCo'
    services = 'Services | NexNovaCo'
    about = 'About | NexNovaCo'
    projects = 'Projects | NexNovaCo'
    team = 'Team | NexNovaCo'
    contact = 'Contact | NexNovaCo'
}
$projects = Get-Content -LiteralPath (Join-Path $root 'wwwroot/data/projects.json') -Raw | ConvertFrom-Json
$members = Get-Content -LiteralPath (Join-Path $root 'wwwroot/data/member.json') -Raw | ConvertFrom-Json
foreach ($project in $projects) { $routes["projects/$($project.id)"] = "$($project.name) | NexNovaCo" }
foreach ($member in $members) { $routes["team/$($member.id)"] = "$($member.name) | NexNovaCo" }
$assetUrls = [Collections.Generic.HashSet[string]]::new()
$descriptions = [Collections.Generic.HashSet[string]]::new()
foreach ($route in $routes.GetEnumerator()) {
    $response = Get-Response $route.Key
    Assert-True ($response.StatusCode -eq 200) "Route failed: /$($route.Key)"
    $html = $response.Content
    $title = [Net.WebUtility]::HtmlDecode([regex]::Match($html, '<title>(.*?)</title>').Groups[1].Value)
    Assert-True ($title -ceq $route.Value) "Wrong title at /$($route.Key): $title"
    $description = [regex]::Matches($html, '<meta name="description" content="([^"]*)"')
    Assert-True ($description.Count -eq 1 -and $description[0].Groups[1].Value.Length -gt 30) "Missing/duplicate description: /$($route.Key)"
    Assert-True ($descriptions.Add($description[0].Groups[1].Value)) "Duplicate route description: /$($route.Key)"
    Assert-True (-not $html.Contains('HTML template')) "Template metadata leaked: /$($route.Key)"
    Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) "Expected one H1: /$($route.Key)"
    Assert-True ($html.Contains('<main ') -and $html.Contains('<footer ') -and $html.Contains('aria-label="Primary"')) "Missing shared landmarks: /$($route.Key)"
    Assert-True (-not $html.Contains('Foundation diagnostics')) "Development diagnostics exposed: /$($route.Key)"
    Assert-True ($html.Contains('Newsletter signup is not available yet.')) "Newsletter status missing: /$($route.Key)"
    Assert-True ($html -match '<input[^>]*aria-label="Newsletter email \(unavailable\)"[^>]*disabled') "Newsletter input must remain disabled: /$($route.Key)"
    foreach ($match in [regex]::Matches($html, '(?:src|href)="([^"]+)"')) {
        $path = [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
        if ($path -match '^(css/|js/|image/|_content/|_framework/|Components/Layout/|NexNovaCo.Web\.)') {
            $null = $assetUrls.Add($path)
        }
    }
    foreach ($script in $excludedScripts) {
        Assert-True ($html -notmatch ('<script[^>]+src="[^"]*/' + [regex]::Escape($script) + '"')) "Legacy script loaded: $script"
    }
    Assert-True ([regex]::Matches($html, '<script[^>]+src="[^"]*blazor\.web(?:\.[a-z0-9]+)?\.js"').Count -eq 1) "Blazor script missing/duplicated: /$($route.Key)"
    Assert-True ([regex]::Matches($html, '<script[^>]+src="[^"]*MudBlazor\.min(?:\.[a-z0-9]+)?\.js"').Count -eq 1) "MudBlazor script missing/duplicated: /$($route.Key)"
    Assert-True ([regex]::Matches($html, '<link[^>]+href="[^"]*MudBlazor\.min(?:\.[a-z0-9]+)?\.css"').Count -eq 1) "MudBlazor stylesheet missing/duplicated: /$($route.Key)"
}
foreach ($asset in $assetUrls) {
    Assert-True ((Get-Response $asset).StatusCode -eq 200) "Broken rendered asset: $asset"
}
foreach ($script in @('public-shell.js', 'home.js', 'services.js', 'about.js', 'projects.js', 'project-detail.js', 'team.js', 'contact-page.js', 'carousels.js', 'reveal.js', 'jquery-3.7.1.min.js', 'owl.carousel.min.js', 'countUp.umd.js')) {
    Assert-True ((Get-Response "js/$script").StatusCode -eq 200) "Missing active enhancement: $script"
}
foreach ($script in $excludedScripts) {
    Assert-True ((Get-Response "js/$script").StatusCode -eq 404) "Excluded script still served: $script"
    Assert-True ((Get-Response "assets/js/$script").StatusCode -eq 404) "Excluded script still served through alias: $script"
}
foreach ($privatePath in @('appsettings.json', 'appsettings.Development.json', '.env', 'Properties/launchSettings.json', 'NexNovaCo.Web.dll', 'NexNovaCo.Web.pdb', 'index.html')) {
    Assert-True ((Get-Response $privatePath).StatusCode -eq 404) "Private/source file is web-accessible: $privatePath"
}
foreach ($invalid in @('projects/not-a-real-project', 'team/not-a-real-member', 'not-a-real-route')) {
    $response = Get-Response $invalid
    Assert-True ($response.StatusCode -eq 404 -and $response.Content.Contains('Page not found')) "Unsafe not-found handling: $invalid"
    Assert-True ($response.Content.Contains('name="robots" content="noindex"')) "Not-found page missing noindex: $invalid"
}
Assert-True (-not (Get-Response '?verify=foundation').Content.Contains('Foundation diagnostics')) 'Diagnostics query must not enable Development UI in Production'
$contactHtml = (Get-Response 'contact').Content
Assert-True ($contactHtml.Contains('Demo only: this form does not send or store messages.')) 'Contact must disclose demo behavior before input'
Assert-True ($contactHtml.Contains('aria-describedby="contact-demo-notice"')) 'Contact demo notice must be associated with the form'
Assert-True ($contactHtml.Contains('title="Map showing Calgary Tower in downtown Calgary"')) 'Map title missing'
Assert-True ($contactHtml.Contains('https://www.google.com/maps/embed?')) 'Map embed missing'
$errorHtml = (Get-Response 'Error').Content
Assert-True ($errorHtml.Contains('Something went wrong') -and -not $errorHtml.Contains('StackTrace')) 'Error route must remain generic'

# Existing non-square source artwork is retained; advertised dimensions must be truthful.
$homeHtml = (Get-Response '').Content
foreach ($name in @('favicon-32x32.png', 'favicon-16x16.png')) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $root "wwwroot/image/favicon/$name"))
    $width = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes, 16))
    $height = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes, 20))
    Assert-True ($homeHtml.Contains("sizes=`"${width}x${height}`"")) "Favicon declaration must match PNG dimensions: $name"
}

Write-Output "PASS: clean publish, $($routes.Count) routes with unique titles/descriptions, $($assetUrls.Count) rendered assets, active JS, excluded legacy/private paths, safe 404s, Production diagnostics guard, and honest demo forms."
