# Phase 3 — Services migration and verification

Completed 2026-09-19. Scope: `/services` only, on the existing .NET 10 / Interactive Server / MudBlazor 9.10.0 foundation. The app renders Services through Razor and the shared layout; root `service.html` is a comparison artifact, not an app-route dependency. No other full page was migrated.

## Branch and commits

- Branch throughout: `feature/blazor-public-site`. No new branch, main checkout, history rewrite or push.
- Starting state: clean at `06d9f22` (`Integrate Home interactions and visual parity fixes`).
- Implementation: `a71df7c` — `Migrate Services page structure and Blazor interactions`.
- Verification: `Verify Services visual parity and regressions` — the commit containing this record, tests and final reveal correction. Its hash is reported in the task handoff and is available with `git log -2 --oneline`.
- Ending state: all Phase 3 changes committed; clean working tree checked after the verification commit.

The implementation was inspected before editing against `service.html`, original global/Services CSS and scripts, the placeholder, Phase 2 components and content providers. Static HTML, all original stylesheets and all 82 copied legacy assets remain unchanged.

## Files by responsibility

Paths below the application are relative to `src/NexNovaCo.Web/`.

| Group | Added or changed files |
| --- | --- |
| Page/composition | `Components/Pages/Services.razor`, `Components/_Imports.razor` |
| Services sections | `Components/Sections/Services/{ServicesGridSection,BenefitsSection,ProcessSection,PricingSection,FaqSection}.razor` |
| Shared components | `Components/Shared/{InnerPageHero,BenefitCard,ProcessStep,PricingCard,FaqItem}.razor` |
| Models | `Models/ServicesContent.cs` |
| Content/registration | `Services/IServicesContentService.cs`, `Services/ServicesContentService.cs`, `Services/ServiceCatalog.cs`, `Services/HomeContentService.cs`, `Program.cs` |
| CSS | New `wwwroot/css/services-blazor.css`; approved `service.css` and global `styles.css` unchanged |
| JavaScript | New `wwwroot/js/services.js`, new shared `wwwroot/js/reveal.js`; `wwwroot/js/home.js` reuses the latter |
| Tests/helpers | New root `scripts/Test-Services.ps1`, `scripts/Test-ServicesInterop.mjs`, `scripts/Serve-StaticReference.mjs`; updated `scripts/Test-Foundation.ps1`, `scripts/Test-HomeInterop.mjs` |
| Documentation | Root `README.md`, this record |

The shared layout/header/navigation/footer, Home Razor sections and other page placeholders are unchanged.

## Final component hierarchy and rendered order

```text
MainLayout (existing header, navigation, main, footer and MudBlazor providers)
└── Services.razor
    ├── InnerPageHero
    ├── ServicesGridSection
    │   └── ServiceCard × 6 (existing)
    ├── BenefitsSection
    │   ├── SectionHeader (existing)
    │   └── BenefitCard × 3
    ├── ProcessSection
    │   ├── SectionHeader (mobile) / center heading (desktop)
    │   └── ordered list → ProcessStep × 5
    ├── PricingSection
    │   ├── SectionHeader
    │   └── PricingCard × 3
    └── FaqSection
        ├── SectionHeader
        └── FaqItem × 6
```

FAQ transitions directly to the existing footer with the approved spacing; no extra CTA section was invented. A redundant `ServicesHero` wrapper was not needed: the page uses the reusable inner hero directly. It is deliberately separate from `HomeHero`. The inner hero has typed desktop/mobile title, description and CTA content, with no arbitrary style props. Route CSS retains the image, overlays, clip paths and geometry. Future inner-page use must first be checked against that page's approved structure.

Phase 2 reuse is exact: `ServiceCard`, `SectionHeader`, `ServiceSummary` and `SectionHeading` have **no API changes**. Services-only fields were unnecessary. The shared shell/providers are also reused unchanged. Home changes are limited to canonical catalog reuse and extracting its reveal implementation; carousel/counter behavior remains in its existing module.

