# Phase 6 — Project Detail migration and verification

Completed 2026-09-19 (America/Denver). Scope: `/projects/{slug}` only, on the existing .NET 10 / global Interactive Server / MudBlazor 9.10.0 foundation. This record covers the implementation and user-requested continuations. Team, Member Detail and Contact remain placeholders.

Before implementation, inspected `inner-project.html`, `assets/css/inner-project.css`, `assets/js/inner-project.js`, canonical project JSON, the existing model/catalog/detail placeholder, Home/listing usage, shared hero/carousel/reveal infrastructure, Bootstrap's gallery behavior and the Phase 5 verification record. The source gallery is a manual Bootstrap fade, not an Owl carousel. The source Features panel is deliberately hidden at <=1200px.

## 1. Git status

- Branch throughout: `feature/blazor-public-site`.
- Starting state: clean at `6868819509e6be64557d423627f88ae721a761d3` — `Fix Projects testimonial contrast at tablet breakpoint`. Continuations resumed the same Phase 6 changes.
- Implementation: `a39d040` — `Migrate Project Detail page with typed canonical content`.
- Verification: `Verify Project Detail visual parity and regressions` — the commit containing this record, README and test updates; its hash is reported in the handoff.
- Ending state: all Phase 6 changes committed; working-tree cleanliness checked after the verification commit.
- No new branch, main checkout, history rewrite or push. No dependency, lock-file, canonical JSON or original asset changes. All 82 copied legacy assets remain unchanged.

## 2. Files changed

Application paths below are relative to `src/NexNovaCo.Web/`.

| Group | Files |
| --- | --- |
| Project Detail page | Added `Components/Pages/ProjectDetail.razor`; removed `ProjectDetailPlaceholder.razor` |
| Sections | Added `Components/Sections/Projects/ProjectDetailContent.razor` and `ProjectGallery.razor` |
| Shared components | Added `Components/Shared/Breadcrumbs.razor`; reused `InnerPageHero` unchanged |
| Models | Added `Models/ProjectDetail.cs` containing `ProjectDetail`, `ProjectImage`, `ProjectMetadata` |
| Content provider | Extended `Services/IProjectCatalog.cs` and `ProjectCatalog.cs` |
| CSS | Added `wwwroot/css/project-detail-blazor.css`; extended existing route selectors in `inner-page-blazor.css` |
| JavaScript | Added reveal-only `wwwroot/js/project-detail.js`; no shared carousel/reveal changes |
| Tests | Added `scripts/Test-ProjectDetail.ps1` and `Test-ProjectDetailInterop.mjs`; updated `Test-Home.ps1` and `Test-Projects.ps1` |
| Documentation | `README.md`, this record |

## 3. Project Detail component tree

```text
MainLayout — existing header/navigation, main, footer and MudBlazor providers
└── ProjectDetail.razor — route, catalog lookup, PageTitle, route-local CSS
    └── ProjectDetailContent — keyed by canonical slug
        ├── InnerPageHero — actual name/subtitle/current-route anchor
        └── Original project overview grid
            ├── Decorative sidebar
            ├── White content wrapper
            │   ├── ProjectGallery — ordered images and Razor interaction state
            │   ├── Tagline heading
            │   ├── Breadcrumbs — Home / Projects / current name
            │   ├── Full description
            │   ├── Metadata heading/list
            │   └── View More Projects link
            └── Desktop Features aside — heading/list within original hexagon
```

Summary, metadata and Features remain small semantic fragments in the detail composition; no speculative component hierarchy was introduced.

## 4. Canonical data changes

`IProjectCatalog.GetDetailAsync(slug)` extends the existing catalog; there is no independent detail provider or duplicated content catalog. One lazy application-lifetime snapshot deserializes all seven `wwwroot/data/projects.json` records. Its read-only summary and detail collections share the same `ProjectSummary` objects. Home still selects its original five records, listing still uses all seven in source order, and detail resolves the same IDs.

The additional typed fields map only existing JSON: `FullDescription` from `summary`, ordered `Gallery` from `gallery`, `Metadata` from `details`, and feature strings from `features`. Metadata contains Client, Category, Date and Technologies. Date deliberately stays the editorial string (for example a month/year), without inventing a day or transforming its presentation.

