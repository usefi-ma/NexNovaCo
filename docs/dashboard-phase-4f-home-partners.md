# Dashboard Phase 4F — Home Partners section CMS

Verified 2026-09-22. Only the existing Home Partners title and description became editable. There is no Partner entity management.

## 1. Git

Started on `feature/dashboard-cms` with a clean working tree at `f4e6508`. Stayed on that branch, with no main checkout, history rewrite, push, or merge. This report accompanies the focused Phase 4F commit.

## 2. Sidebar / route

Home has Hero, Welcome, Services, Projects, Team, Statistics, and Partners. The new canonical route is `/dashboard/content/home/partners`, using the existing Admin-only Dashboard layout. Direct load/refresh, expanded Home group, Partners active state, mobile drawer closure, and keyboard Enter navigation were verified. No Testimonials route was added.

## 3. Home vs About architecture — inspection findings

The distinction was established before implementation by inspecting both original HTML pages, both content services, the Partner model/catalog, shared components, route CSS, and carousel lifecycle:

| Scope | Existing data | Phase 4F treatment |
| --- | --- | --- |
| Global Partner entities | Six names, card descriptions, logo paths, logo-background flags, optional destinations | Same canonical read-only collection; no edits |
| Home presentation | “Our Partners” title and the existing introduction paragraph | Home-only SQLite singleton |
| About presentation | Same approved title/paragraph, separately supplied to its section instance | Remains static and unchanged |
| Shared rendering | PartnersSection, SectionHeader, PartnerCard, ThemeCarousel | Unchanged |

Home and About previously took the same immutable heading value from `PartnerCatalog.Heading`. The heading is section presentation, not Partner entity data. This phase makes the existing Home instance editable without changing the About instance. There is no Home-only Partner catalog and no About-only Partner catalog.

## 4. Canonical Partner source

`PartnerCatalog.All` remains the sole collection for both pages: Tech Co, Digital Co, NeTech Co, NeDigital Co, Alpha Co, and NeAlpha Co in approved order. Names, card descriptions, image paths, alt text, logo backgrounds, and absent destinations are unchanged. Automated tests assert reference identity between Home and About collections, not just equal copies. Browser checks compare the six original cards, excluding Owl's temporary loop clones.

## 5. Actual Home fields

Home's approved design contains a title and description, so an informational-only placeholder would leave meaningful content uneditable. Only those two fields are exposed. There is no eyebrow, CTA, or new presentation control.

`HomePartnersSectionDefaults.Content` references the existing approved `PartnerCatalog.Heading` for initialization/fallback. It does not duplicate Partner entities or mutate the shared heading when an Admin saves.

## 6. Database / migration / service

Migration `20260922194221_AddHomePartnersSectionSettings` only adds `HomePartnersSectionSettings`: singleton Id 1, Title, Description, and UpdatedAtUtc. The PK/check constraint prevents another Id. There are no Partner entity columns, foreign keys, logos, or URLs in this table; existing schemas are untouched.

Controlled startup initialization inserts approved copy only when the singleton is missing. It never overwrites saved edits. Upgrade tests begin at the previous Statistics migration with edits in all six earlier Home slices and an Identity account/role mapping, then verify preservation and no model drift.

The scoped `HomePartnersSectionContentService` uses short-lived factory contexts and no-tracking reads. Missing/unavailable/invalid public settings log the issue and use approved defaults without writes. Editor failures surface safely rather than silently presenting fallback as persisted content. Saves validate, update the singleton and UTC timestamp, and persist.

## 7. Editor behavior and validation

`HomePartnersSectionEditor.razor` matches existing MudBlazor editor patterns. It explicitly explains that partner cards are shared and About's introduction is unchanged. Title is required with max length 120; Description is required with max length 1000. Whitespace-only and overlength values are rejected in both model/service paths. Text stays Razor-encoded, never raw HTML.

