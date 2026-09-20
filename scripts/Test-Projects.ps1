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

$html = Get-Page 'projects'
Assert-True ($html.Contains('<title>Projects | NexNovaCo</title>')) 'Incorrect Projects title'
Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Projects must have one logical H1'
Assert-True (-not $html.Contains('migration-placeholder')) 'Projects is still a placeholder'
$lastIndex = -1
foreach ($section in @('inner_page_header', 'project', 'testimonial')) {
    $match = [regex]::Match($html, ('<section class="' + $section + '"'))
    Assert-True ($match.Success -and $match.Index -gt $lastIndex) "Missing/out-of-order section: $section"
    $lastIndex = $match.Index
}
Assert-True ($html.IndexOf('<footer') -gt $lastIndex) 'Shared footer must follow testimonials'

$staticHtml = Get-Content (Join-Path $repoRoot 'project.html') -Raw
$staticMain = $staticHtml.Substring($staticHtml.IndexOf('<section class="inner_page_header">'))
$staticMain = $staticMain.Substring(0, $staticMain.IndexOf('<footer'))
$renderedText = Plain-Text $html
foreach ($block in [regex]::Matches($staticMain, '<(p|h[1-6])\b[^>]*>(.*?)</\1>', 'Singleline')) {
    $value = Plain-Text $block.Groups[2].Value
    Assert-True ($renderedText.Contains($value)) "Approved Projects text missing: $value"
}

$canonical = @(Get-Content (Join-Path $repoRoot 'assets/data/projects.json') -Raw | ConvertFrom-Json)
$approvedSlugs = @([regex]::Matches($staticMain, 'inner-project\.html\?id=([a-z0-9-]+)') | ForEach-Object { $_.Groups[1].Value })
$cards = @([regex]::Matches($html, '<article class="item project_item".*?</article>', 'Singleline'))
Assert-True ($cards.Count -eq 7 -and $cards.Count -eq $approvedSlugs.Count -and $cards.Count -eq $canonical.Count) 'Expected seven approved project cards'
for ($i = 0; $i -lt $cards.Count; $i++) {
    $project = $canonical[$i]
    $card = $cards[$i].Value
    Assert-True ($project.id -ceq $approvedSlugs[$i]) 'Canonical and approved listing order diverged'
    foreach ($value in @($project.name, $project.secondname, $project.subtitle, "image/project/$($project.id).jpg", "href=""projects/$($project.id)""")) {
        Assert-True ($card.Contains($value)) "Card $i has missing/non-canonical content: $value"
    }
    Assert-True ([regex]::Matches($card, '<h2>').Count -eq 1 -and -not $card.Contains('<h5')) 'Project name must be H2; tagline is not a heading'
    Assert-True ($card.Contains('class="project-tagline"')) 'Missing shared tagline semantics'
    Assert-True ($card.Contains("aria-label=""Read more about $($project.name)""")) 'Read More requires a descriptive label'
    Assert-True (-not $card.Contains('target="_blank"')) 'Detail route should use normal app navigation'
    $detail = Get-Page "projects/$($project.id)"
    Assert-True ($detail.Contains("<h1>$($project.name)</h1>") -and $detail.Contains('This project detail page has not been migrated yet.')) 'Detail must remain a working placeholder'
    Assert-True (-not $detail.Contains('id="projectGallery"') -and -not [regex]::IsMatch($detail, '<script[^>]+src="[^"]*inner-project')) 'Full detail behavior leaked into Phase 5'
}
$missing = Invoke-WebRequest -Uri ([Uri]::new($baseUri, 'projects/not-a-project')) -SkipHttpErrorCheck
Assert-True ($missing.StatusCode -eq 404 -and $missing.Content.Contains('Page not found')) 'Unknown slug must return the 404 shell'

$homeHtml = Get-Page ''
$cardPattern = '<div class="project_img">.*?</a>\s*</div>\s*</div>'
$listingCards = @([regex]::Matches($html, $cardPattern, 'Singleline'))
$featuredCards = @([regex]::Matches($homeHtml, $cardPattern, 'Singleline'))
Assert-True ($listingCards.Count -eq 7 -and $featuredCards.Count -eq 5) 'Expected seven listing and five Home cards'
for ($i = 0; $i -lt 5; $i++) { Assert-True ($listingCards[$i].Value -ceq $featuredCards[$i].Value) 'Shared ProjectCard markup/content differs between Home and listing' }

$pagination = [regex]::Match($html, '<div class="pagination" aria-hidden="true">.*?</div>', 'Singleline').Value
Assert-True ([regex]::Matches($pagination, '<span\b').Count -eq 8) 'Expected eight decorative pagination glyphs'
Assert-True ((Plain-Text $pagination) -ceq '« 1 2 3 4 5 6 »') 'Decorative pagination labels changed'
Assert-True ($pagination.Contains('<span class="active">2</span>')) 'Approved decorative active marker changed'
Assert-True (-not [regex]::IsMatch($pagination, '<a\b|<button\b|tabindex|onclick|role="navigation"')) 'Decorative pagination must not be interactive'

$testimonialPattern = '<div class="testimonial_content">.*?<p class="client-name">.*?</p>\s*</div>\s*</div>'
$testimonials = @([regex]::Matches($html, $testimonialPattern, 'Singleline'))
$homeTestimonials = @([regex]::Matches($homeHtml, $testimonialPattern, 'Singleline'))
Assert-True ($testimonials.Count -eq 2 -and $homeTestimonials.Count -eq 2) 'Expected two shared testimonials'
for ($i = 0; $i -lt 2; $i++) { Assert-True ($testimonials[$i].Value -ceq $homeTestimonials[$i].Value) 'Shared testimonial content diverged' }
Assert-True ([regex]::Matches($html, 'data-carousel-kind=').Count -eq 1 -and $html.Contains('data-carousel-kind="testimonials"')) 'Only testimonials should be a carousel'
Assert-True (-not $html.Contains('data-count-value')) 'Projects must not initialize Home counters'
Assert-True ($html.Contains('href="projects#Project"') -and $html.Contains('id="Project"')) 'Hero requires a working route-safe listing anchor'
foreach ($obsolete in @('inner-project.html', 'project.html', 'src="js/script.js"', 'src="js/aos.js"', 'src="js/inner-project.js"', 'src="js/bootstrap')) {
    Assert-True (-not $html.Contains($obsolete)) "Obsolete Projects URL/script: $obsolete"
}
foreach ($image in [regex]::Matches($html, '<img\b[^>]*>')) {
    Assert-True ($image.Value -match '\balt="[^"]+"') 'Every image requires alt text'
    $path = [regex]::Match($image.Value, '\bsrc="([^"]+)"').Groups[1].Value
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
foreach ($path in @('css/project.css', 'css/projects-blazor.css', 'css/inner-page-blazor.css', 'css/project-card-blazor.css', 'css/carousel-blazor.css', 'css/testimonials-blazor.css', 'js/projects.js', 'js/carousels.js', 'js/reveal.js')) {
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
Write-Output 'PASS: three ordered Projects sections, seven canonical cards in approved order, five cards identical to Home, seven detail placeholders and unknown-slug 404, decorative pagination, two shared testimonials, one H1, clean routes, images and shared/route assets.'
