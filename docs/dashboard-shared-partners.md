# Dashboard Phase 6 — Shared Partners CRUD + Reorder

## Git and scope

Implemented on `feature/dashboard-cms` from clean commit `296cd3f`. This phase adds only shared Partners management. No branch change, push, merge, normal developer database reset, media upload or other collection editor.

## Architecture inspected

The approved public `Partner` record already has Name, Description, ImagePath, HasLogoBackground and nullable Href. PartnerCard renders all five; alt text is derived from Name. The six approved records, their order, descriptions, logo paths, null links and two white-background flags are unchanged.

Home and About previously consumed PartnerCatalog directly. Their introductions are independent: Home uses HomePartnersSectionSettings; About retains its approved editorial heading. Shared PartnerCard, PartnersSection, ThemeCarousel, public CSS and carousel JavaScript remain unchanged. Only empty-collection guards were added to the public pages.

## Dashboard information architecture

- Home → **Partners Section** edits only the Home heading/description.
- Shared Content → **Partners** edits the canonical collection used by Home and About.
- Existing Home subpages and shared Testimonials remain available.

## Data and additive migration

`PartnerEntity` stores Id (generated), DisplayOrder, Name (80), Description (200), ImagePath (200), HasLogoBackground, nullable Href (500) and UpdatedAtUtc.

`20260922215318_AddSharedPartners` adds only Partners, its positive-order constraint/index, and the singleton PartnerInitializationState table. The EF snapshot/designer are committed. Runtime SQLite files remain ignored.

Reads order by DisplayOrder then Id. New records append; delete normalizes remaining positions. UpdatedAtUtc is an EF concurrency token, matching Testimonials. This is not an editorial versioning system: sequential saves still use the established last-save behavior.

## One-time initialization

Controlled startup checks the persistent marker, not collection emptiness alone. Defaults and marker commit in one transaction. An unmarked database with existing Partner rows is adopted without overwriting its content. A marked database is never reseeded, including after deletion of every row. Failed initialization rolls back both defaults and marker, permitting a safe retry.

The new collection and marker are introduced together, so no legacy Partners-history inference is necessary. Existing production initialization opt-in remains unchanged.

## Service

`IPartnerContentService / PartnerContentService` reuse the Testimonials collection pattern: factory-created short-lived contexts, ordered no-tracking public/admin reads, typed create/update, ID-based delete and exact-ID-set transactional reorder. IDs/order cannot be bound through the edit model. Malformed/stale reorder requests are rejected without partial writes.

Public empty reads return empty. Database/validation failures log an error and return approved PartnerCatalog defaults without writes. The catalog now has only initializer/fallback consumers; it is not the normal production source. Admin operations propagate safe-handled errors rather than disguising failures as defaults.

## List and add/edit

Admin-only bookmarkable routes:

- `/dashboard/content/partners`
- `/dashboard/content/partners/new`
- `/dashboard/content/partners/{id:int}`

DashboardLayout and inherited Admin authorization are reused. The responsive MudTable has order, logo, name, link/status and labeled keyboard-accessible actions. Add/edit share PartnerEditor with Mud inputs/select/checkbox. Creating a record redirects to its edit route after the clean snapshot reaches NavigationLock, avoiding accidental duplicate creation on refresh.

Name and description are required bounded plain text. Razor encodes content. Logo selection is restricted to the six existing bundled PNG paths; this is an asset allow-list, not duplicate Partner content. No upload, filesystem reads/writes, arbitrary paths or external logo URLs. PartnerLogoPreview refuses unsafe paths and shows “No logo” for unselected/failed images; changing the path resets the failed-image state.

Optional links accept the existing public route syntax (root, about, services, contact, projects/team and their slug routes) or absolute HTTPS. Whitespace/control characters, credentials, unsafe schemes, backslashes, relative traversal and percent-encoded path syntax are rejected. HTTPS query/fragment values are allowed. Link previews in the list are plain text, not navigable unvalidated anchors.

## Delete, reorder and unsaved changes

