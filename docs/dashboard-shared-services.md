# Dashboard Phase 7 — Shared Services + Home Featured

## 1. Git and scope

Started from clean `ac76434` on `feature/dashboard-cms`. No branch switch, history rewrite, push or merge. Only shared Services CRUD/order and Home Featured selection/order were added. Runtime databases are ignored; destructive QA used isolated databases, not the developer's normal DB.

## 2. Dashboard information architecture

- Home → **Services Section**: existing Home title/description editor.
- Shared Content → **Services**: canonical catalog management.
- **Home Featured** button inside Services opens `/dashboard/content/services/home-featured`; no duplicate sidebar navigation.

## 3. Architecture inspection and data model

The actual public record is `ServiceSummary(Id, Name, Tagline, Description, IconPath)`. ServiceCard renders name, tagline, description and icon; there is no link, long description or Service Detail route.

The original six internal identities are software, web-mobile, ai, consulting, design and strategy. Home originally uses the first five in that order. The Services grid uses the identity for its Razor key and the approved Strategy icon treatment. These internal identities are not slugs/routes.

`ServiceEntity` stores generated integer Id, DisplayOrder, immutable unique ContentKey, Name (60), Tagline (80), Description (200), IconPath (200) and UpdatedAtUtc (concurrency token). Approved ContentKeys are retained; new records receive generated keys. No editable slug or detail-route behavior was invented.

`HomeFeaturedService` stores only ServiceId (PK/FK) and DisplayOrder. There are no duplicate Home content fields and no page-specific featured flag on ServiceEntity. A Service can exist without a relation. Cascade deletion enforces referential integrity.

## 4. Migration and one-time initialization

`20260922222025_AddSharedServicesAndHomeFeatured` adds Services, HomeFeaturedServices and ServiceInitializationState, indexes and constraints. Existing tables are untouched. Migration source/designer/snapshot are included.

One transaction initializes the exact six approved Services, their canonical icon assignments/order, the five approved Home selections/order and a durable singleton marker. Catalog and initial relation are one initialization unit.

The marker distinguishes never initialized from intentionally empty. Restarts do not recreate deleted Services, restore cleared selections or overwrite edits/order. An unmarked existing catalog is adopted without modifying its content/selection. A forced marker failure test verifies rollback of both entity and relation inserts.

## 5. Content service

`IServiceContentService / ServiceContentService` use short-lived factory contexts. Public reads are no-tracking: one reads the full ordered catalog, the other joins HomeFeaturedServices and orders by the relation. Admin list/edit/create/update/delete, full-catalog reorder and featured-selection save are protected independently of UI authorization.

Shared reorder validates the complete current ID set and updates atomically. Featured save accepts a unique ordered subset of existing IDs, applying additions/removals/order in a transaction. Stale deleted IDs and malformed selections fail without partial writes. Service edits never bind IDs, ContentKey or shared order.

## 6. Services list

Responsive MudTable columns: order, icon preview, name, Home Featured status/order and actions. Add, Edit, confirmed Delete, Refresh and immediate Move Up/Down follow the established collection pattern. Icon buttons have specific accessible names; edge movement is disabled. No search, categories, bulk operations or drag-and-drop.

## 7. Add/edit and safe icons

Routes:

- `/dashboard/content/services`
- `/dashboard/content/services/new`
- `/dashboard/content/services/{id:int}`
- `/dashboard/content/services/home-featured`

ServiceEditor exposes only Name, Tagline, Description and IconPath. All are required and length-limited; text is Razor-encoded. A MudSelect allows only the six existing bundled icon paths. No remote icons, arbitrary paths, filesystem browsing/writes or uploads. A constrained icon preview shows a placeholder when unselected/unsafe or after image-load failure. New records redirect to their edit route only after the clean snapshot reaches NavigationLock.

## 8. Delete

MudDialog names the Service and warns when it is selected on Home. Cancel leaves the record untouched. Confirm deletes the canonical row and its relation, then normalizes shared and remaining featured positions within the same transaction. Last-item messaging explains that restart does not restore data and new Services require an explicit Home selection.

## 9. Independent shared ordering

List Move Up/Down saves immediately to Service.DisplayOrder. Public /services follows that order. Featured relation order is unchanged. Deleting a row closes gaps in both affected orders, but does not rearrange the remaining relative order.

## 10. Home Featured editor and layout capacity

Available Services use checkboxes; a second table shows the staged featured order with Up/Down/Remove. Checking appends, unchecking removes. **Save featured selection** commits membership and order together. Zero is valid.

The existing Home layout is two initial cards plus three cards in its second row. `index.css` fixes the desktop background at 915px, and at 1025–1199px its Bootstrap columns allow three cards in the second row; extra cards wrap into another row within that fixed-height composition. To preserve the existing public CSS/markup, this phase caps Home Featured at five. The UI explains the cap and disables only unchecked options at capacity; the service enforces it. The full shared catalog has no five-item limit.

Supporting more featured cards would require a separately approved public-layout change, not an arbitrary expansion in this CMS phase.

## 11. Unsaved changes

