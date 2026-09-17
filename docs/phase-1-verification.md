# Phase 1 verification record

Verified locally on 2026-09-16. Scope is the shared foundation shell, not full-page parity.

## Environment and builds

- Windows; .NET SDK 10.0.401; target `net10.0`.
- `dotnet restore NexNovaCo.sln --locked-mode`: passed.
- Debug and Release builds: passed, zero warnings and zero errors.
- MudBlazor resolves to exactly 9.10.0. SDK auto-reference: Microsoft.AspNetCore.App.Internal.Assets 10.0.12. No other explicit packages.
- Interactive Server development host: `http://localhost:5138`.
- The sandbox initially blocked access to ASP.NET's user-level data-protection keys; running the local host with authorized access resolved this. No application security/data-protection workaround was added.

## Browser matrix

Each of `/`, `/about`, `/services`, `/projects`, `/team`, `/contact` was exercised through the shared header at every width below. Desktop links were used at 1440px; the existing mobile-menu breakpoint applies at the other widths.

| Width | Six routes | Active links in both navs | Menu closes after route | Horizontal overflow | Broken rendered images |
| --- | --- | --- | --- | --- | --- |
| 1440px | Pass | Pass | N/A (desktop) | None | None |
| 1366px | Pass | Pass | Pass | None | None |
| 768px | Pass | Pass | Pass | None | None |
| 390px | Pass | Pass | Pass | None | None |

Additional browser checks:

- One shared header and footer; route heading and document title update.
- Mobile open/close labels and `aria-expanded` track Blazor state.
- Escape from the toggle and from a focused menu link closes the menu and restores focus to the toggle.
- Closed mobile links become hidden, including from the accessibility tree after the slide transition.
- Footer routing and browser Back work. Route changes close the menu.
- Scrolling past 100px enables the original fixed-header styling and back-to-top control. Back-to-top returns scroll position to zero and focuses the main landmark.
- Screenshots inspected at all four widths, including the 1366px open menu and the 390px lower footer/newsletter.
- Console: no warning/error entries from the app during the completed checks. Fonts and rendered logo images loaded.
- At `/?verify=foundation`, MudSelect opened its listbox and accepted a selection, the snackbar showed its success message, and the dialog opened and closed. Overlays appear above the public header. This is an opt-in Development-only check, not a public route or migrated page.

## Theme and reference comparison

The unchanged static `index.html` was served separately for comparison. Computed shared styles match the baseline: Poppins 16px body text, Lato headings, 90px header, 45px-wide logo, dark `#454545` footer, orange `#CC7722` accents and the original newsletter gradient. Desktop footer height was approximately 485.8px in both. Small width differences from browser scrollbar allocation are not treated as redesigns. Header hero geometry is intentionally absent from placeholders and remains deferred.

The only targeted appearance adaptations are a visible blue hamburger on the pale placeholder header, semantic button/focus treatment, and header stacking below MudBlazor overlays. No vendor/legacy stylesheet was edited. The computer-use skill's browser workflow informed the screenshot and interaction checks.

## Repeatable HTTP and asset checks

Run `./scripts/Test-Foundation.ps1` in PowerShell 7 while the app is running. It checks:

- All six routes directly over HTTP, expected headings and shared shell markup.
- Absence of diagnostics from normal URLs.
- SHA-256 equality of all 82 copied assets against the untouched originals.
- HTTP 200 and file length for canonical and `/assets/` alias URLs.
- Actual rendered/fingerprinted resources, MudBlazor CSS/JS, Blazor runtime, and shell resources.
- An unknown URL returns HTTP 404 with the shared not-found page.

The baseline's unused Owl video-play image is absent from the source assets. It is not referenced by any loaded Phase 1 stylesheet/widget. Google Fonts remain external dependencies. Live deployment, TLS provisioning, circuit reconnect fault injection, cross-browser coverage, and full-page visual parity are not claimed by these local checks.

## Deferred acceptance work

Full Home/About/Services/Projects/Team/Contact content, detail routes, page-specific scripts, counters, carousels, AOS, FAQ, real social destinations, newsletter/email services, content models, CMS, database, admin and authentication are deliberately not implemented. The static reference remains untouched. Phase 2 requires approval.
