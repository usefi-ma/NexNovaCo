# Dashboard Phase 16 — Global Site Settings

## Git

Continued on `feature/dashboard-cms` from `ce49b81`. The working tree was clean before editing. No branch switch, history rewrite, push or merge. Changes are grouped into global data/services, Dashboard/public-shell integration, and focused verification/report commits.

Implementation commits: `69c5deb` (data/services) and `776fc43` (editors/public shell). The verification commit contains this report and the focused tests.

The developer's normal SQLite database and uploaded files were not modified or reset. Browser QA used only `artifacts/global-settings-qa-d16105f01d954a9790c213bf5eeb0b22`; automated tests create separate temporary database/media/key directories. The temporary QA server was stopped and browser viewport override reset.

## Global Settings information architecture

| Page | Route |
| --- | --- |
| Site Identity | `/dashboard/settings/site` |
| Navigation | `/dashboard/settings/navigation` |
| Footer | `/dashboard/settings/footer` |
| Social Links | `/dashboard/settings/social` |
| Contact Info | `/dashboard/settings/contact` |

`/dashboard/settings` redirects to Site Identity. Navigation/Social editors use `/new` and `/{Id:int}`. The top-level Global Settings sidebar group expands on direct navigation; list links remain active on Add/Edit. All routes inherit the existing Admin-only Dashboard layout/authorization.

There is no separate Header page: inspection found no meaningful Header-only content beyond identity and navigation.

## Ownership map

| Existing public content | Canonical owner | Consumers / decision |
| --- | --- | --- |
| Company name and primary logo | SiteIdentitySettings | Header and Footer share one loaded identity. No alternate/distinct Footer logo exists. |
| Home, About, Services, Projects, Team, Contact | NavigationItems | One ordered collection for desktop, mobile and Footer. No FooterLink collection. |
| Footer description and copyright | FooterSettings | Footer only. These are authored text, not automatic rewrites of every brand mention. |
| Newsletter heading, placeholder, button label and visibility | FooterSettings | Existing presentation only; fixed unavailable notice and disabled controls. |
| Email / LinkedIn / Telegram icon order and profiles | SocialLinkItems | Global Footer icons only; member-specific SocialLinks remains unchanged. |
| Phone, email, address | Existing SiteContactSettings | Contact page plus Footer email icon. No duplicate contact table or copied email URL. |
| Contact page heading | Existing ContactPageSettings | Remains page-specific; excluded from Global Contact Info. |

Inspected SiteHeader, PrimaryNavigation, MobileNavigation, SiteFooter, SocialLinks, MainLayout, shared shell JS/CSS, Contact service/settings and media upload foundation before implementation. Public page metadata, authored section copy, and Dashboard product branding are outside this global-shell ownership change; no SEO editor was introduced.

## Data models / migration

Additive migration: `20260923220737_AddGlobalSiteSettings`.

Six new tables:

- `SiteIdentitySettings`: singleton name/logo, update timestamp.
- `FooterSettings`: singleton description/copyright/newsletter presentation, update timestamp.
- `NavigationItems`: ID, display order, label, internal route, update timestamp.
- `NavigationInitializationState`: persistent singleton marker.
- `SocialLinkItems`: ID, display order, supported platform, optional HTTPS URL, update timestamp.
- `SocialLinkInitializationState`: persistent singleton marker.

Order indexes, positive-order checks, singleton checks and a unique social-platform index are included. No existing table/column is altered or dropped by Up. Existing SiteContactSettings is reused without schema changes. No HeaderSettings, FooterLink, alternate-logo or redundant contact table.

Services use short-lived factory-created contexts: GlobalSettingsService, NavigationContentService and SocialLinkContentService; ContactPageCmsService gains global-contact read-for-edit/save methods.

## Initialization

GlobalSiteInitializer initializes missing identity/Footer singletons without overwriting Admin changes. Each ordered collection has a marker saved transactionally with its initial approved rows. A missing marker with existing rows adopts those rows without duplicating or overwriting them.

Fresh navigation is exactly Home → About → Services → Projects → Team → Contact. Home is stored as the canonical root `/` and rendered with the original empty href/exact active match. Other approved labels/routes/order are preserved.

Fresh global icons are Email → LinkedIn → Telegram. The existing LinkedIn/Telegram icons had no destinations; no profile URLs were invented.

