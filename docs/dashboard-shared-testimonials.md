# Shared Testimonials CMS — CRUD and ordering

Verified 2026-09-22. First shared-content collection manager only; no other collection editor was added.

## 1. Git

Started clean on `feature/dashboard-cms` at `3270c9d`. Stayed on the same branch without main checkout, history rewrite, push, or merge. This report accompanies the focused shared Testimonials commit.

## 2. Dashboard information architecture

The sidebar retains all eight Home subpages. The Home label is now **Testimonials Section**, with the same `/dashboard/content/home/testimonials` route and unchanged two-field brand-panel editor. A separate **Shared Content → Testimonials** link manages the actual quotes used by Home and Projects. The shared link stays active for list/new/edit routes; all use DashboardLayout and Admin authorization.

## 3. Data model and inspection findings

Inspected the approved static copy, existing model/catalog, both page providers, shared section/card/carousel components, and responsive behavior before implementation. There are exactly two testimonials in approved order: Olivia Carter, COO at Alpha Co; Daniel Kim, Marketing Director at Tech Co. Each has two quote paragraphs. Existing public markup renders one combined attribution string, not separate name/role/company fields. There are no avatars, images, links, or visibility fields.

`TestimonialEntity` maps to `Testimonials` in the existing SQLite database:

| Field | Purpose |
| --- | --- |
| Id | SQLite-generated stable integer identity |
| DisplayOrder | Positive, one-based explicit order |
| Attribution | Existing combined author name / role / company line; required, max 180 |
| Quote | Plain text; blank lines separate paragraphs; required, max 1600 |
| UpdatedAtUtc | UTC timestamp and EF concurrency token |

The typed edit model has only Attribution and Quote. It maps back to the unchanged public `Testimonial(Attribution, Paragraphs)` contract, preserving paragraph order. No speculative split author schema or image field was added. Quotes and attribution are Razor-encoded, never interpreted as HTML.

Additive migration `20260922203426_AddSharedTestimonials` creates only this table, its primary key, positive-order constraint, and order index. Runtime DB files remain ignored. Existing Identity and all eight Home settings schemas remain unchanged.

## 4. Initialization

The controlled startup initializer runs after migrations. Inside a transaction it seeds the exact approved count/copy/order only if there are no testimonials. A nonempty collection is never modified, topped up, or overwritten. Repeated startup preserves Admin additions, edits, deletions, and ordering.

Important empty-collection behavior, matching the request: deleting every row makes public reads return an empty collection; no hidden read-time seeding occurs. The next controlled startup reseeds the approved defaults because the collection is empty. The last-item delete dialog explains this explicitly. Both public pages omit an empty testimonial section rather than initializing Owl with zero items.

## 5. Content service and canonical source

`ITestimonialContentService` / `TestimonialContentService` uses short-lived factory DbContexts. Public reads are no-tracking and ordered by DisplayOrder then Id. Home and Projects query the same table through this service on every call, outside their static snapshots. There is no per-page collection or static production copy. ProjectsContentService is now scoped to match its scoped dependency.

`TestimonialCatalog.All` is referenced only by startup initialization and logged public read-failure fallback. Approved brand-panel copy was separated into `TestimonialPresentationDefaults`, so the old catalog itself is exclusively seed/fallback data. Missing/unavailable/invalid database reads never write fallback values. A valid empty collection is not a failure. Admin reads fail visibly instead of silently showing fallback as persisted content.

Create validates and appends; update changes only content/timestamp, not Id/order; delete removes by Id and normalizes remaining order; reorder validates the exact full set of current IDs and rejects duplicates, omitted/unknown IDs, or a stale membership set before saving.

Create/delete/reorder use transactions, keeping membership/order changes atomic. DisplayOrder is indexed but not unique, avoiding transient uniqueness failures during swaps; deterministic Id tie-breaking and transactional normalization provide stable reads. EF timestamp concurrency detects conflicting writes after a row has been loaded. Missing IDs and concurrency/database failures give safe UI feedback. This is deliberately not a revision-based editor: two separately opened stale forms may still use last-save-wins if the earlier save completed before the later service read.

## 6. List page

`/dashboard/content/testimonials` uses MudTable with Order, Author / role / company, Quote preview, and Actions. Add, Edit, Delete, Move Up, Move Down, and Refresh list are available; first/last move buttons are disabled appropriately. Labels identify the target author for assistive technology. A responsive stacked table uses MudBlazor's existing breakpoint behavior. Small isolated CSS handles long text and readable mobile labels; no public stylesheet changed. No search, filters, bulk actions, or drag-and-drop were added.

## 7. Add/Edit

Routes: `/dashboard/content/testimonials/new` and `/dashboard/content/testimonials/{id:int}`. One reusable routed form provides required author attribution and multiline Quote. Blank lines create paragraphs. Whitespace-only and overlength submissions are rejected by the model/service. The existing fixed-height public design remains unchanged; helper copy explicitly asks for concise content and public preview remains advisable.

