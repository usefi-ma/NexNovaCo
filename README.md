# NexNovaCo — Blazor public-site migration

The public-site migration and Phase 9 hardening are merged into `main`. All eight approved public page types remain anonymous on .NET 10 / Interactive Server / **MudBlazor 9.10.0**. The long-lived `feature/dashboard-cms` branch adds ASP.NET Core Identity, EF Core/SQLite, static HTTP login/logout, an Admin-only `/dashboard`, and Home Hero/Welcome text and CTA editing. Other content and the public design remain unchanged. See [Home CMS navigation and verification](docs/dashboard-phase-4a-home-navigation.md), [Home Hero setup](docs/dashboard-phase-2-home-hero.md), [authentication setup](docs/dashboard-phase-1-authentication.md), and the earlier [production-hardening report](docs/phase-9-production-hardening.md).

## Run locally

The Dashboard has a responsive MudBlazor application shell and avatar menu. Home is a collapsible sidebar group with independent Admin-only editors at `/dashboard/content/home/hero` and `/dashboard/content/home/welcome`; there are no Hero/Welcome tabs. Both `/dashboard/content/home` and the old `/dashboard/home/hero` redirect to canonical Hero. Both forms protect unsaved edits with a Stay/Leave dialog for Dashboard navigation and the framework's browser-native unload protection for public-site actions, refresh, and close. See [Phase 4A verification and browser limitations](docs/dashboard-phase-4a-home-navigation.md), the earlier [Welcome CMS implementation](docs/dashboard-home-cms-welcome.md), and [Dashboard UI redesign](docs/dashboard-ui-redesign.md).