## Typed content and component behavior

| Contract | Role |
| --- | --- |
| `InnerPageHeroContent` | Desktop/mobile title, description, CTA label and actual route anchor |
| `BenefitContent` | Title and description; no invented icon field because the reference has empty decorative icon wrappers |
| `ProcessIcon` / `ProcessStepContent` | Five approved SVG choices plus ordered number/title; no nonexistent step description |
| `PricingTreatment` / `PricingPlan` | Named Basic/Standard/Premium geometry, title/subtitle, formatted display price, feature list, ideal audience and CTA label |
| `FaqContent` | Question and answer only; interaction state belongs to the component |
| `ServicesContent` | Aggregate of typed collections and existing `SectionHeading` values |
| Existing `ServiceSummary` | Unchanged canonical service identity, copy and icon path |

`IServicesContentService.GetAsync(CancellationToken)` and its temporary singleton implementation provide a read-only in-memory editorial snapshot of the approved source. Business content does not live inside reusable cards or page composition. DI can later replace the provider without rewriting the visual components. There is no database, persistence, CRUD, admin model or extra storage layer.

`ServiceCatalog.All` holds six canonical entries. Home selects its original five IDs from the same objects, while Services also includes Strategy. Both routes render identical card content for the five shared services. Canonical icons remain Web & Mobile App → `software.png`, AI Solutions → `analysing.png`, UX/UI Design → `custom.png`.

Benefits use three typed cards plus the original fourth empty grid position, preserving the desktop decorative image, borders/radii, minimum sizing, hover treatment and responsive wrapping. The image is decorative CSS; no fake content was added to fill the empty slot.

Process data sorts by number in a semantic ordered list. Five explicit approved slot classes retain the desktop diagram and SVG shapes; the list uses `display: contents` so it does not introduce a new layout box. At 768px the colored hexagons form a 3+2 grid; at 390px they stack vertically. Step labels/numbers are content, not embedded in the decorative component markup.

Pricing retains CAD 500 / CAD 900 / CAD 1,800, the exact features/audiences, three shapes and orange button appearance. Display price is intentionally a string: this phase has no calculations, billing intervals or payment contract. Actions are native disabled buttons, with an explanatory title and `aria-describedby` demo notice. There are no fake links, checkout, subscriptions or payment logic. The original 400px minimum remains, but the 440px maximum is removed to prevent future content clipping; current approved copy retains the observed geometry.

FAQ is Blazor-owned boolean state per item: the first is initially open, and multiple answers can remain open, matching the original Bootstrap markup without an exclusive parent. Native buttons support Enter/Space and expose `aria-expanded`/`aria-controls`. Unique question/panel IDs and labelled regions keep relationships valid. Closed panels are `aria-hidden` and `inert`. CSS grid interpolation preserves the approximately 350ms collapse feel; the chevron and collapsed class follow state. No FAQ DOM queries, Bootstrap collapse handlers or `data-bs-*` attributes remain in the app output.

## Interaction ownership and lifecycle

| Interaction | Classification / implementation |
| --- | --- |
| FAQ expansion | Blazor state; CSS transition only |
| Pricing | Disabled demo UI; no handler or destination |
| Hero “Explore Our Services” | Normal route-safe `services#Service` anchor |
| AOS-style scroll reveals | Temporary JS interop using original AOS CSS/transforms/delays |
| Card/shape hover and FAQ animation | Existing CSS, with targeted reduced-motion adaptations |
| Mobile menu and route-active links | Existing global shell; Blazor state/NavLink |
| Sticky header and back-to-top | Existing global shell JS |
| Bootstrap collapse/global AOS initialization/legacy `script.js` | Obsolete for Services; not loaded or initialized |
| Home Owl and CountUp | Existing Home-only temporary JS interop, regression-tested |

Each route imports its module after content renders. WeakMap initialization guards prevent duplicate setup. Browser-side removal observers clean detached routes even if a circuit is unavailable; cleanup disconnects observers, removes listeners and cancels pending animation work. Disposal cancels provider work and releases the JS module reference, including a late-import guard.

