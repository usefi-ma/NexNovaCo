# Phase 20 — Detail-page template CMS

## Pre-implementation audit and ownership

Started on `feature/dashboard-cms` with a clean working tree after pre-phase commit `59898df` (only two approved public color refinements). `SiteFooter.razor` had no staged/unstaged content or mode change; its Git-normalized hash equaled HEAD. It was restored and its index stat refreshed without a committed change. No push/merge.

Focused implementation commits:

- `fa71b32` — Add detail page template settings.
- `82486b8` — Add detail template editors and connect public pages.
- `247716e` — Verify detail template CMS ownership and regressions.

This report follows in its own documentation commit. The final handoff verifies a clean working tree; generated screenshots/fixtures are ignored, the test host was stopped and its disposable session-cookie file removed. No branch switch, history rewrite, push or merge.

Inspected ProjectDetail, ProjectDetailContent, InnerPageHero, ProjectGallery, Breadcrumbs, PublicPageHead, IProjectContentService/ProjectContentService, ProjectCatalog defaults, ProjectEditModel/ProjectEditor; MemberDetail, MemberProfileContent, SocialLinks, IMemberContentService/MemberContentService, MemberCatalog defaults, MemberEditor; detail CSS, Dashboard layout, page-settings service/initializer patterns and UnsavedChangesGuard.

| Public field / behavior | Ownership / decision |
| --- | --- |
| Project slug, hero desktop/mobile name, summary, tagline, full description | Shared Projects; no duplicate settings |
| Project Client, Category, Date, Technologies **values** | Shared Projects |
| Cover, gallery sources/order/alt text, feature values/order | Shared Projects |
| Hero CTA `Explore the project` | Project template setting |
| Breadcrumb `Home` / `Projects` | Project template settings; exact current plural `Projects`, not prompt example `Project` |
| `Project Details`, `Features of the project` | Project template settings |
| Metadata labels `Client`, `Category`, `Date`, `Technologies` | Project template settings; colon stays presentation punctuation |
| Return CTA `View More Projects?` | Real additional Project template setting |
| Hero anchor, return link, breadcrumb URLs, current project breadcrumb | Fixed code routes + dynamic project name |
| Hero decorative source imagery, shape/position, gallery behavior, feature geometry/animation | Presentation-only; no media/control fields |
| Features hidden at `max-width: 1200px` | Preserve existing CSS exactly; no toggle |
| Member slug, name, role, profile image/alt, biography, skill values/order, email/LinkedIn/Telegram | Shared Team Members |
| Member breadcrumb `Home` / `Team` | Member template settings |
| Visible `Skills` heading | Real Member template setting (three fields total, not a breadcrumbs-only editor) |
| Member profile geometry/background, two-column skills layout, social icons/derived accessible labels, reveals | Presentation-only; no invented About/Connect/Follow fields |
| Page title, description, OG image, canonical | Existing SEO system using entity data; not template settings |
| BreadcrumbList names | Same edited template breadcrumb labels + dynamic entity name; URLs remain code-controlled |
| Sitemap paths, missing/deleted record behavior | Existing shared catalogs; template settings have no route ownership |
| Header/footer/identity/navigation/contact | Existing global CMS; unchanged in this phase |

Implementation decision: two small singleton tables (10 Project text labels and 3 Member text labels), no entity schema changes, media fields, arbitrary URLs or presentation controls. Separate focused settings service with short-lived contexts and existing Admin-session checks. Two editors under the existing Projects/Team groups, canonical manager links and current-record sample selection with listing fallback. Reuse EditorSnapshot + UnsavedChangesGuard.

## Dashboard IA

- Content → Projects → **Project Detail Template**: `/dashboard/content/projects/detail-template`.
- Content → Team → **Member Detail Template**: `/dashboard/content/team/detail-template`.
- Each uses the existing Dashboard shell/MudBlazor editor, matching active sidebar/title, and no public MudBlazor UI.
- Manage Projects links to `/dashboard/content/shared-projects`; Manage Team Members links to `/dashboard/content/shared-team`.
- View Sample queries the existing catalog at click time and opens the current first record. No slug is hardcoded. Empty catalogs fall back to `/projects` or `/team`.
- Existing routes/redirects are unchanged. Both editors explicitly explain shared entity ownership; no second collection manager.

## Models, migration and initialization

Additive migration: `20260927012048_AddDetailTemplateSettings` creates only:

- `ProjectDetailTemplateSettings`: singleton Id, the 10 text properties below, UpdatedAtUtc.
- `MemberDetailTemplateSettings`: singleton Id, the 3 text properties below, UpdatedAtUtc.

Both tables constrain Id to 1, use non-generated keys and the existing UpdatedAtUtc concurrency-token convention. No foreign keys or shared entity schema changes. No media, layout, URL, SEO, visibility, role or page-builder fields.

