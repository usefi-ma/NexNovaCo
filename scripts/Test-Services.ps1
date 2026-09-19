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

$html = Get-Page 'services'
Assert-True ($html.Contains('<title>Services | NexNovaCo</title>')) 'Incorrect Services title'
Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Services must have one H1'
Assert-True (-not $html.Contains('migration-placeholder')) 'Services is still a placeholder'
$lastIndex = -1
foreach ($section in @('inner_page_header', 'service', 'features', 'how_it_works', 'pricing', 'FAQ')) {
    $match = [regex]::Match($html, ('<section class="' + $section + '"'))
    Assert-True ($match.Success -and $match.Index -gt $lastIndex) "Missing/out-of-order section: $section"
    $lastIndex = $match.Index
}
Assert-True ($html.IndexOf('<footer') -gt $lastIndex) 'Shared footer must follow FAQ'
Assert-True ([regex]::Matches($html, 'class="service_box"').Count -eq 6) 'Expected six services'
Assert-True ([regex]::Matches($html, 'class="features_wrapper"').Count -eq 3) 'Expected three benefits'
Assert-True ([regex]::Matches($html, '<li[^>]+class="outer_horizental_hexagon ').Count -eq 5) 'Expected five ordered process steps'
Assert-True ($html.Contains('<ol class="process-steps"')) 'Process must retain semantic list order'

# Compare all approved editorial headings/paragraphs and feature bullets against the Razor response.
$staticHtml = Get-Content (Join-Path $repoRoot 'service.html') -Raw
$staticMain = $staticHtml.Substring($staticHtml.IndexOf('<section class="inner_page_header">'))
$staticMain = $staticMain.Substring(0, $staticMain.IndexOf('<footer'))
$renderedText = Plain-Text $html
foreach ($block in [regex]::Matches($staticMain, '<(p|h[1-6])\b[^>]*>(.*?)</\1>', 'Singleline')) {
    $value = Plain-Text $block.Groups[2].Value
    Assert-True ($renderedText.Contains($value)) "Approved Services text missing: $value"
}
$sourceFeatures = [regex]::Match($staticMain, '<section class="pricing">(.*?)</section>', 'Singleline').Groups[1].Value
foreach ($feature in [regex]::Matches($sourceFeatures, '<li>(.*?)</li>', 'Singleline')) {
    Assert-True ($renderedText.Contains((Plain-Text $feature.Groups[1].Value))) 'Missing pricing feature'
}

# Home's five cards must be identical; Services adds Strategy, not another version of the catalog.
$homeHtml = Get-Page ''
$pattern = '<div class="service_box">.*?</p>\s*</div>\s*</div>'
$servicesCards = @([regex]::Matches($html, $pattern, 'Singleline') | ForEach-Object { $_.Value })
$homeCards = @([regex]::Matches($homeHtml, $pattern, 'Singleline') | ForEach-Object { $_.Value })
Assert-True ($homeCards.Count -eq 5 -and $servicesCards.Count -eq 6) 'Could not locate canonical cards'
foreach ($card in $homeCards) { Assert-True ($servicesCards -ccontains $card) 'Home and Services card content diverged' }
foreach ($assignment in @(@('Web & Mobile App', 'software.png'), @('AI Solutions', 'analysing.png'), @('UX/UI Design', 'custom.png'))) {
    $card = $servicesCards | Where-Object { $_.Contains("<h3>$($assignment[0])</h3>") }
    Assert-True ($card.Contains("image/service/icons/$($assignment[1])")) "Wrong canonical icon: $($assignment[0])"
}

$pricing = [regex]::Match($html, '<section class="pricing">(.*?)</section>', 'Singleline').Value
$buttons = [regex]::Matches($pricing, '<button\b[^>]+>')
Assert-True ($buttons.Count -eq 3) 'Expected three pricing actions'
foreach ($button in $buttons) {
    Assert-True ($button.Value -match '\bdisabled(?:\s|>|=)' -and $button.Value.Contains('aria-describedby=')) 'Pricing action must be honestly disabled and explained'
}
Assert-True (-not [regex]::IsMatch($pricing, '<a\b')) 'Pricing must not invent checkout/navigation links'

$faq = [regex]::Match($html, '<section class="FAQ">(.*?)</section>', 'Singleline').Value
$toggles = [regex]::Matches($faq, '<button\b[^>]+>')
Assert-True ($toggles.Count -eq 6) 'Expected six FAQ buttons'
Assert-True ([regex]::Matches($faq, 'aria-expanded="true"').Count -eq 1) 'Only the first FAQ starts expanded'
Assert-True ([regex]::Matches($faq, 'aria-expanded="false"').Count -eq 5) 'Remaining FAQs start collapsed'
Assert-True ([regex]::Matches($faq, 'aria-hidden="true" inert').Count -eq 5) 'Closed answers must be hidden from assistive technology and inert'
$ids = @([regex]::Matches($faq, '\bid="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
Assert-True ($ids.Count -eq 12 -and ($ids | Select-Object -Unique).Count -eq 12) 'FAQ IDs must be unique'
foreach ($toggle in $toggles) {
    $controls = [regex]::Match($toggle.Value, 'aria-controls="([^"]+)"').Groups[1].Value
    Assert-True ($ids -contains $controls) 'FAQ aria-controls must reference a real answer'
}
foreach ($obsolete in @('data-bs-toggle', 'data-bs-target', 'src="js/bootstrap', 'src="js/script.js"', 'src="js/aos.js"', 'service.html')) {
    Assert-True (-not $html.Contains($obsolete)) "Obsolete Services interaction/URL: $obsolete"
}
Assert-True ($html.Contains('href="services#Service"')) 'Hero anchor must stay on the Services route'
foreach ($image in [regex]::Matches($html, '<img\b[^>]*>')) {
    Assert-True ($image.Value -match '\balt="[^"]+"') 'Image requires meaningful alt text'
    $path = [regex]::Match($image.Value, '\bsrc="([^"]+)"').Groups[1].Value
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
foreach ($path in @('css/service.css', 'css/inner-page-blazor.css', 'css/services-blazor.css', 'js/services.js', 'js/reveal.js')) {
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
Write-Output 'PASS: six ordered Services sections, approved text/pricing features, six canonical cards shared with Home, three benefits, five process steps, three disabled pricing actions, six accessible FAQ panels, one H1, images and route-local assets.'