The shared reveal helper measures **untransformed layout offsets**. Browser testing found that observing AOS-translated rectangles could leave the last FAQ row invisible until an extra scroll; the final correction restores source-like reveal thresholds. Passive scroll/resize handlers coalesce into one requested animation frame. A ResizeObserver handles font loading and FAQ height changes; it is not an unbounded refresh loop. Reduced motion reveals content without delays. SSR content is readable before enhancement. JS never fetches or generates Services business content.

## Targeted CSS and accessibility

The original stylesheet is loaded unchanged before a small route-local adaptation file. Changes are confined to shared-header placement/white inner-page navigation, the original >1400px header spacer, neutralizing MudBlazor's page background/letter-spacing/scrollbar differences, one logical responsive H1, semantic process typography/list boundaries, content-safe pricing maximum height, FAQ state/focus rules and reduced motion.

There is no breakpoint redesign, spacing-system rewrite, class renaming, new palette/font system, mass `!important`, replacement asset or MudBlazor public-card styling. Lato/Poppins and original blue/cyan/orange shapes remain. Heading levels are corrected without changing the visual treatments. Images have alt text; decorative SVGs/shapes are hidden from assistive technology. A visually clipped logical mobile H1 avoids a duplicate heading while keeping the approved visible “Our Services” title. Keyboard FAQ and menu focus outlines were observed.

Sizing intentionally retained: the source hero/rectangle dimensions, five-slot process geometry and mobile 12em hexagons require concise editorial content; they are not a generic editable page-builder layout. Benefits retain their 260px minimum, which can grow. No broad fixed-height redesign was performed.

## Visual comparison

Method: Windows Chrome responsive emulation, actual local static `service.html` at `127.0.0.1:5140` versus Blazor `/services` at `localhost:5138`. Screenshots were inspected section by section, including settled animations and the footer transition, using the computer-use skill. These are visual comparisons, not automated pixel diffs. Screenshots are recorded inline in the task, not checked-in image artifacts. Desktop/tablet heights were 1000px; phone height was 600px.

| Width | Result |
| --- | --- |
| 1440px | Matching desktop hero/header geometry and text placement; services 4+2; benefit row plus decorative image; five-slot desktop process; three pricing cards; FAQ rows and footer spacing. The reveal-threshold defect found here was corrected and rechecked. |
| 1366px | Matching hamburger breakpoint, hero spacing without the wide-screen header spacer, services 3+3, benefit/image composition, desktop process, three pricing cards and FAQ/footer transition. |
| 768px | Matching mobile inner hero/title, services 3+3, benefits 2+1 with the existing empty position and hidden photo, process 3+2, pricing 2+1, multiline FAQ wrapping and footer transition. |
| 390px | Matching photo hero/title, single-column service cards and icon placement, full-width benefit cards, five vertically stacked process steps, single-column prices and multiline FAQ. Compared heading/card wrapping, radii, spacing and footer boundary. |

No significant unintended visual differences or horizontal overflow were observed at the four required widths. This is a visual overflow check, not a numeric `scrollWidth` assertion. An optional console measurement was not executed because Chrome's paste protection blocked it; that protection was left unchanged.

Intentional/inherited differences and constraints:

- Accessible heading/button semantics and visible keyboard focus are improved without redesigning resting visuals.
- Pricing can grow beyond the old maximum, though approved content currently fits.
- FAQ animation uses a CSS grid transition rather than Bootstrap's measured inline height; state and overall timing are preserved, not every intermediate pixel.
- Shared shell behavior from Phases 1–2 remains: honest newsletter demo state, route-aware navigation and accessible back-to-top/menu behavior. Static back-to-top timing/hover can differ from that existing shell implementation.
- At 768px the source's decorative white photo edge intersects the white brand treatment; this inherited visual limitation was preserved, not redesigned.
- Chrome reports advisory issues for lazy images without explicit dimensions and form id/name metadata in the existing shell/Home/diagnostics. These are not application console errors and were not expanded into a full-site cleanup.

