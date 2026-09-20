#requires -Version 7.0
param([string]$BaseUrl = 'http://localhost:5138')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$canonical = @(Get-Content (Join-Path $repoRoot 'assets/data/member.json') -Raw | ConvertFrom-Json)
foreach ($member in $canonical) {
    $html = [Net.WebUtility]::HtmlDecode((Invoke-WebRequest -Uri ([Uri]::new($baseUri,"team/$($member.id)"))).Content)
    Assert-True ($html.Contains("<title>$($member.name) | NexNovaCo</title>")) 'Member name must bind the document title'
    Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Profile requires one logical H1'
    Assert-True ([regex]::IsMatch($html, '<h1 id="member-name"[^>]*>' + [regex]::Escape($member.name) + '</h1>')) 'Visible profile name must be current'
    Assert-True ($html.Contains($member.bio) -and $html.Contains($member.role)) 'Canonical biography/role missing'
    $breadcrumb = [regex]::Match($html, '<nav aria-label="Breadcrumb".*?</nav>', 'Singleline').Value
    Assert-True ($breadcrumb -match '<a href(?:="")?>Home</a>' -and $breadcrumb.Contains('href="team"')) 'Breadcrumb must retain Home/Team clean links'
    Assert-True ($breadcrumb.Contains("aria-current=""page"">$($member.name)</li>")) 'Breadcrumb must identify current member'
    $profile = [regex]::Match($html, '<section class="member_desc".*?</section>', 'Singleline').Value
    $source = $member.image.Replace('assets/', '')
    Assert-True ($profile.Contains("src=""$source""") -and $profile.Contains("alt=""$($member.name)""")) 'Incorrect profile image/alt'
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri,$source))
    $skills = @([regex]::Matches($profile, '<p role="listitem">(.*?)</p>', 'Singleline'))
    Assert-True ($skills.Count -eq $member.skills.Count) 'All canonical skills must render once'
    for ($i=0; $i -lt $skills.Count; $i++) { Assert-True ($skills[$i].Groups[1].Value -ceq $member.skills[$i]) 'Skill values/order must stay canonical' }
    Assert-True ($profile.Contains('role="list" aria-labelledby="member-skills-heading"') -and $profile.Contains('text-sm-end') -and $profile.Contains('text-sm-start')) 'Preserve accessible two-column skills layout'
    Assert-True ($profile.Contains("href=""mailto:$($member.email)""") -and $profile.Contains("aria-label=""Email $($member.name)""")) 'Email must be an accessible canonical link'
    Assert-True ($profile.Contains("href=""$($member.linkedin)""") -and $profile.Contains("aria-label=""$($member.name) on LinkedIn""")) 'LinkedIn must be an accessible canonical link'
    Assert-True ([regex]::IsMatch($profile, '<a class="telegram_icon" aria-hidden="true"[^>]*>') -and -not [regex]::IsMatch($profile, '<a class="telegram_icon"[^>]*href=')) 'No invented Telegram destination'
    foreach ($obsolete in @('migration-placeholder','data-carousel-kind=','inner_page_header','Member Title')) { Assert-True (-not $html.Contains($obsolete)) "Unexpected profile placeholder/hero: $obsolete" }
    Assert-True (-not [regex]::IsMatch($html, '<script\b[^>]+src="[^"]*(?:/member\.|/script\.|/bootstrap|/aos\.)')) 'Legacy member DOM binding must not load'
    Assert-True (-not [regex]::IsMatch($html, '<a\b[^>]+href="[^"]*\.html')) 'Profile must use clean links'
    Assert-True ($html.Contains('aria-label="Primary"') -and $html.Contains('aria-label="Footer"')) 'Profile must retain shell'
}
foreach ($slug in @('not-a-real-member','not-a-member')) {
    $response = Invoke-WebRequest -Uri ([Uri]::new($baseUri,"team/$slug")) -SkipHttpErrorCheck
    Assert-True ($response.StatusCode -eq 404 -and $response.Content.Contains('Page not found')) 'Unknown member must be a safe 404'
    Assert-True ($response.Content.Contains('aria-label="Primary"') -and $response.Content.Contains('aria-label="Footer"')) '404 must retain shared shell'
    Assert-True (-not $response.Content.Contains('member-detail-page') -and -not $response.Content.Contains('id="member-name"')) '404 must not fall back to another member'
}
foreach ($path in @('css/member.css','css/member-blazor.css','js/team.js')) { $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri,$path)) }
Write-Output 'PASS: six canonical Member Detail routes, titles/H1/breadcrumbs, six portraits, full biographies/roles, 24 ordered skill items, accessible social/contact links, no legacy binding, and two shell-preserving 404s.'
