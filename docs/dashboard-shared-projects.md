# Shared Projects CMS + Home Featured Projects

## 1. Git and scope

Started from clean `a678db2` on `feature/dashboard-cms`. Work is limited to shared Projects CRUD, detail child content, listing order, and independent Home selection/order. No branch switch, history rewrite, push, or merge. Runtime databases remain ignored. The developer's normal database was not reset or used for destructive QA.

## 2. Dashboard information architecture

- Home → **Projects Section** remains the existing title/introduction settings editor.
- Shared Content → **Projects** manages the canonical collection.
- The **Home Featured** action opens a separate editor within Projects management; no duplicated sidebar tree.

## 3. Architecture inspection and data model

The existing `ProjectSummary` contains Slug, Name, Tagline, Description and ImagePath. `ProjectDetail` adds FullDescription, ordered Gallery (Source/Alt), metadata (Client/Category/Date/Technologies), and ordered Features. The JSON's `subtitle` maps to the card summary; its `summary` maps to the full description. Cover images and gallery images are distinct.

`ProjectEntity` stores those actual fields plus integer Id, DisplayOrder and UpdatedAtUtc. Technologies and editorial Date remain their existing free-text metadata; neither was a typed repeated collection in the approved source. No invented technology table, date conversion, separate detail hero image, or duplicate title fields.

`ProjectGalleryImage` and `ProjectFeature` are proper child entities, each with an explicit DisplayOrder and required ProjectId FK. `HomeFeaturedProject` stores only ProjectId (PK/FK) and its independent DisplayOrder, without duplicated content or an IsFeatured flag on ProjectEntity. Child rows and the Home relation cascade on deletion.

## 4. Migration and initialization

`20260922230323_AddSharedProjectsAndHomeFeatured` adds five tables: Projects, ProjectGalleryImages, ProjectFeatures, HomeFeaturedProjects, and ProjectInitializationState. It adds indexes, positive-order constraints, a case-insensitive unique slug index, and a singleton-marker constraint. It does not alter or drop existing tables in Up.

One transaction initializes the exact seven approved projects, eight gallery images, ordered features and all metadata, plus the original five Home selections in their approved order. The persistent marker commits with the content. Once marked, deleted records and cleared selections stay absent across restarts. Existing unmarked content is adopted as-is, not overwritten. A forced-marker-failure test verifies rollback and safe retry.

## 5. Content service

`IProjectContentService / ProjectContentService` provide full catalog, slug detail, joined Home selection, Admin CRUD, shared reorder and featured-save operations using short-lived DbContexts. Public reads are no-tracking and ordered explicitly. The existing `IProjectCatalog` interface now resolves to this scoped SQLite service; the JSON-backed `ProjectCatalog` class is used only for initialization and read-failure fallback.

Shared reorder validates the exact current ID set. Featured save validates a unique subset of current IDs. Both apply atomically. Create appends without auto-featuring. Update preserves entity ID/shared order and replaces the edited ordered child content in the same SaveChanges transaction. Sequential saves follow the existing last-save-wins CMS pattern; UpdatedAtUtc is an EF concurrency token, not an editor version-history system.

## 6. Projects list

Responsive MudTable columns: order, cover preview, name, slug, Home Featured status/order, and actions. Add, Edit, confirmed Delete, Refresh, and immediate Move Up/Down follow existing shared-collection patterns. Edge moves are disabled and controls have item-specific accessible labels.

## 7. Add/edit

Routes:

- `/dashboard/content/shared-projects`
- `/dashboard/content/shared-projects/new`
- `/dashboard/content/shared-projects/{id:int}`
- `/dashboard/content/shared-projects/home-featured`

ProjectEditor groups Basic Information, Project Details, Media/Gallery, and Features. Fields have required/length validation where applicable; nested gallery/feature validation runs at the service boundary and on form submission. Gallery and Features support Add, Remove and Up/Down; there is no drag/drop.

Cover and gallery images use a MudSelect constrained to the nine existing approved bundled JPEG assets. Unsafe, remote, arbitrary filesystem, traversal and missing paths are rejected. Bounded previews show a safe placeholder for missing/invalid images. Text uses normal Razor encoding. There are no uploads or filesystem browsing controls.

## 8. Slug

Slugs are trimmed and lowercased consistently, required, at most 80 characters, and restricted to letters/numbers separated by single hyphens. Internal spaces, traversal, encoded separators and unsafe punctuation are rejected. Both a service check and the database unique index enforce uniqueness, including case variations. The existing public route has no conflicting fixed child routes requiring a reserved-word list.

Changing a slug changes its detail URL. The editor warns that the old URL will not redirect; unknown and malformed slugs retain the existing safe not-found behavior. Redirect history is not implemented.

## 9. Delete

MudDialog identifies the project, warns about the detail URL and child content, and additionally warns when it is selected on Home. Cancel does not delete. Confirmation deletes the canonical row and cascades gallery/features/Home membership, then normalizes remaining shared and featured positions in a transaction. Last-item messaging explains that restart will not restore deleted content.

## 10. Shared reorder

Move Up/Down saves listing order immediately. It does not change Home order. Deletion closes order gaps without changing the relative order of remaining records.

## 11. Home Featured

Checkbox selection and the separate ordered table are staged until **Save featured selection**. Checking appends; unchecking removes; Up/Down changes only Home order. Zero is valid. There is no artificial five-project maximum: the approved Home carousel supports additional items, unlike the fixed Services layout. Browser QA included six featured projects.

## 12. Unsaved changes