ServiceEditor and HomeFeaturedServicesEditor reuse EditorSnapshot/UnsavedChangesGuard. Load/save are clean; staged membership/order and field changes are dirty. Validation/write failures retain input and dirty state. Reverting selections restores clean state. Browser QA verified Leave/Stay flows, including validation failure. Native tab-close/reload beforeunload dialogs were not manually exercised; existing NavigationLock wiring remains unchanged.

## 12. Public Services integration

ServicesContentService is now scoped and reads shared cards fresh from SQLite outside its static editorial snapshot. Hero, benefits, process, pricing and FAQ stay unchanged. ServiceCard, ServicesGridSection, public CSS and JavaScript are unchanged. The Services anchor/section remains available with zero cards, keeping the existing hero anchor valid.

## 13. Public Home integration

Home reads featured cards through the relation on every GetAsync. HomeServicesSectionSettings continues to supply only its independent introduction. Shared updates are visible wherever selected; there are no Home-specific Service copies. When the selection is empty, Home omits its Services section cleanly while retaining saved intro settings.

## 14. Empty and fallback behavior

Valid empty catalog/selection returns empty, never defaults. On DB/validation read failures, public services log and use approved full or featured defaults without writes. Partner/Testimonial-style fallback is isolated from initialization. Admin failures are surfaced safely, not hidden by fallback content.

ServiceCatalog is now referenced only by initialization and read-failure fallback; normal runtime catalog/featured reads use SQLite.

## 15. Authorization

Inherited Admin route authorization and DashboardLayout apply to all four routes. Anonymous requests challenge to login, non-Admins are denied and Admin requests support bookmarks/refresh. Admin service methods recheck the current database user, role membership and security stamp. No public mutation endpoint.

## 16. Automated verification

- **1,457 auth/CMS checks passed**, including all previous suites.
- Fresh exact catalog/icon/subset/order, repeat initialization and no EF model drift.
- Actual application-host restarts preserve records, internal keys, timestamps, shared order and independent featured order.
- Delete-one, delete-all, repeated empty restart, clear featured while catalog remains populated, add-after-empty and explicit feature-after-add.
- CRUD, cascade/normalization, missing IDs, exact-ID shared reorder, duplicate/missing/over-capacity selection rejection and atomic no-change on failure.
- Required/length/icon-path validation, HTML encoding, logged fallback paths and valid-empty semantics.
- Anonymous/non-Admin/Admin routes and direct service authorization; revoked role/stamp protection.
- Add/edit and featured dirty state, revert, validation/write failure retention and clean successful saves.
- Additive upgrade verifies all eight Home CMS slices, Identity account/hash/role mapping, Testimonials, Partners and prior markers remain intact. Invalid relation FK is rejected. Failed initialization rolls back entities, relations and marker.
- **8 existing Home/Services/Dashboard JavaScript lifecycle tests passed.**
- **Debug and Release: zero warnings and errors.** Debug used `-p:OutputPath=bin/ServicesDebug/` to avoid IDE output locks.
- Test-Home.ps1 and Test-Services.ps1 passed against approved data, both before and after the temporary CRUD workflow.

## 17. Browser / public smoke

Localhost-only QA server with a separate ignored database and ephemeral test Admin credentials:

- Services list and Home Featured editor verified at **1440, 1024, 768 and 390**. No horizontal overflow; icons loaded; responsive table rows and actions remained usable.
- Added QA Service using an approved icon: appeared on /services but not Home before selection.
- Required-field errors retained input; Cancel showed the existing unsaved warning.
- Featured selection/reorder remained unpublished until Save. Leave discarded a staged change. Saved new selection appeared on Home.
- Keyboard Enter on featured/shared Move Up worked. Home-only reorder left full order unchanged; shared reorder left Home order unchanged.
- Edited shared name/tagline appeared on Home and /services through the same entity.
- Actual process restart preserved edits, both orders and membership.
- Delete dialog warned about Home membership; Cancel kept the Service; confirm removed it everywhere and normalized the relation.
- Clearing Home Featured left all six catalog Services available. Actual process restart preserved both the deletion and empty Home selection.
- Restored approved Home selection in the isolated DB for public visual smoke. Home and /services cards/icons checked at 1440 and 390; settled page and card geometry had no horizontal overflow. No public styling changes.
- Finally deleted all six disposable QA catalog records through confirmations: empty list, empty Featured editor, Home and /services rendered safely without cards or console errors. Automated tests additionally verify this state across repeated restarts.
- Browser warning/error logs were empty throughout the workflow. Interactive Server remained functional through the repeated edits/navigation.
- Temporary browser tabs, viewport override, credential pipe and QA server were cleaned up. The normal developer DB was never used for destructive QA.

No full-site visual regression was run.

## 18. Deferred work

No uploads/media library, filesystem manager, Service detail routes, Projects/Team CRUD, categories/tags, bulk actions, drafts/versioning, audit logging, localization, analytics or generic page builder.

## 19. Recommendation

Recommend **Shared Projects** next, with a separate Home Featured Projects relation while preserving existing detail-route identities. This phase does not implement it. Stop for approval.

Phase 12 routing note: Shared Services now lives at `/dashboard/content/shared-services` (including `/new`, `/{id}`, and `/home-featured`). The old child routes redirect to their equivalents. `/dashboard/content/services` now opens the page-specific Services Hero editor; shared data and CRUD semantics are unchanged.
