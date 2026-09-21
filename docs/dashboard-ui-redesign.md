# Dashboard UI redesign — MudBlazor 9.10.0

## Scope and Git

Continued on clean `feature/dashboard-cms` from `1be5257`. This is a Dashboard/Admin presentation pass only. No packages, Identity configuration, authorization policies, database models/migrations, content services, validation models, public components, or public theme files changed.

## Shell and components

`DashboardLayout.razor` owns the fixed `MudAppBar`, responsive `MudDrawer`, `MudNavMenu`, `MudMainContent`, semantic main landmark, skip link, and constrained `MudContainer`. A small `DashboardUserMenu.razor` keeps the account interaction separate. AppBar and drawer stay in the layout rather than being split unnecessarily.

The left drawer has the existing NexNovaCo mark/name, a small Content Management label, Dashboard, and Content → Home Hero. There are no placeholder destinations. The active route has a blue-tinted background and stronger text/icon treatment.

The drawer uses MudBlazor's `DrawerVariant.Responsive` and `Breakpoint.Md` (960px): 252px persistent sidebar on desktop, closed-by-default overlay below that breakpoint. MudBlazor handles resizing and closing on route navigation. The hamburger can also collapse the desktop drawer. Closed navigation is inert. A `MudFocusTrap` applies only to the open compact drawer; Escape closes it and returns focus to the toggle. No custom drawer JavaScript was added.

The lightweight white AppBar displays NexNovaCo/current-page context, a desktop/tablet View website action, and the account avatar menu. The redundant top-bar website action hides below 600px; it remains available in the menu.

## User menu and secure logout

The avatar displays the first character of the authenticated identity, with no profile-image system. The menu shows the full identity, Administrator label, separator, View website, and Log out. Long identities wrap within the bounded menu width.

`MudAvatar` visually overlays MudMenu's native button activator; the decorative avatar ignores pointer events and is hidden from assistive technology. The native button has the accessible name **Open administrator menu**. Using the native activator preserves MudBlazor's Enter/arrow-key/Escape behavior and focus restoration without nested buttons or a custom keyboard implementation.

All View website/Home actions force a full HTTP navigation so the public resource/theme boundary is rebuilt. The underlying public theme stays unchanged.

Logout retains the existing `/admin/logout` **native POST form with AntiforgeryToken**. MudMenuItem renders a div rather than a submit button, so the tiny, lazily imported `dashboard-ui.js` helper calls only `form.requestSubmit()`. It does not fetch an auth endpoint, read/write cookies, change auth state, store credentials, or implement custom authentication. The existing server endpoint still validates antiforgery, invokes Identity sign-out, and redirects to login. The HTTP integration tests verify the native form and rejection of missing tokens/GET logout; the browser verifies actual menu-triggered logout.

## Dashboard Home and editor

Home has a page heading, welcome using the real signed-in identity, Admin chip, a single real Home Hero quick-access card, and a small public-site review action. No analytics, visitor counts, fake charts, or future CMS menu items were added.

The existing editor uses one outlined `MudPaper` with two semantic sections:

- **Headline & introduction:** the three existing title fields and description textarea.
- **Call to action:** existing CTA text and route.

`MudGrid` keeps the title fields in three columns from the large breakpoint and stacks them below it. CTA fields use two columns from the small breakpoint, otherwise one. `MudStack` uses `Breakpoint.Xs` for full-width stacked actions on phones and a row above 600px. Save changes and View Home Page share a clearly separated action region. Existing alerts, disabled-while-saving behavior, edit model, validation attributes, service calls, exception handling, and success wording are unchanged.

## Theme and styling

`DashboardTheme.Light` is supplied only by the Dashboard and static Admin account layouts. It uses white surfaces, a light neutral background, dark readable text, a restrained blue/cyan primary, modest 12px radii, deliberate typography, and no dark-mode toggle or system dark-mode switching.

Small additions to the already admin-only `admin.css` handle brand alignment, active nav, menu/avatar geometry, spacing, focus/skip-link treatment, and the same standard Mud breakpoints. Selectors are scoped under `.admin-site`; no new `!important` declarations, public CSS edits, or parallel CSS framework were added. Muted text, primary text/buttons, and validation colors were reviewed for contrast; the primary and error shades are deliberately darker than the initial mockup/default error red.

## Responsive and browser verification

Both Dashboard and Home Hero were inspected at the requested widths:

| Width | Drawer/content | Editor/actions/menu |
| --- | --- | --- |
| 1440 | Persistent 252px drawer; main starts beside it and below AppBar | Three title columns; paired CTA fields; row actions; menu within viewport |
| 1024 | Persistent drawer; correctly aligned main container | Stacked title fields; paired CTA fields; row actions; menu within viewport |
| 768 | Closed overlay drawer; full-width main; selecting a route closes drawer | Stacked titles; paired CTA fields; row actions; menu within viewport |
| 390 | Closed overlay drawer; compact AppBar; no permanent width loss | Single-column fields and full-width stacked buttons; avatar menu fits |

No horizontal overflow was observed on either Dashboard page at these widths. Keyboard checks covered avatar Enter, menu arrow navigation, Escape with focus returned to the trigger, drawer focus entry/wrapping, Escape back to hamburger, and hidden drawer inertness. Responsive reflow was verified at narrow viewports; no separate browser-zoom automation or exhaustive accessibility audit is claimed.

Tested normal login → mobile drawer/editor → unsafe CTA rejected → valid text edit → Save confirmation → public Home reflects edit → refresh preserves it → restore approved copy → menu View website → secure menu logout → protected Dashboard challenges again. Test content was confined to an isolated QA SQLite database, with approved values restored. The user's normal database and running Visual Studio session were not modified/stopped.

Local screenshots are kept out of Git under `artifacts/dashboard-ui-redesign/` (Dashboard/editor at all four widths, mobile actions, and avatar menu). Deliberate QA server rebuilds can generate connection errors in the old test tab; final runtime logs are checked after the last restart rather than treating those intentional interruptions as regressions.

## Automated verification

```powershell
dotnet build NexNovaCo.sln -c Release --no-restore
dotnet build NexNovaCo.sln -c Debug --no-restore -p:OutputPath=bin/DashboardUiDebug/
dotnet run --project tests/NexNovaCo.Auth.Tests -c Release --no-build --no-restore
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore
```

- Release and isolated Debug: zero warnings/errors. Debug uses a separate output folder to avoid the existing Visual Studio file locks.
- **138 authentication/CMS checks**: anonymous redirect, valid Admin, non-Admin denial, login/logout/antiforgery/lockout, role/stamp revocation, migrations/bootstrap, CMS validation/persistence/fallback, plus shell/native avatar/native logout form assertions.
- **21 JavaScript checks**, including the new presentation-only logout-form bridge test.
- **21 Contact model/service checks**.
- No migration required; models, migration sources, service/validation logic, and Identity startup/endpoint code have no changes in this pass.

## Deferred work

No new CMS editors, uploads/media library, analytics, notifications, search, settings/profile/user/role-management pages, dark mode, database schema, or public-site redesign. Further functionality requires separate approval.
