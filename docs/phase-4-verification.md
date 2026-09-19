# Phase 4 — About migration and verification

Completed 2026-09-19. Scope: `/about`, following approval to continue after the Phase 3 handoff. The .NET 10 / Interactive Server / MudBlazor 9.10.0 foundation remains unchanged. About is now Razor-rendered; root `about.html` is the approved comparison reference, not an app-route dependency. Projects, Team and Contact remain placeholders.

## Branch and commits

- Branch throughout: `feature/blazor-public-site`. No new branch, main checkout, history rewrite or push.
- Starting state: clean at `9f012ff` (`Verify Services visual parity and regressions`). Continuation resumed the uncommitted Phase 4 work rather than discarding or restarting it.
- Implementation: `51d6c57` — `Migrate About page and share partner components`.
- Verification: `Verify About visual parity and regressions` — the commit containing this record, tests and README updates. Its hash is reported in the task handoff.
- Ending state: all Phase 4 changes committed; clean working tree checked after the verification commit.

The original About markup, stylesheet, global styles and interactions were inspected before implementation. Root static HTML, original CSS/JS/assets and all 82 copied legacy asset files remain unchanged. No database, authentication, CMS, admin UI or backend was added. Shared layout, navigation, footer and MudBlazor providers are unchanged.

## Files by responsibility

Application paths below are relative to `src/NexNovaCo.Web/`.

| Group | Added or changed files |
| --- | --- |
| Page/composition | `Components/Pages/About.razor`, `Components/_Imports.razor`; minimal shared-section/style imports in `Home.razor` and `Services.razor` |
| About sections | `Components/Sections/About/{AboutStorySection,AboutVisionSection,AboutTimelineSection,AboutMissionSection}.razor` |
| Shared section | Moved `Sections/Home/HomePartnersSection.razor` to `Sections/Shared/PartnersSection.razor`; rendering and parameter API preserved |
| Models | `Models/AboutContent.cs` |
| Content/registration | `Services/{IAboutContentService,AboutContentService,PartnerCatalog}.cs`, partner catalog reuse in `HomeContentService.cs`, `Program.cs` |
| CSS | New `about-blazor.css`, `inner-page-blazor.css`, `carousel-blazor.css`; corresponding small extractions from `home-blazor.css` and `services-blazor.css` |
| JavaScript | New `about.js`, shared `carousels.js`; extraction from `home.js`; responsive-clone reveal correction in `reveal.js` |
| Tests | New `scripts/Test-About.ps1`, `scripts/Test-AboutInterop.mjs`, `scripts/fixtures/CarouselFixture.mjs`; updated Foundation/Home/Services HTTP suites and Home/Services interop fixtures |
| Documentation | `README.md`, this record |

## Component hierarchy and exact rendered order

```text
MainLayout (existing shared header, navigation, main, footer and providers)
└── About.razor
    ├── InnerPageHero
    ├── AboutStorySection              — What We Do
    ├── AboutVisionSection             — Our Vision
    ├── AboutTimelineSection           — Our Timeline
    │   ├── SectionHeader
    │   └── ordered list → 3 milestones
    ├── AboutMissionSection            — Our Mission and 4 commitments
    └── PartnersSection                — Our Partners
        ├── SectionHeader
        └── ThemeCarousel
            └── PartnerCard × 6
```

Partners transitions directly into the existing footer; no new CTA section was invented. There is no redundant About-specific hero or partner-card copy. The story retains its distinctive subtitle treatment instead of forcing it into a mismatched heading abstraction. Tiny one-off mission SVGs remain within their section.

`InnerPageHero`, `SectionHeader`, `ThemeCarousel`, `PartnerCard`, `InnerPageHeroContent`, `SectionHeading` and `Partner` are reused with no parameter/model API changes. The former Home partners section was already an exact match for About's markup, so it became a shared section. Route styles continue to own each page's distinct partner background. No arbitrary styling props were added.

## Typed content and canonical partners

| Contract | Role |
| --- | --- |
| `AboutStoryContent` | Title/subtitle, two introductory paragraphs, two detail paragraphs and closing copy |
| `AboutVisionContent` | Title, paragraphs, real Services CTA, image path and descriptive alt text |
| `TimelineMilestone` | Complete numeric year and approved description |
| `AboutTimelineContent` | Existing section heading, ordered milestone collection and Projects CTA |
| `AboutMissionContent` | Brand heading/copy, mission title/paragraphs and four commitments |
| `AboutContent` | Page aggregate plus existing hero and partner contracts |

`IAboutContentService.GetAsync(CancellationToken)` is registered as a singleton with a typed in-memory editorial snapshot of approved `about.html`. Content lives in the provider, not reusable card markup. A future provider can replace the DI implementation without changing section rendering. No speculative entity fields, CRUD or storage layer were introduced.

