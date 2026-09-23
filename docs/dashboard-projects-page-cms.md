# Dashboard Phase 13 — Complete Projects Page CMS

## Git

Implemented on `feature/dashboard-cms`, starting from clean HEAD `4a9866b`. No branch switch, history rewrite, push or merge. Changes are split into page data/public integration and Dashboard/focused verification commits. Normal developer database was not reset or used for QA.

## Projects Dashboard IA

Content → Projects is a MudNavGroup with Hero, Projects Section and Testimonials Section. Root `/dashboard/content/projects` redirects to `/hero`. Active links, section titles, responsive drawer, mobile close-on-navigation and existing keyboard/focus handling are retained.

The requested root was previously occupied by shared CRUD. The canonical shared manager now lives at `/dashboard/content/shared-projects`; its new, integer edit and home-featured routes move with it. Old `/dashboard/content/projects/new`, `/{id:int}` and `/home-featured` redirect to their counterparts. The old root now intentionally opens page settings.

## Actual section inventory

| Public section | Actual content | Treatment |
| --- | --- | --- |
| InnerPageHero | Desktop title, mobile title, description, CTA label/route, photograph | Editable singleton |
| Project portfolio | Shared project cards, order and detail links | Informational Dashboard page linking shared manager |
| Pagination | Existing decorative 1–6 presentation | Unchanged, no database fields |
| Testimonials | Shared quotes plus page-specific brand title/description | Editable brand singleton; link to shared quotes |
| Shapes, overlay, typography, colors and breakpoints | Theme presentation | Not exposed |

The inherited approved Hero photograph is `image/home/inner-banner.jpg`, verified in `styles.css`. The testimonial decorative image is not a new editable field.

## Shared vs page-specific content

Shared Projects and Testimonials remain canonical. No duplicate entities, catalog CRUD, ordering logic, Home Featured selections or Project Detail storage were introduced. Projects page content composes freshly read settings with the existing shared catalog services.

## Data models/migrations

Additive migration `20260923161152_AddProjectsPageCms` creates only `ProjectsHeroSettings` and `ProjectsTestimonialsSettings`. Both have fixed ID 1, singleton check constraints, required bounded fields and the existing UpdatedAtUtc concurrency-token pattern.

`IProjectsPageCmsService` uses short-lived DbContexts. No generic repository or shared entity mutation methods were added.

## Initialization

Only absent singleton rows receive the exact approved content extracted from the existing public service. Subsequent startup preserves edits. Shared collection initialization is unchanged: initialized-but-empty catalogs remain empty.

Unavailable/invalid settings reads log an error and render approved slice defaults without initializing or saving anything. Dashboard load failures show an error, not fallback values presented as saved content.

## Hero

Title, mobile title, description, CTA label, safe internal CTA route and existing photograph are editable. The approved CTA remains `projects#Project`. Server-side validation rejects external/protocol-relative URLs, unsafe schemes, admin routes, query strings and invalid anchors. Public text remains Razor-encoded.

A single Projects-scoped background-image rule consumes a base-aware absolute URL. Existing crop, overlay, shape and responsive rules are unchanged.

## Projects Section

`/dashboard/content/projects/overview` explains that cards and order belong to Shared Content → Projects and links there. No meaningless listing settings table, intro text or pagination behavior was invented.

## Testimonials Section

`/dashboard/content/projects/testimonials` edits only the existing brand title and description. These are independent of Home's brand settings. Quotes and their order remain in Shared Content → Testimonials.

The existing Count > 0 guard continues to omit the complete testimonial section/carousel when the shared catalog is intentionally empty.

## Shared-content reuse/links

Overview → canonical Shared Projects manager. Testimonials editor → canonical Shared Testimonials manager. Shared Projects retains its Home Featured editor. Existing detail `/projects/{slug}` still reads the shared catalog.

Only shared Dashboard route declarations/navigation links changed; shared CRUD services and public detail components did not.

## Media reuse

