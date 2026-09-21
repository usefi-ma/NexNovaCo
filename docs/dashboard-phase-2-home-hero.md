# Dashboard Phase 2 — Editable Home Hero

## Scope and Git

Continued from clean `feature/dashboard-cms` at `ad68ec2`. No branch switch, main merge, force push, package upgrade, public redesign, or wider CMS implementation. This phase changes only the Home Hero content source and adds its real Dashboard editor, migration, validation, tests, and documentation.

## Existing content is the source of truth

Inspected `Home.razor`, `HomeHero.razor`, `HomeHeroContent`, `HomeContentService`, the CTA, and route-local public styles before choosing fields. The six existing fields remain:

| Field | Approved initial value | Maximum length |
| --- | --- | --- |
| OpeningLine | Smart Software | 60 |
| EmphasisLine | Powerful AI | 60 |
| ClosingLine | Endless Innovation | 60 |
| Description | We build high-performance web, mobile, and AI-driven applications to help businesses grow. | 500 |
| CtaLabel | Discover Our Services | 60 |
| CtaHref | services | 200 |

There is no image field in the existing Hero content contract. All image/background, hexagon geometry, spacing, typography, animations, and breakpoints remain theme-owned. `Home.razor`, `HomeHero.razor`, public CSS, JavaScript, and artwork are unchanged.

## Database and initialization

`HomeHeroSettings` is in the existing `ApplicationDbContext` and the same configured Identity SQLite database. Its fields are the six existing content strings plus `Id` and `UpdatedAtUtc`. The default development path remains `src/NexNovaCo.Web/App_Data/nexnovaco.db`.

Migration **`20260921170044_AddHomeHeroSettings`** creates only `HomeHeroSettings`. It does not alter/drop Identity tables. Migration source is tracked; runtime SQLite/sidecar files remain ignored and excluded from publish.

Singleton strategy: fixed primary key **Id = 1**, `ValueGeneratedNever`, and database check constraint `CK_HomeHeroSettings_Singleton` (`Id = 1`). Therefore neither startup nor saves can create a second active row. Saves update the existing row, never insert an arbitrary id. This intentionally uses last-successful-save-wins behavior for a simple single-admin CMS; no optimistic concurrency/version system was added.

`HomeHeroDefaults` holds the exact approved content above. After migrations, `HomeHeroInitializer` checks for Id 1 and inserts defaults only when missing. It never updates an existing row. This runs inside the existing controlled initialization workflow, before optional Admin user seeding, so missing bootstrap credentials do not prevent Hero initialization.

- **Development:** restart the app; migrations and missing-record initialization run automatically.
- **Production:** retain the Phase 1 controlled single-instance initialization policy. Supply the production connection securely and explicitly enable `Identity__InitializeDatabase=true` for the controlled run to apply migrations and initialize the missing Hero. Disable it afterward. Existing Admin credentials are not needed merely to initialize Hero data; never reset an existing account.
- Applying migrations with `dotnet ef database update` creates the table but does not run the runtime initializer. Complete the controlled initialization run afterward. Until then, public reads display the approved fallback and the editor reports missing data.
- Do not run multiple bootstrap instances concurrently. The fixed key/check constraint prevents duplicates, but competing initializers may fail on a unique-key conflict rather than hiding deployment misconfiguration.

No normal development database was reset, deleted, or populated with test copy during this phase. Its next app startup performs the additive upgrade. The existing Visual Studio session was left running.

## Service and context lifetime

`IHomeHeroContentService` / `HomeHeroContentService` expose:

- `GetAsync`: anonymous public read, `AsNoTracking`, typed `HomeHeroContent`, safe fallback.
- `GetForEditAsync`: Admin-checked read into a separate `HomeHeroEditModel`; failures are reported rather than showing fallback as saved data.
- `UpdateAsync`: fresh Admin/security-stamp check, full model validation, singleton update, `UpdatedAtUtc = DateTime.UtcNow`, save.

`AddDbContextFactory<ApplicationDbContext>` replaces the direct registration. It also supplies the scoped context needed by the existing Identity stores. CMS operations create and asynchronously dispose a new factory context each time; components retain no DbContext. The content services are scoped, but their database contexts are not circuit-lived.

