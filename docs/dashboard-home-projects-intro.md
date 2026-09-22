# Dashboard — Home Projects intro CMS

Verified 2026-09-22. Scope: the existing Home Projects title and description only.

## 1. Git

Started on `feature/dashboard-cms` at `0bc66b8` with a clean working tree. No new branch, history rewrite, merge, or push. The delivery message records the focused implementation commit and final tree state.

## 2. Sidebar

Home now contains Hero / Welcome / Services / Projects. The existing MudNavGroup expands on the Projects route; exact-match MudNavLink highlighting and Home / Projects AppBar context identify the editor. Mobile drawer closure and keyboard Enter navigation were checked. No future destinations were added.

## 3. Route

`/dashboard/content/home/projects` independently supports direct load and refresh. It inherits DashboardLayout and Admin authorization from the existing folder imports. Prior editors and compatibility redirects are unchanged.

## 4. Database

`HomeProjectsSectionSettings` contains Id, Title, Description, and UpdatedAtUtc in the existing ApplicationDbContext/SQLite database. Migration `20260922163222_AddHomeProjectsSectionSettings` creates only this table, with fixed Id 1, a primary key and `Id = 1` check constraint. It does not alter/rebuild/drop existing tables. Migration/designer/snapshot source is committed; runtime databases remain ignored. The generated Down method removes only the new table and would discard its content; no rollback was run.

## 5. Actual structure and initial data

Inspection covered public Home composition, HomeProjectsSection, SectionHeader, ProjectCard, ProjectCatalog, ThemeCarousel, Home/shared carousel interop, and current responsive CSS. The section uses SectionHeader with an H3 and description, followed by the five-card featured carousel. It has no eyebrow or section-level CTA. Read More belongs to each project card and remains non-editable.

HomeProjectsSectionDefaults holds **Our Projects** and the exact existing description moved verbatim from HomeContentService. Controlled startup initializes only a missing singleton; existing edits are never overwritten. Development remains automatic; other environments retain the existing explicit database-initialization opt-in. No normal development database was reset.

## 6. Content service

IHomeProjectsSectionContentService / HomeProjectsSectionContentService follow the established focused CMS pattern. Each operation creates/disposes a short-lived context through IDbContextFactory; reads use AsNoTracking and map to the existing SectionHeading. Writes validate, update the singleton and UTC timestamp, and save. No generic CMS repository was introduced.

Public missing/unavailable/invalid content is logged and falls back to approved defaults without writing. Editor read failures are not disguised as saved default content. Cancellation propagates. Admin editor reads/writes additionally verify the current database user, security stamp and role membership, consistent with previous slices.

## 7. Editor

HomeProjectsSectionEditor has the requested Home / Projects context, Projects Section heading, Title and multiline auto-sizing Description fields, Save changes and View Home Page. It uses the same MudPaper/Grid/Stack/TextField/Button/Alert design as Services. Helper text explains that cards remain catalog-managed. No shell/global CSS redesign.

## 8. Validation

Both fields are required and reject whitespace-only values. Title is limited to 120 characters; Description to 1,000. Limits are represented in editor, edit model and EF model. Content is plain Razor-encoded text, not raw HTML. There is no CTA/URL field, so no new URL policy or allowlist is needed. Keep copy concise for the unchanged public layout; validation length is not a guarantee that every long text has identical visual geometry.

## 9. Unsaved changes

The unchanged EditorSnapshot and UnsavedChangesGuard are reused for the two values. Load/save/revert are clean; edits, invalid submit and failed writes retain dirty state. Stay preserves input; Leave discards local changes and continues navigation. Same-page and clean post-save navigation are not blocked.

View Home Page keeps the full HTTP navigation boundary and framework-native unload warning. A dirty attempt retained route/input; clean post-save navigation succeeded. The in-app browser did not expose the native beforeunload dialog, so visible native Leave/Cancel choices remain a verification limitation, as documented in Phase 4A. Browser prompt suppression/process termination limitations still apply; this is not autosave. A standalone-browser native-prompt spot check remains advisable before release.

## 10. Save flow

