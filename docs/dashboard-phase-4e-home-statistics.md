# Dashboard Phase 4E — Home Statistics CMS

Verified on 2026-09-22. This phase makes only the existing four Home statistic labels and numeric values editable.

## 1. Git

Started with a clean tree on `feature/dashboard-cms` at `6c4f029`. Stayed on that branch. No main checkout, history rewrite, push, or merge. This report accompanies the focused Statistics implementation commit.

## 2. Sidebar and route

Home now contains Hero, Welcome, Services, Projects, Team, and Statistics. The Admin-only editor is `/dashboard/content/home/statistics`, using the existing Dashboard layout. Direct entry and refresh work. Home remains expanded, Statistics alone is active, and selecting the child closes the mobile drawer. Keyboard Enter navigation and dialog Tab navigation were checked.

## 3. Data model

Inspection of `HomeStatisticsSection`, `StatisticItem`, the typed `Statistic` contract, original CSS, and Home's CountUp integration found exactly four items, with no prefix, suffix, icons, or item-specific animation settings:

| Fixed Id/order | Approved label | Approved value |
| --- | --- | --- |
| 1 | PROJECTS | 450 |
| 2 | CLIENTS | 3000 |
| 3 | EMPLOYEES | 1000 |
| 4 | AWARDS | 26 |

`HomeStatistic` stores Id, DisplayOrder, Label, Value, and UpdatedAtUtc. Metadata is not editable. There are no speculative prefix/suffix columns.

## 4. Migration and initialization

Migration `20260922182837_AddHomeStatistics` adds only the HomeStatistics table and its unique order index. Fixed-slot and numeric-range constraints enforce Id 1–4, DisplayOrder = Id, and Value 0–9999. Existing tables are untouched.

`HomeStatisticsInitializer` inserts the exact approved collection only when the table is empty. Repeated startup does not duplicate rows or overwrite edits. Nonempty partial collections are not reseeded or repaired automatically. Public reads of missing/incomplete data use defaults without writes; the editor reports the problem instead of permitting an implicit collection repair.

Upgrade tests migrate the previous Team schema after editing all five existing Home slices and creating an Identity user/role mapping. Values, password hash, and role membership survive; four statistics are added with no model drift. Tests and browser QA use isolated databases; the normal development database was not reset.

## 5. Content service

`IHomeStatisticsContentService` / `HomeStatisticsContentService` uses scoped DI and short-lived factory contexts. Reads are ordered and no-tracking. Reads validate identities, count, order, and field data before returning typed public statistics. Database or validation failure logs the issue and returns the whole approved collection without persisting fallback.

Updates authorize, validate all four items, verify the stored collection, update only Label/Value/timestamps, and call SaveChanges once. There are no add/delete/reorder methods. EF's transaction makes the collection save atomic; a test-only trigger failing the second update proved the first update is rolled back.

## 6. Editor

`HomeStatisticsEditor.razor` uses the existing MudPaper, section, grid, action, and feedback patterns. Each fixed position has a MudTextField label and nullable-integer MudNumericField value. There is one Save changes button for the whole collection and one View Home Page button. Inputs disable during save. There are no Add/Delete/Reorder, style, visibility, or animation controls.

## 7. Validation

Labels are required, reject whitespace-only input, and allow at most 32 characters. Values are required whole numbers from 0 to 9,999: this retains the approved four-digit number footprint and is exactly representable by JavaScript. No decimals or negative numbers are supported. Longer business requirements need a separately approved layout/range review.

The collection implements explicit child validation rather than assuming root DataAnnotations validation recursively checks a list. Summary errors identify the statistic position. Service validation independently rejects missing, extra, duplicate, unknown, or reordered items. All eight editable fields are covered by tests. Labels remain Razor-encoded plain text, not raw HTML. Keep labels concise and preview the fixed public layout after changes.

## 8. Unsaved changes

The editor reuses the same `EditorSnapshot` and `UnsavedChangesGuard` as the earlier slices; those shared implementations are unchanged. Every label/value participates in the snapshot. Load, reverting, successful save, invalid submission, failed save, and reload states were tested against the actual editor lifecycle.

