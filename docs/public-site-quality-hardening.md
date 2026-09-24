# Phase 17 — Public-site quality hardening

## Baseline (before Phase 17 code changes, 2026-09-24)

Branch `feature/dashboard-cms`, clean start at `d1cc683` (`Finalize footer and testimonial UI cleanup`). That separate prerequisite commit contains only the approved Footer flex container, newsletter margin, and Testimonials Author column label. No push or merge.

Source audit: App/Routes/MainLayout, shared header/footer/navigation, all public route declarations, Contact form, FAQ, breadcrumbs, carousel/gallery/reveal modules, page styles, CMS catalogs and production pipeline. Existing Phase 9 report is historical evidence, not a substitute for current verification.

- Public indexable route definitions: `/`, `/about`, `/services`, `/projects`, `/projects/{Slug}`, `/team`, `/team/{Slug}`, `/contact`. Dynamic detail records come from SQLite. Canonical inventory must not follow editable navigation.
- Utility routes: `/Error`, `/not-found`; account `/admin/login`, `/admin/access-denied`, POST `/admin/logout`. All `/dashboard/**` pages and legacy dashboard redirects are private. No other public content route was found. Full source route inventory follows below.
- Existing one main, skip link, FocusOnNavigate(h1), semantic mobile toggle/Escape/focus return, closed-menu visibility, FAQ buttons/panel IDs, carousel controls/pause and reduced-motion handling are present. Contact has associated labels/errors, required/native types/autocomplete, focused/announced demo feedback; required state has no explicit visible instruction.
- Some presentational copy is marked as headings (Welcome introduction, card subtitles); Home Services title is H3. Footer brand/newsletter headings skip levels. Decorative service icons repeat the adjacent title. Member above-fold portrait is lazy loaded. Image containers generally reserve approved fixed geometry, but brand images lack intrinsic hints.
- Per-page titles/descriptions exist but hardcode branding. No canonical, social metadata, structured data, sitemap or robots endpoint. Invalid details already use Navigation.NotFound; errors have noindex. Catalog fallbacks must NOT be used for sitemap, to avoid advertising deleted records during DB failures.
- Poppins loads twice (CSS import + head stylesheet), Lato via blocking CSS import. Existing swap/preconnect/fallbacks are appropriate. Preserve face coverage until proven unused.
- jQuery **3.1.0** remains actively required by Owl **2.3.4** in scoped Home/About/Projects carousels. CountUp **2.0.8** supports Home counters. Six dormant legacy scripts already excluded from publish; root static HTML/assets are reference material, not deployed. No broad theme cleanup justified.
- MapStaticAssets supplies build-time compression/fingerprints; runtime GUID-named uploads are immutable. Dynamic authenticated HTML compression should not be enabled casually because of HTTPS compression side-channel risk.
- Largest source assets: vision.png 585,515 bytes; partner-bg.svg 402,905; asset4.svg 339,301; service-bg-gradient.svg 240,770; project-box-bg-hover.svg 217,054; hexagon-side.svg 193,218; header.jpg 168,160. SVG raster embeddings and uploaded images remain optimization candidates, not safe deletion candidates.
- No Lighthouse executable available. Use source/resource/geometry evidence; no invented performance or accessibility score. Baseline font duplication and loading hints are independently reproducible from source.
- Existing favicon declarations honestly advertise non-square 32×23 and 16×12 PNGs; apple icon is 180×129. Dedicated square favicon/touch artwork and a 1200×630 share design need approved artwork, not code-generated replacements.

## Authoritative source route inventory