## Verification evidence

Final checks ran against the rebuilt local Development app:

| Check | Result |
| --- | --- |
| Debug build | Pass, zero warnings/errors |
| Release build | Pass, zero warnings/errors |
| Foundation HTTP suite | Pass: six public routes, 82 unchanged asset copies, canonical/legacy URLs, 43 rendered asset URLs, runtime resources and HTTP 404 shell |
| Home HTTP suite | Pass: eight ordered sections, one H1, approved text/JSON subsets, nine minimal detail stubs, two detail 404s, 22 images/alt text and route assets |
| Services HTTP suite | Pass: six ordered sections, approved text/features, six canonical cards, three benefits, five ordered steps, three disabled prices, six correctly related FAQ panels and route assets |
| Node interop suite | Five tests passed, zero failures: Home and Services idempotence, detach cleanup, reduced motion, new-route initialization, layout-offset reveals, dynamic height refresh and cancelled frames |
| Services FAQ browser | Mouse opening plus Space closing; first/second panels simultaneously open; correct answer visibility and keyboard focus; toggles still worked after repeated navigation |
| Shared/mobile navigation | Correct active page; mobile open, Escape close/focus restoration and Space reopen; menu closed on selecting Home; header/footer routes worked |
| Repeated navigation | Home → Services → Home → Services via live route links; remounted FAQ still interactive; additional return to Home preserved carousels/counters |
| Home projects | Autoplay observed; Pause changed to Play; Next advanced cards; keyboard Left returned to the previous set |
| Home partners | Six visible at 1440px with hidden unnecessary controls; four visible at 1000px with working autoplay, pause and next |
| Home testimonials | Autoplay, pause and next changed the visible testimonial correctly |
| Home counters | Intermediate animated values observed, then 450 / 3,000 / 1,000 / 26 |
| MudBlazor | Development-only select/popover opened, selection updated, snackbar appeared, dialog opened above the shell and closed |
| Console/circuit | Final running-app console showed normal Blazor connection information and a connected WebSocket, with no application errors/warnings. Live server-side FAQ/menu/provider actions succeeded. |
| Assets/overflow | No broken images seen; HTTP checks passed; no visible horizontal overflow at the four required Services widths |

One intermediate Debug build attempted to replace the running Windows apphost and failed with a file-lock copy error. Stopping our preview released the lock; both final configurations then passed with zero warnings/errors. That deliberate stop produced expected temporary WebSocket/reconnect errors in the browser. After restart and a fresh circuit, the final console was clean apart from normal connection information; no application security settings were changed. These setup events are not hidden as successful test runs.

Reproduce from the repository root (PowerShell 7, with the app running for HTTP checks):

```powershell
dotnet build src/NexNovaCo.Web/NexNovaCo.Web.csproj --no-restore -c Debug
dotnet build src/NexNovaCo.Web/NexNovaCo.Web.csproj --no-restore -c Release
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
node --test scripts/Test-HomeInterop.mjs scripts/Test-ServicesInterop.mjs
```

Stop the preview before rebuilding on Windows to avoid locking the apphost. Automated tests do not substitute for the recorded browser comparisons and interaction checks.

## Deferred work and next approval

About, Projects listing/detail, Team/member detail and Contact remain unmigrated full pages. Existing minimal detail stubs are unchanged. Database/EF Core, CMS, admin/dashboard, authentication/authorization, CRUD, persistence, uploads, payments, email/newsletter backends, localization, full SEO, page builder/theme controls and broad CSS/accessibility refactors remain out of scope. Legacy assets and temporary Home plugins remain for later migration decisions; none were deleted.

Recommended next step: **Phase 4 — About**, first inspecting its approved markup and evaluating the new inner hero and existing shared cards/heading against its actual variants. The experience here supports reusing typed entities and theme-owned geometry, not adding arbitrary styling props. Phase 4 has **not** started. Stop here and await approval.
