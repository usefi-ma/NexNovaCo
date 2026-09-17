# NexNovaCo — Blazor Home migration

Phase 2: the approved Home page now renders through reusable Razor sections on the .NET 10 / global Interactive Server / **MudBlazor 9.10.0** foundation. The five other public pages remain placeholders. No backend, database, CMS or authentication has been added.

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
    Pages/                    # Home, five placeholders, minimal detail stubs, infrastructure
    Sections/Home/            # Eight Home sections, including its dedicated hero
  Models/HomeContent.cs       # Small immutable content contracts
  Services/                   # Replaceable IHomeContentService and temporary implementation
  wwwroot/{css,js,image,data}/
  Program.cs
  NexNovaCo.Web.csproj
  packages.lock.json
scripts/Test-Foundation.ps1
scripts/Test-Home.ps1
scripts/Test-HomeInterop.mjs
docs/phase-1-verification.md
docs/phase-2-verification.md
```

The original root HTML and `assets/` stay in place as the approved static reference. The .NET template's standard `Components/App.razor` and `Components/Routes.razor` locations are intentional. No WebAssembly/client project exists.

## Shell and routes

`MainLayout` owns one header, the main landmark, one footer, the four MudBlazor providers, and lifecycle-managed shell interop. `SiteHeader` owns mobile state; `MobileNavigation` is the accessible toggle; `PrimaryNavigation` supplies the same `NavLink` list to header and footer. The menu closes on a link or route change and on Escape while focus is in the header; Escape restores toggle focus. `SiteFooter` retains the approved grid, copy, social icons, newsletter demo, and copyright toolbar.

Public routes: `/`, `/about`, `/services`, `/projects`, `/team`, `/contact`. Standard `/Error` and `/not-found` infrastructure pages are retained. Featured Home links use minimal `/projects/{slug}` and `/team/{slug}` placeholders; unknown slugs return 404. These are not full detail pages. Newsletter is an inert demo: it neither submits nor stores data. The existing demo email address remains; unconfigured social profiles remain decorative.

## Styling and assets

CSS order is **MudBlazor → existing Bootstrap → AOS/Owl CSS → unchanged styles.css → public-shell.css → Blazor isolated styles**. Home adds unchanged `index.css`, then targeted `home-blazor.css`, through route-local HeadContent. The isolated styles only cover template error/reconnect UI. Public components use HTML, not MudBlazor cards/buttons/drawers. Lato/Poppins remain Google Fonts dependencies, as in the static baseline; no Roboto font link was introduced.

`public-shell.css` carries only the Home header rules formerly embedded in `index.css`, small semantic-button/state adaptations, accessibility focus styles, and placeholder spacing. It keeps the existing 1400px navigation breakpoint, 90px header, orange accents, and footer rules. The hamburger is blue on the pale foundation header at all mobile-menu widths so it remains visible without a hero behind it. Header stacking is below MudBlazor's overlay layers. No mass `!important`, new breakpoint system, or hexagon/asset redesign was introduced. Home loads its original hero geometry through route-local CSS.

All **82 existing asset files** were copied unchanged: 13 CSS, 8 JS, 59 image/icon/SVG files, and 2 JSON files. New components use `css/`, `js/`, `image/`, and `data/` under the application base URL. CSS `../image/` references keep working. A transitional `/assets/` static-file alias serves the same `wwwroot` files, preserving unchanged JSON image paths and dormant legacy `fetch("assets/data/...")` references without duplicating assets or inventing content models. New code should use the canonical paths; retire the alias after the final legacy consumer is migrated.

Bootstrap and AOS/Owl CSS are active. jQuery 3.1.0, Owl Carousel 2.3.4 and a local copy of the original CountUp 2.0.8 load once. Only Home initializes its three carousels and counters. Bootstrap JS, the global AOS engine and unrelated legacy page scripts stay dormant. The baseline's unused Owl video-play image reference has no corresponding asset; no video carousel is enabled.

## JavaScript boundary

All eight legacy JavaScript files remain unchanged in Git. The global `script.js`, contact handling and project/member DOM binding are dormant. `home.js` initializes Owl and CountUp only inside Home, applies the existing AOS CSS effects through scoped IntersectionObservers, and destroys observers/plugins when Home leaves the DOM. A static `ThemeCarousel` rendering boundary prevents Blazor from diffing Owl's temporary wrappers/clones. Do not add stateful/event-bound children inside it; remount changed content with a new key. No JavaScript fetches or generates entity content. See the Phase 2 record for lifecycle limitations and transitional dependencies.

The new `public-shell.js` module only adds the existing scroll-driven fixed-header/back-to-top behavior, honors reduced motion, and removes event listeners when the layout is disposed. Mobile navigation is Blazor state, not a checkbox or jQuery handler. MudBlazor and `_framework/blazor.web.js` are loaded normally; providers run inside the interactive layout.

## Verification

With the app running, use PowerShell 7:

```powershell
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
node --test scripts/Test-HomeInterop.mjs
```

The read-only checks cover routes, asset integrity/serving, approved Home text, featured JSON content, detail stubs and interop lifecycle/reduced-motion logic. Text asset comparisons tolerate only Windows checkout line-ending differences; binary comparisons remain exact. They do not replace interactive browser tests.

In **Development only**, open `http://localhost:5138/?verify=foundation` to exercise a MudSelect/popover, snackbar and dialog below Home. This opt-in component is absent from normal pages and Production, even with the query parameter. See [Phase 1](docs/phase-1-verification.md) and [Phase 2](docs/phase-2-verification.md) for verification evidence.

## Home content boundary

`IHomeContentService.GetAsync` supplies typed Home data. The temporary singleton implementation snapshots canonical `wwwroot/data/projects.json` and `member.json`, selects the approved featured IDs, and combines them with typed editorial copy from `index.html`. Restart after changing JSON. The cards do not own entity values. Replace the service's DI implementation to use a future database; keep the content contracts and visual components. This is not a generic CMS.

## Next phase — approval required

Recommended Phase 3: Services, reusing `ServiceSummary` and `ServiceCard` after inspecting that page's full approved structure. Do not begin without approval. Other full pages, detail implementations, database/CMS, admin/dashboard, authentication, email and newsletter services remain deferred. Keep the static reference.