Delete-one, delete-all and add-after-empty survive host restarts. Empty collections are successful reads, not fallback triggers. Neither rendering nor fallback writes seed data.

## Site Identity

Required site name (80-character maximum) and validated logo selection/upload. The existing media field is reused, with Save Changes and View Website. MainLayout reads the identity and passes the same instance to Header/Footer. Both logo alt text and brand-home accessible label use the canonical name.

The original `image/logo.png` remains the bundled selection and missing-file fallback, and is never deleted by this workflow.

## Navigation

List, Add, Edit, confirmed Delete and immediate Move Up/Down persist to SQLite. Orders are normalized transactionally; stale/incomplete/duplicate reorder sets are rejected. After creation the editor replaces `/new` with its ID route to prevent accidental duplicate creation on refresh.

Labels are required, maximum 40 characters. Routes are required, maximum 200 characters, allowlisted to the existing public routes and project/member detail slug patterns. External links are not added because the existing primary navigation supports only internal routes. Unsafe schemes, protocol-relative URLs, traversal, encoded traversal, query tricks, malformed values and Admin/Dashboard routes are rejected.

## Header

SiteHeader consumes canonical identity and the shared navigation collection. PrimaryNavigation loops over that collection; MobileNavigation remains the existing presentation toggle, not another data source. Exact Home active matching and prefix matching for Projects/Team details remain.

Public HTML structure/classes, sticky behavior, typography, responsive breakpoints, menu close-on-navigation and Escape/focus behavior are retained. Header was not converted to MudBlazor. No public stylesheet or shell JS changes were needed.

When navigation is intentionally empty, brand/toggle remain safe and the empty menu still closes normally.

## Footer

Description, copyright and the actual newsletter text are editable. The primary navigation is reused directly; there is no duplicated Footer menu. Name/logo use Site Identity. Social rendering uses the ordered global collection, and its email item resolves the existing shared contact email.

Newsletter visibility can hide its existing column. When visible, the fixed “Newsletter signup is not available yet.” notice, disabled input/button and unavailable accessibility labels remain. No form submission, success message, provider, API or subscription storage was added.

Existing columns, responsive stacking, icon geometry and BackToTop remain intact.

## Social Links

Supported platforms are limited to the existing Email, LinkedIn and Telegram SVG/icon set. Each platform can occur once. CRUD, confirmed Delete and Move Up/Down are available.

Email has no separate destination input and always uses Global Settings → Contact Info. Non-empty LinkedIn/Telegram destinations must be HTTPS, without credentials, custom ports, unsafe schemes, whitespace/control characters or backslashes. An empty URL preserves the existing decorative, non-focusable icon; clearing an input normalizes null to an empty stored string.

Deleting all rows hides the entire global icon wrapper. Member cards/details continue to use the untouched member-specific SocialLinks component and data.

## Contact Info

Global Settings edits the existing SiteContactSettings singleton through the existing ContactPageCmsService. Phone/email/address are shared with Contact; Footer's Email social item uses that same email. No page heading was promoted to global data, and the Contact-specific editor remains compatible.

Browser and automated checks verified edits reach Contact and Footer and do not alter ContactPageSettings.InfoHeading.

## Media

MediaKind.SiteLogo extends the existing controlled media policy with `uploads/site/{generated-id}.jpg|png|webp`, maximum 3 MB. The existing authenticated upload/signature-validation service, media storage configuration, image preview, upload retry and bundled selection are reused. No upload endpoint or new storage system.

Actual browser file-chooser upload was verified; the same generated path appeared in Header and Footer and survived server restart. Automated checks cover logo-only dirty tracking, failed save/retry, PNG response, nonexistent/cross-folder paths and non-mutating fallback. Original asset remains untouched.

## Unsaved changes / validation

All three singleton editors and both Add/Edit link forms reuse UnsavedChangesGuard, NavigationLock and EditorSnapshot. Pending image selections participate in the same dirty state. Failed saves preserve input; successful persistence clears the snapshot. Browser Stay/cancel navigation and automated save-failure tests cover these forms.

Field limits: site name 80, navigation label 40/route 200, Footer description 1000/copyright 200, newsletter heading 120/placeholder 160/button label 60, social URL 500, contact phone 80/email 254/address 500. Required fields, email format, platform and URL/media policy are enforced server-side as well as by forms. All content is plain Razor-encoded text, never raw HTML.

