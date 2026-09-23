# Shared Team Members CMS + Home Featured Team

## 1. Git

Started from clean `f6c9b45874b34186a8fdb062dc49ec140183e681` on `feature/dashboard-cms`. This phase is limited to shared Team CRUD, existing detail content, shared order and independent Home selection/order. No branch change, history rewrite, push or merge. The normal developer database was not reset or used for QA. Runtime databases remain ignored.

## 2. Dashboard IA

Shared Content → Team Members opens the full list. Its Home Featured action opens `/dashboard/content/team/home-featured`. Home → Team Section remains the separate introduction/CTA editor, with helper text explaining the distinction. No duplicated navigation tree.

## 3. Data model

Inspection covered `TeamMemberSummary`, `MemberDetail`, `member.json`, MemberCatalog, Home, TeamGridSection, TeamMemberCard, MemberProfileContent and SocialLinks. The actual fields are Slug, Name, Role, Introduction, ImagePath, Email, LinkedIn, Telegram, Biography and ordered string Skills. There are no skill percentages or generic social-platform rows in the existing model.

`MemberEntity` stores those scalar fields plus Id, DisplayOrder and UpdatedAtUtc. `MemberSkill` is an ordered child table with MemberId FK. LinkedIn and Telegram remain fixed optional fields: clearing either removes its destination while preserving the approved decorative icon behavior. No invented social platforms/table. `HomeFeaturedMember` holds only MemberId (PK/FK) and independent DisplayOrder; no copied content or Home-specific member entities. Skills and Home membership cascade on member deletion.

## 4. Migration / initialization

`20260923015219_AddSharedMembersAndHomeFeatured` adds Members, MemberSkills, HomeFeaturedMembers and MemberInitializationState. It adds positive-order constraints, a case-insensitive unique slug index, FK/indexes and a singleton-marker constraint. Up does not alter/drop prior tables.

A single transaction seeds the six approved members and all biographies, introductions, contacts, portraits and 24 ordered skills. Team order is Emily Johnson, Emma Williams, Sophia Lee, Daniel Kim, Lena Alvarez, James Park. Home initially selects the first four in that order. Content, relations and the persistent marker commit together. Once marked, edits, deletions and empty selections survive restart. Existing unmarked member content is adopted without overwrite. A forced marker-write failure test verifies atomic rollback and retry.

## 5. Content service

`IMemberContentService / MemberContentService` use short-lived DbContexts. Full catalog, detail-by-slug and joined Home reads are no-tracking and explicitly ordered. Management methods provide CRUD, exact-set shared reorder and unique-subset featured save. Mutations preserve independent orders and use transactions where multiple operations must be atomic. New members append without automatic Home selection.

The existing `IMemberCatalog` resolves to the scoped SQLite service. The concrete JSON-backed MemberCatalog remains only for initialization and read-failure fallback. TeamContentService is scoped; Home and Team read members afresh on every GetAsync rather than retaining editable member snapshots. Sequential edits follow the existing last-save-wins CMS pattern, with UpdatedAtUtc as an EF concurrency token, not editor version history.

## 6. Team list

`/dashboard/content/team` uses a responsive MudTable with order, bounded photo preview, name, role, slug, Home membership/order and actions. Add, Edit, confirmed Delete, Refresh and immediate Up/Down controls match the existing collection UX. Edge moves are disabled and controls have member-specific accessible labels.

## 7. Add/edit

`/dashboard/content/team/new` and `/dashboard/content/team/{id:int}` group basic information, profile, photo, skills and contact/social fields. Skills support Add, Remove, text editing and Up/Down. All repeated values and sequence persist. The portrait selector allows only the six approved bundled Team photos, with a bounded preview and unavailable-image placeholder. Traversal, arbitrary filesystem/remote paths and unapproved assets are rejected. No uploads or file writes.

Required/length and nested skill validation run at the service boundary and on submission. Optional social destinations must be absolute HTTPS URLs without credentials, whitespace or unsafe characters. Email rejects mailto prefixes, query/header injection and multiple addresses. Razor continues to encode editorial text.

## 8. Slug

Slugs are trimmed/lowercased, required, at most 80 characters, and limited to letters/numbers with single separating hyphens. Unsafe punctuation, spaces and traversal are rejected. Service validation plus the database NOCASE unique index enforce uniqueness. The current public route has no conflicting fixed child route needing a reserved-word list. Changing the slug changes the public detail URL; the editor explicitly warns that old URLs do not redirect. Malformed/deleted/unknown detail URLs remain safely not found.

## 9. Delete

MudDialog names the member and warns about removal of the listing, detail URL, skills and any Home selection. Cancel preserves content. Confirmation cascades dependent rows and normalizes remaining shared/Home positions atomically. Last-member messaging explains that restart does not restore deleted content.

## 10. Shared reorder

Up/Down saves Team page order immediately without changing Home order. Reorders validate the exact current ID set, rejecting duplicates, missing/stale IDs and incomplete lists without partial writes. Deletion closes gaps while retaining relative order.

