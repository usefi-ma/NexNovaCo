# Dashboard Phase 15 — Complete Contact Page CMS

## Git

Continued on `feature/dashboard-cms` from clean commit `7b251e8`. Implementation commit: `afb2675` — Add complete Contact page CMS and canonical site contact settings. Focused verification/report follow in a separate commit. No branch switch, history rewrite, push or merge. The developer's normal database was not reset or migrated by QA; all runtime testing used isolated databases.

## Contact Dashboard IA

Content → Contact uses the existing MudNavGroup/MudNavLink pattern:

- Hero: `/dashboard/content/contact/hero`
- Contact Info: `/dashboard/content/contact/info`
- Form: `/dashboard/content/contact/form`
- Map: `/dashboard/content/contact/map`

`/dashboard/content/contact` redirects to Hero. Contact routes expand the group and highlight the active child. Keyboard Enter toggles the group. The compact drawer closes after child navigation. All editors retain the existing dashboard layout, MudPaper, Save changes and View Contact Page actions.

## Actual section inventory

Inspected Contact.razor, ContactInfoPanel, ContactForm, ContactMap, InnerPageHero, typed ContactContent, ContactFormModel, DemoContactFormService, contact CSS/interop, SiteFooter and SocialLinks before implementation.

The actual page has:

1. Hero: identical desktop/mobile title, description, CTA text/route, decorative photograph.
2. Contact Info: heading, phone, email, address.
3. Form: heading; five labels and placeholders; submit label; invalid-submit summary.
4. Map: heading, description, embed URL and accessible iframe title.

No additional sections, icons, visual-style controls or rich text were invented. The information panel's decorative SVG is presentation, not an editable content image.

## Page-specific vs global contact ownership

Phone, email and address are canonical business contact information, stored once in SiteContactSettings. The Contact Info heading remains page-specific. There was no existing Site Settings/contact abstraction.

The Footer has no phone/address block, but its default SocialLinks previously used the unrelated demo email `someone@example.com`. Only the Footer email destination now consumes the same canonical source as Contact. It initially becomes `info@nexnovaco.com`, matching approved Contact content. This intentional destination correction does not alter Footer layout, newsletter behavior, navigation or member-specific social links. Both sets of existing values were demo data; this work does not assert verified business information.

## Data models / migrations

Migration: `20260923182853_AddContactPageCms`.

Exactly three additive tables in ApplicationDbContext:

| Entity | Ownership |
| --- | --- |
| ContactPageSettings | Hero, Contact Info heading and Map presentation |
| ContactFormSettings | Form presentation fields only |
| SiteContactSettings | Canonical phone, email and address |

All are ID=1 singletons with constraints, required bounded strings and UpdatedAtUtc concurrency tokens. There is no message/submission table.

ContactPageCmsService uses short-lived DbContexts and focused methods for each editor. Hero, Map and Info saves update only their own columns, rather than posting the entire page. Contact Info's page heading and shared business values commit atomically in one SaveChanges transaction. IContactContentService remains the public page facade; demo submission stays in its separate service.

## Initialization

ContactPageDefaults preserves exact approved typed values. Each missing singleton is initialized once; existing rows/edits are untouched on subsequent startups. A fresh Contact page retains its existing content, image and map.

Public read failures are logged and fall back to approved content without writes. Fallback is scoped to the affected settings slice. Footer also safely falls back if canonical contact data is unavailable. Invalid stored map URLs are validated again on public reads and cannot become iframe sources.

## Hero

One title drives the existing desktop/mobile representations because their approved values are identical. Description, CTA text, safe public route and photograph are editable. Existing CmsImageField/LocalMediaStorageService handle ContactHero raster uploads under `uploads/contact/` with the existing 5 MiB policy.

Contact.razor supplies a base-aware CSS image variable; one page-scoped background-image rule consumes it. Original public markup structure, crops, geometry and breakpoints are retained.

## Contact Info

Heading, phone, email and address are editable. Email format and field lengths are validated. The editor explicitly explains that email is also used by the Footer. No business values are duplicated in a Contact-only table.

## Form Settings

Editable: heading, First Name/Last Name/Email/Subject/Message labels and placeholders, submit text, and invalid-submit summary.

The demo notice remains fixed: the form does not send or store messages and users should not enter sensitive information. Success text remains code-controlled. Field-level validation messages and required/optional rules remain unchanged. CMS strings are Razor-encoded plain text; a focused script-string test verifies they do not render as executable HTML.

## Map

Stores a URL string, never iframe HTML. Validation allows only HTTPS `www.google.com/maps/embed` URLs with a nonempty query, no credentials, fragment, custom port, whitespace/control characters or markup delimiters; maximum 2048 characters. The existing iframe title, ID, lazy loading, referrer policy and container are unchanged.

Browser QA verified edited URL/title persistence and the public iframe attributes. The in-app browser displayed a gray/blank map surface, with the iframe reported as about:blank, for the approved embed as well as the edited embed. Its URL, accessible title and responsive container were correct; actual third-party map tiles were **not verified**. Check the restored Calgary embed in a normal browser with Google Maps access before deployment. No speculative iframe/network workaround was added.

