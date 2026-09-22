# Dashboard Phase 4G — Home Testimonials presentation CMS

Verified 2026-09-22. Only the existing Home testimonial brand-panel title and description became editable. Shared testimonial entities remain read-only.

## 1. Git

Started clean on `feature/dashboard-cms` at `5c170ca`. Stayed on that branch; no main checkout, branch creation, history rewrite, push, or merge. This report accompanies the focused Phase 4G commit.

## 2. Sidebar / route

Testimonials is the eighth Home child, after Hero, Welcome, Services, Projects, Team, Statistics, and Partners. `/dashboard/content/home/testimonials` uses the existing Admin-only Dashboard layout. Direct load/refresh, expanded Home group, active child, keyboard Enter navigation, and mobile drawer closure were verified. No additional Home item was added.

## 3. Home vs Projects architecture — inspection findings

Before implementation, inspected the original Home/Projects HTML, content services, Testimonial model/catalog, TestimonialsSection, TestimonialCard, ThemeCarousel, carousel lifecycle, and responsive CSS.

| Scope | Existing content | Treatment |
| --- | --- | --- |
| Shared entities | Two testimonials, each with two quote paragraphs and author/role/company attribution; no avatars | Unchanged canonical collection |
| Home presentation | Right-hand decorative brand panel: “NexNovaCo” and its existing description | Two-field Home-only singleton |
| Projects presentation | Same approved brand copy supplied to a separate section instance | Static and unchanged |
| Shared rendering | TestimonialsSection, TestimonialCard, ThemeCarousel | Unchanged |

This is not an invented introduction above the quotes. Both pages previously used `TestimonialCatalog.Brand`; only the Home instance now receives editable presentation. Existing CSS hides `.testimonial_shape_row` at widths of 993px and below. The desktop panel is decorative/fixed-height, so concise copy remains important. No CSS, component markup, or breakpoint changes were needed.

## 4. Canonical Testimonial source

`TestimonialCatalog.All` remains the sole immutable collection, in approved order: Olivia Carter, COO at Alpha Co; Daniel Kim, Marketing Director at Tech Co. Automated checks assert reference identity between Home and Projects, not just equal copies. Quotes, paragraphs, attribution, order, and carousel data remain unchanged. Browser comparisons exclude Owl's temporary loop clones and confirm two original cards and one stage.

## 5. Actual Home-specific settings

The approved brand panel has two meaningful fields, Title and Description. An informational-only page would leave these real fields uneditable. No eyebrow, CTA, route, quote, author, avatar, visibility, or order field was invented. `HomeTestimonialsSectionDefaults.Content` references the existing approved `TestimonialCatalog.Brand` for initialization and fallback; it does not copy or fork testimonial records.

## 6. Database / migration / service

Additive migration `20260922200421_AddHomeTestimonialsSectionSettings` creates only `HomeTestimonialsSectionSettings`: singleton Id 1, Title (120), Description (1000), UpdatedAtUtc. PK/check constraints prevent another Id. No testimonial entities or existing schemas are changed.

Controlled startup inserts the approved copy only when missing and never overwrites saved edits. Upgrade tests start at the prior Partners migration with edited content in all seven earlier Home slices and an Identity account/role mapping; all survive with no model drift. Repeated initialization does not duplicate the row.

The focused scoped service uses short-lived factory DbContexts and no-tracking reads. Missing, invalid, or unavailable public settings log the issue and return approved defaults without writes. Editor reads fail visibly rather than presenting fallback as saved data. Valid saves update only the singleton and UTC timestamp. No generic CMS repository was introduced.

## 7. Editor

The MudBlazor editor matches the existing two-field Home editors. It explains the shared quote boundary and the panel's mobile visibility. Both fields are required, reject whitespace-only/overlength content, and remain Razor-encoded. Save stays on the page with success feedback; expected failures retain form content and show safe feedback while logging details server-side. The description helper requests concise copy for the unchanged public layout.

## 8. Unsaved changes