`DetailTemplateInitializer` runs in the existing database initialization pipeline after migrations. It inserts each missing singleton in a transaction using exact prior strings; existing rows and Admin edits are not updated. Collection initialization semantics are untouched. The existing production migration/initialization operator opt-in remains in force; this phase did not migrate/reset the developer's normal database. Tests exercised fresh initialization and upgrade of an existing isolated database, comparing every prior table's rows before/after.

### Editable Project template defaults

| Property | Exact approved default | Maximum |
| --- | --- | --- |
| HeroCtaText | Explore the project | 60 |
| ReturnCtaText | View More Projects? | 60 |
| BreadcrumbHomeLabel | Home | 60 |
| BreadcrumbSectionLabel | Projects | 60 |
| ProjectDetailsHeading | Project Details | 120 |
| FeaturesHeading | Features of the project | 120 |
| ClientLabel | Client | 60 |
| CategoryLabel | Category | 60 |
| DateLabel | Date | 60 |
| TechnologiesLabel | Technologies | 60 |

The metadata colon is still code-controlled punctuation. Values remain `ProjectMetadata` from the canonical shared record. CTA destinations remain the current detail route plus `#inner-project` and the Projects listing.

### Editable Member template defaults

| Property | Exact approved default | Maximum |
| --- | --- | --- |
| BreadcrumbHomeLabel | Home | 60 |
| BreadcrumbSectionLabel | Team | 60 |
| SkillsHeading | Skills | 120 |

The template visibly already had Skills; no About/Connect/Follow copy was invented. Skills **values**, profile content and contact destinations remain shared entity data.

## Services, validation and fallback

`IDetailTemplateContentService` / `DetailTemplateContentService` is focused on these two singletons, separate from shared Project/Member CRUD. Every operation creates/disposes a short-lived DbContext. Public reads use no tracking. Admin reads never disguise failed loads as editable defaults.

All labels are required plain single-line text, whitespace-only rejected, with the field-specific maximum above. Saves trim surrounding whitespace and preserve normal Unicode/punctuation. HTML tags/control characters are rejected; Razor still HTML-encodes values. There are no arbitrary URL inputs or rich-text renderers.

Missing/corrupt template settings or database read errors produce an error log and exact approved public fallback strings without writing defaults. Cancellation still propagates. Restoring DB availability makes saved Admin values authoritative again. The public record and its routes are still resolved through the existing catalog.

## Public integration and ownership preservation

ProjectDetail and MemberDetail load the corresponding template alongside the current entity. Existing route-request/cancellation guards and keyed child remounts remain. ProjectDetailContent and MemberProfileContent receive template copy as parameters; markup, classes, IDs, image bindings and visual modules are unchanged apart from replacing literals with values.

Shared Projects remains canonical for slug/name/tagline/summary/full description, Client/Category/Date/Technologies values, cover/gallery/alt/order, features and Home featured selection. Shared Team Members remains canonical for slug/name/role/introduction/biography, image, skills, email/LinkedIn/Telegram and Home featured selection. Existing uploads are reused in their canonical managers; no new media UI or storage was added here.

Breadcrumbs accept a Home label with the old default retained for other callers. PublicPageHead receives the same Home/section labels for BreadcrumbList. Current entity name stays dynamic; links and JSON-LD URLs remain `/`, `/projects` or `/team`, and the current entity URL.

## Responsive design and accessibility

No public CSS, responsive breakpoint, geometry, decorative source graphic, gallery controls, typography, social-icon behavior or reveal timing changed. Project Features remains hidden at **max-width: 1200px** and visible above it. No mobile Features copy or CMS toggle was added.

Public semantic headings, breadcrumb navigation/current-page state, gallery alt text/controls, profile alt/name, skills roles and derived social accessible labels remain intact. The existing shared Header/Footer—including decorative brand images with empty alt beside named text—was not altered by Phase 20.

## Unsaved changes and authorization

Both editors use the same `EditorSnapshot` and `UnsavedChangesGuard`/NavigationLock already used throughout CMS. Initial load is clean; edits become dirty; valid save captures the snapshot; validation/write failures preserve input and dirty state. Browser tests verified Stay preserves input and Leave discards unsaved input; refresh after save loads the saved value. Sample/public navigation remains a full load through the existing external-navigation guard.

The Dashboard `_Imports` Admin restriction covers both routes. Service Admin reads/writes also revalidate authenticated principal, Admin role, live DB role membership and Identity security stamp. HTTP tests verified anonymous → login, non-Admin → access-denied, Admin → direct/refresh success. Service tests also verified role/stamp revocation. No public mutation endpoint or test bypass was added to the web application.

## Focused verification

All mutable verification used disposable test databases, not normal development data.

