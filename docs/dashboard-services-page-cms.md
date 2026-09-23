# Dashboard Phase 12 — Complete Services Page CMS

## Git

Branch: `feature/dashboard-cms`. Started clean at `1ae966b`.
Two logical change groups: Services data/public integration, then Dashboard/editors/focused verification.
No branch creation, push, merge, history rewrite, or normal developer database reset.

## Services Dashboard IA

Content → Services has Hero, Services Section, Benefits, Process, Pricing and FAQ.
The group uses the existing MudNavGroup/MudNavLink shell, stays expanded on its routes, highlights the current section (including nested editors), and closes the mobile drawer after navigation.
All editors inherit the Dashboard Admin-only route policy.

Base entry `/dashboard/content/services` redirects to `/hero`.
The six child routes are `/hero`, `/overview`, `/benefits`, `/process`, `/pricing`, `/faq`.
The four collections also have `/new` and `/{Id:int}` children.

## Section inventory

| Actual section | Page-specific fields | Repeated data |
| --- | --- | --- |
| Hero | Desktop/mobile title, description, CTA label/internal route, photograph | None |
| Services cards | No real page-specific intro exists | Existing shared Service catalog |
| Benefits | Heading, description, decorative photograph | Title and description |
| Process | Heading and mobile introduction | Ordered title + approved SVG icon |
| Pricing | Heading and introduction | Name, subtitle, display price, audience, CTA label, treatment and ordered features |
| FAQ | Heading and introduction | Plain-text question/answer |

No invented presentation settings, page-builder schema, payment fields or duplicate Service catalog.

## Data model / migrations

Additive migration: `20260923152857_AddCompleteServicesPageCms`.
Fourteen new tables: five singleton settings, four item collections, four explicit initialization markers, and ordered pricing-plan features.
Existing tables are not altered. Positive-order and singleton checks, timestamp concurrency tokens, cascading plan-feature ownership, and a filtered unique Premium-treatment index are included.
A cohesive `IServicesPageCmsService` uses short-lived contexts and existing live Admin/security-stamp checks.
The public facade combines page content with the canonical `IServiceContentService`.

## Initialization

Missing singleton rows receive exact approved typed content only once; existing Admin edits remain.
Each collection is seeded transactionally only before its persistent marker exists. Existing unmarked rows are adopted.
Marked empty collections remain empty after restarts and accept later Admin-created items without resurrecting defaults.
Approved defaults were extracted verbatim from the existing Services content service.

## Hero

Desktop and mobile titles remain distinct. The original CTA targets `services#Service`.
Internal-route validation rejects external URLs, Dashboard paths, traversal, arbitrary query/fragment values and script schemes.
The original image is `image/about/2025timeline.jpg`.
Base-aware absolute CSS image URLs avoid resolving uploaded/bundled paths relative to the stylesheet directory.

## Services section / Shared Services reuse

Overview is an informational page linking to the existing shared Service editor; there is no meaningless overview table.
The requested page-CMS root previously belonged to shared CRUD, so that existing route family now lives at `/dashboard/content/shared-services`.
Legacy shared `/services/new`, `/services/{id}` and `/services/home-featured` Dashboard children redirect to their new equivalents.
The old shared root intentionally becomes the page-CMS Hero entry.
Shared entity schema, CRUD/business logic, ServiceCard rendering and Home featured selection are unchanged.

## Benefits

Complete add/edit/delete/reorder, explicit persistent order, heading/description editor and section-level image upload.
The three approved benefits retain their original content/order. Empty collections omit cards and decorative image containers.
Additional rows reserve the existing image column at desktop width. There are no invented per-card icon uploads.

## Process

Complete ordered CRUD with automatic sequence numbering and an approved Search/Flask/Wrench/Tools/Rocket selector.
The public SVG icons and five-slot desktop hexagon diagram remain. More than five steps reuse consecutive five-slot groups with global numbering; mobile remains the existing wrapping hexagon presentation.
A six-step collection was checked at 1440 and 390 pixels. Zero steps emit no ring/diagram. No MudStepper or new breakpoint system.

## Pricing

Complete plan CRUD/reorder and ordered relational feature rows. Features can be added, edited, removed and moved before saving.
The three approved Basic/Standard/Premium treatments remain; only one Premium is allowed, enforced in both service validation and a database unique index.
Currency and amount remain one real display-price field (for example CAD 1,800); validation handles grouped amounts and optional decimals.
Public CTA text remains editable but the approved demo button remains disabled. No checkout, subscription, fabricated CTA route or payment logic.
Empty collections show no fallback/fake plans.

## FAQ

Complete CRUD/reorder with safe plain-text answers.
The existing Blazor-owned independent accordion is retained: the first item starts open and opening another does not close it.
Existing ARIA relationships and public styling remain. Empty FAQ collections omit the accordion.
Repeated user-authored titles/questions are no longer used as Razor keys.

