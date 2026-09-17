#requires -Version 7.0
param([string]$BaseUrl = 'http://localhost:5138')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Get-Page([string]$Path) {
    $response = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $Path)) -UseBasicParsing
    Assert-True ($response.StatusCode -eq 200) "HTTP failure: $Path"
    return [Net.WebUtility]::HtmlDecode($response.Content)
}

$html = Get-Page ''
Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Home must have one H1'
Assert-True ($html.Contains('<title>NexNovaCo | Smart Software, Powerful AI</title>')) 'Incorrect Home title'
Assert-True (-not $html.Contains('migration-placeholder')) 'Home is still a placeholder'
$lastIndex = -1
foreach ($section in @('head', 'about', 'service', 'project', 'team', 'counter', 'partnership', 'testimonial')) {
    $match = [regex]::Match($html, ('<section class="' + $section + '(?: |")'))
    Assert-True ($match.Success -and $match.Index -gt $lastIndex) "Missing/out-of-order section: $section"
    $lastIndex = $match.Index
}
Assert-True ([regex]::Matches($html, 'data-carousel-kind=').Count -eq 3) 'Expected three Razor carousel roots'
Assert-True ([regex]::Matches($html, 'class="service_box"').Count -eq 5) 'Expected five featured services'
Assert-True ([regex]::Matches($html, 'class="team_member_box"').Count -eq 4) 'Expected four featured members'
Assert-True ([regex]::Matches($html, 'class="partnership_link"').Count -eq 6) 'Expected six partners'
Assert-True (-not [regex]::IsMatch($html, '<a[^>]+class="partnership_link"')) 'Partners without URLs must not be links'
foreach ($value in @(450, 3000, 1000, 26)) {
    Assert-True ($html.Contains("data-count-value=`"$value`"")) "Missing statistic: $value"
}
foreach ($obsolete in @('loading-container', 'src="js/script.js"', 'src="js/aos.js"', 'projectdetail.html', 'memberdetail.html')) {
    Assert-True (-not $html.Contains($obsolete)) "Obsolete Home behavior/link present: $obsolete"
}

# The complete approved Home text stays canonical, not just the named featured entities.
$staticHtml = Get-Content (Join-Path $repoRoot 'index.html') -Raw
function Get-TextBlocks([string]$source) {
    [regex]::Matches($source, '<(?:p|h[1-6])\b[^>]*>(.*?)</(?:p|h[1-6])>', 'Singleline') | ForEach-Object {
        [regex]::Replace([Net.WebUtility]::HtmlDecode([regex]::Replace($_.Groups[1].Value, '<[^>]+>', ' ')), '\s+', ' ').Trim()
    }
}
$renderedText = @(Get-TextBlocks $html)
foreach ($text in (Get-TextBlocks $staticHtml)) {
    Assert-True ($renderedText -ccontains $text) "Approved static text missing from Home: $text"
}

$projects = Get-Content (Join-Path $repoRoot 'assets/data/projects.json') -Raw | ConvertFrom-Json
$members = Get-Content (Join-Path $repoRoot 'assets/data/member.json') -Raw | ConvertFrom-Json
foreach ($slug in @('nexconnect', 'payflowx', 'medilink', 'tradesync', 'eduvance')) {
    $project = $projects | Where-Object id -EQ $slug
    foreach ($value in @($project.name, $project.secondName, $project.subtitle, "projects/$slug")) {
        Assert-True ($html.Contains($value)) "Non-canonical/missing project content: $value"
    }
    Assert-True ((Get-Page "projects/$slug").Contains("<h1>$($project.name)</h1>")) "Missing detail placeholder: $slug"
}
foreach ($slug in @('emilyjohnson', 'emmawilliams', 'sophialee', 'danielkim')) {
    $member = $members | Where-Object id -EQ $slug
    foreach ($value in @($member.name, $member.role, $member.image.Replace('assets/', ''), "team/$slug", $member.email, $member.linkedIn)) {
        if ($value) { Assert-True ($html.Contains($value)) "Non-canonical/missing member content: $value" }
    }
    Assert-True ((Get-Page "team/$slug").Contains("<h1>$($member.name)</h1>")) "Missing profile placeholder: $slug"
}
foreach ($path in @('projects/not-a-project', 'team/not-a-member')) {
    $response = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -SkipHttpErrorCheck
    Assert-True ($response.StatusCode -eq 404 -and $response.Content.Contains('Page not found')) "Unknown detail should be 404: $path"
}

$images = [regex]::Matches($html, '<img\b[^>]*>')
foreach ($image in $images) {
    Assert-True ([regex]::IsMatch($image.Value, '\balt="[^"]+"')) "Missing meaningful image alt: $image"
    $path = [regex]::Match($image.Value, '\bsrc="([^"]+)"').Groups[1].Value
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
foreach ($path in @('js/home.js', 'js/countUp.umd.js', 'js/countUp.LICENSE.md', 'css/home-blazor.css')) {
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
Write-Output "PASS: eight ordered Home sections, one H1, typed featured subsets/canonical JSON values, nine detail placeholders, two detail 404s, $($images.Count) images/alt text, Home assets, and no obsolete Home script/loader."
