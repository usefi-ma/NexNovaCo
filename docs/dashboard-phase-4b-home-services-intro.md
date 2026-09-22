# Dashboard Phase 4B — Home Services intro CMS

Verified 2026-09-22. Only the existing Home Services title and description are newly editable.

## 1. Git

Started on `feature/dashboard-cms` at `40d1ef2` with a clean working tree. No new branch, history rewrite, merge, or push. The delivery message records the implementation commit and final working-tree state.

## 2. Sidebar

Home now has Hero / Welcome / Services, implemented with the existing MudNavGroup and exact-match MudNavLinks. Services expands Home, highlights its own link, and supplies Home / Services AppBar context. Existing responsive drawer and keyboard behavior are retained. Dashboard's Home quick-access description now includes Services intro editing.

## 3. Route

`/dashboard/content/home/services` is independently refreshable/bookmarkable and inherits DashboardLayout and Admin authorization from the Dashboard folder. Existing Hero/Welcome routes and compatibility redirects are unchanged.

## 4. Database

`HomeServicesSectionSettings` adds only Id, Title, Description, and UpdatedAtUtc to the existing ApplicationDbContext/SQLite database. Migration `20260922161150_AddHomeServicesSectionSettings` creates only this table. Its primary key is fixed at 1 with a database check constraint; a second identity is rejected. Migration and designer source plus model snapshot are tracked; runtime databases remain ignored.

The forward migration has no drop, rename, existing-table rebuild, or destructive data operation. The generated rollback removes only the new table and would discard that slice's saved values; no rollback was run. Normal development data was not reset or used for browser QA.

## 5. Initial data and actual section structure

Inspection covered HomeContentService, HomeServicesSection, SectionHeader, ServiceCatalog, public Home composition, and existing responsive CSS. The Home Services component consumes `SectionHeading` and renders its own H3/paragraph with approved zoom-out animation; it does not use the shared SectionHeader component. There is no eyebrow or CTA. The approved title is **Our Services**, and the existing full description was moved verbatim into `HomeServicesSectionDefaults`.

Controlled startup applies the migration and initializes these defaults only when Id 1 is absent. Existing saved content is never overwritten. Development startup remains automatic; other environments retain the existing explicit initialization opt-in. No new database or account bootstrap mechanism was introduced.

## 6. Content service

`IHomeServicesSectionContentService` / `HomeServicesSectionContentService` follow the established focused CMS pattern. Every operation creates and disposes its own short-lived DbContext via IDbContextFactory. Reads use AsNoTracking and map to the existing typed SectionHeading. Updates validate, modify the singleton, set UTC update time, and save.

Public reads log missing/unavailable/invalid data and return approved defaults without persisting fallback. Editor reads expose safe failure feedback instead of pretending fallback is saved data. Cancellation is not swallowed. Editor reads and writes additionally verify the current authenticated Admin principal, current database role membership, and security stamp, matching Hero/Welcome.

## 7. Editor

HomeServicesSectionEditor reuses the existing MudPaper, MudGrid, MudTextField, MudStack, MudButton, and MudAlert styling. It has only Title and Description, a multiline auto-sizing description field, Save changes, and View Home Page. Helper text explicitly says service cards are not editable. No global shell/CSS redesign.

## 8. Validation

Both fields are required (including rejecting whitespace-only values). Title is limited to 120 characters and Description to 1,000; the same limits appear in the editor, edit model, and EF model. Plain text is Razor-encoded, not MarkupString/HTML. No URL field exists, so no CTA route validation or new route allowlist is needed. Editors should keep copy concise for the unchanged public layout; maximum lengths are not a promise that every possible long text has identical layout geometry.

## 9. Unsaved changes

The existing EditorSnapshot and UnsavedChangesGuard are reused without modification, listing only the two actual fields. Load/save capture a clean snapshot; edits become dirty; reverting restores clean; invalid submit and failed persistence retain dirty/input state. Browser checks exercised Stay, Leave, return-after-discard, required-description validation followed by a navigation warning, keyboard focus, and clean navigation after save.

View Home Page retains the existing full-load public style boundary and framework-native unload protection. A dirty View Home Page attempt retained the route/input, and a clean post-save attempt navigated normally. As in Phase 4A, the in-app browser did not expose the native beforeunload prompt through its dialog API, so its visible native Leave/Cancel choices were not independently verified. Browser-controlled prompt suppression/process termination limitations remain; no custom unload JavaScript or autosave claim was added. A normal-browser native-prompt spot check is advisable before release.

