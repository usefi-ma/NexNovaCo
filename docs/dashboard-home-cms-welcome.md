# Home CMS container + Welcome slice

## Scope and navigation

Continued from clean `feature/dashboard-cms` at `df8c5ef`. The new Admin-only `/dashboard/content/home` uses the existing Dashboard shell and theme. The drawer and Dashboard quick-access card now link to **Home**. There are only two tabs: Hero and Welcome. No additional CMS functionality or public styling changes are included.

The old `/dashboard/home/hero` route remains compatible. `HomeHeroEditor` is the same component on that route and inside the Hero tab (`Embedded=true` changes only its heading/PageTitle presentation). Its edit model, validation, load/save methods, and service are unchanged. There is no duplicate Hero implementation.

`MudTabs` keeps independent panels alive and initializes each on first visit (`KeepPanelsAlive` + `LazyLoadPanels`). Switching tabs preserves unsaved input and each form's validation/success state. Reload or navigating away discards unsaved input; drafts are not implemented. The page opens on Hero. No query routing or custom tab JavaScript was added.

## Actual Welcome structure

The source of truth is the existing `HomeWelcomeSection.razor` and approved content formerly in `HomeContentService`:

| Field | Public element | Required / maximum |
| --- | --- | --- |
| Title | h2 | 120 characters |
| Introduction | h5 | 500 characters |
| Body paragraph 1 | first p | 1,000 characters |
| Body paragraph 2 | second p | 1,000 characters |
| CTA text | existing button label | 60 characters |
| CTA route | existing button href | 200 characters |

There is no invented eyebrow, rich-text field, paragraph builder, image selector, or layout control. The approved title is “Welcome to NexNovaCo”; CTA is “Learn more” → `about`. All introduction/body copy was transferred verbatim to `HomeWelcomeDefaults`. Existing `image/home/welcome.jpg`, hexagon artwork, markup, animation attributes, CSS and responsive rules are unchanged.

## Persistence and migration

`HomeWelcomeSettings` lives in the existing `ApplicationDbContext` and SQLite connection. Migration `20260921223434_AddHomeWelcomeSettings` only creates its table: six text columns, `UpdatedAtUtc`, and fixed primary key `Id=1` with a database check constraint. It neither rebuilds nor modifies Identity/Hero tables.

`HomeWelcomeInitializer` follows the existing Hero controlled-startup pattern: insert approved defaults only when missing, never overwrite an existing row. The database prevents any second singleton ID. Updates load and modify the existing row, then set UTC modification time. Public fallback never writes to SQLite.

Development applies the additive migration through the existing startup path. Production retains the existing explicit controlled migration/initialization opt-in; there is no new automatic production migration policy. Back up the deployed database before applying migrations as usual. No database reset is required. Runtime databases and QA artifacts stay gitignored.

## Service, validation and security

`IHomeWelcomeContentService` / `HomeWelcomeContentService` use short-lived contexts from `IDbContextFactory`, with `AsNoTracking` for reads. Public reads map the singleton to the existing `WelcomeContent` contract. Missing, invalid or unavailable data logs the problem and renders approved defaults without persisting them; editor reads instead report a safe failure.

`HomeWelcomeEditModel` validates all six required fields and length limits. Its CTA allowlist is identical to Hero's: relative/root-relative public routes only. Schemes, protocol-relative links, encodings, traversal, backslashes, admin/dashboard paths and invalid/query paths are rejected. Text is Razor-encoded, not rendered as HTML.

The page inherits the existing Dashboard Admin authorization. Service editor reads and writes also recheck authenticated Admin claims, live user/security stamp and database role membership. A stale/revoked circuit cannot save. Login, cookies, logout and antiforgery architecture are unchanged.

## Editor and public integration

The Welcome tab uses existing themed MudPaper, MudGrid, MudTextField, MudStack, MudButton and MudAlert patterns. Introduction/body textareas use MudBlazor 9.10 `InputSizing.Auto` with minimum line counts. Lazy panel initialization lets them measure visible widths. There is no fixed-height panel or nested panel scrolling. CTA inputs pair on wider screens and stack on phones; phone actions are full width.

Save validates, updates SQLite, remains on the selected tab and reports **Home Welcome section updated successfully.** Failures keep entered values, show safe feedback and log technical details server-side. Save disables during the operation. View Home Page uses the existing full-navigation boundary.

`HomeContentService` now reads both Hero and Welcome on each request/navigation. All other Home content stays static. `Home.razor` and `HomeWelcomeSection.razor` required no changes; only the content provider changed. Welcome remains public-site HTML, not MudBlazor.

## Verification

- Debug and Release builds: **0 warnings / 0 errors**. Debug used `bin/HomeCmsDebug/` to avoid interfering with the existing Visual Studio session.
- **205 authentication/CMS checks passed**, including old Hero compatibility, new route authorization, service authorization, unsafe/valid CTA paths, whitespace/length boundaries, HTML encoding, missing/invalid/unavailable read fallback, singleton constraint, repeated initialization, restart persistence, and preserved Admin password/roles/Hero edits through the additive upgrade.
- EF `has-pending-model-changes`: no model drift.
- **21 JavaScript checks** and **21 Contact checks** passed.
- Browser: Admin login, Home navigation, tab switching with unsaved input, isolated validation/success state, Hero save, Welcome save, public display, refresh and actual QA-process restart persistence. Approved copy restored afterward.
- Responsive checks at **1440, 1024, 768 and 390px**: both tabs fit; no horizontal page overflow; desktop sidebar and mobile overlay; CTA/actions stack on mobile. No new CSS breakpoints or shell redesign.
- Focused public smoke only: Home and Services. Public markup/styles/artwork are untouched. This is not a full public-site visual regression or comprehensive accessibility audit.
- Mobile drawer navigation closed correctly; menu logout returned to login. Anonymous Home/Services rendered without Admin styles and had no horizontal overflow after initialization. Final browser warning/error logs were empty (excluding intentional QA-server restarts).

Tests used disposable/isolated databases, including local browser QA under ignored `artifacts/home-cms-qa-67ecc8e3`. The user's normal database and Visual Studio process were not modified or stopped. Local screenshots are under ignored `artifacts/home-cms-welcome/`.

### Reproduction commands

```powershell
dotnet build NexNovaCo.sln -c Release --no-restore
dotnet build NexNovaCo.sln -c Debug --no-restore -p:OutputPath=bin/HomeCmsDebug/
dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build --no-restore
dotnet ef migrations has-pending-model-changes --project src/NexNovaCo.Web --configuration Release --no-build
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore
```

## Deferred and recommended next slice

No other Home sections, image/media tools, Services/Projects/Team CMS, header/footer/settings/SEO editors, drafts, audit history, localization or role-management UI were implemented.

Recommended next small slice, pending approval: the **Home Services section heading and introductory copy only**, retaining the existing service catalog/cards and public layout. Do not conflate that with a Services-page CMS or build a generic page builder.