Successful save stays on the editor, displays “Home Projects section updated successfully.” and captures a clean snapshot. Expected failures preserve entered values, display safe feedback and log details server-side. Automated tests exercise actual component lifecycle/save methods with validation and simulated database failures; the user's data was not used for fault injection.

## 11. Public integration

Only HomeContentService's ProjectsHeading now reads fresh settings outside its static-content cache. The canonical IProjectCatalog/ProjectCatalog and JSON, five featured identities/order, ProjectCard, HomeProjectsSection, SectionHeader, ThemeCarousel, all public CSS and JavaScript are unchanged. Home still features nexconnect, payflowx, medilink, tradesync and eduvance. `/projects` still displays all seven catalog entries. Images, card copy, detail URLs, carousel/grid behavior and responsive breakpoints are not CMS controls.

## 12. Authorization

Tests confirm anonymous requests redirect to login, authenticated non-Admins are denied, and Admin can load/refresh the new route. Direct service access rejects anonymous/non-Admin and stale role/security-stamp callers. No public write endpoint was added. Home and Projects remain anonymous.

## 13. Verification

Browser QA used an isolated, ignored database at `artifacts/projects-intro-qa-af580e41/identity.db` with memory-only test credentials. The normal development database and any existing IDE session were not used/reset.

- Login, direct route, refresh, expanded Home and active Projects child checked.
- Edited title: Stay retained it; Leave navigated; returning showed unchanged stored copy.
- Empty Description produced the required-field error and still triggered the dirty dialog.
- Saved both fields via UI; public Home displayed them after refresh.
- Actually stopped/restarted the QA server against the same file: saved title/description and Admin access survived startup.
- Compared all five original non-cloned card text/image/href tuples before editing, after saving, after restart and after navigation: identical order/content/routes.
- Restored approved copy through the editor; clean sibling navigation to Services and keyboard return to Projects worked.
- Dashboard 1440/1024/768/390: no horizontal overflow. At 390px, actions/dialog fit, Tab reached Leave, Enter on Projects navigated and the drawer closed.
- Upgrade tests begin with the preceding Services migration, edited Hero/Welcome/Services values and an existing Identity account/hash/role mapping. All survive the additive migration with no model drift.
- Tests cover one-row constraint, defaults, timestamps, repeated initialization, restart, prior settings preservation, validation, encoded text, missing/invalid/unavailable fallback without writes, dirty state after save failures, and unchanged public Projects content/cards.

Final results:

| Check | Result |
| --- | --- |
| Debug solution build, isolated `bin/ProjectsIntroDebug/` | 0 warnings / 0 errors |
| Release solution build | 0 warnings / 0 errors |
| Auth/CMS harness | 404 checks passed |
| Home + Projects interop tests | 6 passed |
| Existing Home HTTP suite | Passed |
| Existing Projects HTTP suite | Passed |
| Git whitespace check | Passed |

Commands: `dotnet build NexNovaCo.sln -c Debug --no-restore -p:OutputPath=bin/ProjectsIntroDebug/`, `dotnet build NexNovaCo.sln -c Release --no-restore`, `dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build`, `node --test scripts/Test-HomeInterop.mjs scripts/Test-ProjectsInterop.mjs`, and Test-Home.ps1/Test-Projects.ps1 against the isolated QA server.

## 14. Public regression

Focused Home and Projects verification only, not full public-site regression. Home's carousel showed 1/2/3 active cards at 390/768/1440px. Pause and Next worked, with an observed active-slide change; Previous was also exercised. Home → Projects → Home remounted exactly one project-carousel root and one Next control, with all five canonical card tuples intact. The Projects listing rendered seven cards/routes. Captured console warnings/errors were empty; no visible reconnect UI or checked horizontal overflow; public Home did not load Dashboard styles. Temporary QA tab/server were closed and viewport reset.

## 15. Deferred work

No project CRUD, featured selection, ordering, image upload, other Home CMS sections, Services/Projects page CMS, Header/Footer, site settings, drafts/versioning/audit history or generic page builder. No merge/deployment.

## 16. Recommendation

After separate approval, inspect and add only the existing Home Team introductory content/CTA as the next small Home child. Keep member cards and featured selection catalog-driven. This next slice is not implemented.