Listing covers remain `image/project/{slug}.jpg`. Detail media comes from the separate gallery collection, with original ordering/alt text and a route-safe path (remove the old `assets/` prefix). NexConnect intentionally has `nexconnect.jpg` as its listing cover but `nextConnectProject.jpg` and `nextConnectInnerProject.jpg` as gallery media. The other six projects each have one image. No data or images were invented or edited. Restart after changing this temporary JSON snapshot; no persistence layer was added.

## 5. Routing

`@page "/projects/{Slug}"` uses normal Blazor parameters and exact ordinal canonical-slug lookup. The page clears stale content while loading, guards asynchronous results with a request version and lifetime cancellation, and keys its detail composition by slug. Changing records remounts gallery/reveal state rather than leaking the previous selection.

All seven direct URLs return their correct project, including AutoTracker and FinVault. Existing Home/listing card links remain clean routes. Breadcrumbs and the return CTA use normal application links; the hero's anchor remains on the current detail URL.

`/projects/not-a-real-project` and `/projects/not-a-project` return HTTP 404 with the existing shell and clear not-found content through `NavigationManager.NotFound()`. They do not render another project or throw a circuit exception. The first was also checked directly in the browser, including Return home.

## 6. Hero / breadcrumb / page title

The current canonical name binds desktop hero, mobile/tablet visual title, current breadcrumb and `<PageTitle>` (`NexConnect | NexNovaCo`, `PayFlowX | NexNovaCo`, etc.). The subtitle is canonical, not a hardcoded project string. No literal `Project Title` remains in rendered detail HTML.

`InnerPageHero` preserves one logical H1 and its existing accessible mobile-title treatment. The source's second H1 becomes an H2 tagline; metadata is H3; Features is H2. Targeted CSS retains their former font sizes/weights. The shared breadcrumb is a small labeled navigation landmark and ordered list, with clean Home/Projects links and `aria-current="page"`; it is not a general-purpose navigation framework.

## 7. Gallery

`ProjectGallery` receives an ordered `IReadOnlyList<ProjectImage>` and the actual project name. Razor owns the active index, slides, controls and live position status. The approved Bootstrap CSS frame, rounded corners, fade and arrows are retained; Bootstrap JavaScript is not loaded. Source image sizing remains 480px on desktop and 180px through 768px, including the original crop/stretch behavior (no new object-fit rule).

The source gallery was manual and non-autoplaying. Reusing `ThemeCarousel` would introduce Owl wrappers, looping/autoplay assumptions and a frozen render boundary, so it is intentionally not used here. No second JavaScript carousel implementation was created. Native buttons, Left/Right keys and horizontal touch/pen swipes wrap the index; inactive slides are inert and hidden from assistive technology. Gallery focus is visible, image dragging is disabled, and CSS honors reduced motion. Single-image projects omit navigation controls and the gallery-only tab stop. No gallery timers, global listeners, plugin handlers or cloned slides exist.

Browser checks exercised NexConnect Next/Previous, Left key, a phone swipe, and controls after list/detail remounts. PayFlowX displayed its one canonical image without arrows. HTTP checks cover initial active/inert state and all ordered media; Node tests cover reveal lifecycle, not Razor event execution.

## 8. Metadata / content

The full summary, second name/tagline, hero subtitle, Client, Category, Date, Technologies and feature strings all render directly from typed canonical properties. Metadata labels preserve the original UI; absent/blank metadata would omit its list item rather than invent a value. Technologies and dates retain the source's text, not speculative normalized schemas. HTTP checks compare every project against the original JSON.

PayFlowX's distinct subtitle, gallery, summary, features and metadata (including TechSphere, Fintech / Payment Solutions and June 2025) were checked in the browser after navigating from NexConnect. Project-specific values do not live in reusable components.

## 9. Features