ProjectEditor snapshots the entire typed model, including nested row content and sequence. FeaturedEditor snapshots its selected ordered IDs. Both reuse UnsavedChangesGuard/NavigationLock. Load/save establish clean state; reverted values become clean; validation/write failures retain input and dirty state. New-project save redirects to the edit URL after the clean state has rendered.

Automated tests cover top-level fields, child addition/text/order/removal, revert and failed saves. Browser QA verified validation failure, retained inputs, and Stay protection on the Project and Featured editors. Native tab-close/reload beforeunload dialogs were not manually exercised.

## 13. Public Projects listing

ProjectsContentService reads the current SQLite catalog on every GetAsync instead of caching project records in its editorial snapshot. ProjectCard, listing markup, decorative pagination, testimonials, public CSS and public JavaScript remain unchanged.

## 14. Public Project Detail

The existing detail route still uses IProjectCatalog, now backed by SQLite. Hero/title/breadcrumb, summary/full description, metadata, ordered gallery, features, keyboard/gallery controls and not-found rendering are preserved. No legacy JSON runtime binding remains in normal public reads. Features remain intentionally hidden on tablet/mobile.

## 15. Public Home

Home obtains cards through the joined HomeFeaturedProject relation on every read. HomeProjectsSectionSettings remains the independent introduction source. Shared edits reach listing/detail/Home without copies. The only rendering guard added omits the Home Projects section when its selection is empty, avoiding an empty carousel root.

## 16. Empty and fallback behavior

Valid empty catalog/selection returns empty, never defaults. Listing remains safe, deleted/unknown detail URLs return not found, Home omits the empty section, and an empty gallery already has a rendering guard. No fake projects are introduced.

On database or content-validation read failure, the service logs the problem and returns the approved JSON-derived full catalog, matching detail, or original Home subset. Fallback performs no writes and is distinct from initialization. Admin failures are surfaced instead of reporting false success.

## 17. Authorization

All management routes inherit Admin authorization and DashboardLayout. Anonymous users are challenged, non-Admins denied, and Admin bookmarks/refresh work. Every management-service method checks authentication, current database role membership and security stamp. No public mutation endpoints were added.

## 18. Verification

- **1,906 auth/CMS checks passed**, including all prior suites.
- Exact default project/detail/child content and order, marker idempotency, and EF model/migration agreement.
- Real application-host restarts: edited metadata/child content, both independent orders, membership, IDs/timestamps, delete-one, delete-all, repeated empty restart, clear selection, and add-after-empty.
- CRUD, cascade cleanup, order normalization, missing IDs, invalid/stale/duplicate order sets, and atomic rejection of invalid selections.
- Safe images, nested validation, required/length validation, HTML encoding, slug normalization and service/database uniqueness.
- Full/detail/Home fallback and valid-empty semantics; fresh same-scope reads prevent stale public snapshots.
- Anonymous/non-Admin/Admin routes and direct service boundaries; revoked role/stamp handling.
- Additive upgrade preserves Identity credentials/roles, all eight Home settings slices, Testimonials, Partners and Services/Featured content. Existing Admin credentials survive restart.
- **12 Home/Projects/Project Detail/Dashboard JavaScript lifecycle tests passed.**
- **Debug and Release builds passed with zero warnings/errors.** Debug used `-p:OutputPath=bin/ProjectsDebug/` to avoid IDE output locks.
- Test-Home.ps1, Test-Projects.ps1 and Test-ProjectDetail.ps1 passed against restored approved data. These also verify all seven approved detail routes/content/images, unknown-slug handling and the 768px testimonial guard.

Browser end-to-end used a separate ignored database at `artifacts/projects-cms-qa-1946b527/identity.db` with ephemeral test credentials. Added a project with two gallery images and two features, reordered both child lists, saved, selected/reordered it on Home, independently reordered the shared list, edited its name/full description and removed a feature, then verified all public consumers. A real server restart retained everything. Delete confirmation Cancel preserved the project; confirmation removed it everywhere. Clearing Featured remained empty after another restart. Approved catalog and Home selection were restored in this isolated database for final smoke checks.

## 19. Public and responsive verification

- Dashboard list, Project form and Featured editor inspected at **1440, 1024, 768 and 390**, with no horizontal overflow and functional previews/controls.
- Home carousel and Projects listing checked at desktop/mobile; settled page widths had no horizontal overflow and card design was retained.
- Project Detail checked at desktop, 768 and 390. Gallery Next/Previous/ArrowLeft worked, including after listing/detail navigation. Features were visible on desktop and `display:none` at 768/390.
- At exactly **768px**, Projects testimonials retained their dark background and readable text; carousel controls worked.
- No application console warnings/errors during stable operation or the final post-restart checks. Deliberate server shutdowns produced expected transient SignalR connection errors in already-open tabs; reloading after restart restored working interactive controls.
- Temporary QA tabs, viewport override, credentials pipe and server were cleaned up. No full-site visual regression was run.

## 20. Deferred work

No Team CRUD, upload/media library, categories management, SEO redirect history, drafts/versioning, audit logging, bulk actions, localization, analytics, or generic page builder.

## 21. Recommendation

Recommend **Shared Team Members + Home Featured Team** next, preserving existing member-detail identities and separating shared content from Home selection/order. It is not implemented here. Stop for approval.

Phase 13 route note: `/dashboard/content/projects` now redirects to the page-specific Hero editor. Legacy `/projects/new`, `/projects/{id:int}` and `/projects/home-featured` dashboard routes redirect to the corresponding `/dashboard/content/shared-projects/...` routes. Shared CRUD and public detail routes are unchanged.
