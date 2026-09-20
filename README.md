# NexNovaCo — Blazor public-site migration

Phase 5: the approved Home, Services, About and Projects listing now render through reusable Razor sections on the .NET 10 / global Interactive Server / **MudBlazor 9.10.0** foundation. Project Detail, Team, Member Detail and Contact remain placeholders. No backend, database, CMS or authentication has been added. Remaining public-site migration work stays on `feature/blazor-public-site`.

## Run locally

Install the verified .NET SDK 10.0.401 (or a compatible servicing patch), then run from the repository root:

```powershell
dotnet restore NexNovaCo.sln --locked-mode
dotnet build NexNovaCo.sln --no-restore
dotnet run --project src/NexNovaCo.Web --no-build --launch-profile http
```

Open `http://localhost:5138`. The `https` profile also supports `https://localhost:7188` with a trusted local ASP.NET development certificate. Development permits HTTP for local verification; production retains HTTPS redirection and HSTS. Do not expose the Development profile publicly. Data protection uses ASP.NET's standard user-level key storage; a restricted sandbox must allow access to that directory when running the app.

`global.json` selects the verified .NET 10.0.4xx SDK feature band. The project targets `net10.0`. MudBlazor uses an exact `[9.10.0]` NuGet version constraint and a checked-in `packages.lock.json`. If an SDK update changes the auto-referenced ASP.NET assets package, review that change before regenerating the lock file; do not bypass locked-mode failures in CI.

## Structure

```text
NexNovaCo.sln
src/NexNovaCo.Web/
  Components/
    App.razor                 # Document, styles, scripts, global render modes
    Routes.razor              # Router, layout, heading focus
    Layout/                   # MainLayout, header/navigation/footer, reconnect UI
    Shared/                   # Typed cards, headings, carousel boundary, shell helpers
    Pages/                    # Four migrated pages, Team/Contact placeholders, detail stubs, infrastructure
    Sections/Home/            # Home-only sections, including its dedicated hero
    Sections/Services/        # Grid, benefits, process, pricing and FAQ
    Sections/About/           # Story, vision, timeline and mission
    Sections/Projects/        # Full project grid and decorative pagination
    Sections/Shared/          # PartnersSection and TestimonialsSection
  Models/                     # Home, Services, About and Projects typed content contracts
  Services/                   # Replaceable providers and canonical service/partner/project/testimonial catalogs
  wwwroot/{css,js,image,data}/
  Program.cs
  NexNovaCo.Web.csproj
  packages.lock.json
scripts/Test-Foundation.ps1
scripts/Test-Home.ps1
scripts/Test-HomeInterop.mjs
scripts/Test-Services.ps1
scripts/Test-ServicesInterop.mjs
scripts/Test-About.ps1
scripts/Test-AboutInterop.mjs
scripts/Test-Projects.ps1
scripts/Test-ProjectsInterop.mjs
scripts/fixtures/CarouselFixture.mjs
scripts/Serve-StaticReference.mjs
docs/phase-1-verification.md
docs/phase-2-verification.md
docs/phase-3-verification.md
docs/phase-4-verification.md
docs/phase-5-verification.md
```

The original root HTML and `assets/` stay in place as the approved static reference. The .NET template's standard `Components/App.razor` and `Components/Routes.razor` locations are intentional. No WebAssembly/client project exists.

## Shell and routes

`MainLayout` owns one header, the main landmark, one footer, the four MudBlazor providers, and lifecycle-managed shell interop. `SiteHeader` owns mobile state; `MobileNavigation` is the accessible toggle; `PrimaryNavigation` supplies the same `NavLink` list to header and footer. The menu closes on a link or route change and on Escape while focus is in the header; Escape restores toggle focus. `SiteFooter` retains the approved grid, copy, social icons, newsletter demo, and copyright toolbar.

Public routes: `/`, `/about`, `/services`, `/projects`, `/team`, `/contact`. Standard `/Error` and `/not-found` infrastructure pages are retained. Home and Projects use minimal `/projects/{slug}` placeholders for all seven canonical projects; Home's `/team/{slug}` links also remain placeholders. Unknown slugs return 404. These are not full detail pages. Newsletter is an inert demo: it neither submits nor stores data. The existing demo email address remains; unconfigured social profiles remain decorative.

## Styling and assets

