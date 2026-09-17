# NexNovaCo — Blazor foundation

Phase 1 only: .NET 10 Blazor Web App, global Interactive Server rendering, and **MudBlazor 9.10.0**. The six public routes intentionally contain migration placeholders. No full page or backend has been migrated.

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
    Shared/                   # SocialLinks, BackToTop, development diagnostics
    Pages/                    # Six placeholders, Error and NotFound
    Sections/                 # Reserved; no full-page sections yet
  Models/                     # Reserved; no content architecture yet
  Services/                   # Reserved; no backend services yet
  wwwroot/{css,js,image,data}/
  Program.cs
  NexNovaCo.Web.csproj
  packages.lock.json
scripts/Test-Foundation.ps1
docs/phase-1-verification.md
```

The original root HTML and `assets/` stay in place as the approved static reference. The .NET template's standard `Components/App.razor` and `Components/Routes.razor` locations are intentional. No WebAssembly/client project exists.

## Shell and routes

`MainLayout` owns one header, the main landmark, one footer, the four MudBlazor providers, and lifecycle-managed shell interop. `SiteHeader` owns mobile state; `MobileNavigation` is the accessible toggle; `PrimaryNavigation` supplies the same `NavLink` list to header and footer. The menu closes on a link or route change and on Escape while focus is in the header; Escape restores toggle focus. `SiteFooter` retains the approved grid, copy, social icons, newsletter demo, and copyright toolbar.

Public routes: `/`, `/about`, `/services`, `/projects`, `/team`, `/contact`. Standard `/Error` and `/not-found` infrastructure pages are also retained. There are no detail routes. Newsletter is an inert demo: it neither submits nor stores data. The existing demo email address remains; unconfigured social profiles remain decorative.

## Styling and assets

CSS order is **MudBlazor → existing Bootstrap → unchanged styles.css → public-shell.css → Blazor isolated styles**. The isolated styles only cover template error/reconnect UI. Public components use HTML, not MudBlazor cards/buttons/drawers. Lato/Poppins remain Google Fonts dependencies, as in the static baseline; no Roboto font link was introduced.

`public-shell.css` carries only the Home header rules formerly embedded in `index.css`, small semantic-button/state adaptations, accessibility focus styles, and placeholder spacing. It keeps the existing 1400px navigation breakpoint, 90px header, orange accents, and footer rules. The hamburger is blue on the pale foundation header at all mobile-menu widths so it remains visible without a hero behind it. Header stacking is below MudBlazor's overlay layers. No mass `!important`, new breakpoint system, or hexagon/asset redesign was introduced. Page-specific hero geometry remains in the copied CSS for Phase 2.

All **82 existing asset files** were copied unchanged: 13 CSS, 8 JS, 59 image/icon/SVG files, and 2 JSON files. New components use `css/`, `js/`, `image/`, and `data/` under the application base URL. CSS `../image/` references keep working. A transitional `/assets/` static-file alias serves the same `wwwroot` files, preserving unchanged JSON image paths and dormant legacy `fetch("assets/data/...")` references without duplicating assets or inventing content models. New code should use the canonical paths; retire the alias after the final legacy consumer is migrated.

Bootstrap CSS is active. AOS/Owl CSS, Bootstrap JS, jQuery 3.1.0, Owl Carousel and AOS JS are copied but not loaded by this shell, which has none of their widgets. CountUp's external CDN reference and SweetAlert's contact-page reference are deferred with their pages. The baseline's unused Owl video-play image reference has no corresponding asset; no video carousel is loaded in Phase 1.

## JavaScript boundary

All eight legacy JavaScript files remain byte-for-byte copies. `script.js`, contact handling, project/member binding, carousels, counters, loader and AOS initialization are deliberately dormant: they depend on full-page markup and document-ready lifecycle events. Do not globally enable them on Blazor navigation without a per-component lifecycle review.

The new `public-shell.js` module only adds the existing scroll-driven fixed-header/back-to-top behavior, honors reduced motion, and removes event listeners when the layout is disposed. Mobile navigation is Blazor state, not a checkbox or jQuery handler. MudBlazor and `_framework/blazor.web.js` are loaded normally; providers run inside the interactive layout.

## Verification

With the app running, use PowerShell 7:

```powershell
./scripts/Test-Foundation.ps1
```

This read-only smoke check covers direct routes, asset serving, legacy aliases and asset-copy integrity. It does not replace interactive browser tests.

In **Development only**, open `http://localhost:5138/?verify=foundation` to exercise a MudSelect/popover, snackbar and dialog. This opt-in component is absent from normal pages and Production, even with the query parameter. See [the verification record](docs/phase-1-verification.md) for the browser matrix and manual checklist.

## Next phase — approval required

Migrate Home first, section by section against `index.html`, beginning with its hero and welcome section. Bind only the content needed by that page and introduce lifecycle-aware animation/carousel integration when its markup exists. Full pages, detail routes, models/CMS, database, admin/dashboard, authentication, email and newsletter services remain out of scope. Do not remove the static reference yet.
