#requires -Version 7.0
param([string]$BaseUrl = 'http://localhost:5138')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$wwwroot = Join-Path $repoRoot 'src/NexNovaCo.Web/wwwroot'
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Get-Resource([string]$Path) {
    $response = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $Path)) -UseBasicParsing
    Assert-True ($response.StatusCode -eq 200) "HTTP failure: $Path"
    return $response
}

$routes = [ordered]@{ '' = 'Home'; about = 'About'; services = 'Services'; projects = 'Projects'; team = 'Team'; contact = 'Contact' }
$renderedAssets = [Collections.Generic.HashSet[string]]::new()
foreach ($route in $routes.GetEnumerator()) {
    $html = (Get-Resource $route.Key).Content
    $hasHeading = if ($route.Key -eq '') { $html.Contains('Smart Software') -and $html.Contains('<h1>') }
        elseif ($route.Key -eq 'services') { $html.Contains('What We Offer') -and $html.Contains('services-page') }
        elseif ($route.Key -eq 'about') { $html.Contains('About Us') -and $html.Contains('about-page') }
        elseif ($route.Key -eq 'projects') { $html.Contains('Our Projects') -and $html.Contains('projects-page') }
        else { $html.Contains("<h1>$($route.Value)</h1>") }
    Assert-True $hasHeading "Missing heading at /$($route.Key)"
    Assert-True ($html.Contains('aria-label="Primary"') -and $html.Contains('aria-label="Footer"')) "Missing shared navigation at /$($route.Key)"
    Assert-True ($html.Contains('aria-expanded="false"')) "Mobile state missing at /$($route.Key)"
    Assert-True (-not $html.Contains('Foundation diagnostics')) "Diagnostics leaked into normal route /$($route.Key)"
    foreach ($match in [regex]::Matches($html, '(?:src|href)="([^"]+)"')) {
        $path = $match.Groups[1].Value
        if ($path -match '^(css/|js/|image/|_content/|_framework/|Components/Layout/|NexNovaCo.Web\.)') {
            $null = $renderedAssets.Add($path)
        }
    }
}

foreach ($path in $renderedAssets) { $null = Get-Resource $path }

$assetCount = 0
foreach ($folder in @('css', 'js', 'image', 'data')) {
    $sourceRoot = Join-Path $repoRoot "assets/$folder"
    foreach ($source in Get-ChildItem -LiteralPath $sourceRoot -File -Recurse) {
        $relative = [IO.Path]::GetRelativePath((Join-Path $repoRoot 'assets'), $source.FullName).Replace('\', '/')
        $copy = Join-Path $wwwroot $relative
        Assert-True (Test-Path -LiteralPath $copy) "Missing copied asset: $relative"
        $same = (Get-FileHash -LiteralPath $source.FullName).Hash -eq (Get-FileHash -LiteralPath $copy).Hash
        if (-not $same -and $source.Extension -in @('.css', '.js', '.json', '.svg')) {
            # Git on Windows can check out an unchanged text blob with different line endings.
            $same = [IO.File]::ReadAllText($source.FullName).Replace("`r`n", "`n") -ceq [IO.File]::ReadAllText($copy).Replace("`r`n", "`n")
        }
        Assert-True $same "Changed legacy asset copy: $relative"
        $canonical = Get-Resource $relative
        $alias = Get-Resource "assets/$relative"
        $servedLength = (Get-Item -LiteralPath $copy).Length
        Assert-True ($canonical.RawContentLength -eq $servedLength) "Incorrect canonical asset: $relative"
        Assert-True ($alias.RawContentLength -eq $servedLength) "Incorrect alias asset: $relative"
        $assetCount++
    }
}

foreach ($path in @('_content/MudBlazor/MudBlazor.min.css', '_content/MudBlazor/MudBlazor.min.js', '_framework/blazor.web.js', 'css/public-shell.css', 'js/public-shell.js')) {
    $null = Get-Resource $path
}

$missing = Invoke-WebRequest -Uri ([Uri]::new($baseUri, 'missing-phase-1-check')) -UseBasicParsing -SkipHttpErrorCheck
Assert-True ($missing.StatusCode -eq 404) 'Unknown route should return HTTP 404'
Assert-True ($missing.Content.Contains('Page not found')) 'Unknown route should render the not-found shell'

Write-Output "PASS: $($routes.Count) direct routes, $assetCount unchanged asset copies, canonical + legacy asset URLs, $($renderedAssets.Count) rendered asset URLs (including fingerprints), runtime resources, and HTTP 404 shell."
