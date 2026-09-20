#requires -Version 7.0
param([string]$BaseUrl = 'http://localhost:5138')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$baseUri = [Uri]($BaseUrl.TrimEnd('/') + '/')
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$html = [Net.WebUtility]::HtmlDecode((Invoke-WebRequest ([Uri]::new($baseUri, 'contact'))).Content)
$reference = Get-Content (Join-Path $repoRoot 'contact.html') -Raw
Assert-True ($html.Contains('<title>Contact | NexNovaCo</title>') -and $html.Contains('contact-page')) 'Contact route/title missing'
Assert-True ([regex]::Matches($html, '<h1(?:\s|>)').Count -eq 1) 'Contact needs one logical H1'
foreach ($text in @('Contact Us','Contact Info:','Get in Touch:',"We're Here!",'Come visit us in downtown Calgary','+1 (587) 555-1234','info@nexnovaco.com','NexNovaCo Inc. 123 Innovation Drive, Suite 456 Calgary, AB T2P 1A1')) {
    Assert-True ($html.Contains($text)) "Missing approved copy: $text"
}
Assert-True ($html.Contains('href="contact#Contact"') -and $html.Contains('id="Contact"')) 'Hero CTA must target Contact'
$form = [regex]::Match($html, '<form\b[^>]*id="contactForm"[\s\S]*?</form>').Value
Assert-True ($form -ne '' -and $form.Contains('novalidate') -and $form.Contains('aria-labelledby="contact-form-heading"')) 'Accessible Blazor form missing'
$fields = [ordered]@{ firstName = 'given-name'; lastName = 'family-name'; email = 'email'; subject = 'off'; message = '' }
foreach ($field in $fields.GetEnumerator()) {
    $input = [regex]::Match($form, '<(?:input|textarea)\b[^>]*id="' + $field.Key + '"[^>]*>').Value
    Assert-True ($input.Contains("name=""$($field.Key)""")) "Field name missing: $($field.Key)"
    Assert-True ($form.Contains("for=""$($field.Key)""")) "Field label missing: $($field.Key)"
    Assert-True ($input.Contains("aria-describedby=""$($field.Key)-error""")) 'Errors must be associated with fields'
    Assert-True ($form.Contains("id=""$($field.Key)-error""")) 'Field error region missing'
    if ($field.Value) { Assert-True ($input.Contains("autocomplete=""$($field.Value)""")) 'Autocomplete mismatch' }
    Assert-True ($input.Contains('required') -eq ($field.Key -ne 'subject')) 'Only subject should be optional'
}
Assert-True ([regex]::IsMatch($form, '<input\b[^>]*type="email"')) 'Email input type must remain email'
Assert-True ([regex]::IsMatch($form, '<textarea\b[^>]*rows="5"[^>]*></textarea>')) 'Message must start truly empty and retain five rows'
Assert-True ([regex]::IsMatch($form, '<button\b[^>]*type="submit"[^>]*id="SubmitButton"')) 'Real submit button missing'
Assert-True ($html.Contains('aria-live="polite"') -and $html.Contains('aria-atomic="true"')) 'Submission feedback must be accessible'
$frame = [regex]::Match($html, '<iframe\b[^>]*>').Value
$sourceMap = [regex]::Match($reference, '<iframe\b[^>]*src="([^"]+)"').Groups[1].Value
Assert-True ($frame.Contains("src=""$sourceMap""")) 'Map must preserve the exact approved Google embed URL'
Assert-True ($frame.Contains('title="Map showing Calgary Tower in downtown Calgary"') -and $frame.Contains('loading="lazy"')) 'Accessible map title/lazy load missing'
Assert-True (-not [regex]::IsMatch($html, '<script\b[^>]*src="[^"]*(?:contact\.|sweetalert|/script\.)')) 'Legacy contact behavior must remain unloaded'
Assert-True (-not $html.Contains('migration-placeholder')) 'Contact placeholder remains'
foreach ($path in @('css/contact.css','css/contact-blazor.css','js/contact-page.js','image/contact/contact-us.jpg','image/about/about-mission-bg.svg')) {
    $null = Invoke-WebRequest ([Uri]::new($baseUri,$path))
}
# Deliberately lightweight previous-page smoke coverage, not full visual/content re-verification.
foreach ($path in @('','services','about','projects','projects/nexconnect','team','team/emilyjohnson')) {
    $page = (Invoke-WebRequest ([Uri]::new($baseUri,$path))).Content
    Assert-True ($page.Contains('aria-label="Primary"') -and $page.Contains('aria-label="Footer"')) "Shared shell missing at /$path"
    Assert-True (-not $page.Contains('migration-placeholder') -and $page.Contains('<h1')) "Migrated route failed: /$path"
    foreach ($img in [regex]::Matches($page, '<img\b[^>]*src="([^"]+)"')) {
        $null = Invoke-WebRequest ([Uri]::new($baseUri,$img.Groups[1].Value))
    }
}
Write-Output 'PASS: Contact content, five labeled fields, required/optional semantics, empty textarea, status region, exact map URL/title, assets and seven previous-route smoke checks.'