Deletion requires MudDialog confirmation; Cancel keeps the row. The last-item warning explains both public sections will stay hidden until a new record is added. Move Up/Down saves immediately in a transaction; first/last actions are disabled appropriately, including a single-row list.

Existing EditorSnapshot and UnsavedChangesGuard are reused for every editable field. Load/save establish clean state; validation/save failures keep user input dirty. Browser QA verified the internal navigation warning after validation failure. The native browser tab-close/reload beforeunload dialog was not manually exercised; its existing NavigationLock wiring is unchanged.

## Public integration and fallback

Home reads shared entities fresh on every GetAsync while retaining its separate CMS heading. AboutContentService is now scoped and reads the same service each time, outside its static editorial snapshot. About's unrelated content and heading remain unchanged.

Zero items omit the whole PartnersSection on either page, so no empty Owl root is mounted. Populated rendering uses the unchanged card/carousel pipeline. Single-item rendering uses existing Owl behavior; autoplay/navigation are suppressed when every item fits.

## Authorization

Anonymous routes challenge to login; non-Admin routes deny access; Admin routes support refresh. All admin service reads/writes recheck authenticated role, current database membership and current security stamp. Tests cover anonymous/non-Admin direct calls and revoked-role/stamp circuits. Public reads remain anonymous; there is no public mutation endpoint.

## Verification

All destructive tests used isolated databases. The developer's normal database was not modified.

- Full auth/CMS executable: **1,164 checks passed**, including all earlier CMS/Auth checks.
- Fresh seed, repeated initialization, actual host restarts, delete-one, delete-all, repeated empty restarts and add-after-empty verified. IDs, timestamps, content and order are preserved.
- Create/edit/reorder/delete, invalid/stale IDs, rejected reorder atomicity, field validation, link/path attacks, public HTML encoding, fallback and dirty-state checks passed.
- Additive upgrade preserves existing Home Partners intro and shared Testimonials edits/marker; prior suites continue to cover Identity and earlier CMS preservation. No EF model drift.
- Forced marker failure proves seed + marker rollback together.
- **9 JavaScript lifecycle checks passed**, including empty Home/About enhancement, detach/remount and no duplicate initialization.
- Release and Debug builds: **0 warnings, 0 errors**. Debug used `-p:OutputPath=bin/PartnersDebug/` to avoid existing IDE output locks.
- Existing Test-Home.ps1 and Test-About.ps1 smoke checks passed with the six approved records. Home's fixed-count test initially rejected the intentional seventh QA Partner, then passed after its confirmed deletion.

### Browser QA

Localhost-only app with a separate ignored QA SQLite database and temporary Admin credentials:

- List at **1440 / 1024 / 768 / 390**: usable desktop/stacked rows, loaded logo previews, accessible actions and no horizontal overflow.
- Add using an existing logo + local link appeared on Home and About.
- Unsafe URL produced inline validation; Cancel after the failure showed Unsaved changes; Stay retained input.
- Edit persisted; Enter on Move Up changed order on both pages; actual process restart preserved it.
- Delete Cancel preserved the record; confirmed Delete removed it from both pages.
- Deleting all six disposable defaults showed the list empty state; actual restart stayed empty. Home/About omitted Partners without console errors.
- Add-after-empty produced just one record on both pages. Repeated enhanced Home/About navigation had one carousel root/stage/control set when populated, none when empty.
- Public Next/Previous and Pause/Play worked, with loaded logos. No browser warning/error logs during the workflow; Interactive Server remained usable through repeated CRUD.
- Public styles were not altered. Home's pre-existing reveal transforms/scrollbar sizing can yield a small scrollWidth/clientWidth discrepancy while its existing body overflow-x remains hidden; no unrelated geometry cleanup was attempted.

Only focused Home/About public smoke checks were run, not a full-site visual regression.

## Deferred and recommendation

No uploads/media library/file manager, Service/Project/Team CRUD, About CMS, categories, analytics, drafts/versioning, audit logging, localization or bulk actions.

Recommend **Services** as the next shared-content collection, after agreeing how its Home-featured subset should be represented. It is not implemented in this phase. Stop for approval.