```text
src/NexNovaCo.Web/Components\Pages\Contact.razor:@page "/contact"
src/NexNovaCo.Web/Components\Pages\About.razor:@page "/about"
src/NexNovaCo.Web/Components\Pages\MemberDetail.razor:@page "/team/{Slug}"
src/NexNovaCo.Web/Components\Pages\ProjectDetail.razor:@page "/projects/{Slug}"
src/NexNovaCo.Web/Components\Pages\Team.razor:@page "/team"
src/NexNovaCo.Web/Components\Pages\Home.razor:@page "/"
src/NexNovaCo.Web/Components\Pages\NotFound.razor:@page "/not-found"
src/NexNovaCo.Web/Components\Pages\Services.razor:@page "/services"
src/NexNovaCo.Web/Components\Account\Pages\Login.razor:@page "/admin/login"
src/NexNovaCo.Web/Components\Pages\Error.razor:@page "/Error"
src/NexNovaCo.Web/Components\Pages\Projects.razor:@page "/projects"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutContentEditor.razor:@page "/dashboard/content/about"
src/NexNovaCo.Web/Components\Account\Pages\AccessDenied.razor:@page "/admin/access-denied"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutHeroEditor.razor:@page "/dashboard/content/about/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutMissionEditor.razor:@page "/dashboard/content/about/mission"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutMissionPointItemEditor.razor:@page "/dashboard/content/about/mission/points/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutMissionPointItemEditor.razor:@page "/dashboard/content/about/mission/points/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutPartnersEditor.razor:@page "/dashboard/content/about/partners"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutStoryEditor.razor:@page "/dashboard/content/about/story"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutTimelineEditor.razor:@page "/dashboard/content/about/timeline"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutTimelineItemEditor.razor:@page "/dashboard/content/about/timeline/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutTimelineItemEditor.razor:@page "/dashboard/content/about/timeline/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\AboutVisionEditor.razor:@page "/dashboard/content/about/vision"
src/NexNovaCo.Web/Components\Pages\Dashboard\ContactContentEditor.razor:@page "/dashboard/content/contact"
src/NexNovaCo.Web/Components\Pages\Dashboard\ContactFormEditor.razor:@page "/dashboard/content/contact/form"
src/NexNovaCo.Web/Components\Pages\Dashboard\ContactHeroEditor.razor:@page "/dashboard/content/contact/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\ContactInfoEditor.razor:@page "/dashboard/content/contact/info"
src/NexNovaCo.Web/Components\Pages\Dashboard\ContactMapEditor.razor:@page "/dashboard/content/contact/map"
src/NexNovaCo.Web/Components\Pages\Dashboard\GlobalFooterEditor.razor:@page "/dashboard/settings/footer"
src/NexNovaCo.Web/Components\Pages\Dashboard\GlobalNavigationList.razor:@page "/dashboard/settings/navigation"
src/NexNovaCo.Web/Components\Pages\Dashboard\GlobalSettingsIndex.razor:@page "/dashboard/settings"
src/NexNovaCo.Web/Components\Pages\Dashboard\GlobalSiteContactEditor.razor:@page "/dashboard/settings/contact"
src/NexNovaCo.Web/Components\Pages\Dashboard\GlobalSiteIdentityEditor.razor:@page "/dashboard/settings/site"
src/NexNovaCo.Web/Components\Pages\Dashboard\GlobalSocialLinkList.razor:@page "/dashboard/settings/social"
src/NexNovaCo.Web/Components\Pages\Dashboard\Home.razor:@page "/dashboard"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeContentEditor.razor:@page "/dashboard/content/home"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeContentEditor.razor:@page "/dashboard/home/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeFeaturedMembersEditor.razor:@page "/dashboard/content/shared-team/home-featured"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeFeaturedProjectsEditor.razor:@page "/dashboard/content/shared-projects/home-featured"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeFeaturedServicesEditor.razor:@page "/dashboard/content/shared-services/home-featured"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeHeroEditor.razor:@page "/dashboard/content/home/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeProjectsSectionEditor.razor:@page "/dashboard/content/home/projects"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomePartnersSectionEditor.razor:@page "/dashboard/content/home/partners"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeServicesSectionEditor.razor:@page "/dashboard/content/home/services"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeStatisticsEditor.razor:@page "/dashboard/content/home/statistics"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeTestimonialsSectionEditor.razor:@page "/dashboard/content/home/testimonials"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeTeamSectionEditor.razor:@page "/dashboard/content/home/team"
src/NexNovaCo.Web/Components\Pages\Dashboard\HomeWelcomeEditor.razor:@page "/dashboard/content/home/welcome"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedProjectsRedirect.razor:@page "/dashboard/content/projects/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedProjectsRedirect.razor:@page "/dashboard/content/projects/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedProjectsRedirect.razor:@page "/dashboard/content/projects/home-featured"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedTeamRedirect.razor:@page "/dashboard/content/team/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedTeamRedirect.razor:@page "/dashboard/content/team/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedTeamRedirect.razor:@page "/dashboard/content/team/home-featured"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedServicesRedirect.razor:@page "/dashboard/content/services/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedServicesRedirect.razor:@page "/dashboard/content/services/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\LegacySharedServicesRedirect.razor:@page "/dashboard/content/services/home-featured"
src/NexNovaCo.Web/Components\Pages\Dashboard\MemberEditor.razor:@page "/dashboard/content/shared-team/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\MemberEditor.razor:@page "/dashboard/content/shared-team/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\Members.razor:@page "/dashboard/content/shared-team"
src/NexNovaCo.Web/Components\Pages\Dashboard\NavigationEditor.razor:@page "/dashboard/settings/navigation/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\NavigationEditor.razor:@page "/dashboard/settings/navigation/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\PartnerEditor.razor:@page "/dashboard/content/partners/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\PartnerEditor.razor:@page "/dashboard/content/partners/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\Partners.razor:@page "/dashboard/content/partners"
src/NexNovaCo.Web/Components\Pages\Dashboard\Projects.razor:@page "/dashboard/content/shared-projects"
src/NexNovaCo.Web/Components\Pages\Dashboard\ProjectEditor.razor:@page "/dashboard/content/shared-projects/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\ProjectEditor.razor:@page "/dashboard/content/shared-projects/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\ProjectsHeroEditor.razor:@page "/dashboard/content/projects/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\ProjectsContentEditor.razor:@page "/dashboard/content/projects"
src/NexNovaCo.Web/Components\Pages\Dashboard\Services.razor:@page "/dashboard/content/shared-services"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServiceEditor.razor:@page "/dashboard/content/shared-services/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServiceEditor.razor:@page "/dashboard/content/shared-services/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\ProjectsOverview.razor:@page "/dashboard/content/projects/overview"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesBenefitsEditor.razor:@page "/dashboard/content/services/benefits"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesContentEditor.razor:@page "/dashboard/content/services"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesFaqItemEditor.razor:@page "/dashboard/content/services/faq/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesFaqItemEditor.razor:@page "/dashboard/content/services/faq/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesHeroEditor.razor:@page "/dashboard/content/services/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesFaqEditor.razor:@page "/dashboard/content/services/faq"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesPricingEditor.razor:@page "/dashboard/content/services/pricing"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesOverview.razor:@page "/dashboard/content/services/overview"
src/NexNovaCo.Web/Components\Pages\Dashboard\ProjectsTestimonialsEditor.razor:@page "/dashboard/content/projects/testimonials"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesProcessEditor.razor:@page "/dashboard/content/services/process"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesBenefitItemEditor.razor:@page "/dashboard/content/services/benefits/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesBenefitItemEditor.razor:@page "/dashboard/content/services/benefits/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesPricingItemEditor.razor:@page "/dashboard/content/services/pricing/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesPricingItemEditor.razor:@page "/dashboard/content/services/pricing/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesProcessItemEditor.razor:@page "/dashboard/content/services/process/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\ServicesProcessItemEditor.razor:@page "/dashboard/content/services/process/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\SocialLinkEditor.razor:@page "/dashboard/settings/social/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\SocialLinkEditor.razor:@page "/dashboard/settings/social/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\TeamContentEditor.razor:@page "/dashboard/content/team"
src/NexNovaCo.Web/Components\Pages\Dashboard\TeamHeroEditor.razor:@page "/dashboard/content/team/hero"
src/NexNovaCo.Web/Components\Pages\Dashboard\TeamSectionEditor.razor:@page "/dashboard/content/team/overview"
src/NexNovaCo.Web/Components\Pages\Dashboard\TestimonialEditor.razor:@page "/dashboard/content/testimonials/new"
src/NexNovaCo.Web/Components\Pages\Dashboard\TestimonialEditor.razor:@page "/dashboard/content/testimonials/{Id:int}"
src/NexNovaCo.Web/Components\Pages\Dashboard\Testimonials.razor:@page "/dashboard/content/testimonials"
```