This follows [Microsoft's Blazor EF Core context-per-operation guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-10.0). No generic repository, new caching infrastructure, or public write endpoint was introduced.

## Editor, validation, authorization, and save flow

Route: **`/dashboard/home/hero`**. It inherits the existing Dashboard folder's Admin authorization and `DashboardLayout`. The navigation gains exactly one real item: **Home Hero**.

The editor uses `EditForm`, `DataAnnotationsValidator`, six `MudTextField` controls, `MudButton`, `MudPaper`, `MudText`, and status/error `MudAlert` messages. The Mud inputs use their `For` expressions to integrate with normal Blazor model validation, as described in [MudBlazor's EditForm guidance](https://www.mudblazor.com/components/form).

All six fields are required and reject whitespace-only values. Limits are shown above and enforced in both UI/model and update service. The CTA is deliberately restricted to current public route shapes: `/`, `about`, `services`, `contact`, `projects`, `projects/{slug}`, `team`, `team/{slug}`, with an optional leading slash except Home. Slugs accept lowercase alphanumeric words separated by hyphens. Route shape validation does not guarantee a detail slug exists; the site's normal 404 behavior still applies.

External URLs, JavaScript/data schemes, protocol-relative URLs, encoded redirects, backslashes, queries/fragments, traversal, and Admin routes are rejected. Text is rendered with normal Razor encoding, not `MarkupString`/raw HTML.

Save validates, disables repeat submissions while pending, writes SQLite, and displays **Home Hero updated successfully.** The Admin stays on the edit page. Expected database, missing-record, authorization, or validation errors receive safe feedback; entered values stay in the form and the server logs the failure. No stack traces or database details appear in the UI. Failed loading does not present an editable fallback record.

Authorization is not just nav visibility. The endpoint/route requires Admin, and the read-for-edit/update service obtains the server authentication state and checks current database membership plus security stamp. Removing the role or revoking the stamp blocks a subsequent save even before scheduled circuit revalidation. The HTTP integration suite also guards future `/dashboard` and `/admin` routes against missing authorization.

## Public integration and fallback

`HomeContentService.GetAsync` replaces only the Hero in its typed `HomeContent` result with a fresh CMS read. The other Home sections keep their existing static/catalog sources. SSR awaits the content before returning the complete Home page; no separate client fetch/loading placeholder was introduced. Refresh/new navigation reads current saved values rather than a cached Hero.

If the row is missing, the database read fails, or persisted content fails validation, the service logs the problem and returns `HomeHeroDefaults.Content`. A public read never persists fallback values. This handles runtime read failures; it does not promise the application can start with an unavailable database while an operator-requested startup migration is running.

Browser testing caught an existing cross-area navigation limitation: `data-enhance-nav="false"` disables SSR enhancement but does not disable an interactive Router's navigation interception. Both Dashboard-to-public links now explicitly call `Navigation.NavigateTo("/", forceLoad: true)` while retaining ordinary anchor destinations. This rebuilds `App.razor` with the public stylesheet/vendor set instead of leaving admin styles active. Public CSS itself was not changed.

## Verification

Commands from repository root:

```powershell
dotnet restore NexNovaCo.sln --locked-mode
dotnet build NexNovaCo.sln -c Release --no-restore
dotnet build NexNovaCo.sln -c Debug --no-restore -p:OutputPath=bin/Phase2Debug/
dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build --no-restore
dotnet ef migrations has-pending-model-changes --project src/NexNovaCo.Web --configuration Release --no-build
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore
```

Debug uses a separate output folder because an existing Visual Studio/app process locks the ordinary Debug binaries. Initial attempts against the locked output reported copy warnings/errors; the isolated Debug output and Release builds both finish with **0 warnings / 0 errors**, without stopping the user's session.

**134 authentication/CMS checks pass**, covering the Phase 1 suite plus:

- Anonymous challenge, Admin editor access, non-Admin denial, interactive Mud editor markup.
- Exact approved initialization; repeated initialization doesn't overwrite; fixed singleton enforcement at the database boundary.
- Saves visible to anonymous Home and refresh; new application host preserves copy and existing Admin password.
- Required/max-length/CTA validation; rejected changes do not alter saved content; rendered text stays HTML-encoded.
- Direct service write denial for anonymous/non-Admin users, removed Admin roles, and revoked security stamps.
- Missing-row/unavailable-database fallback without writes; editor missing/read/write failures propagate for safe UI handling.
- Upgrade of an isolated Phase 1 database containing an Admin account preserves its password hash, role, and mapping; the new table exists and the model has no migration drift.

Existing suites: **20 JavaScript lifecycle tests**, **21 Contact tests** pass. Tests use temporary isolated databases, never the user's normal SQLite file. No passwords or hashes are printed.

Locked restore and Release publish also pass. The published output contains no database/sidecar files, App_Data, development settings, secrets files, symbols, or test assemblies.

### Browser workflow

Used a separate loopback-only QA instance and isolated SQLite database. Temporary credentials were generated in memory and supplied through process configuration; no personal account/password or persisted plaintext credential was used.

Verified normal Identity login → real Home Hero nav → required/unsafe CTA validation → edit OpeningLine/CTA → Save confirmation → public Home shows saved text/link → browser refresh and QA server restart preserve edits → return to editor → restore approved copy → public Home confirms restoration → logout → editor challenges again.

Also verified both cross-area links load `public-site` and public CSS without admin CSS. Fresh anonymous Home/Services smoke checks passed with one header/footer, the original CTA navigation, working server-side Services FAQ, no broken Home images, and no steady-state horizontal overflow. Existing entrance animations briefly expand layout during initial rendering; no animation/CSS changes were made. The deliberate server restart produced expected connection warnings in the old test tab; the final fresh smoke tab had no warning/error logs.

No historical multi-breakpoint public regression was run. Local screenshots are ignored QA artifacts under `artifacts/home-hero-phase2-qa/`.

## Deferred work and recommendation

No image upload/replacement, media library, Welcome/Services/Projects/Team/Testimonial/Contact editor, navigation/footer/SEO/settings editor, page builder, generic CMS engine, content versioning, drafts/publishing, audit history, languages, or extra roles/permissions.

Recommended next planning slice: Home Welcome text/CTA, after approval of this Hero workflow and its constrained validation model. Do not begin that implementation without approval.
