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

$canonical = @(Get-Content (Join-Path $repoRoot 'assets/data/projects.json') -Raw | ConvertFrom-Json)
$listing = Get-Page 'projects'
$homeHtml = Get-Page ''
$imageCount = 0
foreach ($project in $canonical) {
    $html = Get-Page "projects/$($project.id)"
    Assert-True ($html.Contains("<title>$($project.name) | NexNovaCo</title>")) 'Document title must use the current project'
    Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Detail must have exactly one logical H1'
    foreach ($class in @('inner-hero-desktop-heading', 'inner-hero-mobile-heading', 'inner-hero-mobile-title')) {
        Assert-True ([regex]::IsMatch($html, ('class="' + $class + '"[^>]*>' + [regex]::Escape($project.name) + '<'))) "Incorrect responsive hero title: $class"
    }
    Assert-True ($html.Contains("href=""projects/$($project.id)#inner-project""") -and $html.Contains('id="inner-project"')) 'Hero anchor must remain on the current clean detail route'
    Assert-True ($html.Contains('aria-label="Primary"') -and $html.Contains('aria-label="Footer"')) 'Detail must retain the shared shell'
    foreach ($value in @($project.secondname, $project.subtitle, $project.summary)) {
        Assert-True ($html.Contains($value)) "Missing canonical detail content: $value"
    }
    $breadcrumb = [regex]::Match($html, '<nav aria-label="Breadcrumb".*?</nav>', 'Singleline').Value
    Assert-True ($breadcrumb -match '<a href(?:="")?>Home</a>' -and $breadcrumb.Contains('href="projects"')) 'Breadcrumb must link Home and Projects via clean routes'
    Assert-True ($breadcrumb.Contains("aria-current=""page"">$($project.name)</li>")) 'Breadcrumb must identify the current project'
    $metadata = [regex]::Match($html, '<div class="project-info-box".*?</div>', 'Singleline').Value
    foreach ($field in @('client', 'category', 'date', 'technologies')) {
        Assert-True ($metadata.Contains($project.details.$field)) "Missing project metadata: $field"
    }
    $features = [regex]::Match($html, '<aside class="hexagon inner_project_content_hexagon".*?</aside>', 'Singleline').Value
    Assert-True ([regex]::Matches($features, '<li\b').Count -eq $project.features.Count) 'Features must render once in the original desktop panel'
    foreach ($feature in $project.features) { Assert-True ($features.Contains($feature)) 'Non-canonical/missing feature' }
    Assert-True ([regex]::Matches($html, 'id="project-features-heading"').Count -eq 1) 'Do not duplicate Features for mobile'

    $gallery = [regex]::Match($html, '<div class="carousel slide carousel-fade inner_project_wrapper_slide project-gallery".*?</section>', 'Singleline').Value
    Assert-True ($gallery.Contains("aria-label=""$($project.name) gallery""")) 'Gallery label must bind the current project name'
    $slides = @([regex]::Matches($gallery, '<div class="carousel-item[^>]*>\s*<img[^>]*>\s*</div>'))
    Assert-True ($slides.Count -eq $project.gallery.Count) 'Gallery must contain the ordered canonical media only'
    for ($i = 0; $i -lt $slides.Count; $i++) {
        $expected = $project.gallery[$i]
        $source = $expected.src.Replace('assets/', '')
        Assert-True ($slides[$i].Value.Contains("src=""$source""") -and $slides[$i].Value.Contains("alt=""$($expected.alt)""")) 'Wrong gallery image/order/alt'
        Assert-True ($slides[$i].Value.Contains("aria-label=""$($i + 1) of $($slides.Count)""")) 'Missing slide position semantics'
        if ($i -eq 0) {
            Assert-True ($slides[$i].Value.Contains('carousel-item active') -and $slides[$i].Value.Contains('aria-hidden="false"')) 'First slide must be active on direct navigation'
        } else {
            Assert-True ($slides[$i].Value.Contains('aria-hidden="true"') -and $slides[$i].Value.Contains(' inert')) 'Inactive slides must be hidden from interaction/assistive technology'
        }
        $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $source)) -UseBasicParsing
        $imageCount++
    }
    $hasControls = $gallery.Contains('aria-label="Next project image"') -and $gallery.Contains('aria-label="Previous project image"')
    Assert-True ($hasControls -eq ($slides.Count -gt 1)) 'Only multi-image galleries need navigation controls'
    Assert-True ($html.Contains("image/project/$($project.id).jpg") -or $project.id -eq 'nexconnect') 'Expected project-specific detail media'
    Assert-True ($listing.Contains("image/project/$($project.id).jpg")) 'Listing cover must remain separate from detail media'
    Assert-True ($html.Contains('href="projects" class="custome_btn"')) 'Return-to-listing CTA must be a normal link'
    foreach ($obsolete in @('Project Title', 'migration-placeholder', 'data-bs-target', 'data-bs-slide', 'data-carousel-kind=')) {
        Assert-True (-not $html.Contains($obsolete)) "Obsolete/inappropriate detail behavior: $obsolete"
    }
    Assert-True (-not [regex]::IsMatch($html, '<script\b[^>]+src="[^"]*(?:inner-project|bootstrap|script\.|aos\.)')) 'Legacy DOM binding/Bootstrap JS must remain dormant'
    Assert-True (-not [regex]::IsMatch($html, '<a\b[^>]+href="[^"]*\.html')) 'Legacy HTML navigation must not remain'
    foreach ($image in [regex]::Matches($html, '<img\b[^>]*>')) {
        Assert-True ($image.Value -match '\balt="[^"]+"') 'Every rendered image requires alt text'
    }
}

$nexConnect = Get-Page 'projects/nexconnect'
Assert-True ($nexConnect.Contains('image/project/nextConnectProject.jpg') -and $nexConnect.Contains('image/project/nextConnectInnerProject.jpg')) 'NexConnect must retain both distinct gallery images'
Assert-True ($homeHtml.Contains('image/project/nexconnect.jpg')) 'Home must retain the approved NexConnect cover'
foreach ($path in @('projects/not-a-real-project', 'projects/not-a-project')) {
    $missing = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -SkipHttpErrorCheck
    Assert-True ($missing.StatusCode -eq 404 -and $missing.Content.Contains('Page not found')) 'Invalid slug must return the existing 404 page'
    Assert-True ($missing.Content.Contains('aria-label="Primary"') -and $missing.Content.Contains('aria-label="Footer"')) 'Not found must retain the shared shell'
    Assert-True (-not $missing.Content.Contains('project-detail-page') -and -not $missing.Content.Contains('project-gallery')) 'Invalid slug must not fall back to another project'
}
foreach ($path in @('css/inner-project.css', 'css/inner-page-blazor.css', 'css/project-detail-blazor.css', 'js/project-detail.js')) {
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, $path)) -UseBasicParsing
}
$originalCss = Get-Content (Join-Path $repoRoot 'assets/css/inner-project.css') -Raw
Assert-True ([regex]::IsMatch($originalCss, '(?s)@media screen and \(max-width:\s*1200px\)\s*\{\s*\.inner_project_content_hexagon\s*\{\s*display:\s*none')) 'Approved Features hiding breakpoint must remain 1200px'
Write-Output "PASS: $($canonical.Count) typed Project Detail routes, responsive titles, breadcrumbs, all summaries/metadata/features, $imageCount ordered gallery images, separate covers, single-image controls, clean assets, and two shell-preserving 404s."
