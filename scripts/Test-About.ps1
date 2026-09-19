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
function Plain-Text([string]$Markup) {
    [regex]::Replace([Net.WebUtility]::HtmlDecode([regex]::Replace($Markup, '<[^>]+>', ' ')), '\s+', ' ').Trim()
}

$html = Get-Page 'about'
Assert-True ($html.Contains('<title>About | NexNovaCo</title>')) 'Incorrect About title'
Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'About must have one logical H1'
Assert-True (-not $html.Contains('migration-placeholder')) 'About is still a placeholder'
$lastIndex = -1
foreach ($section in @('inner_page_header', 'About', 'vision', 'history', 'mision', 'partnership')) {
    $match = [regex]::Match($html, ('<section class="' + $section + '"'))
    Assert-True ($match.Success -and $match.Index -gt $lastIndex) "Missing/out-of-order section: $section"
    $lastIndex = $match.Index
}
Assert-True ($html.IndexOf('<footer') -gt $lastIndex) 'Shared footer must follow partners'

$staticHtml = Get-Content (Join-Path $repoRoot 'about.html') -Raw
$staticMain = $staticHtml.Substring($staticHtml.IndexOf('<section class="inner_page_header">'))
$staticMain = $staticMain.Substring(0, $staticMain.IndexOf('<footer'))
$renderedText = Plain-Text $html
foreach ($block in [regex]::Matches($staticMain, '<(p|h[1-6]|span)\b[^>]*>(.*?)</\1>', 'Singleline')) {
    $value = Plain-Text $block.Groups[2].Value
    Assert-True ($renderedText.Contains($value)) "Approved About text missing: $value"
}

$timeline = [regex]::Match($html, '<ol class="row history_boxes about-timeline".*?</ol>', 'Singleline').Value
Assert-True ([regex]::Matches($timeline, '<li\b').Count -eq 3) 'Expected three semantic milestones'
$previous = -1
foreach ($year in 2023, 2024, 2025) {
    $index = $timeline.IndexOf("<h3 class=""visually-hidden"">$year</h3>")
    Assert-True ($index -gt $previous) 'Milestones must expose full years in chronological order'
    $previous = $index
}
Assert-True ([regex]::Matches($timeline, 'class="hexagon" aria-hidden="true"').Count -eq 3) 'Split years must be decorative'
Assert-True ([regex]::Matches($html, 'role="listitem"').Count -eq 4) 'Expected four mission commitments'

$homeHtml = Get-Page ''
$partnerPattern = '<div class="partnership_link">.*?</p>\s*</div>'
$aboutPartners = @([regex]::Matches($html, $partnerPattern, 'Singleline') | ForEach-Object { $_.Value })
$homePartners = @([regex]::Matches($homeHtml, $partnerPattern, 'Singleline') | ForEach-Object { $_.Value })
Assert-True ($aboutPartners.Count -eq 6 -and $homePartners.Count -eq 6) 'Expected six canonical partners per page'
for ($i = 0; $i -lt 6; $i++) { Assert-True ($aboutPartners[$i] -ceq $homePartners[$i]) 'About and Home partner markup/content diverged' }
Assert-True ([regex]::Matches($html, 'data-carousel-kind="partners"').Count -eq 1) 'Expected one partner carousel boundary'
Assert-True (-not $html.Contains('data-count-value')) 'About must not render Home counters'
foreach ($href in @('about#About', 'services', 'projects')) {
    Assert-True ($html.Contains("href=""$href""")) "Missing route-safe About CTA: $href"
}
Assert-True ($html.Contains('id="About"')) 'Story CTA target must exist'
foreach ($obsolete in @('about.html', 'service.html', 'project.html', 'src="js/script.js"', 'src="js/aos.js"', 'src="js/bootstrap')) {
    Assert-True (-not $html.Contains($obsolete)) "Obsolete About URL/script: $obsolete"
}
foreach ($image in [regex]::Matches($html, '<img\b[^>]*>')) {
    Assert-True ($image.Value -match '\balt="[^"]+"') 'About image requires meaningful alt text'
    $path = [regex]::Match($image.Value, '\bsrc="([^"]+)"').Groups[1].Value
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
foreach ($path in @('css/about.css', 'css/about-blazor.css', 'css/inner-page-blazor.css', 'css/carousel-blazor.css', 'js/about.js', 'js/carousels.js', 'js/reveal.js')) {
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
Write-Output 'PASS: six ordered About sections, approved text, three chronological accessible milestones, four commitments, six canonical partners identical to Home, one carousel, one H1, route-safe CTAs, images and shared/route assets.'