| Verification | Result |
| --- | --- |
| New detail-template harness | **277 checks passed** |
| Existing public quality/SEO harness | **215 checks passed** |
| Project Detail + Team/Member JavaScript lifecycle | **6 tests passed** |
| Existing Project Detail HTTP suite | 7 project routes, 8 gallery images, metadata/features, breadcrumbs, clean assets and 404 checks passed |
| Existing Team HTTP suite | Canonical cards/Home reuse, profile destinations, social semantics and images passed |
| Existing Member Detail HTTP suite | 6 member routes, portraits, biographies/roles, 24 skills, social links and 404 checks passed |
| Solution Debug | 0 warnings / 0 errors |
| Solution Release | 0 warnings / 0 errors |

The new harness verifies every property on every current detail record (7 Projects, 6 Members), required/max/whitespace/HTML/control validation, trimming, real failed-save trigger, actual editor dirty-state handlers, restart persistence, approved read fallback, no fallback overwrite, singleton counts, migration-model agreement, prior table preservation, authorization, dynamic sample links and empty fallback.

SEO checks verify entity-derived titles/descriptions, canonical and OG URLs/images, and edited BreadcrumbList names without changing breadcrumb routes. Template edits leave the sitemap unchanged. Shared Project/Member updates still reach detail pages. Deleting records removes their sitemap paths and returns real 404s; deleting all records stays empty after a new host starts, while listings/editors remain safe.

Two old HTTP assertions required nonempty alt on **all** images, conflicting with the pre-existing decorative Header/Footer logos. Their alt checks are now scoped to content images after excluding header/footer markup; strict gallery/profile alt checks remain. No public markup was changed to satisfy this outdated assertion.

### Browser results

| Browser | Version | Project Detail | Member Detail |
| --- | --- | --- | --- |
| Chrome | 153.0.8010.54 | 1440 / 1024 / 768 / 390 passed; extra 1200 / 1201 boundary checks | 1440 / 1024 / 768 / 390 passed; extra 1200 / 1201 checks |
| Edge | 154.0.4258.37 | 1440 / 390 passed | 1440 / 390 passed |
| Firefox | 156.0.1 | 1440 / 390 passed | 1440 / 390 passed |

Both Dashboard editors passed Chrome at 1440 / 1024 / 768 / 390, with correct active sidebar and no horizontal overflow. Both interactive editor workflows verified initial clean navigation, edit, invalid save, Stay/Leave, successful save, refresh, two public records inheriting edited labels, matching JSON-LD, sample links and restoring approved values in the isolated DB.

Public tests checked the actual hero/record name, breadcrumbs, metadata/features, image loading, gallery next control, biography/skills/social links, and content reveals after scrolling—not just hidden DOM text. Features was verified visible at 1201/1440 and hidden at 1200/1024/768/390. All **30 browser cases** (2 editor workflows, 8 Dashboard viewport cases, 20 public viewport/browser cases) reported zero console errors; all viewport cases had zero horizontal overflow. Screenshots were visually reviewed for public desktop/mobile pages and both editor layouts. Safari was not available/tested. This is focused verification, not final comprehensive CMS QA.

Local browser fixtures/screenshots/results reside under ignored `artifacts/detail-templates/`. `--detail-browser` exists only in the test executable, creates an isolated DB and random synthetic Admin, uses the real login flow, and exports only disposable test-session cookies for local browser checks. It does not expose a production bypass or contact delivery. Do not commit the fixture file or reuse its cookies outside the local test.

### Reproduction commands

```powershell
dotnet build NexNovaCo.sln --no-restore -c Debug -p:OutputPath=bin/DetailTemplateDebug/net10.0/
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --detail-templates
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --public-quality
node --test scripts/Test-ProjectDetailInterop.mjs scripts/Test-TeamInterop.mjs
# Against a local isolated host containing the approved defaults:
pwsh -File scripts/Test-ProjectDetail.ps1 -BaseUrl http://localhost:5199
pwsh -File scripts/Test-Team.ps1 -BaseUrl http://localhost:5199
pwsh -File scripts/Test-MemberDetail.ps1 -BaseUrl http://localhost:5199
```

Debug output was isolated to avoid overwriting the developer's running Debug executable. Tests never sent email. Production operators should apply the additive migration using the existing controlled deployment/initialization process; a normal database backup remains appropriate before deployment.

## Deferred work and recommendation

Presentation-only controls, generic gallery/ARIA wording, decorative images, arbitrary routes, a second media editor, duplicate entity CRUD, related content, category systems, page builders, SEO CMS, Contact changes and Newsletter functionality were intentionally not added. Normal data and presentation remain in their established owners.

With this scoped completion, the previously implemented public pages plus the Project/Member detail templates all have an explicit CMS ownership path; this does not mean every technical/presentation string should be editable. The project can proceed to **final comprehensive CMS QA and merge review after Contact's real provider configuration and delivery test**. Real delivery remains pending; it was not performed here. No final QA, push or merge was started. Stop for approval.