CSS order is **MudBlazor → existing Bootstrap → AOS/Owl CSS → unchanged styles.css → public-shell.css → Blazor isolated styles**. Route-local HeadContent then loads the unchanged page stylesheet (`index.css`, `service.css`, `about.css` or `project.css`), applicable shared adaptations, and its small `*-blazor.css` file. `inner-page-blazor.css` holds the Services/About/Projects hero and shell adaptations; `carousel-blazor.css` holds shared fallback grids and accessible controls. Home and Projects share small `project-card-blazor.css` and `testimonials-blazor.css` semantic typography/control adaptations; their original page styles still own card geometry. The isolated styles only cover template error/reconnect UI. Public components use HTML, not MudBlazor cards/buttons/drawers. Lato/Poppins remain Google Fonts dependencies, as in the static baseline; no Roboto font link was introduced.

`public-shell.css` carries only the Home header rules formerly embedded in `index.css`, small semantic-button/state adaptations, accessibility focus styles, and placeholder spacing. It keeps the existing 1400px navigation breakpoint, 90px header, orange accents, and footer rules. The hamburger is blue on the pale foundation header at all mobile-menu widths so it remains visible without a hero behind it. Header stacking is below MudBlazor's overlay layers. No mass `!important`, new breakpoint system, or hexagon/asset redesign was introduced. Home loads its original hero geometry through route-local CSS.

All **82 existing asset files** were copied unchanged: 13 CSS, 8 JS, 59 image/icon/SVG files, and 2 JSON files. New components use `css/`, `js/`, `image/`, and `data/` under the application base URL. CSS `../image/` references keep working. A transitional `/assets/` static-file alias serves the same `wwwroot` files, preserving unchanged JSON image paths and dormant legacy `fetch("assets/data/...")` references without duplicating assets or inventing content models. New code should use the canonical paths; retire the alias after the final legacy consumer is migrated.

Bootstrap and AOS/Owl CSS are active. jQuery 3.1.0, Owl Carousel 2.3.4 and a local copy of the original CountUp 2.0.8 load once. Home initializes three carousels and its counters; About initializes only its partner carousel; Projects initializes only testimonials, not the listing. Bootstrap JS, the global AOS engine and unrelated legacy page scripts stay dormant. The baseline's unused Owl video-play image reference has no corresponding asset; no video carousel is enabled.

## JavaScript boundary

All eight legacy JavaScript files remain unchanged in Git. The global `script.js`, contact handling and project/member DOM binding are dormant. `home.js`, `about.js` and `projects.js` use shared `carousels.js` inside their route roots and destroy observers/plugins when detached. CountUp stays Home-only. All four migrated pages share `reveal.js`: existing AOS CSS effects use layout offsets, coalesced scroll/resize work and a ResizeObserver. A route-local carousel refresh event updates reveal targets when Owl replaces responsive loop clones. Each route removes its listeners when detached; initialization is idempotent. `services.js` owns only its reveal lifecycle. FAQ expansion is native Blazor state, with no Bootstrap collapse JavaScript. A static `ThemeCarousel` rendering boundary prevents Blazor from diffing Owl's temporary wrappers/clones. Do not add stateful/event-bound children inside it; remount changed content with a new key. No JavaScript fetches or generates entity content. See the phase records for lifecycle limitations and transitional dependencies.

The new `public-shell.js` module only adds the existing scroll-driven fixed-header/back-to-top behavior, honors reduced motion, and removes event listeners when the layout is disposed. Mobile navigation is Blazor state, not a checkbox or jQuery handler. MudBlazor and `_framework/blazor.web.js` are loaded normally; providers run inside the interactive layout.

## Verification

With the app running, use PowerShell 7:

```powershell
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
./scripts/Test-About.ps1
./scripts/Test-Projects.ps1
node --test scripts/Test-HomeInterop.mjs scripts/Test-ServicesInterop.mjs scripts/Test-AboutInterop.mjs scripts/Test-ProjectsInterop.mjs
```

The five read-only HTTP suites cover routes, asset integrity/serving, approved content, shared canonical service/partner/project cards and testimonials, featured JSON subsets, detail stubs, decorative pagination, pricing/FAQ/timeline semantics and images. Twelve Node tests cover interop lifecycle/reduced-motion/resize logic, including Home → Projects → About → Projects. Text asset comparisons tolerate only Windows checkout line-ending differences; binary comparisons remain exact. They do not replace interactive browser tests. For approved comparisons, run `node scripts/Serve-StaticReference.mjs` and open `http://127.0.0.1:5140/project.html`, `about.html` or `service.html`; this loopback-only helper serves root HTML and approved assets, not repository metadata.