## 11. Home Featured

Checkbox membership and its separate ordered table are staged until explicit Save. Checking appends; removal and Up/Down affect only Home selection/order. Zero is valid and survives restart. No artificial card maximum: inspection showed the existing responsive grid supports additional rows, separate from the fixed-height introduction graphic. Browser QA included five featured members.

## 12. Unsaved changes

MemberEditor snapshots the complete typed model, including skills and optional social fields. FeaturedEditor snapshots ordered selected IDs. Both reuse UnsavedChangesGuard/NavigationLock. Load/save establish clean state; reverting restores clean state; failed validation/writes retain input. New-member save redirects to its edit URL after the clean state renders. Automated checks cover fields, child add/edit/remove/reorder, revert and failed saves. Browser checks exercised Stay protection in both editors. Native tab-close beforeunload dialogs were not manually exercised.

## 13. Public Team listing

`/team` reads the full SQLite catalog. Existing TeamMemberCard, portrait silhouettes, roles, introductions, social behavior, editorial composition and responsive grid remain unchanged.

## 14. Public Member Detail

`/team/{slug}` uses the same SQLite source through IMemberCatalog. Existing portrait, name/role, biography, ordered two-column skills, contacts, title/breadcrumb and not-found behavior remain unchanged. No JSON binding is used in normal runtime reads.

## 15. Public Home

Home joins HomeFeaturedMember to the shared member records in its independent order. HomeTeamSectionSettings remains the separate intro/CTA source. Shared edits appear on all consumers without duplicate content. The approved responsive card grid and portrait shapes are unchanged.

## 16. Empty/fallback

Valid empty collections return empty, never defaults. Team renders safely without cards; unknown detail is 404; Home retains its introduction but renders no member cards. Existing loops and skills rendering already handle zero safely, so no public markup guard/redesign was needed.

Database/content-validation read failures are logged and return approved full/detail/Home defaults without writes. The Home fallback preserves the original four-member subset. Management failures surface to the editor instead of reporting false success. Fallback and initialization are separate.

## 17. Authorization

All four management routes inherit Admin-only authorization and DashboardLayout. Anonymous users redirect to login; non-Admins are denied; Admin direct links and refresh work. Every management-service method checks authentication, current database role membership and security stamp. Revoked role/stamp tests pass. No public write endpoint was added.

## 18. Verification

- 2,351 auth/CMS checks passed, including all prior suites.
- Exact fresh defaults, once-only marker, repeat initialization, rollback/retry, EF model agreement and service/database slug uniqueness.
- Real application-host restarts preserve content, IDs/timestamps, skills, contacts, shared order and independent featured selection/order. Delete-one, delete-all, repeated empty restart, clear selection and add-after-empty pass.
- CRUD, cascade cleanup, invalid order rejection, safe assets/social/email validation, nested validation, HTML encoding and full/detail/Home fallback pass.
- Additive upgrade from the Projects migration preserves Identity credentials/roles, all Home settings, Testimonials, Partners, Services/Featured and Projects/detail/Featured content.
- 8 Home/Team/Dashboard JavaScript lifecycle checks passed, including repeated Home → Team → Member navigation and disposal.
- Debug and Release builds passed with zero warnings/errors. Debug used `-p:OutputPath=bin/MembersDebug/` to avoid IDE output locks.
- Test-Home.ps1, Test-Team.ps1 and Test-MemberDetail.ps1 passed against restored approved QA content. All six member details, portraits, 24 skills, contacts and not-found cases pass.

Browser end-to-end used only the ignored `artifacts/members-cms-qa-1173f92c/browser.db` database and an ephemeral Admin. Added a member, chose a portrait, added/reordered skills, tested unsafe-link validation and dirty guards, featured/reordered it, independently changed shared order, edited role/biography/skills and removed Telegram, then checked all public consumers. A real server restart preserved everything. Delete Cancel preserved the member; confirmation removed it everywhere, with its old URL returning 404 after restart. Clearing Home selection remained empty after another restart. Approved six-member content and four-member Home selection were restored for final smoke tests.

## 19. Public regression

Focused only on Home, Team and Member Detail; no full-site visual regression. Dashboard list, form and Featured editor checked at 1440/1024/768/390 with no horizontal overflow and functioning previews/controls. Public desktop/mobile checks retained card/profile styling and safe widths after reveal animations settled. Member links and repeated navigation worked. No public CSS, JavaScript or public Razor component was changed.

Stable browser operation had no application console warnings/errors. Deliberate server stops produced expected transient SignalR errors in already-open tabs; reload restored Interactive Server controls. Fresh post-restart public navigation was checked separately with a clean console.

## 20. Deferred work

No upload/media library, role management, Team categories, redirects/history, drafts/versioning, audit logging, bulk actions, localization, analytics or generic page builder. No next phase implemented.

## 21. Recommendation

Review and approve this shared Team phase before expanding scope. Keep the established shared-entity/independent-Home-selection pattern for future work. A future media-management phase should be scoped separately; it is not started here.