`PartnerCatalog` is the single shared source for the partner heading and six entries, including their approved order, spelling, descriptions, images and existing white-background treatment. Home and About render identical partner-card markup from these objects; the HTTP suite checks that equality. No partner URLs were supplied, so no fake destinations were added. Home's other content and the canonical ServiceCatalog are unchanged.

The timeline uses a semantic ordered list, with full accessible year headings for 2023, 2024 and 2025. The original split numerals remain visually identical but are decorative and hidden from assistive technology. Explicit three-slot placement preserves the uneven desktop geometry and the center description's alternate position. At 768px the first two milestones sit side by side and the third centers below; at 390px all three stack. The four mission commitments expose list/listitem semantics without altering the original row wrappers.

## CSS and JavaScript boundaries

The original `about.css` is loaded unchanged. Targeted adaptations are split into:

- `inner-page-blazor.css`: extracted Services/About shell colors, header placement, wide-screen spacer, baseline typography/background and one logical responsive H1. The Services selectors were extended to About; source geometry and breakpoints were preserved.
- `carousel-blazor.css`: extracted Home fallback grids, focus styles and pause-control presentation, now available to both routes.
- `about-blazor.css`: SSR-readable reveal content, original transition duration, semantic timeline list reset, visually equivalent mission brand paragraph and reduced-motion rules.

Home/Services route-only adaptations remain in their existing files. No public-facing MudBlazor styling, new palette, typography system, breakpoint redesign, asset replacement, mass `!important` or broad CSS refactor was introduced.

| Interaction | Ownership |
| --- | --- |
| Partner slider | Temporary shared Owl JS interop; Razor owns all content |
| Scroll reveals | Shared `reveal.js` using approved AOS CSS effects, not the global AOS engine |
| Hero story / vision / timeline CTAs | Normal links: `about#About`, `services`, `projects` |
| Mobile menu and active routes | Existing Blazor shell state and NavLink |
| Sticky header and back-to-top | Existing shared shell JS |
| Hover treatment | Existing theme CSS; targeted reduced-motion rules |
| Home counters | Remain Home-only; About never initializes CountUp |
| Legacy global page scripts / Bootstrap JS | Dormant; not newly loaded or initialized |

`carousels.js` contains the existing Home behavior: responsive items, autoplay with pause/play, keyboard arrows, focus/hover/visibility/reduced-motion pausing, inactive-slide semantics, full-row autoplay suppression and plugin cleanup. `ThemeCarousel` still freezes the plugin-owned subtree; do not put Blazor event-bound/stateful children inside it. Changed content requires keyed remounting, not diffing Owl's wrappers.

About has its own WeakMap-guarded initialization lifecycle, cancellation-aware content load and late-import disposal guard. Route removal and pagehide disconnect observers/listeners and destroy the carousel even when the server circuit is unavailable. JS neither fetches nor creates business content.

### Responsive clone correction found during verification

The Home regression check exposed a pre-existing edge case while moving from 390px to 1440px: Owl rebuilt loop clones, but the reveal helper still held the old nodes. Wrapped cards could remain transparent. The extraction itself had retained the original behavior.

The shared carousel now emits a route-local `theme:carousel-refreshed` event after Owl refresh. The reveal helper reacquires only that route's animated targets and applies the existing layout-offset visibility calculation. Both listeners are removed on disposal. This is a bounded plugin lifecycle correction, not an unbounded MutationObserver refresh loop. A new dependency-free test checks replacement visibility and cleanup; the previously failing EduVance/NexConnect/PayFlowX wraparound was rechecked successfully in Chrome after rebuilding and resizing.

## Visual comparison

Method: local Chrome responsive emulation comparing approved `http://127.0.0.1:5140/about.html` with Blazor `http://localhost:5138/about`. The Computer Use skill was used for section-by-section screenshots and interaction checks. All four viewports used 1000px height. Screenshots were inspected inline in the task; this is visual comparison, not automated pixel-diff or numeric scroll-width testing.

| Width | Result |
| --- | --- |
| 1440px | Matching hero/header spacer, story text/columns, vision image and copy, rotated timeline shapes/dotted line and alternate description placement, three mission columns, six partner logos and footer transition. |
| 1366px | Matching hamburger breakpoint, hero without the wide-screen spacer, story/vision composition, timeline geometry, mission columns and partner row. |
| 768px | Matching mobile hero title, story's two-column detail, image above vision copy, two-plus-one timeline, two-column brand/mission with full-width commitments below, three visible partners and footer boundary. |
| 390px | Matching hero crop/title, story wrapping, clipped vision photo above text, three vertically stacked milestones, mission brand panel then paragraphs, vertically stacked commitment icons/text, one visible partner and footer transition. |

No significant unintended visual differences, broken images or horizontal overflow were observed at the required widths. Text checks independently confirm approved copy. Intentional/inherited differences and constraints:

- Partner pause/play, keyboard operation and inactive-slide accessibility are inherited from migrated Home. When all six partners fit, autoplay is suppressed because Owl hides the controls; the static source continues autoplay. Slide identity/position therefore need not match at the same elapsed time.
- One logical H1, full timeline year headings, decorative split numerals and mission list semantics improve accessibility without changing resting composition. Existing partner-card heading semantics are unchanged; this is not a full-site accessibility audit.
- The shared Blazor shell retains its existing newsletter demo, current-year copyright, active-route behavior, focus treatment and back-to-top timing. These can differ from the static shell.
- At 768px the source's white decorative hero edge intersects the white brand treatment. This inherited limitation was retained.
- Original desktop vision/mission fixed heights and clipped brand/image shapes remain. The timeline is explicitly a three-milestone, four-digit-year design. Future editable content must stay concise and respect these editorial/layout constraints; no generic page builder was introduced.
- Chrome's Issues panel still reports advisory missing explicit dimensions on lazy-loaded images and id/name metadata on a form field. It reports zero page errors and zero breaking changes. These inherited full-site cleanup items were not expanded into this phase.

## Final verification

| Check | Result |
| --- | --- |
| Debug / Release builds | Both pass, zero warnings/errors |
| Foundation HTTP | Pass: six public routes, 82 unchanged legacy asset copies, canonical/legacy serving, 48 rendered asset URLs, runtime resources and HTTP 404 shell |
| About HTTP | Pass: six ordered sections, approved text, one H1, three chronological accessible milestones, four commitments, six partners identical to Home, route-safe CTAs, images and route/shared assets |
| Home HTTP | Pass: eight sections, approved text/JSON subsets, nine detail stubs, two detail 404s, 22 images/alt text and route/shared assets |
| Services HTTP | Pass: six sections, canonical services, benefits/process, disabled pricing, accessible FAQ markup and route/shared assets |
| Node interop | Nine tests pass, zero failures: lifecycle/idempotence/remounts, detach/pagehide cleanup, reduced motion, full-row pause, missing-Owl fallback, counters, layout reveals and responsive clone replacement |
| About partners | Autoplay observed; pause changes label to Play; Next advances one card; keyboard Left returns to previous cards. Pause/Next also work after route re-entry and resizing. |
| Mobile navigation | About active state; opening, Escape close with visible toggle focus, Space reopen, Home selection closes menu and navigates |
| Route sequence | About → Home → About → Services → About through live shell/CTA/footer links; remounted About partner controls remain operational |
| About CTAs | Hero reaches the story anchor; vision CTA opens Services; Projects destination and route-safe markup verified by HTTP (full Projects page remains deferred) |
| Home browser regression | Projects autoplay/pause/next/keyboard work; phone-to-desktop clone wraparound fixed and rechecked; six partners visible; counters show 450 / 3,000 / 1,000 / 26 |
| Services browser regression | Hero/shared styling intact; FAQ opens by mouse and closes with Space, with visible keyboard focus |
| Console / Interactive Server | Final console shows normal normalization/connected-WebSocket information and no application errors/warnings; live server-side menu/FAQ actions succeed |
| MudBlazor | Provider/layout code unchanged; runtime assets pass Foundation checks. Full provider UI exercise was completed in Phase 3 and was not repeated in Phase 4. |
| Git / source integrity | Staged diff whitespace check passes; approved source/legacy assets unchanged; no unrelated page migration |

An initial new About test used a PowerShell variable name that conflicted with read-only `HOME`; it was corrected to `$homeHtml` and the suite rerun successfully. The preview was intentionally stopped before final builds to avoid Windows apphost locks, then restarted and hard-reloaded before final console/browser checks. Earlier browser automation interruption was resumed without bypassing a safety barrier. No browser security settings were changed.

Reproduce from the repository root (stop the local preview before builds on Windows):

```powershell
dotnet build src/NexNovaCo.Web/NexNovaCo.Web.csproj --no-restore -c Debug
dotnet build src/NexNovaCo.Web/NexNovaCo.Web.csproj --no-restore -c Release
node --test scripts/Test-HomeInterop.mjs scripts/Test-ServicesInterop.mjs scripts/Test-AboutInterop.mjs
# With the rebuilt app running:
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
./scripts/Test-About.ps1
```

## Deferred work and next approval

Projects listing/full detail, Team/full member detail and Contact are not migrated. Existing minimal detail stubs remain unchanged. Database/EF Core, persistence, CMS/admin/dashboard, authentication/authorization, CRUD, uploads, payments, email/newsletter backends, localization, full SEO, page builders and broad theme/accessibility rewrites remain out of scope. Temporary Owl/jQuery/CountUp dependencies and the static reference remain available for later migration decisions.

Recommended next step: **Phase 5 — Projects listing**, inspecting the actual approved listing before deciding how to reuse the existing inner hero, ProjectCard and canonical JSON. Detail-page scope should be agreed separately. Phase 5 has not started; stop here and await approval.