Save stays on the page and displays “Home Partners section updated successfully.” Expected failures preserve input, show safe feedback, and log server-side details. The helper text requests concise copy for the unchanged public layout. No card, URL, upload, visibility, ordering, or animation controls exist.

## 8. Unsaved changes

The editor reuses the existing `EditorSnapshot` and `UnsavedChangesGuard` without modifying either. Both fields participate. Tests cover load, edits/revert, validation failure, save failure, successful save, and reload.

In-browser Stay retained the edited title; Leave navigated and a subsequent return loaded persisted copy. A blank title was rejected, remained dirty, and still triggered the shared sidebar dialog. Keyboard Tab reached Leave from Stay; mobile selection closed the drawer.

A dirty View Home action retained the editor URL, but the in-app browser did not expose the native unload dialog through its dialog API. Native prompt appearance/accept/cancel still requires a manual standard-browser check, as documented in prior phases. The existing NavigationLock path is retained; no custom unload logic was added.

## 9. Public Home integration

Only `HomeContentService.PartnersHeading` now reads through the new service on every call, outside the static-content snapshot. Partner items still come from `PartnerCatalog.All`. PartnerCard, PartnersSection, ThemeCarousel, public Razor markup, CSS, JavaScript, and assets are unchanged.

Isolated browser QA changed both fields to a test title/introduction. Home showed both saved values while its original six cards stayed identical. Refresh and an actual server stop/restart preserved both fields. Approved text was then restored and saved through the editor, and public content was compared with the initial baseline.

## 10. About regression and carousel

After Home edits, About retained the exact approved title, description, and six original cards. Pause/Next worked on Home and About at 390px, visibly changing the active partner while retaining six original items and one Owl stage. At 1440px all six cards remained visible and autoplay was paused as designed. Home → About → Home navigation remounted one partner carousel with no duplicate stages. No browser warnings/errors were recorded.

Home and About desktop/mobile smoke checks found no horizontal overflow at the measured widths. Existing Home/About HTTP suites passed, including approved logos, canonical partner equality, and assets. No full visual regression was performed.

## 11. Authorization

Anonymous editor requests redirect to login; non-Admins are denied; Admin direct load/refresh/save succeeds. The service also checks the authenticated Admin principal, security stamp, and current database role membership for editor reads/writes. Tests cover direct unauthorized calls and role/stamp revocation. No public write endpoint was introduced.

## 12. Verification

- Debug and Release builds: zero warnings/errors.
- Auth/CMS integration harness: **681 checks passed** across all seven Home slices.
- Home/About interop suites: **7 tests passed**, including carousel disposal, repeated navigation, reduced motion, and unavailable-Owl fallback.
- Existing `Test-Home.ps1` and `Test-About.ps1`: passed against the isolated QA server.
- Dashboard visual inspection at **1440, 1024, 768, 390px**: correct two-field form and no horizontal overflow.
- Browser: login, route/refresh, sidebar active state, validation, Stay/Leave, keyboard/mobile navigation, save, Home/About isolation, carousel controls, reload/restart persistence, and restored approved text.
- Database: one row, correct defaults, no duplicate/overwrite on initialization, rejected second Id, safe fallback, all six earlier CMS slices and Identity preserved during upgrade.
- `git diff --check`: passed.

The normal development database was not reset or used for test edits. QA used an isolated database under ignored artifacts. Temporary credentials were not printed or committed. The temporary QA server/tab were stopped and viewport override reset after verification.

## 13. Deferred Partner CRUD

No Add/Delete/Reorder, logo upload/editing, destination editing, visibility controls, global Partner editor, About Partners CMS, media library, collection engine, Header/Footer CMS, drafts/versioning, or page builder. No duplicated Partner catalog. Existing shared rendering remains the future management boundary.

## 14. Recommendation — approval required

Testimonials can be inspected next, with an explicit distinction between its shared Home/Projects quote catalog and page-specific presentation. Scope any future editor separately from carousel/style changes. Testimonials is not implemented in this phase. Stop for approval.