Browser checks verified sidebar Stay/Leave, retained values on Stay, discarded local edits on Leave, and dirty state after rejecting 10000. Keyboard Tab reaches Leave from Stay. Dirty View Home Page retained the editor URL; the in-app browser did not expose the native unload dialog through its dialog API. Actual native prompt appearance/accept/cancel remains a manual standard-browser check, as documented in prior phases. The existing NavigationLock external-warning path remains in use; no second implementation or custom unload JavaScript was added.

## 9. Save flow

Successful save stays on the editor with “Home Statistics section updated successfully.” Expected failures retain input, use safe user feedback, and log technical details server-side.

Browser end-to-end QA used an isolated SQLite database: login, edit PROJECTS / 450 to DELIVERIES / 725, save, open Home, observe updated label and number, refresh, stop/restart the server on the same database, and confirm persisted values in Home and the editor. The other three items remained unchanged. Original approved values were restored through the editor afterward. Automated tests also save all four items together.

## 10. Public integration and CountUp

`HomeContentService.GetAsync` reads Statistics from SQLite on each call, outside its static-content snapshot. The public contract, components, markup, CSS, background, responsive grid, assets, and production JavaScript are unchanged. JavaScript only enhances Razor-rendered `data-count-value`; it does not fetch or own content.

The browser observed the saved target 725, an intermediate animated display of 438, and the final display 725. Existing grouped values finished at 3,000 and 1,000. Home → Team → Home navigation retained four counter elements and animated the saved values again. No browser warnings/errors were recorded.

The new dependency-free interop test exercises 0, 725, and 9999, verifies the exact target is passed to CountUp, duplicate initialize calls do not create a second session, and disposal resets the instance/disconnects observers before another Home root. This is direct lifecycle test coverage; browser checks do not introspect private CountUp instance state.

## 11. Authorization

Anonymous editor requests redirect to login; non-Admin requests are denied; Admin direct load/refresh/save succeeds. Editor service reads and writes additionally check authentication, Admin claims, current database role membership, and security stamp. Tests cover direct unauthorized calls, role revocation, and stale stamps. No public write endpoint was introduced.

## 12. Verification

- Debug and Release builds: zero warnings/errors.
- Authentication/CMS integration harness: **610 checks passed**, including all six Home slices.
- Home interop suite: **4 tests passed**, including CMS CountUp targets and existing reduced-motion/disposal behavior.
- Existing `Test-Home.ps1`: passed against the isolated QA server.
- Database checks: approved defaults, four fixed rows, idempotence, all-item validation, atomic failure rollback, partial/empty fallback, restart persistence, prior CMS and Identity preservation.
- Browser checks: direct route/refresh, active sidebar, login/save, validation, Stay/Leave, mobile drawer, keyboard navigation, public values, CountUp, reload, and actual server restart.
- Dashboard screenshots inspected at **1440, 1024, 768, and 390px**; no horizontal overflow.
- `git diff --check`: passed.

Temporary QA credentials were kept out of files/logs/commits. The QA tab was closed and temporary viewport override reset after testing.

## 13. Public regression

Focused Home smoke only, not a full-site regression. Home remains anonymously accessible. Public width measurements at 1440/1024/768/390 showed four counters and no horizontal overflow; desktop and mobile Statistics screenshots were inspected. Existing cards/content remain untouched. The existing Home HTTP suite also checks its linked canonical detail routes/assets. Team was used only as a navigation hop for CountUp lifecycle verification, not as a new page regression task.

## 14. Deferred work

No Add/Delete/Reorder statistics, drag/drop, prefixes/suffixes, icons, layout/animation/visibility controls, Partners or Testimonials CMS, entity CRUD, uploads, Header/Footer CMS, page builder, drafts/versioning, or audit history. No public redesign or CountUp rewrite.

## 15. Recommendation

Partners introductory content is the smaller next slice, subject to approval and inspection of its shared Home/About content source. Keep partner-card editing and Testimonials separate; their shared carousel/content boundaries deserve an explicit scope decision. Nothing from the next phase has been implemented. Stop for approval.