## Media reuse

Existing CmsImageField, MediaPolicy and local raster validation/storage are reused.
Hero and Benefits use `uploads/services/`, with existing JPG/PNG/WebP validation and a 5 MiB limit.
Automated tests cover image-only dirty state, failed database save, retry without duplicate upload, safe media response and public path output for both image kinds.
Browser QA uploaded the approved local Hero photograph, saved it under `uploads/services/`, verified the public CSS URL, then restored the bundled image.
No new upload infrastructure, public mutation endpoint, SVG upload or arbitrary path input.

## Unsaved changes / validation

Existing EditorSnapshot and UnsavedChangesGuard are reused for all singleton and item editors, including the entire pricing feature list.
Verified clean load, dirty edits, invalid save remaining dirty, forced SQLite save failure retaining edits, successful save becoming clean, and image selection/retry behavior.
Browser Stay retained edits; Leave discarded them and completed navigation. Collection saves redirect new items to their edit URL.
Field-specific required/length, enum, route, media-path and pricing validation is enforced server-side.
Failed Admin reads display an error rather than approved defaults masquerading as stored content.

## Authorization

All new routes inherit Admin authorization. Each Admin data read/write rechecks authentication, current database role membership and security stamp.
Focused checks exercise anonymous and non-Admin route challenges, direct/refresh Admin access, all 34 protected service operations, revoked roles and stale security stamps.
Public reads stay anonymous. There are no new mutation HTTP endpoints.

## Verification

Final Release test command:

```powershell
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --services-page
```

392 focused checks passed. The full historical site suite was deliberately not run.

Coverage:
- Fresh approved seed, marker existence, no pending model changes, repeat host initialization without duplication.
- Upgrade from the previous migration preserves every existing Identity, Home, About and shared table row, timestamp and account; migration operations only create tables/indexes.
- Every singleton: edit, forced save failure, validation failure, successful public update, host restart, restore.
- All four collections: create/edit/reorder, invalid ordering, delete-one, delete-all, independent host restart, add-after-empty and explicit CRUD restoration.
- Pricing feature persistence and moving the unique highlighted treatment.
- Public zero-item rendering and slice-specific read-failure fallback without writes.
- Both media kinds and actual editor handlers.
- Shared Service edit reflected on both Home and Services and restored.
- All Services routes: anonymous/non-Admin/Admin and direct refresh.

Debug and Release builds both passed with 0 warnings and 0 errors. Debug used an isolated output directory to avoid interfering with a developer's running Debug application:

```powershell
dotnet build NexNovaCo.sln --no-restore -p:OutputPath=bin/ServicesDebug/net10.0/
dotnet build NexNovaCo.sln -c Release --no-restore
```

All test databases were disposable/isolated. Browser QA used an ignored `artifacts/services-qa-...` database on localhost:5190, not the developer database.
The QA server was stopped afterward; browser-created test content was deleted/restored using normal CRUD. Test artifacts remain ignored.

## Public Services regression

| Viewport | Public Services | All six Dashboard subpages |
| --- | --- | --- |
| 1440 | No horizontal overflow; desktop hero, benefits, diagram, pricing and FAQ inspected | Correct active child/expanded group; no overflow |
| 1024 | No horizontal overflow; hero and five-step diagram inspected | Correct active child/expanded group; no overflow |
| 768 | No horizontal overflow; existing mobile process mode and pricing transition inspected | Correct active child/expanded group; no overflow |
| 390 | No horizontal overflow; stacked cards/steps, FAQ interaction and hero inspected | Responsive forms/tables; drawer closes after navigation; no overflow |

Browser flows covered Benefits create/edit/reorder/delete, sixth Process step add/delete, Pricing feature reorder/save/refresh/restore, rejected duplicate Premium, FAQ create/reorder/delete and singleton save-to-public/restore, dirty navigation, and image upload.
Repeated navigation retained functioning FAQ interaction and Interactive Server form actions. Browser warning/error logs were empty in the fresh post-build QA tab.
Home and the Shared Services dashboard were smoke-checked; the shared catalog still displayed six approved cards.
Public content was not converted to MudBlazor. Original service.css, shared InnerPageHero, ServiceCard, ProcessStep, PricingCard and FaqItem were not redesigned.
This was focused regression, not a full historical pixel-perfect comparison.

## Deferred work

No Services CMS section remains deferred. Projects/Team/Contact page-specific settings, Header/Footer/navigation CMS, payments, SEO editing, rich text, drafts, audit logs, localization and page builders remain outside this phase.

## Recommendation for next whole-page CMS target

Projects page-specific CMS is the next natural target, reusing the canonical shared Project catalog and existing media foundation.
Do not start it without approval.
