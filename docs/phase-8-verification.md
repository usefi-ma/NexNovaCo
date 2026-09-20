# Phase 8 — Contact migration verification

Verified locally on 2026-09-20. Scope: `/contact` only, with lightweight previous-page regression checks.

## 1. Git status and scope

- Branch: `feature/blazor-public-site`; no branch switch, history rewrite, merge or push.
- Starting commit: `90e138bda183a80a9e152dfba47fc3b443d52c1f` (Phase 7 complete). The working tree was clean before Phase 8 edits.
- Focused implementation commit: `d7412dc` — `Migrate Contact page with Blazor demo form state`.
- Focused verification commit: `Verify Contact visual parity and demo validation`.
- Original HTML, `assets/`, all 82 copied baseline assets and dependency versions remain unchanged.
- No shared Razor shell/component implementation changed. The only shared CSS change appends `.contact-page` to existing `:is(...)` selector lists; declarations and breakpoints are unchanged.

## 2. Files changed

All application paths below are under `src/NexNovaCo.Web/`.

| Group | Files |
| --- | --- |
| Page/composition | `Components/Pages/Contact.razor`, `Components/_Imports.razor` |
| Sections | `Components/Sections/Contact/ContactInfoPanel.razor`, `ContactForm.razor`, `ContactMap.razor` |
| Typed content/model | `Models/ContactContent.cs`, `Models/ContactFormModel.cs` |
| Providers/form service | `Services/IContactContentService.cs`, `ContactContentService.cs`, `IContactFormService.cs`, `DemoContactFormService.cs`; registration in `Program.cs` |
| CSS | `wwwroot/css/contact-blazor.css`, selector-only addition in `wwwroot/css/inner-page-blazor.css` |
| JS | `wwwroot/js/contact-page.js` |
| Tests | `scripts/Test-Foundation.ps1`, `scripts/Test-Contact.ps1`, `scripts/Test-ContactInterop.mjs`, `tests/NexNovaCo.Contact.Tests/{NexNovaCo.Contact.Tests.csproj,Program.cs}` |
| Documentation | `README.md`, this record |

## 3. Contact component tree

```text
MainLayout (unchanged header/navigation/footer/providers)
└── Contact
    ├── InnerPageHero (reused unchanged)
    ├── Contact information/form section (original responsive grid)
    │   ├── ContactInfoPanel
    │   └── ContactForm
    │       ├── EditForm + DataAnnotationsValidator
    │       ├── Four InputText fields + InputTextArea + field errors
    │       └── Focused live submission/validation feedback
    └── ContactMap
```

## 4. Typed Contact data

`ContactContentService` provides an immutable snapshot of the approved `contact.html` copy via `IContactContentService`. Records separate the existing hero, contact panel, five field labels/placeholders, form messages and map presentation. No speculative business fields, CMS model or persistence were added. Source phone/email/address are explicitly documented as demo content, not verified business information.

## 5. Form model and validation

- Exactly the original five fields: FirstName, LastName, Email, Subject, Message.
- First/last name, email and message are required. Data annotations reject whitespace-only values as well as empty values.
- Email is trimmed and uses the cleaned source expression `^[^\s@]+@[^\s@]+\.[^\s@]+$`; malformed addresses, embedded spaces and missing dotted domains fail.
- Subject remains optional, including blank/whitespace-only values. Message starts truly empty and keeps five rows.
- Original IDs/names, labels, appropriate autocomplete values, `type="email"` and `required` semantics remain. `novalidate` allows one consistent Blazor validation path rather than competing native popups.
- Each field has an associated error region. Invalid submit focuses an alert; successful submit focuses a status. Both sit inside a live region. Visible focus styles support keyboard use.
- One logical H1 and semantic H2 sections retain source heading appearance. Decorative SVGs are hidden from assistive technology.

## 6. Submission behavior

`EditForm` owns validation, submitted state and reset. Invalid forms never call the service. Valid forms invoke the stateless `IContactFormService` demo implementation, which revalidates at its boundary and returns exactly:

> Demo form submitted successfully. No message was sent.

Success resets all five fields and the modified state using the same model/EditContext and rendered nodes, so reveal-managed content stays visible. The form is ready for another submission. Submit is disabled before interactivity and while awaiting a result; disposal cancels pending work.

There is no SMTP, external form API, HTTP delivery call, storage, background job or logging of submitted values. Values necessarily reach the application's normal Interactive Server circuit while editing; “no message was sent” means no delivery to a recipient or third-party submission backend, not that the form is client-only. Future real delivery must replace the DI implementation after separate approval and data-handling design.

## 7. Legacy JavaScript

Static `assets/js/contact.js` and its copied baseline remain intact. They are not loaded by `/contact`. Their DOM queries, validation, SweetAlert confirmation and reset no longer own the Blazor page. No SweetAlert dependency was introduced. `contact-page.js` only initializes existing visual reveals and cleans up on detach/pagehide; it has no form handlers, fetches, counters or carousels.

## 8. Map and contact information

The original plain-text phone, email and address remain plain text; no destinations were invented. Source icons and clipped blue/cyan panel geometry are retained. `ContactMapContent.EmbedUrl` is a typed `Uri`, not customer-authored iframe HTML. The exact approved Calgary Tower Google embed URL, lazy loading, fullscreen and referrer policy are retained. Added title: `Map showing Calgary Tower in downtown Calgary`.

