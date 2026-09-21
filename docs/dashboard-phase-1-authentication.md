# Dashboard Phase 1 — Identity, EF Core, and SQLite

Historical Phase 1 report. [Dashboard Phase 2](dashboard-phase-2-home-hero.md) now adds one Home Hero table/editor and extends the test suite; authentication/bootstrap instructions below still apply.

Scope: authentication and a protected Dashboard foundation only. No public registration, recovery, email confirmation, MFA, OAuth, CMS entities/editors, uploads, or content migration.

## Git and versions

Started from clean, fast-forwarded `main` at `4b02718` (public-site hardening merged). Work is on the single long-lived `feature/dashboard-cms` branch; subsequent Dashboard/CMS phases should continue there. No history rewrite, force push, production deployment, or main merge was performed.

- .NET SDK 10.0.401 / target `net10.0`; installed .NET/ASP.NET runtime 10.0.12.
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: exact `[10.0.12]`.
- `Microsoft.EntityFrameworkCore.Sqlite`: exact `[10.0.12]`.
- `Microsoft.EntityFrameworkCore.Design`: exact `[10.0.12]`, private development dependency.
- Local `dotnet-ef` tool: 10.0.12, pinned in `.config/dotnet-tools.json`.
- Test-only `Microsoft.AspNetCore.Mvc.Testing`: exact `[10.0.12]`.
- MudBlazor remains exact `[9.10.0]`.

Package lock files cover the application and authentication integration tests. Normal verification uses `dotnet restore NexNovaCo.sln --locked-mode`.

## Database and migrations

`Data/ApplicationUser.cs` is a minimal `IdentityUser`. `Data/ApplicationDbContext.cs` derives from `IdentityDbContext<ApplicationUser>`, supporting standard users, roles, mappings, claims, logins, and tokens. The current .NET 10 Identity schema version also includes its standard passkey table; no passkey authentication endpoints or UI are implemented.

Initial migration: **`20260921020020_InitialIdentity`**, with the generated designer/model snapshot. It contains only Identity-related schema and EF migration bookkeeping. There is no `EnsureCreated`, hand-created authentication table, or CMS entity.

Default configuration:

```json
"ConnectionStrings": {
  "IdentityConnection": "Data Source=App_Data/nexnovaco.db"
}
```

Relative SQLite paths resolve against the application's content root, not an arbitrary shell directory. For normal development this is `src/NexNovaCo.Web/App_Data/nexnovaco.db`. For published execution it is `App_Data/nexnovaco.db` under the publish content root unless overridden. Production should provide a protected persistent data location through `ConnectionStrings__IdentityConnection`, with appropriate filesystem permissions and backup arrangements.

The database lives outside `wwwroot`. `.gitignore` excludes App_Data and SQLite DB/WAL/SHM/journal files; project exclusions also keep them out of publish. Migration source stays in Git. `/App_Data/nexnovaco.db` returns 404.

### Initialization policy

- **Development:** startup runs `Database.MigrateAsync`, then idempotently creates the Admin role and optionally the configured initial Admin.
- **Other environments:** no automatic migration or seeding by default. An operator can use `dotnet ef database update` as a controlled migration step. Alternatively, a single controlled initialization run may explicitly set `Identity__InitializeDatabase=true` and supply bootstrap credentials securely. Disable that flag and remove bootstrap credentials afterward; do not run concurrent bootstrap instances.
- The normal Production app requires an initialized database before anyone can log in. Startup configuration must not be mistaken for a public provisioning endpoint.

Restore tooling and inspect/apply migrations from the repository root:

```powershell
dotnet tool restore
dotnet ef migrations list --project src/NexNovaCo.Web
dotnet ef database update --project src/NexNovaCo.Web
dotnet ef migrations has-pending-model-changes --project src/NexNovaCo.Web
```

Use the intended environment and connection configuration for administrative migration commands. Do not point development commands at a production database accidentally.

## Identity architecture and routing

This follows the installed .NET 10 Blazor Web App Individual-auth template's architectural boundaries:

- `AddIdentityCore<ApplicationUser>` + roles + EF stores + SignInManager + standard token providers.
- `AddAuthentication` uses `IdentityConstants.ApplicationScheme` and the template's `ExternalScheme` sign-in default, with one `AddIdentityCookies` registration. Framework support cookies do not expose external-login or MFA functionality; no OAuth handlers or account APIs were added.
- `AddAuthorization` has **no global fallback requirement**. Public routes remain anonymous.
- Cascading authentication state and a server-side `IdentityRevalidatingAuthenticationStateProvider` are registered. Each revalidation uses a fresh DI scope/DbContext. It checks security stamps and detects removal of the Admin role every minute. Cookie security-stamp validation also uses a one-minute interval.
- Authentication and authorization middleware run before antiforgery middleware.
- `AuthorizeRouteView` protects interactive navigation, while endpoint authorization protects direct HTTP requests/prerendering. Anonymous interactive challenges force a full navigation to login rather than revealing protected content.
- `App.razor` uses `HttpContext.AcceptsInteractiveRouting()` to choose the template's per-request render boundary. Account pages are excluded from interactive routing; Dashboard/public pages remain Interactive Server.

