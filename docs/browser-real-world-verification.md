# Public-site browser and real-world verification

Date: 2026-09-24. Branch: `feature/dashboard-cms`.

## Scope and Git

Started with a clean working tree at `a8cfe16`, after Phase 17. This was public-site verification, not a feature phase, Dashboard redesign or full CMS regression. Contact remains demo-only; Newsletter remains disabled. No developer database reset or content edit, branch switch, history rewrite, push or merge.

Application fix: **`2e5447b` — Fix Home service reveal horizontal overflow**. Only `src/NexNovaCo.Web/wwwroot/css/home-blazor.css` changed. The separate report commit contains this document; its hash and final clean-tree status are supplied in the handoff.

## Environment and actual browser coverage

Used the already-bundled **Playwright 1.62.1** from the desktop runtime. No npm dependency, lockfile, browser package or permanent testing framework was added to the project. Temporary scripts, JSON evidence and screenshots are under ignored `artifacts/browser-verification-20260924/`.

| Tool/browser | Availability and actual use |
| --- | --- |
| Google Chrome **153.0.8010.53** | Installed Chrome executable; Playwright `chrome` channel, headless. Full matrix and interaction checks. This supplies Chromium-engine coverage, not a claim about a separate bundled Chromium build. |
| Microsoft Edge **153.0.4234.48** | Installed Edge executable; Playwright `msedge` channel, headless. Full matrix and interaction checks. Actual Edge, not inferred from Chrome. |
| Firefox **156.0.1** | Installed Mozilla Firefox executable; Playwright `moz-firefox` WebDriver BiDi channel, headless. Full matrix and interaction checks. |
| Bundled Playwright Chromium/Firefox | Browser binaries absent. Existing installed browsers were used instead. Stock Firefox did not speak the default patched-Firefox/Juggler protocol; BiDi worked. |
| Playwright WebKit | Binary absent; **not tested**. No WebKit pass claimed. |
| Safari/macOS/iOS/iPadOS | Unavailable on this Windows host; **not tested**. Manual checklist below. |
| Screenshots | Playwright screenshots captured locally; representative images inspected. No large screenshot directory committed. |

Viewport testing uses desktop browser emulation, **not physical mobile devices**, touch hardware or Safari emulation.

Initial browser runs under the restricted account could not trust the localhost certificate. Running the installed browsers under the normal user context validated the existing certificate. No HTTPS interstitial, certificate verification, OS trust setting, authentication or app security policy was bypassed/changed. Playwright's separate Node HTTP client did not accept that certificate; browser-native same-origin `fetch` and existing PowerShell suites were used for verified HTTPS requests instead.

The QA hosts were published Production builds on local HTTPS ports 7197 (baseline) and 7198 (fix), with an isolated SQLite database and synthetic test Admin. `PublicSite__BaseUrl=https://public.example` was set **only in QA process environments**; this reserved test origin is not a committed deployment setting. The normal developer application was not stopped.

## Route / viewport matrix

Each row below was checked in each of the three actual browsers at **1440, 1024, 768 and 390 CSS pixels**: **96 route/viewport/browser combinations** after the fix. Main matrix height was 1000px; supplementary screenshots used 900px.

“Pass” here means HTTP 200, one main/H1, expected unique route metadata/canonical, no broken loaded images, no Blazor error UI, and **zero measured document horizontal overflow**. It is not a claim of formal accessibility certification or perfect rendering of arbitrary future CMS content.

| Route | Viewports | Chrome | Edge | Firefox |
| --- | --- | --- | --- | --- |
| `/` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/about` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/services` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/projects` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/projects/nexconnect` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/team` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/team/emilyjohnson` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |
| `/contact` | 1440 / 1024 / 768 / 390 | Pass | Pass | Pass |

| Additional route | All three browsers |
| --- | --- |
| `/projects/not-a-real-record` | HTTP 404, safe not-found output, `X-Robots-Tag: noindex, nofollow`; absent from sitemap. |
| `/team/not-a-real-record` | Same safe 404/noindex behavior. |
| `/sitemap.xml` | HTTP 200, `application/xml`, correct configured-origin URLs. |
| `/robots.txt` | HTTP 200, `text/plain`, public allow rule and absolute sitemap. |
| `/admin/login` | Loads, no public canonical/marketing OG, noindex. |
| `/dashboard` | Anonymous request redirects to login; test Admin login succeeds, Dashboard loads without public canonical/OG. Noindex retained. |

## Bug found and minimal fix