## Authorization

Anonymous Global Settings routes redirect to login; authenticated non-Admins are denied; Admin direct navigation/refresh succeeds. Every editing/list/mutation service method also verifies authenticated Admin claims, current database role membership and matching security stamp. Tests include revoked role and stale-stamp rejection.

No public mutation endpoints. Existing media authorization is retained.

## Verification

Final focused commands:

```powershell
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --global-settings
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --contact-page
dotnet build NexNovaCo.sln --no-restore -c Debug -p:OutputPath=bin/GlobalDebug/net10.0/
dotnet tests/NexNovaCo.Auth.Tests/bin/GlobalDebug/net10.0/NexNovaCo.Auth.Tests.dll --global-settings
dotnet tests/NexNovaCo.Auth.Tests/bin/GlobalDebug/net10.0/NexNovaCo.Auth.Tests.dll --contact-page
dotnet run --project tests/NexNovaCo.Contact.Tests -c Release --no-restore
node --test scripts/Test-ContactInterop.mjs
```

- Debug and Release: zero warnings and zero errors.
- Global Settings: **289 focused checks passed in each configuration**.
- Existing Contact CMS: **221 checks passed in each configuration**.
- Existing Contact model/service: **21 passed**; Contact interop: **2 passed**.
- Migration upgrade preserves every prior table's data/timestamps; runtime model has no pending changes.
- Singleton init, exact fresh defaults, repeated startup, collection CRUD/reorder, delete-one/delete-all, add-after-empty and adoption of pre-existing rows verified.
- Brand/Footer/contact persistence, upload retry, empty public rendering, safe encoded text, invalid stored URL fallback, missing-table fallback/logging and no fallback writes verified.
- Prior Identity/CMS rows/timestamps preserved apart from deliberately edited canonical contact. Representative earlier CMS editors/shared managers return successfully.
- Historical phase migration fixtures now reinitialize the new global slice when rebuilding disposable schemas; those full historical suites were not run.

Browser testing covered all five pages plus Add/Edit routes at **1440, 1024, 768 and 390**: correct active sidebar, compact drawer close behavior, readable responsive forms/tables and no horizontal document overflow.

End-to-end browser edits/save/public verification covered brand/logo, Footer/newsletter hide/show, contact email, navigation CRUD/reorder and social CRUD/reorder/unsafe URL rejection. A real QA-server restart preserved the edited global data and uploaded logo. Approved values were restored for the visual matrix. The disposable fixture was subsequently used for intentional delete-all testing; its empty collections are not production/developer data.

Browser errors during the intentional server shutdown were expected connection failures. A fresh browser session after restart had no warning/error logs during public navigation and empty-state tests. Interactive Server menu/form actions worked; no visible Blazor error state.

## Public shell regression

Representative routes: `/`, `/about`, `/services`, `/projects`, `/projects/nexconnect`, `/team`, `/contact`.

| Width | Result |
| --- | --- |
| 1440 | Desktop menu, exact active parent links, shared brand/logo, sticky Header, original Footer columns and disabled newsletter preserved. |
| 768 | Mobile menu open/close, same collection/order, active states, Footer stacking and icons preserved. |
| 390 | Phone menu, Escape/focus restoration, readable Footer wrapping, original logo/name, disabled newsletter preserved. |

All 21 route/width combinations passed shell checks with no settled horizontal document overflow. Header/Footer links matched exactly, logo images loaded, global icons were in approved order, and the Footer email used canonical contact data. Screenshots were visually inspected at each width.

Repeated internal navigation among Home/About/Services/Projects/Team/Contact kept the menu functional and closed it after navigation. Empty navigation/social browser checks retained the brand, rendered no links/social wrapper, preserved Escape return focus and produced no console errors.

## Deferred work

No newsletter delivery/backend/storage, contact delivery, SEO/favicon/theme/font editor, nested menus, public role-based navigation, role management, drafts/versioning, audit logs, localization or page builder. No unrelated public redesign/CSS cleanup. Existing third-party Contact map delivery is outside this phase and remains the previously documented deployment/manual check.

## Readiness

The requested major global CMS area is implemented and focused verification is complete. **Ready for the separately approved final comprehensive Dashboard/CMS QA and merge review**, not a claim that comprehensive QA or production deployment review has already passed.

Final comprehensive QA has not been started. No push/merge or next phase is authorized or performed.