Development initializes the private SQLite Identity database through migrations. It creates no default user: configure your own bootstrap credentials using the [User Secrets instructions](docs/dashboard-phase-1-authentication.md#recommended-net-user-secrets-development-only) before signing in at `/admin/login`. Missing credentials do not affect anonymous public pages. Production account/form flows require HTTPS, including Secure antiforgery cookies.

On the next Development startup, the additive Home Welcome migration applies to the same database and initializes approved copy only if the singleton is missing. Existing Admin accounts and saved Hero/Welcome edits are retained. Stop/restart an existing Visual Studio debugging session to load the new build; no database reset is needed. The public site still uses its approved design and remains anonymous.

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
    Pages/                    # Eight migrated public page types and infrastructure
    Sections/Home/            # Home-only sections, including its dedicated hero
    Sections/Services/        # Grid, benefits, process, pricing and FAQ
    Sections/About/           # Story, vision, timeline and mission
    Sections/Projects/        # Project grid/pagination, detail composition and manual gallery
    Sections/Team/            # Team intro/grid and canonical member profile
    Sections/Contact/         # Contact info, EditForm and titled map embed
    Sections/Shared/          # PartnersSection and TestimonialsSection
  Models/                     # Typed page content and canonical summary/detail contracts
  Services/                   # Replaceable providers and service/partner/project/member/testimonial catalogs
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
scripts/Test-ProjectDetail.ps1
scripts/Test-ProjectDetailInterop.mjs
scripts/Test-Team.ps1
scripts/Test-MemberDetail.ps1
scripts/Test-TeamInterop.mjs
scripts/Test-Contact.ps1
scripts/Test-ContactInterop.mjs
tests/NexNovaCo.Contact.Tests/ # Dependency-free model/demo-service validation checks
scripts/fixtures/CarouselFixture.mjs
scripts/Serve-StaticReference.mjs
docs/phase-1-verification.md
docs/phase-2-verification.md
docs/phase-3-verification.md
docs/phase-4-verification.md
docs/phase-5-verification.md
docs/phase-6-verification.md
docs/phase-7-verification.md
docs/phase-8-verification.md
```

The original root HTML and `assets/` stay in place as the approved static reference. The .NET template's standard `Components/App.razor` and `Components/Routes.razor` locations are intentional. No WebAssembly/client project exists.

## Shell and routes

`MainLayout` owns one header, the main landmark, one footer, the four MudBlazor providers, and lifecycle-managed shell interop. `SiteHeader` owns mobile state; `MobileNavigation` is the accessible toggle; `PrimaryNavigation` supplies the same `NavLink` list to header and footer. The menu closes on a link or route change and on Escape while focus is in the header; Escape restores toggle focus. `SiteFooter` retains the approved grid, copy, social icons, newsletter demo, and copyright toolbar.

Public routes: `/`, `/about`, `/services`, `/projects`, `/projects/{slug}`, `/team`, `/team/{slug}`, `/contact`. Standard `/Error` and `/not-found` infrastructure pages are retained. Home and Projects link to full Project Detail pages for all seven canonical projects; Home and Team link to full profiles from the same six-member catalog. Unknown slugs return the shared-shell 404 rather than falling back to another record. Newsletter is an inert demo: it neither submits nor stores data. The existing demo email address remains; unconfigured social profiles remain decorative.

## Styling and assets

CSS order is **MudBlazor → existing Bootstrap → AOS/Owl CSS → unchanged styles.css → public-shell.css → Blazor isolated styles**. Route-local HeadContent then loads the unchanged page stylesheet (`index.css`, `service.css`, `about.css`, `project.css`, `inner-project.css`, `team.css`, `member.css` or `contact.css`), applicable shared adaptations, and its small `*-blazor.css` file. `inner-page-blazor.css` holds the Services/About/Projects/Project Detail/Team/Contact hero and shell adaptations; the distinct Member Detail header uses only `member-blazor.css`. `carousel-blazor.css` holds shared fallback grids and accessible controls. Home and Projects share small `project-card-blazor.css` and `testimonials-blazor.css` semantic typography/control adaptations; their original page styles still own card geometry. The isolated styles only cover template error/reconnect UI. Public components use HTML, not MudBlazor cards/buttons/drawers. Lato/Poppins remain Google Fonts dependencies, as in the static baseline; no Roboto font link was introduced.

`public-shell.css` carries only the Home header rules formerly embedded in `index.css`, small semantic-button/state adaptations, accessibility focus styles, and placeholder spacing. It keeps the existing 1400px navigation breakpoint, 90px header, orange accents, and footer rules. The hamburger is blue on the pale foundation header at all mobile-menu widths so it remains visible without a hero behind it. Header stacking is below MudBlazor's overlay layers. No mass `!important`, new breakpoint system, or hexagon/asset redesign was introduced. Home loads its original hero geometry through route-local CSS.

All **82 existing asset files** were copied unchanged: 13 CSS, 8 JS, 59 image/icon/SVG files, and 2 JSON files. New components use `css/`, `js/`, `image/`, and `data/` under the application base URL. CSS `../image/` references keep working. A transitional `/assets/` static-file alias serves the same `wwwroot` files, preserving unchanged JSON image paths and dormant legacy `fetch("assets/data/...")` references without duplicating assets or inventing content models. New code should use the canonical paths; retire the alias after the final legacy consumer is migrated.

Bootstrap and AOS/Owl CSS are active. jQuery 3.1.0, Owl Carousel 2.3.4 and a local copy of the original CountUp 2.0.8 load once. Home initializes three carousels and its counters; About initializes only its partner carousel; Projects initializes only testimonials, not the listing. Bootstrap JS, the global AOS engine and unrelated legacy page scripts stay dormant. The baseline's unused Owl video-play image reference has no corresponding asset; no video carousel is enabled.

## JavaScript boundary

All eight legacy JavaScript files remain unchanged in Git. The global `script.js`, legacy `contact.js` and project/member DOM binding are dormant. `home.js`, `about.js` and `projects.js` use shared `carousels.js` inside their route roots and destroy observers/plugins when detached. CountUp stays Home-only. All eight migrated page types share `reveal.js`: existing AOS CSS effects use layout offsets, coalesced scroll/resize work and a ResizeObserver. A route-local carousel refresh event updates reveal targets when Owl replaces responsive loop clones. Each route removes its listeners when detached; initialization is idempotent. `services.js`, `project-detail.js`, the Team/Member `team.js` module and `contact-page.js` own only their reveal lifecycles. FAQ expansion, the Project Detail gallery and Contact validation/submission/reset are native Blazor state, with no Bootstrap JavaScript. A static `ThemeCarousel` rendering boundary prevents Blazor from diffing Owl's temporary wrappers/clones. Do not add stateful/event-bound children inside it; remount changed content with a new key. Project Detail does not use that Owl boundary: its approved gallery is a manual Bootstrap fade, retained through Bootstrap CSS and Razor-owned controls. No JavaScript fetches or generates entity content on Blazor routes. See the phase records for lifecycle limitations and transitional dependencies.

The new `public-shell.js` module only adds the existing scroll-driven fixed-header/back-to-top behavior, honors reduced motion, and removes event listeners when the layout is disposed. Mobile navigation is Blazor state, not a checkbox or jQuery handler. MudBlazor and `_framework/blazor.web.js` are loaded normally; providers run inside the interactive layout.

## Verification

With the app running, use PowerShell 7:

```powershell
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
./scripts/Test-About.ps1
./scripts/Test-Projects.ps1
./scripts/Test-ProjectDetail.ps1
./scripts/Test-Team.ps1
./scripts/Test-MemberDetail.ps1
./scripts/Test-Contact.ps1
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests
```

The nine available read-only HTTP suites cover routes, asset integrity/serving, approved content, shared canonical cards, detail records, Contact field/map semantics and images. Twenty available Node tests cover interop lifecycle/reduced-motion/resize logic, including cross-route carousel navigation and Contact reveal cleanup. The dependency-free Contact console harness runs 21 checks against the production model/service source: required fields, whitespace, email, optional subject, exact demo result, cancellation and reset. Text asset comparisons tolerate only Windows checkout line-ending differences; binary comparisons remain exact. These checks do not replace interactive browser testing.

Phase 8 specifically ran Foundation and Contact HTTP suites, the two Contact Node tests and the 21 C# checks; previous pages received only lightweight HTTP/browser smoke checks, not another full multi-breakpoint comparison. For approved comparisons, run `node scripts/Serve-StaticReference.mjs` and open `http://127.0.0.1:5140/contact.html` (or the other original root HTML pages). This loopback-only helper serves root HTML and approved assets, not repository metadata.

In **Development only**, open `http://localhost:5138/?verify=foundation` to exercise a MudSelect/popover, snackbar and dialog below Home. This opt-in component is absent from normal pages and Production, even with the query parameter. See [Phase 1](docs/phase-1-verification.md), [Phase 2](docs/phase-2-verification.md), [Phase 3](docs/phase-3-verification.md), [Phase 4](docs/phase-4-verification.md), [Phase 5](docs/phase-5-verification.md), [Phase 6](docs/phase-6-verification.md), [Phase 7](docs/phase-7-verification.md) and [Phase 8](docs/phase-8-verification.md) for verification evidence.

## Home content boundary

`IHomeContentService.GetAsync` supplies typed Home data. The temporary singleton implementation reads projects through `IProjectCatalog` and members through `IMemberCatalog`, selects the approved featured IDs, and combines them with typed editorial copy from `index.html`. Restart after changing JSON. The cards do not own entity values. Replace the service's DI implementation to use a future database; keep the content contracts and visual components. This is not a generic CMS.

## Next phase — approval required

Recommended next Dashboard slice: Home Services introductory text as a third Home sidebar child, subject to separate approval. It is not implemented. Other CMS sections, media upload, drafts/versioning, email/newsletter delivery, and merge/deployment remain out of scope. Keep the static reference.

## Services content boundary

`IServicesContentService.GetAsync` supplies typed hero, service, benefit, process, pricing and FAQ content from an in-memory snapshot of approved `service.html`. `ServiceCatalog` is the single service identity/copy/icon source: Services uses all six entries and Home selects its original five by ID. Existing `ServiceSummary`, `ServiceCard` and `SectionHeader` APIs are unchanged. Replace the provider's DI implementation for a future content source; no storage layer or speculative fields were added.

`InnerPageHero` remains separate from `HomeHero`; theme CSS owns its imagery and geometry. Five numbered process slots preserve the desktop diagram and responsive stacking. Pricing remains demo-only with disabled, explained buttons and no checkout destination. FAQ items independently toggle, first open initially, with accessible button/panel relationships and reduced-motion support. Retained sizing constraints and visual comparison results are recorded in Phase 3.

## About content boundary

`IAboutContentService.GetAsync` supplies typed hero, story, vision, timeline and mission content from an in-memory snapshot of approved `about.html`. `PartnerCatalog` supplies one canonical heading and six partner identities/copy/images to Home and About. Both routes use `PartnersSection`, `PartnerCard` and `ThemeCarousel`; existing component APIs are unchanged. No partner destination was invented.

The three chronological timeline slots retain their approved desktop shapes, tablet two-plus-one arrangement and phone stack. An ordered list exposes complete year headings while split decorative numerals remain hidden from assistive technology. Four mission commitments form an accessible list. Original fixed hero/vision/mission sizing is retained for the approved concise copy; future editing must respect the constraints documented in Phase 4.

## Projects content boundary

`IProjectCatalog` is the single application-lifetime snapshot of all seven canonical `wwwroot/data/projects.json` records, projected into the unchanged `ProjectSummary` contract and a typed `ProjectDetail` contract. Home selects its original five IDs; `IProjectsContentService` supplies all seven in approved order plus typed hero/testimonial composition. Detail resolves the same catalog, including AutoTracker and FinVault, and owns the same summary objects. Listing covers use `image/project/{slug}.jpg`; do not substitute detail gallery images (NexConnect intentionally differs).

Projects reuses `InnerPageHero`, `ProjectCard` and the shared `TestimonialsSection`/`TestimonialCard`/`ThemeCarousel`. `TestimonialCatalog` holds the identical approved Home/Projects quotes and brand copy. The listing uses the original three/two/one-column grid. Pagination preserves the sample appearance as hidden-from-assistive-technology spans, with no links, buttons, focus stops or page-changing state. No filtering/search was added.

Original fixed card heights remain for visual parity and fit the approved copy at all four checked widths. Longer future content needs a separate sizing review. The approved source's exact-768px testimonial background conflict is now corrected only within `.projects-page`, using the existing mobile background in `projects-blazor.css`; original assets and shared components remain unchanged. See the [approved breakpoint fix verification](docs/phase-5-verification.md#16-approved-follow-up--projects-testimonial-breakpoint-fix) for the cause, six-width checks and regressions, and the earlier Phase 5 record for intentional accessible-carousel differences.

## Project Detail content boundary

`IProjectCatalog.GetDetailAsync` resolves exact canonical slugs from the same snapshot used by Home/listing. `ProjectDetail` adds the existing full summary, ordered `ProjectImage` collection, `ProjectMetadata` (client/category/editorial date/technologies) and feature strings. No JSON or image assets changed. Route version/cancellation guards prevent stale async results; a slug key remounts detail state. Missing slugs use `NavigationManager.NotFound()` and the existing shell.

`ProjectDetailContent` reuses `InnerPageHero` and simple `Breadcrumbs`, and composes `ProjectGallery`, summary, metadata and one desktop Features panel. The gallery keeps original sizing/crops/fade/arrows, with accessible buttons, Left/Right keys and horizontal touch/pen swipes. It has no autoplay, timers or plugin handlers; single-image galleries omit controls. Original CSS keeps Features hidden at **<=1200px**, without moving or duplicating it. The original fixed decorative hero art and inherited desktop Features clipping remain intentionally unchanged; see the Phase 6 visual record before changing those constraints.

## Team and Member Detail content boundary

`IMemberCatalog` owns one application-lifetime snapshot of all six `wwwroot/data/member.json` records. Its existing `TeamMemberSummary` projection is shared by Home (the original four IDs), Team (all six in canonical/static order), and `MemberDetail` (the same summary plus the existing full biography and read-only skills). Short listing teasers come from approved `team.html`; they are not duplicate biographies. `ITeamContentService` supplies the Team hero/intro editorial composition and the shared catalog collection. Replace these DI implementations for a future data source without rewriting visual components.

`TeamMemberCard` is reused directly, with one typed `Context` parameter: the default `Featured` keeps Home's horizontal portrait hexagon, while `Listing` uses Team's approved vertical hexagon. Original route CSS owns all card geometry. Team reuses `InnerPageHero`; Member Detail intentionally preserves its distinct clipped header, overlapping portrait, centered profile and responsive skills columns. It reuses `Breadcrumbs` and `SocialLinks`, including the original per-icon reveal timing. Configured email/LinkedIn destinations retain accessible names; unconfigured Telegram stays decorative and unfocusable. No profile destinations were invented.

`/team/{slug}` resolves exact canonical IDs, guards stale asynchronous results, keys the profile by slug, and uses `NavigationManager.NotFound()` for missing records. Profile title, H1, breadcrumb, role, biography, skills and links are Razor-bound. There is no member JSON fetch/DOM mutation in browser code. The source's weak white menu-icon contrast at 1366px on the clipped Member header is documented rather than redesigned in this migration; see Phase 7's visual limitations.

## Contact content and submission boundary

`IContactContentService` supplies the approved hero, contact information, labels/placeholders and map configuration as typed records. The source phone/email/address remain demo content, not verified business details. The existing Calgary Tower embed is a typed `Uri`, not arbitrary iframe HTML; its presentation, lazy loading and URL are preserved, with an accessible title added.

`ContactForm` uses `EditForm`, `ContactFormModel` and data annotations. First Name, Last Name, Email and Message reject blank/whitespace values; Subject remains optional. The source email expression is retained, with trimmed email input. Validation messages are associated with fields; a focused live status region replaces the source SweetAlert modal. The textarea starts empty. Successful submission clears all five fields while retaining the rendered form/reveal nodes.

`IContactFormService` is the replacement point for a separately approved future delivery service. The stateless demo implementation validates the model and returns **“Demo form submitted successfully. No message was sent.”** It performs no HTTP/email calls, persistence or logging of submitted values. Form values exist in the normal Interactive Server circuit; this is not a client-only form or a real delivery system. Submit is disabled before interactivity and while awaiting a result. No legacy form listeners, SweetAlert dependency or new backend were introduced. `contact-blazor.css` is scoped to Contact; it preserves source typography for semantic H2 headings and adds validation/status/focus/reveal adaptations only.