Uses existing CmsImageField, LocalMediaStorageService, validation and safe public media handler. Adds ProjectsHero media kind with 5 MiB raster limit and `uploads/projects/<generated-id>.<jpg|png|webp>` scope.

Image-only dirty tracking, failed database-save retention, upload reuse on retry, safe raster serving and bundled restoration were verified. No new media infrastructure.

## Unsaved changes/validation

Both editors use existing EditorSnapshot and UnsavedChangesGuard. Browser verification covered Stay and navigation prompting; automated handler tests cover clean/dirty transitions, required/length validation, database failure retention and successful-save clean state. Pending image selection participates in dirty state.

## Authorization

All page and legacy routes inherit the Dashboard Admin authorization policy. Anonymous requests challenge to login; non-Admins are denied; Admin direct and refresh requests succeed. Every settings read-for-edit/write also checks a live Admin role and security stamp, including revoked sessions. Public reads stay anonymous; no public mutation endpoint was added.

## Verification

Focused command:

```powershell
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --projects-page
```

127 checks passed using isolated AuthFactory databases:

- Exact fresh defaults and idempotent separate-host restart.
- Migration adds only the two intended tables; every prior Identity/Home/About/Services/shared table, row and timestamp is preserved.
- Every real singleton: edit fields, invoke actual editor save, verify public HTML, restart, verify persistence, restore.
- Invalid and forced-failed saves retain changes; successful saves clear dirty state.
- Media upload, failed-save retry, content type/nosniff and public image path.
- Settings read failures use approved defaults, log errors and do not persist fallback.
- Shared Projects/Testimonial catalogs stay untouched by settings workflows.
- Intentional empty Projects/Testimonial collections survive startup; no fake cards, no testimonial carousel on Home/Projects, decorative pagination retained.
- Admin/non-Admin/anonymous routes and service authorization; role/stamp revocation.
- Legacy shared redirects, shared-manager links, Home brand isolation and Project Detail smoke.

Debug and Release solution builds passed with zero warnings and zero errors. Debug uses `-p:OutputPath=bin/ProjectsDebug/net10.0/` to avoid disrupting an existing developer process. Historical full-site suite was not rerun; prior migration tests were made aware of the additive migration.

Browser QA used an isolated database/media folder under ignored `artifacts/projects-qa-50dac63fcff44d96ade415168fe75311` and an ephemeral test Admin. Browser edits and the uploaded photograph were restored to approved values. Developer database was untouched.

## Public Projects regression

| Width | Dashboard | Public Projects |
| --- | --- | --- |
| 1440 | Hero/brand editor, active sidebar, save/guard | Approved Hero, 3-column cards, decorative pagination, readable testimonial panel |
| 1024 | Stable desktop drawer/form | Hero, 3-column cards, pagination, readable testimonial panel |
| 768 | Compact drawer/form | Mobile Hero, 2-column cards, dark testimonial background preserved |
| 390 | Stacked forms/actions; drawer closes on navigation | Mobile Hero, single-column cards, readable testimonials and controls |

No horizontal overflow at the four requested widths. At exactly 768px, the testimonial computed background remains `image/home/project-bg.png` behind white quote text. The existing contrast-fix media query is unchanged.

Carousel next/previous, dot navigation and pause/resume UI were exercised; changing slides was verified. Repeated Dashboard/public/detail/back navigation leaves one carousel/control set. Interactive Server save actions remain functional. Browser warning/error logs were empty, and QA server logs had no application errors.

Smoke checks: `/projects/nexconnect`, Shared Projects, Shared Testimonials and Home Featured Projects. No public MudBlazor redesign, new pagination, card markup changes or testimonial CSS changes.

## Deferred work

All out-of-scope items remain deferred: duplicate entity managers, real pagination, categories, Team/Contact page CMS, Header/Footer/Navigation CMS, SEO editor, drafts/versioning, audit logs, localization and page builder.

## Recommendation for next whole-page CMS target

Team page-specific CMS is the natural next target: inventory its real presentation content first, reuse Shared Team Members, and keep Member Detail canonical. This is a recommendation only; no next-phase implementation has started.