Home's Services heading and introduction use AOS `zoom-out`. Before reveal, the existing transform is `scale(1.2)`, while the section allowed horizontal overflow. At 1440 the transformed heading's right edge measured **1443.6px**; at 390 it measured **396.6px**. The invisible/offscreen copy could therefore enlarge the document by approximately **4px/7px**, including when returning to the top after scrolling. It was not just an engine-specific rounding difference.

Confirmed the cause with a temporary browser-only style on the Services section, then added only:

```css
.home-page .service { overflow-x: clip; }
```

This contains the reveal overflow at its owning section. No global `overflow-x: hidden`, new breakpoint, typography/color change, animation removal or layout rewrite. Vertical overflow remains untouched. Normal and reduced-motion rendering, cards and text were rechecked. All 96 post-fix matrix cases had zero horizontal overflow. The earlier Phase 17 overflow observation is superseded by this verified localized fix.

Other initial automation failures were test setup issues, not product fixes: the real member slug is `emilyjohnson`, navigation hrefs are base-relative, the sticky element is `.header_top` (not the outer header), and back-to-top must be tested after scrolling until it is visible. Corrected those tests and reran. In Firefox, native keyboard typing/blur verified the form after a synthetic `fill` sequence failed to establish the expected state.

## Layout, Header, Footer and keyboard

- Home/Services/Projects/Contact received full-route DOM/layout scans and screenshots at all four widths. About, Project Detail, Team and Member Detail received the same geometry/image/metadata checks and focused behavior smoke checks.
- Inspected representative desktop/mobile Home, Home service/team/statistics sections, Services FAQ, 768px Projects testimonials, Contact desktop/mobile form, mobile menu and Footer screenshots. Approved geometry, image crops, stacking and navigation presentation remain intact in these samples. No engine-specific redesign was made.
- Header/logo loads; desktop and mobile expose the same six labels in the order Home, About, Services, Projects, Team, Contact. Footer repeats that collection, not a second data source. Active navigation changes with the route. `.header_top.fixed` remains at the viewport top after scrolling.
- Blazor FocusOnNavigate initially focuses H1. The skip link is the first ordinary document tab stop, reachable by keyboard, visibly focused, and Enter focuses main without changing the current route. Verified Home and an inner page.
- Desktop tab sampling: brand, six navigation links, primary CTA, subsequent content links. Mobile tab sampling excludes the closed navigation items. Focus-visible remained active on sampled keyboard targets.
- At 768 and 390: visible named toggle, Enter/Space opens/closes, correct `aria-expanded`, links hidden when closed, Escape closes and returns focus to toggle, three repeated open/close cycles, selecting Contact closes and navigates. No measured header/body overflow.
- Home CTA activates by keyboard and routes to Services. Footer Services link routes correctly. Back-to-top activated by keyboard from the scrolled page focuses main, reaches scrollY=0 and stays on Services. No trap encountered in tested paths. A screen-reader or physical-device usability certification was not performed.
- Footer branding, description, navigation, shared `mailto:info@nexnovaco.com`, copyright and mobile stacking present. Newsletter input/button disabled and explicitly accompanied by “Newsletter signup is not available yet.” No submission/API/storage was added.
- Global LinkedIn/Telegram icons have no destination in approved seed content and remain decorative/non-tabbable (`aria-hidden`), not fake `#` links. Email has an accessible name. Member Emily's email and HTTPS LinkedIn path have accessible names; Telegram is decorative. These links use the same tab, so no new-tab rel requirement applies to them. No unsafe scheme or bare `#` destination was found in the sampled markup.
- **Content verification limitation:** `Emily@example.com` is approved sample member content, not a verified real mailbox. The LinkedIn account's ownership/availability and global social destinations are not established by correct markup. Review production contact/member/social content manually; do not infer that external accounts have been verified or silently replace them with invented values.

## FAQ, carousel, Home and details