## Demo submission regression

ContactForm.razor, ContactFormModel, DemoContactFormService and contact-page.js were not changed.

- Four required fields still reject missing/whitespace-only input.
- Invalid email is rejected; email trimming remains.
- Subject remains optional, including blank/whitespace values.
- Valid submission returns exactly: “Demo form submitted successfully. No message was sent.”
- The browser confirmed successful submission with blank Subject and reset of the form fields.
- Edited form labels and invalid-submit summary reached the public form without changing the fixed demo notice.
- No SMTP, third-party delivery, outbound form endpoint, webhook, inbox or message persistence was introduced.

## Unsaved changes / validation

Every editor uses EditorSnapshot and the existing UnsavedChangesGuard. Sidebar navigation warns on dirty Hero, Info, Form and Map editors; Stay retains edits. Native external-navigation protection remains supplied by NavigationLock.

Focused tests invoke the actual editor save handlers. Required-field failures and forced database failures retain local edits/dirty state. Successful saves clear dirty state. Pending image-only changes count as dirty; a database failure retains the selection and a retry reuses the validated upload.

Validation covers all required/maximum-length fields, email, safe internal CTA routes and safe map URLs. Browser QA additionally exercised map HTTPS rejection.

## Authorization

Every Contact editor/root route inherits the Dashboard Admin role requirement. Anonymous requests redirect to login, non-Admin requests are denied, and Admin direct/refresh requests succeed. Each read-for-edit and save method also checks live database role membership and security stamp. Tests cover anonymous/non-Admin service calls, revoked Admin roles and stale stamps for all eight read/write operations. No public CMS mutation endpoint exists.

## Verification

Focused commands:

```powershell
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --contact-page
dotnet build NexNovaCo.sln --no-restore -p:OutputPath=bin/ContactDebug/net10.0/
dotnet tests/NexNovaCo.Auth.Tests/bin/ContactDebug/net10.0/NexNovaCo.Auth.Tests.dll --contact-page
dotnet run --project tests/NexNovaCo.Contact.Tests -c Release --no-restore
node --test scripts/Test-ContactInterop.mjs
```

221 focused CMS checks passed in Debug and Release; 21 existing form model/service checks and 2 Contact interop tests passed. Final builds have zero warnings/errors. The full historical site regression was not run.

Coverage includes exact fresh initialization, restart idempotence, every real editable area save/public read/restart/restore, every presentation field, migration-only table additions, all prior Identity/CMS row/timestamp preservation, upload retry, safe media responses, fallback logging/no writes, invalid stored-map fallback, atomic Contact Info rollback, text encoding and authorization.

Prior Home, Team, Member Detail, Team Hero editor and shared member manager were smoke-checked. Older phase migration checks were updated to reapply the new additive initialization when they rebuild a disposable fixture schema; those historical suites were not run in this phase.

Browser QA used only the disposable `artifacts/contact-page-qa-a45ad405257d45d2b68f0e7b93f6c8b4` database/media area. Hero, Info, Form and Map edits were saved, publicly checked, refreshed and restored. Hero file upload was verified through the actual chooser and restored to the bundled photograph.

Test-environment notes: the first sandboxed server could not access ASP.NET's local encryption keys. Browser QA proceeded after relaunching the isolated server with the required permission. A rebuild attempted while that server held the Release DLL produced file-lock warnings/errors; stopping the QA server and rerunning the build resolved them. No application code was changed to bypass these environment constraints. Public/Admin browser warning/error logs were empty during the successful QA session. The QA server was stopped afterward.

## Public Contact regression

| Width | Dashboard | Public Contact |
| --- | --- | --- |
| 1440 | Expanded Contact group, active child, full editor/actions | Desktop Hero photograph, side-by-side info/form |
| 1024 | Desktop drawer, wrapped form editor/actions | Desktop Hero, side-by-side info/form |
| 768 | Compact drawer, readable Map URL/editor | Mobile Hero, full-width form followed by contact panel, two-column name/email rows |
| 390 | Stacked editor fields/actions, drawer closes on child | Mobile image/title, single-column form, info panel below |

No horizontal document overflow at any requested width. Existing mobile stacking, accessibility labels, fixed demo notice, feedback, image crop and decorative shapes remain. Map container responsiveness and attributes were checked, subject to the third-party rendering limitation above.

## Deferred work

No real delivery, inbox, message storage, email/SMS integration, Header/Footer CMS beyond canonical email consumption, Navigation CMS, SEO editor, rich text, drafts/versioning, audit logs, localization or page builder. The live third-party map display needs the manual deployment check noted above.

## Recommendation for next milestone

A focused CMS-wide QA and merge-readiness review is the next milestone: review public-page ownership, Admin workflows, deployment migrations/media persistence and remaining placeholders. Real contact delivery should remain a separately approved integration phase with its own privacy, validation and abuse-prevention requirements. No next-phase implementation has begun.