## 10. Save flow

Successful saves stay on the editor, clear dirty state, and show “Home Services section updated successfully.” Expected failures preserve input, show safe feedback, and log technical detail server-side. Simulated DB/validation failures exercise the real editor save methods in tests; no real failure was injected into the user's database. Existing Hero/Welcome editors and persistence behavior remain intact.

## 11. Public integration

Only HomeContentService's ServicesHeading is replaced with a fresh CMS read on each request/navigation, outside the cached static-content snapshot. HomeServicesSection markup, public CSS, SectionHeader, ServiceCard, ServiceCatalog, and ServicesContentService are unchanged. Home retains five canonical cards in the original order; `/services` retains its six cards and existing page content. No public Admin UI or write endpoint was added.

## 12. Authorization

HTTP tests confirm anonymous requests to the editor redirect to login, authenticated non-Admins are denied, and Admin loads/refreshes the editor. Service tests reject anonymous/non-Admin callers and immediately reject stale role membership/security stamps. Existing public routes remain anonymous.

## 13. Verification

Browser QA used the isolated ignored database `artifacts/services-intro-qa-15a39f3d/identity.db`, with memory-only temporary credentials. The normal development database and existing IDE session were not used or reset.

- Logged in, loaded Services directly, refreshed, and checked the active child/expanded group.
- Modified Title, chose Stay (input retained), then Leave; returning showed the unchanged database title.
- Submitted empty Description: required error; navigation still showed the unsaved dialog.
- Saved both title and description through the form; public Home displayed them after refresh.
- Stopped/restarted the actual QA server against the same database; saved values and Admin access remained intact, and all five card text/icon tuples were identical.
- Restored approved content through the editor; Hero/Welcome retained their existing values and clean sibling navigation worked.
- Checked Dashboard at 1440, 1024, 768, and 390px: no horizontal overflow. At 390px, dialog/actions fit, Tab reached Leave, Enter on Services navigated, and the mobile drawer closed.
- Automated migration upgrade starts at the previous Welcome migration with edited Hero/Welcome plus an existing Identity account/hash/role mapping, then confirms all are preserved and the Services table/model match.
- Automated checks cover singleton constraint, default copy, repeated initialization, timestamps, restart, read fallback without writes, invalid stored content, encoded text, per-field validation, save failures, and unchanged canonical cards/Services content.

Final results:

| Check | Result |
| --- | --- |
| Debug solution build (isolated `bin/ServicesIntroDebug/`) | 0 warnings / 0 errors |
| Release solution build | 0 warnings / 0 errors |
| Auth/CMS test harness | 338 checks passed |
| Home + Services interop tests | 6 passed |
| Existing Home HTTP suite | Passed |
| Existing Services HTTP suite | Passed |
| Git whitespace check | Passed |

Commands: `dotnet build NexNovaCo.sln -c Debug --no-restore -p:OutputPath=bin/ServicesIntroDebug/`, `dotnet build NexNovaCo.sln -c Release --no-restore`, `dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build`, `node --test scripts/Test-HomeInterop.mjs scripts/Test-ServicesInterop.mjs`, and the existing Test-Home.ps1/Test-Services.ps1 scripts against the isolated QA server.

## 14. Public regression

Focused Home and Services checks only, not full public-site regression. Both pages remain anonymous and render their approved content/cards/assets. Public Home was visually inspected with restored intro at mobile/desktop widths; existing heading text capitalization/animations/layout remain unchanged. Services loaded its approved sections and six cards. No captured browser console warning/error, visible reconnect UI, or checked horizontal overflow; public pages did not load Dashboard styles. The temporary QA server/tab were closed and viewport override reset.

## 15. Deferred work

No service CRUD/order/icons/visibility, other Home section CMS, Services-page CMS, uploads/media, Header/Footer, global settings, drafts/versioning/audit history, page builder, merge, or deployment. The existing canonical catalogs remain authoritative.

## 16. Recommendation

After separate approval, inspect and add only the Home Projects intro title/description as the next small sidebar child. Keep project records, cards, and featured ordering in their canonical catalog. This next slice is not implemented here.
