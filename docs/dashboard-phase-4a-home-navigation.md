# Dashboard Phase 4A — Home sidebar subpages

Verified 2026-09-22. Scope: navigation and unsaved-edit protection only.

## 1. Git

Work started on `feature/dashboard-cms` at `6ef0746` with a clean working tree. No branch switch, history rewrite, merge, or push. This report is included in the focused implementation commit; the delivery message records its hash and final working-tree state.

## 2. Sidebar structure

`DashboardLayout.razor` now uses one `MudNavGroup` for Home and exact-match `MudNavLink` children for Hero and Welcome. No future destinations are shown. Entering either Home route expands the group and highlights its child. The user can collapse/expand it manually; the next Home route change expands it again. The existing responsive drawer, focus trap, theme, and public-site navigation boundary are retained. Dashboard's Edit Home shortcut targets canonical Hero.

## 3. Routes

| Route | Authorized Admin result |
| --- | --- |
| `/dashboard/content/home/hero` | Hero editor only; direct load and refresh work |
| `/dashboard/content/home/welcome` | Welcome editor only; direct load and refresh work |
| `/dashboard/content/home` | Redirect to canonical Hero |
| `/dashboard/home/hero` | Redirect to canonical Hero |

The two aliases share the small `HomeContentEditor` redirect component, using history replacement. Direct authorized HTTP requests return 302 to canonical Hero. There was no independent legacy Welcome route to redirect. All four routes inherit the same Dashboard layout and Admin authorization from the folder's `_Imports.razor`.

## 4. Tab removal

Removed the Hero/Welcome `MudTabs` container. There is no second primary navigation system and no extra Home landing page. Titles, H1s, descriptive text, and AppBar context distinguish Home / Hero from Home / Welcome. Editor section headings are H2s.

## 5. Editor reuse

The existing `HomeHeroEditor` and `HomeWelcomeEditor` are the canonical page components; no wrapper duplicates the forms. Existing MudPaper/Grid/Stack/field/button layouts, validation, loading, save calls, errors, and success feedback are retained. Hero's obsolete embedded-mode switch is removed. Models, services, database entities, initialization, singleton behavior, public components, and CSS are unchanged. No migration was added.

## 6. Unsaved changes

Each editor explicitly lists its six editable string values. A shared UI-only `EditorSnapshot` copies these values after a successful load or save and compares them ordinally. Initial/reloaded/saved forms are clean; editing any field is dirty; restoring all saved values is clean. Validation and persistence errors do not capture a new snapshot and retain input/dirty state. No reset button or draft storage was introduced.

`UnsavedChangesGuard` uses framework `NavigationLock`:

- Dirty Dashboard navigation opens the shared MudBlazor dialog: “You have unsaved changes. Leave without saving?” with Stay / Leave. Stay is the first focused action. Escape cancels; backdrop clicks do not confirm leaving.
- Stay retains the route and input. Leave continues the requested navigation without writing to the database. A superseded navigation cancels its previous dialog; component disposal closes it.
- Same-path navigation is not blocked. Clean post-save navigation proceeds normally.
- Existing View Home Page, AppBar View website, and account-menu public actions intentionally use a full HTTP load to maintain the public/Dashboard stylesheet boundary. These use `ConfirmExternalNavigation`, the standard browser-native unload warning, rather than showing both custom and native prompts. Refresh/close and full-load logout share that browser protection. Future public links must retain this full-load boundary; do not introduce enhanced internal public links without reviewing the guard.

Browser limitations: browsers control native prompt text/buttons, generally require prior interaction, and may suppress unload prompts (notably mobile process termination). This is not an autosave guarantee. See [Microsoft's .NET 10 NavigationLock documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0#navigationlock-component).

Verification limitation: the in-app browser did not expose a native beforeunload dialog through its dialog API. Dirty View Home Page/AppBar attempts and a dirty reload retained the current route and input, while clean public navigation succeeded. The native prompt's visible presentation and its Leave/Cancel choices, tab-close behavior, and browser Back/Forward were **not independently verified** in a standalone browser. The custom internal Stay/Leave workflow was exercised for both editors. A normal-browser native-prompt spot check remains advisable before release; no custom unload JavaScript was added.

## 7. Responsive behavior

| Width | Verification |
| --- | --- |
| 1440 | Hero and Welcome, expanded Home group, active child, AppBar, internal Stay/Leave, no horizontal overflow |
| 1024 | Persistent sidebar, both editor layouts, fitted confirmation dialog, no horizontal overflow |
| 768 | Overlay drawer, both editors, active child; choosing sibling closes drawer; dialog keyboard focus reaches Leave; no horizontal overflow |
| 390 | Stacked editor fields/actions, compact AppBar, drawer closes on child navigation; dialog fits; invalid CTA remains protected; no horizontal overflow |

Keyboard Enter collapses/expands Home; Tab reaches the dialog actions. At 390px, an invalid Welcome CTA produced its existing validation error; Stay preserved it. Reverting to the saved CTA allowed clean sibling navigation without a dialog. Direct clean refresh of both canonical routes succeeded in the browser. Local screenshots are under ignored `artifacts/home-cms-navigation/` (not tracked). The temporary viewport override was reset and the QA tab/server were closed.

## 8. Authorization

Automated checks cover all four routes: anonymous requests redirect to login; an authenticated non-Admin redirects to access denied; Admin receives the correct editor or compatibility redirect. Direct refresh is also checked. Existing Identity, role checks, static login/logout, and antiforgery behavior are unchanged.

## 9. CMS regression and builds

Browser QA used an isolated SQLite database under ignored `artifacts/home-nav-qa-92addc8c/`, not the user's normal development database. Both editors were modified, canceled with Stay, left without saving, and revisited to confirm the original database value. Both were then saved through the actual form: navigation was clean and public Home displayed the saved values after refresh. Approved original values were restored through the same UI.

Automated editor checks invoke the actual component lifecycle/save methods with test services: all fields participate in the snapshot; revert/load/save become clean; invalid-form submission, service validation exceptions, and simulated database failures retain dirty state and input. Existing real SQLite tests continue to verify persistence, validation, singleton initialization, and public fallback behavior.

Final commands/results:

```text
dotnet build NexNovaCo.sln -c Debug --no-restore -p:OutputPath=bin/HomeNavDebug/
  PASS: 0 warnings, 0 errors (isolated output avoids an existing IDE build)
dotnet build NexNovaCo.sln -c Release --no-restore
  PASS: 0 warnings, 0 errors
dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build
  PASS: 274 auth/CMS checks
node --test scripts/Test-HomeInterop.mjs
  PASS: 3 Home interop lifecycle checks
git diff --check
  PASS
```

## 10. Public regression

Focused Home smoke only, not full public-site regression. Hero/Welcome still render the SQLite-backed saved values and approved restored copy. Public Home remains anonymous via existing tests. Final browser smoke found no horizontal overflow, no Dashboard stylesheet on Home, no visible reconnect UI, and no captured console warnings/errors. Interactive form saves/navigation succeeded throughout the session. No public markup, CSS, assets, interop, or content-service implementation changed.

## 11. Deferred work

No Home Services CMS, other content sections, entities/migrations, uploads/media library, Header/Footer CMS, drafts/versioning, navigation engine, breadcrumb framework, role changes, or Dashboard/public redesign. No merge or deployment.

## 12. Recommendation

After approval, add Home Services introductory text as the next small Home sidebar child, preserving the established editor, validation, and unsaved-edit pattern. Do not implement that next phase as part of this navigation refactor.