Create moves to the new stable edit URL after the clean dirty-state snapshot is rendered, preventing a refresh of `/new` from implying a second create. Edit preserves identity/order. Successful saves display feedback; failures retain entered content and avoid technical exception disclosure. Missing IDs render a safe explanation and return-to-list action. Both routes support direct load and refresh.

## 8. Delete

Delete opens a MudBlazor dialog naming the attribution and explaining the effect on both pages. Cancel has initial focus; confirmation is visually destructive. No mutation occurs until confirmed. Browser QA canceled once (record retained), then confirmed deletion of the newly created isolated QA record. Missing/already-deleted IDs are handled safely; there is no restore/versioning feature.

## 9. Reorder

Move Up/Down persists immediately, with no staged ordering state and no extra Save. Buttons are keyboard accessible. The service receives explicit ordered IDs, validates collection membership, then normalizes to 1..N atomically. Browser QA moved the third test record into first position, verified the same order in both public pages, and confirmed it survived process restart. Deleting that record restored contiguous order for the original two.

## 10. Unsaved changes

Add/Edit reuses `EditorSnapshot` and `UnsavedChangesGuard` unchanged. Initial/reverted/saved forms are clean; editing, invalid submission, and failed saves remain dirty. Cancel and internal navigation use the existing Stay/Leave dialog. Actual browser checks confirmed Add → Stay retains content and Edit → Leave discards unsaved input. Automated tests exercise both forms' lifecycle and database/validation/missing-item save failures.

The list has no unsaved state because every reorder persists immediately. Existing browser-native unload protection is retained for refresh/close/public actions. Its native prompt appearance and acceptance/cancellation remain a manual standard-browser check, as documented in previous phases; no new unload framework was introduced.

## 11. Public Home integration

Home's brand-panel settings remain in `HomeTestimonialsSectionSettings`; actual quotes come from the shared table. TestimonialCard, TestimonialsSection, ThemeCarousel, paragraph rendering, controls, and public CSS/JS remain unchanged. Only the route's zero-item guard is new; populated markup is unchanged. Shared edits do not modify any of the eight Home settings or unrelated catalog data.

## 12. Public Projects integration

Projects reads exactly the same ordered SQLite collection. Its hero, project listing, brand-panel presentation, and styles remain unchanged. At exactly 768px, browser inspection confirmed white quote text on the existing dark `image/home/project-bg.png` background, with no horizontal overflow. The scoped contrast guard in `projects-blazor.css` was not modified.

## 13. Authorization

All three management routes challenge anonymous users, deny non-Admins, and allow Admins. Each service Admin read/write also checks the authenticated role claim, current database role membership, and security stamp. Tests cover direct unauthorized calls for every CRUD/reorder operation and role/stamp revocation. Public collection reads remain anonymous. No public mutation endpoint exists.

## 14. Verification

- Debug and Release: zero warnings/errors.
- Auth/CMS integration harness: **863 checks passed**, including the existing eight Home slices and shared CRUD/order tests.
- Home/Projects/Dashboard interop: **8 tests passed**.
- Existing Home and Projects HTTP suites: passed with approved restored records.
- Database: generated migration/model agreement, exact initial count/order/paragraphs, repeated initialization, create/edit/reorder/delete, restart persistence, deterministic normalization, invalid/stale ordering rejection, missing IDs, authorization, plain-text encoding, read fallback, empty-state behavior, and preservation of unrelated Home content.
- Browser: login, direct routes, list/edit refresh, required validation, Add/Edit save, shared public content, keyboard reorder, Stay/Leave, delete Cancel/Confirm, actual process restart, and deleted-record absence after another restart.
- Final list screenshots inspected at **1440, 1024, 768, 390px**; no horizontal overflow. Mobile form and delete dialog were also inspected.

QA used only an isolated database under ignored `artifacts/shared-testimonials-qa-1d262ac5`; the regular development database was not edited/reset. Test credentials were ephemeral and not printed or committed. The created QA record was removed and both original records/copy/order retained. Temporary server/tab and viewport override were cleaned up after verification.

## 15. Focused public regression

Home and Projects both showed the created record, edited paragraphs/attribution, and reordered collection. The same data/order survived server restart. After deletion, both returned to the exact original two records. Controls changed the active author; pause/resume and Home autoplay progression were observed. Repeated Home → Projects → Home → Projects navigation retained one Owl stage and two original items after cleanup, without duplicate initialization. Projects autoplay reported playing after hover/focus left the carousel. No browser warnings/errors were recorded. Public desktop/mobile smoke measurements found no horizontal overflow. No massive visual regression was run.

## 16. Deferred work

No Partner/Service/Project/Team collection CMS, media upload/library, version history, drafts/publish, audit log, bulk actions, search engine, public submission, extra roles, localization, or generic collection framework. Shared testimonial imagery and visibility controls were not invented. No push/merge/deployment was performed.

## 17. Recommendation — approval required

Scope shared Partners CRUD next, preserving one Home/About collection and separate page-intro settings. Confirm existing logo-path/background/destination fields and validation before implementation; image upload can remain a later milestone. This recommendation is not implemented. Stop for approval.