Reference: [Microsoft Blazor authentication/authorization guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0). The installed SDK's generated Individual-auth template was inspected locally; its unrelated registration/recovery/passkey screens were not copied into the app.

## Admin bootstrap — configure your own credentials

`IdentityDatabaseInitializer` reads only configuration keys `AdminUser:Email` and `AdminUser:Password`. There is no default email/password. The Admin role is created even if credentials are absent. Missing credentials produce a safe Development warning and **no default user**.

If configured and missing, the user is created through `UserManager.CreateAsync` and added through `AddToRoleAsync`. An existing Admin's password is never reset by startup. Promoting an existing non-Admin account additionally requires the configured password to match that account, preventing accidental privilege assignment to a mismatched account. Bootstrap failures report only Identity error codes, never submitted passwords or hashes.

The local development database created during verification currently has the Admin role but no user because no personal bootstrap credentials were supplied. Integration tests verified successful creation with generated, runtime-only credentials in separate temporary databases. **Configure your own account before trying a valid login in the normal app.**

### Recommended: .NET User Secrets (Development only)

The project already has a UserSecretsId. From the repository root, this PowerShell sequence avoids putting the password in shell history or command-line arguments:

```powershell
$bootstrapEmail = Read-Host 'Initial admin email'
$bootstrapPassword = Read-Host 'Initial admin password (12+ characters)' -AsSecureString
$bootstrapCredential = [PSCredential]::new($bootstrapEmail, $bootstrapPassword)
@{
  'AdminUser:Email' = $bootstrapEmail
  'AdminUser:Password' = $bootstrapCredential.GetNetworkCredential().Password
} | ConvertTo-Json -Compress | dotnet user-secrets set --project src/NexNovaCo.Web
Remove-Variable bootstrapEmail, bootstrapPassword, bootstrapCredential
dotnet run --project src/NexNovaCo.Web --launch-profile http
```

Open `http://localhost:5138/admin/login`. User Secrets are stored outside Git but are not an encrypted production vault. Do not print/list secrets in shared logs, commit them, or place them in appsettings/README files.

After the account has been created, remove bootstrap values without deleting the account:

```powershell
dotnet user-secrets remove 'AdminUser:Password' --project src/NexNovaCo.Web
dotnet user-secrets remove 'AdminUser:Email' --project src/NexNovaCo.Web
```

### Alternative: process environment variables

```powershell
$env:AdminUser__Email = Read-Host 'Initial admin email'
$bootstrapPassword = Read-Host 'Initial admin password (12+ characters)' -AsSecureString
$env:AdminUser__Password = [Net.NetworkCredential]::new('', $bootstrapPassword).Password
try {
  dotnet run --project src/NexNovaCo.Web --launch-profile http
}
finally {
  Remove-Item Env:AdminUser__Password, Env:AdminUser__Email
  Remove-Variable bootstrapPassword
}
```

These variables affect the current process/children, not a committed `.env` file. For a non-Development bootstrap, use the deployment's secure configuration mechanism and the explicit initialization policy above. Changing the bootstrap secret does not rotate an existing Admin password; deliberate Identity account management is required. Password recovery UI is intentionally absent.

## Login and safe redirects

Route: **`/admin/login`**. The account folder defaults to Admin authorization and static SSR; this login page explicitly allows anonymous access.

The UI is a centered MudPaper/MudText/MudButton panel with labeled SSR `InputText` email/password fields. A normal `EditForm method="post"` includes antiforgery and server-side data-annotation validation. `PasswordSignInAsync` writes the Identity cookie on the HTTP response, never from an established SignalR circuit. Failed submissions clear the password field and show the same generic error for unknown accounts, wrong passwords, lockout, and disallowed sign-in.

Successful authentication redirects to `/dashboard`; an existing Admin opening login also redirects there. Challenge URLs carry the requested return path, but because this phase has exactly one Dashboard page, successful login deliberately uses the fixed local `/dashboard` destination and does not trust supplied return URLs. No registration links/endpoints exist.

Valid credentials for a non-Admin can authenticate, but they cannot enter the Dashboard: role authorization redirects to the generic `/admin/access-denied` page. That page offers POST logout so the user can leave the nonprivileged session.

## Protecting all Dashboard pages

All new Dashboard pages must live under `Components/Pages/Dashboard/`. Its `_Imports.razor` applies:

```razor
@layout DashboardLayout
@attribute [Authorize(Roles = IdentityDatabaseInitializer.AdminRole)]
```

The account page folder has the same Admin default plus `ExcludeFromInteractiveRouting`. Only login/access-denied deliberately use `AllowAnonymous`. Logout is a separate authenticated POST endpoint, available to non-Admins too so they can sign out.

The integration suite reflects every routable `/dashboard...` and `/admin...` component and fails if a future protected page lacks Admin authorization or adds `AllowAnonymous` outside the two explicit exceptions. Thus a misplaced future page is caught by tests, not merely hidden in navigation. Do not treat layout/nav visibility as authorization. Future data services/endpoints must enforce their own appropriate policies too.