The map loaded successfully during browser comparisons. This remains an existing third-party embed/network dependency; no new map API or keys were added. The demo address and Calgary Tower embed have not been asserted to describe a real business location.

## 9. Targeted styling

The unchanged `contact.css` and global theme remain the visual source of truth. New `.contact-page` rules only preserve source H3 appearance on logical H2 headings, add visible field/status focus and invalid-state presentation, and adapt existing reveal timing/fallback/reduced-motion behavior. Existing shared hero selectors include Contact without changing their declarations.

No new layout breakpoints, global overrides, mass `!important`, spacing normalization, asset changes or MudBlazor form redesign. The source fixed map height and its existing footer transition/overlap are intentionally preserved.

## 10. Contact visual verification

Compared the rendered Blazor route at `http://localhost:5138/contact` with the actual static `http://localhost:5140/contact.html` in Chrome responsive mode. Used native browser UI/screenshots under the Computer Use skill. This was visual inspection, not automated pixel-diff or numerical DOM overflow testing. Captures were inspected in the task; no screenshot artifacts were written into the repository.

| Width | Result | Observed layout |
| --- | --- | --- |
| 1440px | Pass | Desktop hero, clipped side panel, paired inputs, textarea/button, map and footer transition match. |
| 1366px | Pass | Hero/mobile-header breakpoint treatment, desktop panel/form proportions, spacing and map/footer match. |
| 768px | Pass | Source tablet layout preserved: paired inputs, form above the clipped information panel, matching map/footer. |
| 390px | Pass | Single-column fields and mobile panel stack, typography, button, map placement and footer match. |

All four comparisons included hero, panel, labels, input sizing, textarea, submit button, spacing, rendered map and footer boundary. No visible horizontal overflow or broken local assets was observed. Intentional semantic/behavior difference: accessible inline field/status feedback replaces the legacy SweetAlert modal; the empty/idle design is preserved.

## 11. Lightweight previous-page regression

HTTP and one-width browser smoke checks passed for `/`, `/services`, `/about`, `/projects`, `/projects/nexconnect`, `/team` and `/team/emilyjohnson`.

Checked route loading, visible header/footer, representative page content/images, no obvious CSS breakage, and application console health. Shared mobile menu opened and closed on Escape with focus restored. Client-side navigation through these routes and back to Contact worked; Contact remounted with all fields empty and no stale feedback.

No full multi-breakpoint visual comparisons of old pages, exhaustive gallery/carousel retests or unrelated fixes were performed. Existing known design limitations from earlier phase records remain outside this phase.

## 12. Runtime and automated verification

| Check | Result |
| --- | --- |
| `dotnet build NexNovaCo.sln --no-restore -c Debug` | Pass; 0 warnings, 0 errors |
| `dotnet build NexNovaCo.sln --no-restore -c Release` | Pass; 0 warnings, 0 errors |
| `scripts/Test-Foundation.ps1` | Pass; 6 direct routes, 82 unchanged assets, 60 rendered asset URLs, runtime resources and 404 shell |
| `scripts/Test-Contact.ps1` | Pass; copy, field semantics, empty textarea, status region, exact map URL/title, assets and 7 previous-route smoke checks |
| `node --test scripts/Test-ContactInterop.mjs` | 2/2 pass; idempotency, detach/pagehide, reduced motion and remount |
| `dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore` | 21/21 production model/demo-service assertions pass |
| `git diff --check` | Pass |

The C# harness links the production model/service sources, with no new test package dependencies. It does not claim to automate Razor events; those were exercised in the browser.

Live browser validation passed: empty form shows four required errors; malformed email fails; whitespace-only message fails even with a valid email; blank subject succeeds with valid required fields; keyboard submit and visible focus work; success explicitly disclaims delivery; all five fields reset; resubmission requires new valid input; content remains visible after reset.

Interactive Server remained connected across the form checks and client navigation. Console showed WebSocket connection information and no application exceptions/circuit errors. Contact and the seven-route sequence showed no console errors or warnings in the console message list. Existing Google embed verbose messages and a brief forced-reflow diagnostic remained. Chrome also reported nonblocking form-metadata/lazy-image advisories; its top toolbar retained an unmatched error badge while the message list reported no errors, so this is not a claim of a completely silent browser/profile.

The Development-only foundation check successfully opened/selected the MudSelect popover, displayed the snackbar, opened the dialog and closed it. That diagnostic interaction produced a Chrome `aria-hidden` focus warning on the existing MudSelect SVG adornment; no exception or circuit loss occurred. It is not Contact form behavior, and no shared provider/library change was made in this scoped phase. Include it in final accessibility QA.

## 13. Deferred work

Real email/submission delivery, production business contact details/map destination, persistence, database/EF Core, authentication/authorization, dashboard/admin/CMS/CRUD, uploads, newsletter delivery, SEO systems, localization and theme/page builders remain deferred. Cross-browser/screen-reader QA, deployment verification and inherited accessibility/design advisories belong to the approved final QA pass, not an unrequested redesign here.

## 14. Recommendation after Phase 8

Approve a final public-site QA and merge review, resolve or explicitly accept the documented demo-content and inherited design/accessibility limitations, then decide whether to merge `feature/blazor-public-site`. Keep the static reference until that review is complete. Do not start dashboard/backend implementation or merge without approval.