## Implemented: accessibility

Practical target: [WCAG 2.2 AA](https://www.w3.org/TR/WCAG22/), not certification. Native controls and the existing theme were retained.

- Fixed an actual keyboard bug found in-browser: with `<base href="/">`, the old `href="#main-content"` sent an inner-page skip action to Home. The link now names the current URL, and the shared shell focuses/scrolls the current main without navigating. Verified on Contact and after interactive navigation to Services; added an idempotency/disposal test. Existing FocusOnNavigate continues focusing the route H1 after real navigation.
- One main and H1 per public route. Home Services/Projects use H2, nested featured project cards H3, partner cards H3, footer sections H2. Welcome introduction is prose, with its former typography preserved. Real project/member names remain the H1; responsive duplicate visual hero titles remain hidden from accessibility APIs. Other existing card subtitles remain subordinate headings, not new sections. No public content was rewritten.
- Branding marks and service icons repeating adjacent names are decorative (`alt=""`). Linked portraits retain the person's destination name; project/gallery/vision images retain meaningful content-derived alt text. No image-alt database schema or editor added.
- Contact retains explicit labels, native types/autocomplete/required, field-specific error IDs, Blazor aria-invalid, focused feedback and announcements. Added visible required/optional instructions. Browser invalid-submit focused feedback and exposed four invalid fields; valid keyboard entry produced **“Demo form submitted successfully. No message was sent.”** and reset fields. No delivery, storage, API or new form mechanics.
- Mobile button name/expanded/controls, hidden closed links, Escape/focus return, close-on-navigation and active `aria-current="page"` remain. FAQ native Enter toggle and associated panels verified. Existing accessible breadcrumbs remain; gallery Next advanced to image 2 of 2.
- Public actionable focus uses a navy/white two-tone ring; focus-within makes reveal content visible, and scroll margins account for the sticky header. Existing field/FAQ focus is preserved. The error-dismiss glyph is now a named native button.
- Owl remains scoped and disposable, with named arrows/dots, persistent pause/play, hover/focus/visibility pauses, inert inactive slides, and reduced-motion autoplay disabled. CountUp, reveals and back-to-top retain reduced-motion behavior, covered by existing tests. No carousel replacement. Home → About → Projects → About → Home produced exactly 3/1/1/1/3 carousel boundaries/stages, no duplicates. Projects 768px background fix is unchanged.
- Targeted contrast changes: dark text on orange CTA backgrounds; darker orange active links on light Home header; lighter orange on dark inner/sticky header, footer, testimonial attribution and dark-stage carousel controls. No global theme palette change. Representative calculated ratios: original orange `#cc7722` on `#454545` 2.84:1; new `#ffba69` 5.70:1; `#995410` on white 5.79:1. Disabled newsletter controls remain exempt inactive controls, with a readable unavailable notice.

### Explicit remaining design/accessibility decisions

The approved design is **not fully AA-conformant**. White text on the bright Home hero blue `#03a3e1` is about 2.86:1 (below even the 3:1 large-text target). Other blue/image overlays, Services process labels on mobile, and Project Features pale text/fixed clipped hexagon require a coordinated text/background/layout decision and testing against actual editorial content. This pass does not globally darken those illustrations or redesign their geometry. Long CMS content can still exceed inherited fixed-height cards. Home reveal transitions briefly produced 3px desktop / 5px phone horizontal overflow; settled rechecks were zero. These inherited animation/design constraints are recorded, not concealed with a blanket overflow rule.

No full screen-reader audit or automated accessibility score was produced. Final manual testing should include keyboard focus through the full-screen mobile overlay and long-content states. Dashboard accessibility beyond the requested smoke is deferred, including its existing root-relative skip-link pattern.

## SEO, metadata and indexing

`PublicPageHead` owns one PageTitle and one HeadContent per public page, including that page's stylesheet fragment. This avoids competing HeadContent providers. All eight route types use it; 19 seeded concrete routes have unique titles and descriptions. Home title is the canonical site name. Top-level descriptions use existing hero content where meaningful; Contact explicitly describes the demo. Project description/image and member name/role/introduction/image derive from their records. Site Identity changes drive title suffixes, site_name, logo and conservative structured data, not authored page copy. The only Dashboard change is correcting the Site Identity explanatory sentence; no Dashboard redesign or new SEO fields.

### Deployment setting (required before indexing)

Set **`PublicSite__BaseUrl`** (or `PublicSite:BaseUrl`) to the approved absolute HTTPS public origin, including the intentional www/non-www choice. No path, credentials, query, fragment, nondefault port, IP or loopback host. A production domain was not supplied, so appsettings deliberately contains an empty value, **not** localhost, public.example, or a guessed company domain. `https://public.example` is only an isolated test setting.

Configured Production emits absolute canonical URLs independent of request Host, query/fragment, or editable navigation; detail paths use the current stored slug. Non-Production uses noindex headers/meta and disallows crawling. An unset origin logs a warning, keeps pages noindex, omits canonical/share/schema URLs and returns 503 for discovery endpoints. Invalid configured origins fail startup clearly. This is fail-closed deployment behavior, not a ready-to-index default. Configure the real origin before release; use proper Production environment/TLS/proxy/AllowedHosts settings as documented in Phase 9.

All Admin/Dashboard responses (including redirects), Error/not-found and HTTP errors get `X-Robots-Tag: noindex, nofollow`. Existing private-page meta remains. Robots is not authorization; existing Identity/Admin enforcement is untouched. Direct `/not-found` now correctly returns 404 rather than 200. Missing Project/Member slugs remain actual 404/noindex and have no canonical. Generic error pages do not expose internal details.

### Discovery and structured data

- `/sitemap.xml`: XML sitemap namespace/content type, six fixed public routes plus live SQLite Projects/Members. Existing reliable detail UpdatedAtUtc becomes lastmod; static pages omit lastmod. Navigation, private/redirect/utility and invalid slugs excluded. Deleting records removes URLs immediately and across restart; empty navigation cannot change the inventory. Short-lived DbContext, no cached snapshot, no fallback catalog. A deliberately failed test DB read returned 503 without resurrecting deleted URLs or exposing the exception.
- `/robots.txt`: configured Production allows public crawling, discourages Admin/Dashboard discovery and names the absolute sitemap. Non-Production disallows indexing. No accidental production `Disallow: /` with a configured origin.
- JSON-LD: Organization + WebSite with real brand/name/logo/URL; BreadcrumbList only for existing visible Project/Member breadcrumbs. Default JSON encoder escapes CMS HTML characters. No invented ratings, reviews, accounts, postal-address claims, Person credentials or Project-specific schema.
- OG: title, description, website type, URL, image, image alt and site_name. Twitter: summary/title/description/image. Appropriate existing detail imagery, otherwise canonical brand logo. Only existing local/controlled-media images are selected; all tested share URLs resolved. No fabricated X handle or large-image-card claim. A dedicated approved 1200×630 share design remains desirable, not required for this pass.

No migrations, initialization changes, extra settings tables, SEO CMS, mutation endpoints or production data edits.

## Performance, media, fonts and legacy dependencies

No Lighthouse installation or score, LCP timing, CLS score or mobile-throttling claim. Evidence is source/resource inventory, HTTP headers/bytes, DOM geometry and current-browser checks.

| Area | Change / disposition |
| --- | --- |
| jQuery | Active Owl dependency upgraded **3.1.0 → 3.7.1**, downloaded from the official CDN with license header intact. 3.x compatibility boundary retained; no 4.x framework migration. Source bytes 104,122 → 87,533; published Brotli 27,445, gzip 30,683. Exact official artifact retained. |
| Old jQuery | No longer loaded and excluded from publish, including compressed copies/manifests. Source/reference retained deliberately; no unrelated reference deletion. |
| Owl 2.3.4 | Still required for Home/About/Projects; retained and checked with real 3.7.1 DOM behavior and lifecycle tests. Existing AutoHeight leak workaround retained. |
| CountUp 2.0.8 | Still required only by Home counters; retained. Global vendor load is once per public document so subsequent interactive navigation can enter Home without missing dependencies. |
| AOS / Bootstrap | CSS still used for approved theme/layout. JS unused by Blazor and already excluded, along with script.js, inner-project.js, member.js and contact.js. No second initialization path added. |
| MudBlazor / Blazor | Existing exact versions/providers retained. No broad framework replacement or public Mud redesign. |
| Fonts | Removed both CSS imports. Lato now loads from head; existing Poppins head link remains the sole Poppins request. Kept swap, preconnects, fallback families and face coverage (including italics); no speculative weight removal or typography change. |
| Images | Above-fold member portrait no longer lazy-loaded. Brand fallback's real 418×310 intrinsic hints plus auto-height preserve 45px display; arbitrary uploaded logos do not receive invented intrinsic sizes. Footer logo lazy/async and decorative service icon async decoding. Existing below-fold loading/reserved frames retained; heroes not blindly lazy-loaded or given speculative priority. |
| Large assets/uploads | No destructive image conversion. Large PNG/embedded-raster SVGs remain approved artwork; optimized source artwork/responsive variants and upload dimensions are future work. Fixed hero/card geometry already reserves most space; no measured CLS improvement claimed. |
| Caching/compression | MapStaticAssets retained, not duplicated. Raw modules `no-cache`; fingerprinted jQuery served HTTP 200, Brotli **27,445 bytes**, ETag and `max-age=31536000, immutable`. Runtime uploads remain immutable GUID filenames; no immutable mutable-filename override. Dynamic authenticated HTML compression not added casually. |

Actual favicon files: PNG 16×12 (1,176 bytes), 32×23 (2,258), apple 180×129 (18,970). All three URLs returned 200 image/png; advertised favicon sizes remain truthful. Non-square legacy apple artwork remains wired as existing behavior; approved square touch/favicon artwork is still needed. No PWA/manifest/favicon manager or generated artwork.

Sources informing the bounded choices: [jQuery 3.7.1](https://blog.jquery.com/2023/08/28/jquery-3-7-1-released-reliable-table-row-dimensions/), [jQuery upgrade/security guidance](https://blog.jquery.com/2024/04/17/upgrading-jquery-working-towards-a-healthy-web/), [Microsoft static assets](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0), [HTTPS compression considerations](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0), [Google sitemap guidance](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap), [WAI pause/stop behavior](https://www.w3.org/WAI/WCAG22/Understanding/pause-stop-hide).

## Verification — current run

- Debug and Release solution builds: **0 warnings, 0 errors**. Release publish succeeded. Debug used isolated `bin/QualityDebug/net10.0/` output to avoid a running development application's binaries.
- **215 public-quality checks** in both configurations: unique metadata, canonicals, schema JSON, absolute resolving share images, noindex/404/private responses, unsafe origin rejection, robots, valid sitemap, delete/restart, navigation independence, CMS data flow, DB failure without fallback, Contact associations, FAQ/skip semantics, login/Dashboard smoke.
- **289 Global Settings checks** in both configurations: existing Admin/write enforcement, initialization/delete-all, media, dirty-state/editors and shared data behavior retained. Updated only the assertions whose intentional H2/logo metadata contract changed.
- **25 JavaScript tests** pass (existing route lifecycle/reduced-motion suite plus new skip-link/back-to-top/listener-disposal regression). **21 Contact model/demo-service checks** pass.
- Existing Home HTTP suite passes: eight sections, approved text/subsets, five project/four member detail routes, two 404s and 22 image checks. Updated title expectation and intentional decorative-alt classification. Existing Contact HTTP suite passes.
- Existing Production publish suite passes over **validated localhost HTTPS**: 19 concrete routes with unique titles/descriptions, 66 rendered assets, excluded/private paths, real 404s, no Development diagnostics, honest forms. Initial loopback HTTP attempt failed the existing SecurePolicy.Always antiforgery requirement, as expected. Repeated with the already-installed certificate and normal Windows trust store; no certificate checks/trust/authentication were bypassed or weakened.
- Published output excludes old jQuery, its compressed variants and developer DB. No normal developer DB reset/migration/content change. Tests use temp DBs; browser used `artifacts/quality-qa-*/identity.db` and dedicated credentials. Browser Site Identity edit propagated to Header/title/OG and survived restart; restored NexNovaCo, saved and reloaded to confirm.
- Local QA servers stopped; temporary browser tabs closed and viewport reset. Final settled Development/published browser logs contain no app errors/warnings; Interactive Server verified through FAQ, menu, gallery and CMS actions. The only Production HTTP error was the deliberately corrected non-TLS test setup described above.

### Responsive/public smoke

All eight public page types (`/`, About, Services, Projects, NexConnect, Team, Emily Johnson, Contact) were inspected at each width below, checking H1/main, metadata, broken images, overflow and error UI. Visual samples include desktop Home/footer, 1024 About/footer, 768 Projects testimonial and 390 Contact/form/footer. This is scoped current-browser validation, **not** the deferred engine matrix or a full historical pixel suite.

| Width | Result |
| --- | --- |
| 1440 | All route types render; desktop navigation, branding, footer and images intact. Home transient reveal overflow 3px; settled 0. |
| 1024 | All route types render with no measured horizontal overflow; mobile-toggle header and tablet footer intact. |
| 768 | All route types render with no measured horizontal overflow. Home/Projects testimonials keep their dark image background, white body copy and readable attribution; pause/next work. |
| 390 | All route types render; mobile menu/Escape/navigation, form feedback and stacked footer usable. Home transient reveal overflow 5px; settled 0. |

No broken images in the 32 checks. Safe new-tab rel markup retained; known public routes/detail links checked by HTTP suites and sitemap tests. Decorative partner/social items remain non-links; no fake destinations or public Admin navigation introduced. Google Maps embed/title/lazy loading unchanged; internal gray rendering is **not** treated as a failure.

### Reproduction

```powershell
dotnet build NexNovaCo.sln --no-restore -c Debug -p:OutputPath=bin/QualityDebug/net10.0/
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --public-quality
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --global-settings
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore
# Against an isolated, approved-default-content QA host:
./scripts/Test-Home.ps1 -BaseUrl <local-qa-url>
./scripts/Test-Contact.ps1 -BaseUrl <local-qa-url>
./scripts/Test-Publish.ps1 -PublishPath <fresh-publish-directory> -BaseUrl <local-https-production-qa-url>
```

## Deferred and readiness

Ready to proceed, **only with approval**, to (1) final Chrome/Edge/Firefox/Safari/manual verification, (2) Contact/Newsletter product decisions, and (3) comprehensive CMS QA + merge review. This is not approval to deploy or claim WCAG compliance. Carry the contrast/long-content/reveal limitations into that review, and supply the real PublicSite origin before enabling production indexing.

Explicitly deferred: final browser engine matrix; real Google Maps/location verification; Contact delivery backend; Newsletter backend; square favicon/touch and dedicated share artwork; approved fixes for inherited blue-overlay contrast/clipped content; larger image optimization. No next phase, push, merge, branch switch or history rewrite performed.

## Git closeout

Branch: `feature/dashboard-cms` throughout. The pre-phase diff contained only the three approved Footer, newsletter CSS and Testimonials column-label changes; these were committed separately and the working tree was clean before Phase 17 began.

- `d1cc683` — Finalize footer and testimonial UI cleanup (pre-Phase-17).
- `9f809a7` — Add canonical metadata and dynamic public discovery.
- `76d7939` — Harden public accessibility and frontend assets.
- The accompanying `Verify public-site quality hardening` commit contains this report and focused verification changes; its hash and post-commit clean-tree check are supplied in the final handoff.

No push or merge. No developer database reset, branch switch or history rewrite. The next phases require approval.