One typed feature list renders inside the original right-hand decorative hexagon. It is visible at 1440px and 1366px and intentionally hidden at 768px and 390px. The unchanged original `@media screen and (max-width: 1200px)` rule remains responsible for hiding it. No mobile copy, relocated panel, additional breakpoint or JavaScript visibility logic was introduced.

The approved static fixed-size/right-positioned hexagon clips some Features text at the right edge at both checked desktop widths. Direct static/app comparison confirmed this is inherited, not introduced by the migration. It remains unchanged under the no-redesign scope. Improving that clipping needs separate design approval.

## 10. Legacy JavaScript

Legacy `inner-project.js` previously selected the query-string ID, fetched JSON and mutated title, breadcrumb, text, metadata, features and gallery DOM. None of those responsibilities run on the Blazor route. Route selection and all business content/media now belong to Razor; the legacy file is not loaded.

The new `project-detail.js` only attaches shared `reveal.js` effects within the detail root. Initialization is idempotent through a WeakMap; a removal observer/pagehide handler cleans up the reveal lifecycle. It contains no fetch, innerHTML, content population, Owl initialization or counters. The existing eight legacy JS files and all root static reference files remain intact and dormant in the Blazor app.

## 11. Styling

Unmodified `inner-project.css` remains the visual source of truth. `inner-page-blazor.css` only adds `.project-detail-page` to existing inner-page selectors so shared header geometry and hero accessibility apply. It does not change existing Services/About/Projects declarations or their breakpoints.

The small detail stylesheet supplies visible server-rendered/reduced-motion reveal fallbacks, equivalent typography for semantic heading changes, Razor-owned mounted fade slides, touch behavior and focus indication. No broad overrides, `!important`, new breakpoint system or MudBlazor public-page redesign were added. The prior Projects 768px testimonial fix was not modified.

Static-only heading selectors, Bootstrap data-binding expectations and fixed geometry remain in the original stylesheet/reference files. The original hero illustration is fixed NexConnect-themed art for every project; the source binder does not replace it. That decorative art remains fixed while actual project-specific images bind in the gallery. Changing either it or inherited desktop Features clipping would be a separate visual change.

## 12. Visual verification

Compared the approved `http://127.0.0.1:5140/inner-project.html?id=nexconnect` directly with `http://localhost:5138/projects/nexconnect` in Chrome responsive mode. NexConnect is the fullest two-image record. Inspection covered hero/title, breadcrumb, gallery/crops, content widths, full summary/metadata, Features visibility, typography, spacing and footer transition.

| Viewport width | Result |
| --- | --- |
| 1440px | Parity passes: desktop hero, 480px rounded gallery, original content/sidebar/Features geometry, summary/metadata and footer. Inherited Features edge clipping matches source. |
| 1366px | Parity passes: narrower desktop hero/content, 480px gallery and right Features panel; summary/breadcrumb/metadata/footer match. Same inherited Features clipping. |
| 768px | Parity passes: actual responsive title, 180px gallery/crop, single content column, metadata/footer; Features hidden. Gallery controls work. |
| 390px | Parity passes: phone hero/title, rounded 180px gallery/crop, wrapped summary/breadcrumb/metadata and footer; Features hidden. Next, keyboard and horizontal swipe checked. |

PayFlowX was additionally verified at 1440px and 768px for dynamic title/subtitle/breadcrumb, unique media/content/metadata, desktop Features versus hidden tablet state and one-image control omission. This is a second-record binding check, not a claim that all seven records received four-width visual comparison.

No page-level horizontal scrollbar or unintended horizontal navigation was observed at the tested widths. This is browser visual evidence, not a numeric DOM-width assertion or automated screenshot pixel diff. Original overflow clipping remains, including the separately noted Features issue. Screenshots were inspected inline through the Computer Use workflow; no screenshot artifacts were saved. An earlier UI-policy interruption was respected and verification resumed only after the user's continuation.

Intentional changes from static behavior are semantic headings/breadcrumbs, accessible gallery controls/status/inactive state, and omitting no-op arrows for single images. Decorative design and intentional responsive visibility are otherwise preserved.

## 13. Runtime / regression verification

Executed successfully against the final application implementation:

```powershell
dotnet build NexNovaCo.sln --configuration Debug --no-restore
dotnet build NexNovaCo.sln --configuration Release --no-restore
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
./scripts/Test-About.ps1
./scripts/Test-Projects.ps1
./scripts/Test-ProjectDetail.ps1
node --test scripts/Test-HomeInterop.mjs scripts/Test-ServicesInterop.mjs scripts/Test-AboutInterop.mjs scripts/Test-ProjectsInterop.mjs scripts/Test-ProjectDetailInterop.mjs
git diff --check
```

- Debug and Release: zero warnings, zero errors.
- All six HTTP suites pass: six primary routes, 82 unchanged copied assets, 54 foundation-rendered asset URLs, existing Home/Services/About/Projects content, all seven real project detail routes, eight gallery images with alt text/order, separate covers, semantic headings/breadcrumbs/metadata/features, single-image behavior and two shell-preserving detail 404s.
- All 15 Node tests pass. Three new tests verify detail reveal idempotence/disposal, Projects → Detail → Projects → another Detail enhancement lifecycle, and reduced motion/pagehide without vendor plugins. Existing carousel and Services tests remain green.
- Browser repeated internal navigation: detail → listing → NexConnect → listing → PayFlowX. Card links, breadcrumb, return CTA, title changes and gallery controls after remount work, without duplicated controls.
- Home: hero and content render; testimonial pause/next works (Daniel → Olivia). Existing featured cards and canonical routes also pass HTTP checks.
- About: hero intact; partner carousel pause/next advances cards, with one set of controls.
- Services: hero intact; mobile-friendly FAQ expands by click and closes with Space, retaining a visible focus ring.
- Projects: listing renders/navigates correctly; exact-768px testimonial quote stays readable over the restored dark image after returning from detail. Pause/next switches Daniel → Olivia. The scoped background guard remains in the automated suite.
- Mobile menu on detail: correct Projects active state, opening, Escape/return focus, keyboard reopening and link-navigation close all work.
- Development-only foundation diagnostics: MudSelect/popover opens and selects `Server interactivity works`; success snackbar appears above the header; dialog opens and closes above the shell. Shared providers remain functional.
- Valid-page console: no errors or warnings; normal `_blazor` WebSocket connection logged. Stateful gallery/FAQ/menu/provider actions work without reconnect UI. No application/circuit exception was observed.
- Invalid direct URL: expected HTTP 404 document request appears as a Chrome network error; this is not a JavaScript/application failure and was not suppressed. The shell and Return home continue working.
- Chrome still reports improvement advisories for existing form fields and lazy-loaded image dimensions, plus an observed 36ms forced-reflow verbose message on Home diagnostics. These are not new build warnings or application exceptions and are outside this migration's scope.

The HTTP suites and fixture-based interop tests do not replace the recorded browser interactions, and no automated C# gallery event test or quantitative layout/performance benchmark is claimed.

## 14. Deferred work

Team, Member Detail, Contact, database/EF Core, authentication, dashboard/CMS/CRUD, uploads, SEO system, search/filter, comments, payments, localization, email/newsletter backend and page builder remain out of scope. No Phase 7 code was started.

Preserved constraints: static decorative hero art across projects, original desktop Features clipping, original fixed image sizing/crops, Google Fonts dependency, existing form/image improvement advisories, and transitional dormant assets/legacy alias. Any redesign, asset cleanup or performance/accessibility expansion needs separate scope.

## 15. Recommendation for Phase 7 — approval required

Migrate Team + Member Detail together after inspecting `team.html`, `inner-member.html`, their CSS/JS and canonical `member.json`. Establish one typed member catalog shared with Home, the listing and detail; preserve canonical IDs and distinguish listing/profile/detail media only where the actual source does. Reuse existing member cards, `InnerPageHero` and the simple breadcrumb when they fit, without inventing fields or forcing project-specific gallery behavior onto member pages. Preserve approved responsive behavior, remove member DOM-binding responsibility from the Blazor route, and verify direct/invalid routes, repeated navigation, all four comparison widths and earlier-page regressions.

Stop after Phase 6 and await explicit approval before implementing this recommendation.
