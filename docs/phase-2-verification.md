# Phase 2 — Home migration and verification

## Scope and Git provenance

Home only. The public design remains the approved `index.html` theme, rendered by Razor inside the existing Interactive Server layout. .NET 10 and the exact MudBlazor 9.10.0 package constraint are unchanged. Other full pages remain placeholders; no persistence, CMS, authentication, admin, email or newsletter backend was introduced.

The starting working tree was clean on `main` at `ab4ed81` (`Phase1`). Inspection found that commit tracked build/IDE outputs, not the completed Blazor source. The verified source was on `feature/blazor-foundation` at `38718c9`. This was disclosed before editing, and `feature/home-migration` was created from that source branch. `main`, the remote and Phase 1 history were not rewritten or pushed.

Logical commits:

1. `0a479e0` — Create Home page content models and provider.
2. `0ad7d69` — Migrate Home page sections to Razor components.
3. Integrate Home interactions and visual parity fixes — integration, targeted adaptations, tests and this record.

## Files created or modified

Paths below are relative to the repository. All application paths are under `src/NexNovaCo.Web/` unless stated otherwise.

| Group | Files |
| --- | --- |
| Pages | `Components/Pages/Home.razor` (replaced placeholder), `ProjectDetailPlaceholder.razor`, `MemberDetailPlaceholder.razor` (new, in the same directory) |
| Sections | `Components/Sections/Home/HomeHero.razor`, `HomeWelcomeSection.razor`, `HomeServicesSection.razor`, `HomeProjectsSection.razor`, `HomeTeamSection.razor`, `HomeStatisticsSection.razor`, `HomePartnersSection.razor`, `HomeTestimonialsSection.razor` |
| Shared | `Components/Shared/SectionHeader.razor`, `ServiceCard.razor`, `ProjectCard.razor`, `TeamMemberCard.razor`, `StatisticItem.razor`, `PartnerCard.razor`, `TestimonialCard.razor`, `ThemeCarousel.razor`; parameterized existing `SocialLinks.razor` |
| Models | `Models/HomeContent.cs` |
| Services/content | `Services/IHomeContentService.cs`, `Services/HomeContentService.cs`; DI registration in `Program.cs` |
| CSS | New `wwwroot/css/home-blazor.css`; the existing `index.css`, `styles.css` and all other approved asset files remain unchanged in Git |
| JavaScript | New `wwwroot/js/home.js`, local `wwwroot/js/countUp.umd.js`, upstream `wwwroot/js/countUp.LICENSE.md`; existing jQuery/Owl files are activated without editing them |
| Document/import wiring | `Components/App.razor`, `Components/_Imports.razor` |
| Tests | Updated repository `scripts/Test-Foundation.ps1`; new `scripts/Test-Home.ps1`, `scripts/Test-HomeInterop.mjs` |
| Documentation | Repository `README.md`, `docs/phase-2-verification.md` |

The root static HTML, `assets/`, five existing public placeholder pages, layout implementation and NuGet lock file were not changed by this phase.

## Final Home structure

```text
MainLayout (existing header, navigation, providers and footer)
└── Home.razor — loads IHomeContentService; scopes Home CSS and interop
    ├── HomeHero ← HomeHeroContent (dedicated composition, not an inner-page hero)
    ├── HomeWelcomeSection ← WelcomeContent
    ├── HomeServicesSection
    │   └── ServiceCard × 5
    ├── HomeProjectsSection
    │   ├── SectionHeader
    │   └── ThemeCarousel → ProjectCard × 5
    ├── HomeTeamSection
    │   └── TeamMemberCard × 4 → SocialLinks
    ├── HomeStatisticsSection
    │   └── StatisticItem × 4
    ├── HomePartnersSection
    │   ├── SectionHeader
    │   └── ThemeCarousel → PartnerCard × 6
    └── HomeTestimonialsSection
        └── ThemeCarousel → TestimonialCard × 2
```

## Models and replaceable content

All records are small rendering contracts, not database entities.