- Services FAQ: Enter toggles and Space restores the first panel at all four widths in all three browsers; `aria-expanded` tracks state, focus outline visible, no app console error or broken panel layout in samples.
- Home testimonial autoplay visibly changed the stage after 6.5 seconds. Pause state, keyboard Next/Previous and accessible names verified. Gallery Next changed “Image 1 of 2” to “Image 2 of 2”; ArrowLeft restored it.
- Same session resize **1440 → 768 → 390 → 1440** retained three Home carousel stages and no overflow. Repeated interactive About → Projects → Home → Projects → Home navigation produced **1 → 1 → 3 → 1 → 3** stages, with no duplicate initialization.
- Projects testimonials at exactly 768 retain the dark background, white body copy and readable orange attribution/controls; screenshot inspected. No change to the existing 768px contrast rule.
- Home Hero, Welcome, service/project/team content, statistics, partners and testimonials render with approved content/assets. CountUp final values **450, 3,000, 1,000, 26** verified with reduced motion and after revisiting Home; no duplicate stages or initialization exception.
- Chrome/Edge `prefers-reduced-motion` emulation worked. Firefox's BiDi `emulateMedia` did **not** change `matchMedia`; that failed emulation was not counted as a product pass. Repeated Firefox checks with temporary-profile `ui.prefersReducedMotion=1`: real `matchMedia` true, all three carousels paused with disabled autoplay control, final counters readable, manual Next usable, no layout failure. No user's OS/browser preference was changed.
- NexConnect and Emily Johnson detail title/content/metadata match actual records; gallery/profile images load. Existing responsive Project Features treatment retained. Long copy and pale text inside its fixed hexagon remain design limitations recorded in Phase 17, not resolved by this pass.

## Contact and Google Maps: separate results

### Form and technical integration

In all three browsers: five labeled fields, optional subject, four required-field errors, whitespace-only names/message rejected, invalid email rejected, native keyboard entry/blur works, valid demo submit resets fields and focuses feedback. Exact honest feedback: **“Demo form submitted successfully. No message was sent.”** Interactive Server behavior confirmed by the form, FAQ, navigation and gallery actions.

Map iframe exists with HTTPS Google Maps embed URL, meaningful title, non-zero visible dimensions and expected responsive container. Existing Contact HTTP suite verifies the exact configured source/title. No local app exception or map integration change. No Contact delivery, persistence, inbox, newsletter provider or fake subscription success was implemented.

### External visual rendering

**Observed:** Chrome rendered actual Google map tiles and the **Calgary Tower** pin/nearby streets at 768px, captured in `contact-map.png`. It was not gray/blank in that observation. This establishes rendering for that browser/session, **not** that the business location is correct or that every physical device/network works. Map control interaction was not exhaustively tested.

Some earlier iframe requests were canceled (`ERR_ABORTED`/`NS_BINDING_ABORTED`) as tests navigated away. These were separated from application failures, not reported as broken local assets. Keep the real-device/location checklist below.

## Metadata, favicon, sitemap, robots and deployment

- Every matrix page has one useful title, description and canonical, route-specific OG/Twitter values and configured-origin absolute canonical/OG/image URLs, not localhost in public metadata. NexConnect and Emily metadata derive from their records. No public marketing metadata leaked onto login/Dashboard.
- Browser discovery requests returned the six static routes plus **7 Projects and 6 Members (19 URLs)**. Admin/Dashboard, missing/deleted items and redirects are excluded. XML namespace/content type, uniqueness, CMS record changes/deletion/restart and fallback/DB-failure behavior also passed the existing 215-check public-quality suite using isolated databases.
- Production robots: `Allow: /`, Admin/Dashboard guidance, `Sitemap: https://public.example/sitemap.xml` for this QA environment. No accidental Production disallow-all rule with a configured origin.
- `appsettings.json` intentionally still has an empty `PublicSite:BaseUrl`. Before public deployment, set **`PublicSite__BaseUrl` to the approved real HTTPS origin**, with no path/query/credentials. No final domain was invented. Missing origin safely logs a warning, marks public pages noindex and returns 503 for sitemap/robots. Non-Production is not indexable. Configuration tests cover blank/development and invalid values; no code change here.
- All three rendered favicon/apple-touch URLs returned 200 `image/png`. Existing dimensions/declarations remain 16×12, 32×23 and apple 180×129, not invented square dimensions. Headless page screenshots cannot confirm the browser tab icon or iOS saved-home-screen result; manual checks remain.
- Social image URLs and head tags are technically verified. External social-preview crawlers were not invoked against a fake/local origin. A public/staging origin and approved artwork are required for that next check.

## Console, network and tests

Final public matrix/interaction runs: no unhandled page errors, Blazor error UI, failed local script/style/image requests, broken images or failed font requests. Fonts reached `loaded`. Chrome/Edge logged the expected two 404 responses when deliberately requesting invalid detail URLs; Firefox had no console entries. These expected negative tests are not app defects. No blanket suppression of console errors was used.