Both fields use the existing `EditorSnapshot` and `UnsavedChangesGuard`; neither shared implementation changed. Automated lifecycle checks cover initial/reverted/dirty state, invalid submit, failed save, successful save, and reload. Browser Stay retained the edit, Leave discarded it, and returning loaded saved copy. Blank Title displayed required validation. Keyboard and mobile navigation worked.

A dirty View Home action retained the editor URL, but the in-app browser did not expose the native unload dialog through its dialog API. Native prompt appearance/accept/cancel remains a manual standard-browser check, consistent with earlier phases. No replacement unload framework was added.

## 9. Public Home integration

Only `HomeContentService.TestimonialBrand` reads from the new SQLite-backed service, freshly on each call outside the static snapshot. `Testimonials` still references `TestimonialCatalog.All`. TestimonialCard, TestimonialsSection, ThemeCarousel, public markup, CSS, JavaScript, and assets are untouched.

Isolated browser QA edited both fields to “Home QA” / “Home-only testimonial panel.” Home rendered both, with all original cards unchanged. An actual server stop/restart preserved both values in public Home and the editor. Approved copy was restored through the editor and compared against the initial public baseline. The normal development database was not reset or edited.

## 10. Projects regression

Projects retained its exact original title, description, two quotes, and authors after Home edits and after restoration. Pause/Next/Play controls worked on both pages, with active authors changing and two originals/one stage retained. Home → Projects → Home → Projects navigation correctly remounted carousels without duplication. Home autoplay was observed advancing after focus/hover left the carousel; Projects retained its playing state and working controls.

At exactly **768px**, Projects rendered white testimonial text over the dark `image/home/project-bg.png` background, with no horizontal overflow. Its existing scoped max-width 768px guard in `projects-blazor.css` is untouched. The original min-width 768px page-background rule and max-width 768px slide-background rule therefore still cannot produce the former white-on-white defect. No full visual regression was performed.

## 11. Authorization

Anonymous requests redirect to login, non-Admins are denied, and Admin direct route/refresh/save succeeds. Editor service reads/writes also check the authenticated Admin principal, current database role membership, and security stamp; tests cover direct unauthorized calls and revocation. No public write endpoint exists.

## 12. Verification

- Debug and Release builds: **zero warnings/errors**.
- Auth/CMS integration harness: **753 checks passed**, covering all eight Home slices, migration/initialization, auth, validation, fallback, persistence, and preservation.
- Home/Projects interop suites: **7 tests passed**, including repeated navigation/disposal, reduced motion, and unavailable-Owl fallback.
- Existing `Test-Home.ps1` and `Test-Projects.ps1`: passed against the isolated QA server.
- Dashboard screenshots inspected at **1440, 1024, 768, 390px**: correct two-field form, usable responsive actions, no horizontal overflow.
- Browser: login, direct route/refresh, active child, Stay/Leave, required validation, mobile/keyboard navigation, save, Home/Projects isolation, controls, repeated navigation, actual restart persistence, and baseline restoration.
- Public smoke checks: Home desktop/mobile and Projects desktop/768/mobile had no measured horizontal overflow; Projects 768px contrast remained correct.
- No browser warnings/errors during the focused flow; interactive saves and navigation continued to work.
- `git diff --check`: passed.

QA used an ignored isolated database under `artifacts/testimonials-qa-20898f6e`. Temporary credentials were not printed or committed. Temporary server/tab and viewport override were cleaned up after verification.

## 13. Deferred work

No Testimonial Add/Delete/Edit/Reorder, author/quote editing, avatar upload, visibility controls, shared editor, Projects Testimonials CMS, media library, Header/Footer CMS, Site Settings, drafts/versioning, page builder, or generic collection engine. No duplicated testimonial catalog.

## 14. Recommendation — approval required

With all eight Home section settings available, separately scope a shared Testimonials collection CMS using the current canonical Home/Projects boundary. Agree on fields, ordering, validation, and shared-page impact before implementing CRUD. This next milestone is not implemented. Stop for approval.