| Model | Purpose |
| --- | --- |
| `HomeHeroContent` | Three headline lines, description and CTA |
| `SectionHeading` | Shared title and optional description |
| `WelcomeContent` | Welcome title, introduction, paragraphs and CTA |
| `ServiceSummary` | Canonical ID/name, tagline, description and icon |
| `ProjectSummary` | Slug, name, tagline, summary, cover and clean detail link |
| `TeamMemberSummary` | Slug, name, canonical role/image/social data, editorial teaser and profile link |
| `TeamSectionContent` | Home's one-off team promotional text and CTA |
| `Statistic` | Label and integer value |
| `Partner` | Name, description, logo treatment and optional real URL |
| `Testimonial` | Paragraphs and attribution |
| `HomeContent` | Aggregate supplied to the Home composition |

`IHomeContentService.GetAsync(CancellationToken)` is the replacement boundary. Its temporary singleton implementation reads the canonical JSON once per application lifetime and combines it with the approved editorial Home copy. Restart after editing the data files. A future database-backed implementation can be registered in DI without changing the visual components or adding persistence responsibilities to these records.

- `wwwroot/data/projects.json`: canonical name, second name/tagline and subtitle. Featured order: NexConnect, PayFlowX, MediLink, TradeSync, EduVance. Listing cover paths intentionally follow `image/project/{slug}.jpg`; they are not inferred from detail galleries, which differ for NexConnect.
- `wwwroot/data/member.json`: canonical identity, role, image, email, LinkedIn and Telegram. Featured order: Emily Johnson, Emma Williams, Sophia Lee, Daniel Kim. Existing `assets/` image prefixes are normalized for new markup.
- Typed in-memory content from `index.html`: hero/welcome/promotional copy; short member editorial teasers; service content and approved icon mapping; statistics; partner logos/descriptions; both testimonials; featured ordering.
- Services keep Software Dev/online-services, Web & Mobile App/software, AI Solutions/analysing, Tech Consulting/process, UX/UI Design/custom. No conflicting entity roles or project summaries were copied into cards.
- Partner URLs remain absent. Cards render a non-link when no URL is supplied. Empty member social destinations remain decorative; no URL was invented.

## Reusable component contracts

| Component | Typed input / rendering | Expected later reuse |
| --- | --- | --- |
| `SectionHeader` | `SectionHeading`; actual shared heading/description pattern, semantic H2/H3 choice | Projects, Partners/About where the same pattern exists |
| `ServiceCard` | `ServiceSummary`; original icon and shaped service card | Services |
| `ProjectCard` | `ProjectSummary`; original cover, overlapping hexagonal copy and detail CTA | Projects listing |
| `TeamMemberCard` | `TeamMemberSummary`; original portrait, linked name, role, teaser and social row | Team |
| `StatisticItem` | `Statistic`; readable final value plus an enhancement target without a global ID | About / other statistics bands |
| `PartnerCard` | `Partner`; logo, optional logo background, description and optional link | About / partner presentation |
| `TestimonialCard` | `Testimonial`; existing quote icon, paragraphs and attribution | About or other testimonial sections |
| `SocialLinks` | Name plus optional email/LinkedIn/Telegram, with compatible footer defaults | Team/member pages and shared footer |
| `ThemeCarousel` | Kind, accessible label and static Razor child content; original Owl container | Only pages that actually use this same carousel pattern |

Reusable does not mean visually universal. No generic layout/style engine or speculative card variants were added. Home-specific backgrounds, breakpoints and promotional compositions remain in Home section components.

## Interaction classification and lifecycle

| Behavior | Implementation / migration classification |
| --- | --- |
| Content rendering, featured lists, internal routes | Blazor/Razor; no client-side entity fetching or HTML generation |
| Mobile navigation | Already handled by Phase 1 Blazor state, accessible toggle and Escape behavior |
| Sticky header and back-to-top | Already handled by Phase 1's scoped `public-shell.js` interop |
| Three Owl carousels | Retained Owl 2.3.4 + jQuery 3.1.0, temporarily enhanced through Home-scoped interop |
| Scroll entrance effects | Keep the existing AOS CSS, transforms and delays through interop; replace the global AOS JS engine with scoped IntersectionObservers because its refresh path adds global listeners without a destroy API |
| Statistics | Retain CountUp 2.0.8 through Home-scoped interop; animate on first intersection; no hardcoded global IDs |
| Pause/play, arrows, focus/hover pause | Scoped JS enhancement; labeled controls and live/hidden state |
| Loader/document-ready gate | Removed from the Blazor execution path as obsolete; SSR content is readable immediately |
| Legacy `script.js`, project/member binding, contact and Bootstrap JS | Not activated; unrelated/full-document handlers remain dormant |