- Final **Debug: 0 warnings, 0 errors**, using `-p:OutputPath=bin/BrowserVerificationDebug/net10.0/`.
- Final **Release: 0 warnings, 0 errors**; corrected Release publish succeeded.
- Initial normal Debug output was locked by the already-running developer executable and reported MSB3026/copy errors. Did not terminate that application. Rebuilt successfully into the isolated output above; this was an environment/output-lock issue, not a source fix.
- **215 public-quality checks passed** after the fix (metadata, discovery, noindex/404, safe config, deletion/restart, CMS metadata flow and small Auth/Dashboard smoke).
- **25 JavaScript interop tests passed** (route lifecycle, reduced motion, carousel teardown, skip/back-to-top).
- Existing **Home HTTP suite passed**: eight sections, approved content, featured subsets, five Project/four Member detail routes, two 404s, 22 image checks.
- Existing **Contact HTTP suite passed**: field semantics, feedback/map/assets and seven previous-route smoke checks.
- Real-browser Admin login/Dashboard smoke passed in Chrome, Edge and Firefox. No comprehensive CMS regression was started.

Reproduction commands for existing durable suites:

```powershell
dotnet build NexNovaCo.sln --no-restore -c Debug -p:OutputPath=bin/BrowserVerificationDebug/net10.0/
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --public-quality
node --test scripts/Test-*Interop.mjs
./scripts/Test-Home.ps1 -BaseUrl <isolated-https-qa-origin>
./scripts/Test-Contact.ps1 -BaseUrl <isolated-https-qa-origin>
```

Temporary browser scripts: `verify.cjs`, `supplement.cjs`, `diagnose.cjs` in the ignored evidence folder. They target this machine's bundled Playwright/local QA server and are not a new portable CI contract. `chrome.json`, `msedge.json`, `moz-firefox.json` and `supplement.json` contain final measured evidence. The Firefox main script records the unavailable BiDi media emulation separately; the supplementary native-preference result is the verified reduced-motion result. Screenshots are temporary and uncommitted, including Home desktop/mobile, Services FAQ, Projects 768 testimonials, Contact forms/map/Footer and mobile menu open.

## Remaining manual checklist

### Real Safari — required, not tested here

- [ ] macOS Safari: Home and all main navigation; fonts/images and Footer; no overlap/overflow.
- [ ] iPhone/iPad Safari: Home at real device width; mobile menu open/close, Escape with hardware keyboard if applicable, route closing and focus.
- [ ] Services FAQ: expand/collapse, focus and scrolling.
- [ ] Home/Projects carousel: previous/next/pause, orientation changes, 768-ish width, Reduce Motion enabled.
- [ ] Project Detail gallery/features and Member Detail/profile links.
- [ ] Contact required/invalid/whitespace cases, keyboard/autofill, demo success/reset; no delivery expectation.
- [ ] Footer links, disabled newsletter and back-to-top. Check at iPhone width and landscape.

No external browser service was purchased or account created. **WebKit and Safari have not passed automated checks.** Actual Edge was tested, so there is no missing-Edge-engine requirement; a normal headed Edge/user-device spot check remains useful.

### Google Maps and real location

- [ ] Open `/contact` in normal Chrome or Edge on the intended deployment/network.
- [ ] Confirm the map visibly renders, not gray/blank.
- [ ] Confirm the displayed location is the **intended business location** (automated observation was Calgary Tower).
- [ ] Confirm expected map controls render and work.
- [ ] Resize to narrow/mobile width.
- [ ] Confirm no horizontal overflow.
- [ ] Confirm no visible Google Maps error. Repeat on Safari/iPhone.

### Favicon, social previews and production content

- [ ] Visually confirm tab/bookmark favicon in normal Chrome, Edge, Firefox and Safari; check iOS home-screen icon if used. Approve proper square artwork separately if needed.
- [ ] Supply and configure the real canonical HTTPS origin; inspect live canonical/sitemap/robots and environment/indexing rules after deployment.
- [ ] Only after a public/staging URL exists: inspect previews with the desired social platforms' official preview tools; verify publicly reachable images, cropping and actual title/description. Do not treat localhost or the reserved QA domain as a public deployment.
- [ ] Confirm real company address/map, phone, contact/member email and social account ownership. Replace approved sample content through the existing CMS when actual values are approved.
- [ ] Carry forward Phase 17's known contrast/long-content decisions: white-on-blue Hero/overlays, pale Project Features copy/fixed hexagon, longer future CMS content. This phase does not certify WCAG AA or redesign these areas.

## Readiness

Ready to proceed **with approval** to Contact/Newsletter product decisions, comprehensive CMS QA and merge review. This is not a merge/deployment approval: Safari/device checks, production-origin setup and content/design decisions above remain explicit. No next phase was started automatically. QA servers and temporary browser sessions are closed during closeout; final Git status is checked after the report commit.
