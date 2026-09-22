# Dashboard Phase 4D — Home Team intro CMS

Verified on 2026-09-22. Scope is only the existing Home Team introduction; the Team listing and canonical member cards remain unchanged.

## 1. Git

Started on `feature/dashboard-cms` with a clean working tree at `9a1c4df`. Stayed on that branch; no main checkout, history rewrite, push, or merge. This report accompanies the focused implementation commit.

## 2. Sidebar

Home now contains Hero, Welcome, Services, Projects, and Team in that order. The existing Home group stays expanded on the Team editor; Team alone is active. The mobile drawer closes on selection. Existing MudBlazor keyboard navigation and the shared unsaved-change dialog are reused.

## 3. Route

`/dashboard/content/home/team` uses the existing Dashboard layout and Admin authorization inherited through the Dashboard imports. Direct entry and refresh were verified. No future editor routes were added.

## 4. Database

`HomeTeamSectionSettings` uses the existing `ApplicationDbContext` and SQLite database. Migration `20260922172549_AddHomeTeamSectionSettings` only adds its table. Id is the primary key with a check constraint requiring Id 1; the six existing content fields and `UpdatedAtUtc` are stored. No Identity or previous Home table is altered. Runtime databases remain ignored.

Upgrade tests begin at the previous Projects migration with edited values in all four existing Home singletons plus an Admin account. Applying the Team migration preserves those values, the password hash, and role membership. Model/migration consistency passes.

## 5. Initial data

`HomeTeamSectionDefaults` contains the exact previously approved `TeamSectionContent`, including the relative `team` CTA. There is no eyebrow. `HomeTeamSectionInitializer` inserts only when Id 1 is absent during controlled startup initialization. Repeated startup neither duplicates the row nor overwrites edits. Public fallback reads never initialize data.

## 6. Content service

`IHomeTeamSectionContentService` / `HomeTeamSectionContentService` is scoped and uses short-lived factory-created contexts. Public reads are no-tracking and map to the existing `TeamSectionContent`. Missing records, database read failures, and invalid stored content use approved defaults with server-side logging and no writes. Editor read failures surface safely instead of pretending fallback content was saved. Updates validate, update the singleton and UTC timestamp, and save.

## 7. Editor

`HomeTeamSectionEditor.razor` follows the existing MudBlazor editor pattern: content fields, CTA group, Save Changes, View Home Page, and accessible feedback. Its six fields correspond exactly to the existing public markup:

| Field | Public element | Maximum length |
| --- | --- | --- |
| Title | Team H2 | 120 |
| Introduction | First paragraph | 500 |
| Highlight | Existing highlighted sentence | 300 |
| Description | Second paragraph | 1000 |
| CTA text | Existing link label | 60 |
| CTA route | Existing link destination | 200 |

There are no member/card, image, visibility, ordering, or layout controls. Helper text advises concise copy because the approved public layout retains fixed heights; maximum lengths are data limits, not a guarantee that arbitrary long copy fits every viewport.

## 8. Validation

All six fields are required and reject whitespace-only values. Maximum lengths are checked by the form and service. CTA uses the same public-route allowlist as existing editors; external schemes, protocol-relative URLs, encoded destinations, and Dashboard routes are rejected. Text remains Razor-encoded, not raw HTML. Automated checks cover each field, invalid routes, unchanged storage after rejection, and encoded output. The browser also rejected a `javascript:` CTA.

## 9. Unsaved changes

The editor reuses `EditorSnapshot` and `UnsavedChangesGuard`; neither shared implementation changed. All six fields participate. Load and successful save establish a clean snapshot; edit is dirty, reverting is clean, and validation/save failure remains dirty. Tests cover actual editor lifecycle and failed/successful saves.

Browser checks confirmed sidebar confirmation, Stay retaining edited text, Leave navigating, and keyboard focus moving between dialog actions. The mobile dialog fits at 390px. A dirty View Home action retained the editor URL; the in-app browser did not expose the native unload prompt through its dialog API. Native prompt appearance/accept/cancel therefore remains a manual browser verification limitation, as in Phase 4A; the existing framework `NavigationLock` external-warning path is unchanged. No custom unload JavaScript was added.

## 10. Save flow

Successful save stays in the editor and displays “Home Team section updated successfully.” Failed save retains input, shows safe feedback, and logs technical details server-side. Using an isolated QA database, all six fields were changed through the browser, saved, and observed on public Home. Refresh and a real server stop/restart preserved all values. The approved original values were then restored through the editor and saved, even though the database was isolated.

## 11. Public integration

`HomeContentService.GetAsync` obtains fresh Team intro content from the new service, outside its static snapshot. Only that content source changed. `HomeTeamSection`, shared cards, public CSS/JS/assets, `MemberCatalog`, featured member IDs, and `TeamContentService` are untouched. All four Home members retain identical names, roles, text, images, routes, and social data before and after editing. The separate `/team` introduction remains static and independent.

## 12. Authorization

Anonymous editor requests redirect to login, non-Admins are denied, and Admins can load/refresh/save. The content service also checks the authenticated principal, security stamp, and current database Admin membership before editor reads/writes. Tests cover role revocation and stale security stamps. No public write endpoint was introduced.

## 13. Verification

- Release build: zero warnings/errors.
- Debug build with isolated `bin/TeamIntroDebug/` output: zero warnings/errors.
- `dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build`: **499 checks passed** across authentication and all five Home CMS slices.
- `node --test scripts/Test-HomeInterop.mjs scripts/Test-TeamInterop.mjs`: **6 tests passed**.
- `scripts/Test-Home.ps1` and `scripts/Test-Team.ps1`: passed against the isolated QA server.
- Browser: login, canonical route/refresh, active sidebar, all six defaults, validation, dirty-state dialog, save, public output, reload, real restart persistence, restoration, and clean navigation verified.
- Dashboard widths 1440, 1024, 768, and 390: no horizontal overflow. Mobile drawer close and keyboard action verified.
- No browser warnings/errors recorded during the focused QA flow. Interactive saves and repeated navigation continued working.
- `git diff --check`: passed.

Database tests and the browser server used isolated databases. The normal development database was not reset or used for edits. The temporary server and browser tab were stopped after verification; no credentials were committed.

## 14. Public regression

Focused smoke only, as requested: `/` and `/team` remain anonymously accessible. Home renders the four unchanged featured members; Team renders all six canonical members. Existing HTTP checks verify profile routes, social links, and assets. Home Team was visually inspected at 1440 and 390 after restoration, with no horizontal overflow; `/team` also had no overflow at 1440. Public pages load no Dashboard stylesheet. No full public-site regression or redesign was performed.

## 15. Deferred work

No Team CRUD, member selection/order/bio/social editing, image upload, Team-page CMS, Statistics/Partners/Testimonials CMS, Header/Footer settings, site settings, draft/version history, audit history, or generic page builder. Existing fixed-height layout and native unload-prompt testing limitation are documented rather than expanded into unrelated work.

## 16. Recommendation

Home Statistics labels/counts would be a small next slice, subject to explicit approval and inspection of the current counter lifecycle. It is not implemented. Stop here for approval; no next phase, push, merge, or deployment.
