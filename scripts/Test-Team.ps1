#requires -Version 7.0
param([string]$BaseUrl = 'http://localhost:5138')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Get-Page([string]$Path) { [Net.WebUtility]::HtmlDecode((Invoke-WebRequest -Uri ([Uri]::new($baseUri, $Path))).Content) }
$html = Get-Page 'team'
$homeHtml = Get-Page ''
$canonical = @(Get-Content (Join-Path $repoRoot 'assets/data/member.json') -Raw | ConvertFrom-Json)
$reference = Get-Content (Join-Path $repoRoot 'team.html') -Raw
Assert-True ($html.Contains('<title>Team | NexNovaCo</title>')) 'Incorrect Team title'
Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Team requires one logical H1'
Assert-True ($html.Contains('href="team#Team"') -and $html.Contains('id="Team"')) 'Hero anchor must stay on Team'
Assert-True ($html.Contains('href="contact"')) 'Collaboration CTA must retain the Contact route'
foreach ($class in @('inner-hero-desktop-heading','inner-hero-mobile-heading','inner-hero-mobile-title')) {
    Assert-True ([regex]::IsMatch($html, 'class="' + $class + '"[^>]*>Our Team</')) 'Incorrect responsive Team hero title'
}
function Text-Blocks([string]$Source) {
    [regex]::Matches($Source, '<(?:p|h[1-6])\b[^>]*>(.*?)</(?:p|h[1-6])>', 'Singleline') | ForEach-Object {
        [regex]::Replace([Net.WebUtility]::HtmlDecode([regex]::Replace($_.Groups[1].Value, '<[^>]+>', ' ')), '\s+', ' ').Trim()
    }
}
$actual = @(Text-Blocks $html)
foreach ($text in (Text-Blocks $reference)) { Assert-True ($actual -ccontains $text) "Missing approved Team copy: $text" }
$cards = @([regex]::Matches($html, '<div class="team_member_box">.*?</svg>\s*</a>\s*</div>\s*</div>\s*</div>', 'Singleline'))
$homeCards = @([regex]::Matches($homeHtml, '<div class="team_member_box">.*?</svg>\s*</a>\s*</div>\s*</div>\s*</div>', 'Singleline'))
Assert-True ($cards.Count -eq 6 -and $homeCards.Count -eq 4) 'Expected six full-team and four featured cards'
for ($i = 0; $i -lt $canonical.Count; $i++) {
    $member = $canonical[$i]
    $card = $cards[$i].Value
    foreach ($value in @($member.name, $member.role, $member.email, $member.linkedin, $member.image.Replace('assets/', ''), "team/$($member.id)")) {
        Assert-True ($card.Contains($value)) "Missing/out-of-order canonical Team field: $value"
    }
    Assert-True ($card.Contains('team_back hexagon')) 'Team must preserve its vertical portrait shape'
    Assert-True ($card.Contains("aria-label=""Email $($member.name)""") -and $card.Contains("aria-label=""$($member.name) on LinkedIn""")) 'Configured social links require accessible names'
    Assert-True ([regex]::IsMatch($card, '<a class="telegram_icon" aria-hidden="true">')) 'Unconfigured Telegram must be decorative, not a fake link'
    if ($i -lt 4) {
        Assert-True ($homeCards[$i].Value.Replace('horizental_hexagon', 'hexagon') -ceq $card) 'Home/Team must share card markup and canonical content apart from the approved silhouette'
    }
    $detail = Get-Page "team/$($member.id)"
    Assert-True ($detail.Contains("<title>$($member.name) | NexNovaCo</title>") -and $detail.Contains('member-detail-page')) 'Team card route must resolve to the current member'
}
Assert-True (-not [regex]::IsMatch($html, '<a\b[^>]+href="(?:#|[^"]*\.html[^"]*)"')) 'No fake/legacy Team links'
Assert-True (-not $html.Contains('migration-placeholder') -and -not $html.Contains('data-carousel-kind=')) 'Team is a grid, not a placeholder or carousel'
Assert-True ($html.Contains('aria-label="Primary"') -and $html.Contains('aria-label="Footer"')) 'Shared shell missing'
foreach ($img in [regex]::Matches($html, '<img\b[^>]*>')) {
    Assert-True ($img.Value -match 'alt="[^"]+"') 'Image requires meaningful alt'
    $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri, [regex]::Match($img.Value, 'src="([^"]+)"').Groups[1].Value))
}
foreach ($path in @('css/team.css','css/team-blazor.css','js/team.js')) { $null = Invoke-WebRequest -Uri ([Uri]::new($baseUri,$path)) }
Write-Output 'PASS: Team hero/intro, six ordered canonical cards, four identical Home cards except approved portrait silhouette, actual profiles, accessible social links, honest unconfigured accounts, images and clean navigation.'