In **Development only**, open `http://localhost:5138/?verify=foundation` to exercise a MudSelect/popover, snackbar and dialog below Home. This opt-in component is absent from normal pages and Production, even with the query parameter. See [Phase 1](docs/phase-1-verification.md), [Phase 2](docs/phase-2-verification.md), [Phase 3](docs/phase-3-verification.md), [Phase 4](docs/phase-4-verification.md) and [Phase 5](docs/phase-5-verification.md) for verification evidence.

## Home content boundary

`IHomeContentService.GetAsync` supplies typed Home data. The temporary singleton implementation reads projects through `IProjectCatalog`, snapshots `member.json`, selects the approved featured IDs, and combines them with typed editorial copy from `index.html`. Restart after changing JSON. The cards do not own entity values. Replace the service's DI implementation to use a future database; keep the content contracts and visual components. This is not a generic CMS.

## Next phase — approval required

Recommended Phase 6: inspect and migrate Project Detail using the canonical project IDs and the existing `/projects/{slug}` route. Preserve the distinction between listing covers and JSON detail galleries; introduce a detail-specific typed contract/provider only for fields the approved detail page uses. Do not begin without approval. Team, Member Detail, Contact, database/CMS, admin/dashboard, authentication, email and newsletter services remain deferred. Keep the static reference.

## Services content boundary

`IServicesContentService.GetAsync` supplies typed hero, service, benefit, process, pricing and FAQ content from an in-memory snapshot of approved `service.html`. `ServiceCatalog` is the single service identity/copy/icon source: Services uses all six entries and Home selects its original five by ID. Existing `ServiceSummary`, `ServiceCard` and `SectionHeader` APIs are unchanged. Replace the provider's DI implementation for a future content source; no storage layer or speculative fields were added.

`InnerPageHero` remains separate from `HomeHero`; theme CSS owns its imagery and geometry. Five numbered process slots preserve the desktop diagram and responsive stacking. Pricing remains demo-only with disabled, explained buttons and no checkout destination. FAQ items independently toggle, first open initially, with accessible button/panel relationships and reduced-motion support. Retained sizing constraints and visual comparison results are recorded in Phase 3.

## About content boundary

`IAboutContentService.GetAsync` supplies typed hero, story, vision, timeline and mission content from an in-memory snapshot of approved `about.html`. `PartnerCatalog` supplies one canonical heading and six partner identities/copy/images to Home and About. Both routes use `PartnersSection`, `PartnerCard` and `ThemeCarousel`; existing component APIs are unchanged. No partner destination was invented.

The three chronological timeline slots retain their approved desktop shapes, tablet two-plus-one arrangement and phone stack. An ordered list exposes complete year headings while split decorative numerals remain hidden from assistive technology. Four mission commitments form an accessible list. Original fixed hero/vision/mission sizing is retained for the approved concise copy; future editing must respect the constraints documented in Phase 4.

## Projects content boundary

`IProjectCatalog` is the single application-lifetime snapshot of all seven canonical `wwwroot/data/projects.json` records, projected into the unchanged `ProjectSummary` contract. Home selects its original five IDs; `IProjectsContentService` supplies all seven in approved order plus typed hero/testimonial composition. Detail stubs resolve the same catalog, including AutoTracker and FinVault. Listing covers use `image/project/{slug}.jpg`; do not substitute detail gallery images (NexConnect intentionally differs).

Projects reuses `InnerPageHero`, `ProjectCard` and the shared `TestimonialsSection`/`TestimonialCard`/`ThemeCarousel`. `TestimonialCatalog` holds the identical approved Home/Projects quotes and brand copy. The listing uses the original three/two/one-column grid. Pagination preserves the sample appearance as hidden-from-assistive-technology spans, with no links, buttons, focus stops or page-changing state. No filtering/search was added.

Original fixed card heights remain for visual parity and fit the approved copy at all four checked widths. Longer future content needs a separate sizing review. At exactly 768px, the approved source itself has overlapping testimonial background rules that make white body text invisible on white; Phase 5 preserves and documents this inherited defect instead of changing the design without approval. See the Phase 5 record for this limitation and the intentional accessible-carousel differences.