## Dashboard layout

`DashboardLayout` is separate from public `MainLayout`. It contains one set of Mud providers, MudLayout, MudAppBar, current identity, POST logout, one Dashboard nav item, and a main content region. `/dashboard` shows a MudText title and MudPaper welcome panel—no fake CMS menus or editor cards.

`AdminAccountLayout` hosts static account UI with a MudThemeProvider. `admin.css` is loaded only for admin/dashboard requests, without the public theme or carousel vendors. Public theme CSS itself is unchanged. Links crossing back to the public site force normal HTTP navigation so the correct resource/layout boundary is rebuilt; preserve this convention when adding cross-area links.

## Logout and session security

`/admin/logout` accepts **POST only**, requires an authenticated Identity principal, validates antiforgery, calls `SignInManager.SignOutAsync`, and redirects locally to `/admin/login`. The button submits a real HTTP form with `AntiforgeryToken`; it does not clear only client state. Missing tokens fail with 400, and GET does not sign out. The browser's subsequent `/dashboard` request is challenged again.

Cookie policy: host-only `NexNovaCo.Identity`, HttpOnly, SameSite Lax, nonpersistent browser-session cookie, 30-minute ticket lifetime with sliding expiration. Secure is mandatory outside Development; Development follows the request scheme to support the existing loopback HTTP profile. Antiforgery cookies also require Secure outside Development. Password policy keeps standard uppercase/lowercase/digit/non-alphanumeric requirements, with length >=12 and >=4 distinct characters. Unique email is required; five failures cause a 15-minute lockout.

Standard cookie logout clears the current browser session; it is not a custom server-side ticket blacklist or immediate all-device revocation system. Existing other circuits are governed by security-stamp/role revalidation; invalidate security stamps for deliberate all-session revocation. Do not assume hiding a route or signing out one tab instantly removes data already rendered in another tab.

Production needs the earlier hardening pass's TLS, trusted forwarded headers, allowed hosts, WebSockets, persistent protected data-protection keys, and protected persistent database storage. The old Phase 9 loopback **HTTP-only Production** smoke command is no longer suitable for account/form flows because Production now requires Secure antiforgery/auth cookies. Use the Development loopback profile for local UI checks or properly configured HTTPS for Production checks. No certificates/proxy/cloud settings were hardcoded.

## Verification

Commands:

```powershell
dotnet restore NexNovaCo.sln --locked-mode
dotnet build NexNovaCo.sln -c Debug --no-restore
dotnet build NexNovaCo.sln -c Release --no-restore
dotnet run --project tests/NexNovaCo.Auth.Tests --no-restore
dotnet ef migrations has-pending-model-changes --project src/NexNovaCo.Web --no-build
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore
```

- Debug/Release: 0 warnings, 0 errors; locked restore passes; migration matches the runtime model.
- Release publish succeeds; an output audit confirms no database/sidecar files, App_Data, development settings, symbols, secrets files, or test assemblies are shipped.
- **78 integration checks pass** using the real Identity/EF/HTTP pipeline, cookies, and isolated SQLite databases. No fake authentication handler is used. Credentials are generated in memory and are not printed or stored in source/configuration files.
- Checks cover anonymous public pages, login redirect/no protected-content flash, static login versus interactive Dashboard markers, invalid/unknown login, password non-echo, valid Admin login, direct/refresh requests, existing-Admin login redirect, malicious return URL, POST/GET/antiforgery logout, non-Admin denial, lockout, unique email/password policy, migrations/schema, hash verification without disclosure, idempotent/non-resetting bootstrap, missing credentials, role/stamp circuit revalidation, and Production Secure cookies/diagnostics guard.
- Database verification confirms Admin user/role mapping and Identity password hashing in configured test databases. The normal local database initializes via migrations; without credentials it safely contains no default user. SQLite runtime files are ignored and not web-served.
- Browser: anonymous `/dashboard` reaches the login page; invalid login displays a generic error and clears the password. Public Home/Services/Projects/Contact remain anonymous, retain one header/footer and the correct public styling, have no broken images or console errors, and retain mobile-menu/Escape and server-side FAQ behavior. Valid authentication/logout/role cases are covered by HTTP integration tests, not claimed as a personal-account browser login.
- Existing JS lifecycle suite: 20 pass. Contact model/service suite: 21 pass.
- No historical multi-breakpoint public visual regression was rerun. Only admin CSS was added; public CSS/artwork/content stayed unchanged.

## Deferred work and next step

No editable Services/Projects/Team records, CMS CRUD, settings editor, uploads/media library, database content migration, public registration, forgot-password/email/MFA/social workflows, roles management UI, audit log, newsletter, or real Contact delivery was added.

Next: review this secure foundation and plan the first narrowly scoped CMS content model/editor, including server-side authorization and validation at its data boundary. Do not start content-editing implementation without approval. Existing production-launch debt from Phase 9 remains separate from completing this development foundation.