`OnAfterRenderAsync` imports Home's module once per component. A WeakMap prevents duplicate initialization. A browser-side MutationObserver observes the shared main container and disposes plugins, media/visibility/control handlers, count animations and intersection observers when Home is removed, including when server-side interop is no longer available. The module reference is disposed separately by Razor; a late import after component disposal is also released.

Owl temporarily wraps and clones **already-rendered** Razor cards. `ThemeCarousel.ShouldRender` freezes that static child subtree after its first render. Never put stateful Blazor controls or event handlers inside that subtree. A future content update must remount it with a new key and coordinate enhancement; it must not diff plugin-owned DOM. This is a transitional boundary, not a general live-editable carousel API. For context, see Microsoft's [DOM interaction and cleanup guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0) and Owl's [destroy/event API](https://owlcarousel2.github.io/OwlCarousel2/docs/api-events.html).

The unused Owl AutoHeight plugin is excluded at initialization, without modifying its vendor file: this bundled version installs anonymous window listeners its destroy method does not remove. None of the approved Home carousels enables autoHeight. This avoids that known listener leak while retaining the working carousel implementation.

CountUp is the original static page's version, now served locally instead of its CDN. Source: [cdnjs 2.0.8](https://cdnjs.cloudflare.com/ajax/libs/countup.js/2.0.8/countUp.umd.js). License: upstream [countup.js@2.0.8 LICENSE.md](https://cdn.jsdelivr.net/npm/countup.js@2.0.8/LICENSE.md), copied alongside the local script. No vendor upgrade or replacement was performed.

Accessibility improvements: one H1; existing meaningful image labels; named regions; labeled previous/next/dot controls; pause buttons; arrow-key navigation; automatic pause on hover/focus/document hiding; inactive slides and clones are inert/aria-hidden; visible focus styling. When every partner fits and Owl hides controls, autoplay stops rather than animating an unpausable full row; responsive refresh updates this state. Reduced motion disables autoplay, slide transition speed, reveal transforms and counter animation, while retaining readable final numbers. UI remains public theme HTML, not MudBlazor card/paper/grid styling.

## CSS adaptations and known differences

The original Home and global stylesheets remain unchanged. `home-blazor.css` only:

- Removes the placeholder main padding while Home is mounted.
- Restores the theme's normal letter spacing and native scrollbar width on Home, neutralizing inherited Mud defaults that changed text wrapping and geometry.
- Makes SSR/fallback content visible without the old loader, and supplies a non-enhanced carousel fallback.
- Keeps the existing AOS durations and adds reduced-motion handling.
- Styles the added pause control in the existing orange navigation row and provides keyboard focus visibility.
- Repositions desktop testimonial controls inside the existing fixed-height panel: the original 100%-height stage placed them behind the footer. No section-height or artwork change is needed to make them clickable.

No palette, font, breakpoint, shape, image, card, hover or spacing-system redesign occurred. Home CSS leaves with the route. All approved heading/paragraph text is checked against the original HTML by `Test-Home.ps1`.

Known small differences: an extra pause/play control; visible, clickable desktop testimonial controls inside the panel; focus outlines and non-interactive empty social/partner links; SSR content replaces the loader; entrance timing is observer-based rather than the global AOS engine. The inherited Phase 1 hamburger remains blue on the pale header instead of the static desktop-menu variant's white icon over artwork. Carousel controls produce approximately 2px less project-section height and, at tablet/mobile widths, 4px less partner/testimonial height. These small control-row differences are not a layout redesign. The full six-partner desktop row no longer autoplays while its controls are hidden. Autoplay elsewhere means screenshots may show different current slides.

## Verification — 2026-09-17

Direct comparison used the approved static page at `127.0.0.1:5139/index.html` and Blazor at `localhost:5138/` in the in-app Chromium browser, with identical viewport sizes. Verification used viewport screenshot inspection and rendered DOM dimensions, not an automated pixel-diff golden-image assertion. Measurements were taken after CSS/font/entrance transitions settled; early transient captures were not treated as final geometry.

| Viewport | Result |
| --- | --- |
| 1440 × 900 | Original desktop hero/photo/hexagons, side-by-side welcome/services, 3 projects, 4 team cards, statistics band, 6 partners, split testimonials and footer preserved; no horizontal overflow |
| 1366 × 900 | Original hamburger breakpoint and hero artwork switch preserved; service introduction moves above its cards; 3 projects and 6 partners; no horizontal overflow |
| 768 × 900 | Centered hero, hidden welcome photo, original service grid/stacking, 2 projects, 2-column team, 3 partners and single-panel testimonials preserved; no horizontal overflow |
| 390 × 844 | Original mobile hero line structure and CTA, stacked service/team/statistic content, one project/partner/testimonial at a time, mobile footer preserved; no horizontal overflow |

Measured hero heights: 900/900/900/844px. Measured welcome/service/team/statistics/footer section heights match the static reference at all four widths. Project cards remain 600px high, partner logos 150px square, and card widths/portrait layouts match the reference. Small carousel-control height differences are listed above.

Build and automated checks:

- `dotnet restore NexNovaCo.sln --locked-mode`: pass, packages unchanged.
- Debug and Release `dotnet build --no-restore`: pass, 0 warnings and 0 errors.
- `Test-Foundation.ps1`: pass; 6 direct routes, 82 unchanged asset contents, canonical/legacy URLs, 40 rendered resource URLs including fingerprints, runtime resources and 404 shell.
- The inherited asset test encountered mixed/CRLF checkout differences. It now tolerates only CRLF/LF differences for text files and checks served lengths against the deployed copy; binary/content differences still fail. No source asset was rewritten to make the test pass.
- `Test-Home.ps1`: pass; eight ordered sections, one H1, original heading/paragraph text, typed featured subsets, canonical JSON values, all nine detail/profile stubs, two unknown-slug 404s, 22 SSR images with alt text, and Home assets.
- `node --test scripts/Test-HomeInterop.mjs`: 2/2 pass; duplicate initialization guard, plugin destruction/observer disposal, detached listener cleanup, reduced-motion state/final numbers, manual pause and preference changes. These use isolated mocks, not a substitute for real Owl browser tests.
- No new dependencies were needed for tests.

Browser functional checks:

- All five existing placeholder routes were visited through shared navigation and returned to Home. Each return had exactly 3 Owl stages and 3 pause controls; away pages had 0 stages. Mobile clone counts remained stable at 16 rather than accumulating. An additional round trip passed after the final control fix.
- Project previous/next and arrow-key navigation changed the active slide; pause/play state updated. Mobile partner next advanced to Digital Co. Desktop testimonial next advanced from Olivia Carter to Daniel Kim after the control-position fix.
- Inactive slides had `inert` and `aria-hidden`; zero hidden slides were missing the inert attribute in the final check.
- Counters visibly advanced during scrolling and settled at 450 / 3,000 / 1,000 / 26.
- Mobile menu opened, followed an internal link and closed; Escape closed it and returned focus to Open menu. Back-to-top completed its smooth scroll to zero and focused main content.
- The opt-in Development diagnostic displayed a working snackbar, opened/closed a dialog, and opened the selection popover. Selecting “Server interactivity works” was reflected in the combobox's rendered text. These server-handled actions also confirmed the Interactive Server circuit remained connected.
- A featured member link navigated to the minimal Emily Johnson profile placeholder; direct HTTP checks cover all nine featured stubs and unknown-slug 404s.
- Final application browser session: no console errors or warnings, no broken loaded images, no duplicate plugin roots. All 22 SSR image URLs and the full inherited asset set passed HTTP checks. Temporary expected disconnect messages while deliberately restarting the development server were excluded from final-session results.

Reduced-motion logic is covered by the isolated tests and CSS review; this browser tool did not expose OS media-preference emulation, so no claim of a real OS-toggle browser test is made. Cross-browser Safari/Firefox and physical-device touch testing remain outside this local Chromium pass.

## Deferred and recommendation

Do not migrate any additional full page in this phase. Services, Projects listing/details, Team/member details, About and Contact remain deferred, along with persistence, CMS, admin/dashboard, authentication, uploads, email/newsletter backend, full SEO, localization and theme editing. Existing demo statistics/testimonials remain the approved sample content; this phase did not verify or invent business claims or partner destinations.

Recommend **Phase 3: Services** because the canonical service contract and original service card are now reusable. Inspect that full page before deciding whether its distinct layout needs any additional component boundary. Approval is required; Phase 3 has not started.
