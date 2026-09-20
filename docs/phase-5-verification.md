# Phase 5 — Projects listing migration and verification

Completed 2026-09-19. Scope: `/projects` only, on the existing .NET 10 / Interactive Server / MudBlazor 9.10.0 foundation. Project Detail remains a navigation-only placeholder. The original `project.html`, `assets/css/project.css`, global theme styles, project JSON, Home cards, testimonial/carousel implementation and detail stub were inspected before implementation.

## 1. Git status

- Branch throughout: `feature/blazor-public-site`.
- Starting state: clean at `a80db56` — `Verify About visual parity and regressions`. Continuations resumed the same uncommitted Phase 5 work.
- Implementation: `15fa300` — `Migrate Projects listing with canonical shared content`.
- Verification: `Verify Projects visual parity and regressions` — the commit containing this record, README and tests; its hash is reported in the handoff.
- Ending state: all Phase 5 work committed; clean working tree checked after the verification commit.
- No new branch, main checkout, history rewrite or push. Root static HTML/assets and all 82 copied legacy assets remain unchanged. No dependency or lock-file changes.

## 2. Files changed

Application paths below are relative to `src/NexNovaCo.Web/`.

| Group | Files |
| --- | --- |
| Pages | `Components/Pages/Projects.razor`; small imports/composition changes in `Home.razor`; catalog lookup only in `ProjectDetailPlaceholder.razor` |
| Sections | New `Components/Sections/Projects/ProjectsGridSection.razor`; moved Home testimonials to `Components/Sections/Shared/TestimonialsSection.razor`; `Components/_Imports.razor` |
| Shared components | `Components/Shared/ProjectCard.razor`, `TestimonialCard.razor`; ownership comment only in `ThemeCarousel.razor` |
| Models | New `Models/ProjectsContent.cs`; existing entity contracts unchanged |
| Services/content | New `Services/{IProjectCatalog,ProjectCatalog,IProjectsContentService,ProjectsContentService,TestimonialCatalog}.cs`; reuse in `HomeContentService.cs`; DI registrations in `Program.cs` |
| CSS | New `project-card-blazor.css`, `testimonials-blazor.css`, `projects-blazor.css`; small extraction from `home-blazor.css`; extended selectors in `inner-page-blazor.css` |
| JavaScript | New `wwwroot/js/projects.js`; existing shared carousel/reveal modules unchanged |
| Tests | New `scripts/Test-Projects.ps1`, `scripts/Test-ProjectsInterop.mjs`; updated `Test-Foundation.ps1`, `Test-Home.ps1`, `fixtures/CarouselFixture.mjs` |
| Documentation | `README.md`, this record |

## 3. Projects component tree

```text
MainLayout — existing header/navigation, main, footer and MudBlazor providers
└── Projects.razor
    ├── InnerPageHero
    ├── ProjectsGridSection
    │   ├── Bootstrap grid → article → ProjectCard × 7
    │   └── decorative pagination spans
    └── TestimonialsSection
        ├── ThemeCarousel (testimonials)
        │   └── TestimonialCard × 2
        └── existing decorative NexNovaCo brand panel
```

The listing follows the hero; testimonials follow pagination and transition directly to the shared footer. No extra CTA, duplicate hero or Projects-specific testimonial wrapper was needed.

## 4. Existing components reused

`ProjectCard`, `InnerPageHero`, `TestimonialCard` and `ThemeCarousel` keep their existing parameter APIs. The former Home testimonial section becomes a shared section because the approved Home/Projects composition is identical. `SectionHeading` supplies brand content; the decorative panel's existing markup is retained rather than forcing it through `SectionHeader`, whose composition differs. No duplicate shared component or arbitrary style parameter was introduced.

Project names remain H2s below the page's single H1. Taglines and testimonial attributions are content, not additional headings: their former H5s become paragraphs with narrow shared CSS preserving their original font/size/color/spacing. Image alt text, descriptive Read More labels, normal link semantics and existing focus-visible styling are retained. This is not a full-site heading/accessibility rewrite.

## 5. Project data

`IProjectCatalog.GetAsync(CancellationToken)` is the replaceable canonical boundary. Its temporary singleton implementation loads `wwwroot/data/projects.json` once into a read-only collection of the unchanged `ProjectSummary(Slug, Name, Tagline, Description, ImagePath)` records. Caller cancellation does not cancel the shared snapshot; restart after editing JSON.

Home selects its original five IDs from this catalog. Projects uses all seven in JSON order, which matches `project.html`. Detail placeholders resolve the same records. The HTTP suite compares the first five rendered card bodies with Home byte-for-byte, as well as checking JSON values and static order.

The narrow JSON projection uses only `id`, `name`, `secondName` and `subtitle`. Approved listing images are `image/project/{id}.jpg`; these intentionally do not always equal the first detail-gallery image, notably for NexConnect. No gallery/features/detail model was added. `ProjectsContent` aggregates existing hero, project, testimonial and heading contracts; no speculative entity fields, database, CRUD or competing content architecture.

## 6. Project listing

Approved order and copy: **NexConnect → PayFlowX → MediLink → TradeSync → EduVance → AutoTracker → FinVault**. All seven images, names, taglines, descriptions and Read More treatments are retained.

The original `container-fluid container-xl`, row and `col-12 col-sm-6 col-md-6 col-lg-4` wrappers preserve three columns at desktop, two from 576px and one below that. Each record owns a keyed semantic article. AOS-style reveal delays retain the source ordering. There is no list carousel, search, filter or invented project.

## 7. Detail links

Every card targets `/projects/{slug}`. All seven resolve to the existing minimal title/message/back-link placeholder, including the newly reachable AutoTracker and FinVault. Unknown slugs return HTTP 404. Live Chrome testing confirmed AutoTracker's Read More and Back to projects links. Only the stub's lookup dependency changed; no legacy `.html` link, detail gallery, detail script or Phase 6 content was introduced.

## 8. Pagination

The approved `« 1 2 3 4 5 6 »` sample retains its highlighted **2** and spacing. It is an `aria-hidden="true"` div of eight spans, not a navigation landmark. It has no links, buttons, tabindex, event handlers or data paging. Targeted CSS removes pointer interaction/selection. The HTTP suite checks these constraints. No real or fake page-changing behavior exists.

## 9. Testimonials / carousel

`TestimonialCatalog` holds the existing two approved quotes, Olivia Carter and Daniel Kim, plus the shared NexNovaCo brand heading/copy. Both Home and Projects providers use it. The HTTP test confirms their testimonial card markup is identical.

Razor renders the content; shared `ThemeCarousel` provides the static plugin-owned rendering boundary. Projects initializes only this one carousel through existing `carousels.js`. Autoplay, hover/focus pausing, explicit pause/play, previous/next, keyboard arrows, reduced-motion handling and inactive-slide semantics are inherited. Live checks observed autoplay, Pause changing to Play, Next advancing Olivia to Daniel, Left returning to Olivia, and Next working after route re-entry. No duplicate control sets appeared.

## 10. JavaScript / interop classification

| Interaction | Ownership |
| --- | --- |
| Canonical project/testimonial rendering | Razor + typed providers; JavaScript generates no business content |
| Hero anchor and detail links | Native links and Blazor routing |
| Mobile menu / active routes | Existing Blazor shell state / NavLink |
| Testimonials | Existing shared interop retaining Owl 2.3.4 / jQuery temporarily |
| Scroll reveal | Existing `reveal.js` with approved AOS CSS; global AOS engine remains dormant |
| Sticky header / back-to-top | Existing layout-owned shell interop |
| Hover effects | Existing CSS, with reduced-motion adaptation |
| Pagination | Decorative only; obsolete fake navigation removed in the earlier cleanup stays removed |
| Home counters | Home-only CountUp; not initialized by Projects |
| Legacy global/detail scripts and Bootstrap JS | Dormant; no new loading or initialization |

`projects.js` adds only route ownership: WeakMap idempotence, reduced-motion query, shared carousel/reveal initialization, removal observer and pagehide cleanup. The page follows the existing cancellation/late-import/disposal pattern. Detached roots destroy plugins/listeners without late server interop. Never add Blazor event-bound/stateful children inside the plugin-frozen `ThemeCarousel`; use keyed remounting for changed content.

## 11. Styling and sizing

The approved `project.css` and `styles.css` load unchanged. Projects extends the existing shared inner-page hero/shell selectors. The listing-specific adaptation file only handles SSR-visible reveal content, original transition duration, inert pagination and reduced motion.

`project-card-blazor.css` preserves the former tagline H5 typography after its semantic change. No card geometry was extracted from Home: each original page stylesheet already contains its correct geometry. `testimonials-blazor.css` preserves attribution typography and moves Home's existing desktop control-placement adaptation into shared ownership. Home imports both files. No broad selector rename, new breakpoint system, redesigned assets, token rewrite or mass `!important`.

Fixed card sizing remains: 600px item, 300px image, 453px panel positioned 135px from the top; the source's 500–600px special case remains 700px/556px. Approved copy fits at all required widths, including longer AutoTracker/FinVault text. These dimensions are intentional parity debt, not a guarantee for arbitrary future content. A content-driven/min-height redesign should be reviewed separately before editors can lengthen copy.

## 12. Visual verification

Method: Chrome responsive emulation, comparing `http://localhost:5138/projects` directly with `http://127.0.0.1:5140/project.html`, using the Computer Use skill. All required viewports used 1000px height. Screenshots were inspected inline, not saved as a pixel-diff artifact. Overflow findings are visual observations, not numeric `scrollWidth` assertions.

| Width | Result |
| --- | --- |
| 1440px | Hero/header, grid width, three-column rows, all seven cards, image crops, hexagonal panels, typography/gaps, final single-card row, pagination and testimonial/footer transition match. |
| 1366px | Matching hamburger breakpoint, hero geometry, narrower three-column card wrapping and testimonial brand panel. Hero CTA reaches the listing. |
| 768px | Matching mobile hero crop/title, two-column grid, card wrapping, pagination and stacked footer. The source's white-on-white testimonial defect is reproduced, described below. |
| 390px | Matching single-column card geometry, phone hero crop, longer card copy, final-card/pagination gap, quote icon/text wrapping, hidden brand panel and stacked footer. |

No horizontal overflow, broken project images or clipped approved card copy was observed. Home's affected project-card/testimonial typography and desktop composition were rechecked; its full-page four-width baseline remains recorded in earlier phases.

Meaningful intentional/inherited differences and limitations:

- **Inherited 768px defect:** `project.css` sets the testimonial background white at `min-width:768px`, while global `max-width:768px` rules remove the individual content background and leave white text. At exactly 768px, body copy/quote become white-on-white in both the approved reference and Blazor; attribution/controls remain visible. This needs a separately approved, narrowly scoped design correction. It is not represented as an accessible or defect-free tablet result.
- Desktop testimonial controls are kept above the footer through the already-approved Home adaptation; source controls can fall underneath it. Shared controls are centered, labelled and keyboard-accessible, with pause/play and dots; static placement/control accessibility differs. Slide identity can differ because autoplay timing varies.
- The shared shell retains prior demo-newsletter semantics, current-year copyright, active routes, focus and back-to-top behavior. Its newsletter line-height/button metrics differ slightly from the legacy shell; no new footer redesign.
- The source's white decorative hero edge crosses the white brand treatment at 768px. Retained as in Services/About.
- Source fixed heights, clipped shapes and editorial limits remain technical debt. No full contrast or arbitrary-content audit is claimed.

The earlier Computer Use URL-confidence stop was respected. Verification resumed after the user reopened the local preview; no safety or browser security settings were bypassed. The skill constrained testing to visible browser UI and screenshots.

## 13. Runtime / regression verification

| Check | Result |
| --- | --- |
| Debug / Release | Both builds pass with **0 warnings / 0 errors** for the completed implementation |
| Foundation HTTP | Pass: six direct routes, 82 unchanged copied assets, canonical/legacy serving, 54 rendered asset URLs, framework resources and HTTP 404 shell |
| Home HTTP | Pass: eight sections, approved text, original featured JSON subsets, nine original detail links, two unknown-detail 404s, 22 images/alt text and shared styles |
| Services HTTP | Pass: six sections, canonical cards, three benefits, five process slots, three disabled pricing actions and six accessible FAQ panels |
| About HTTP | Pass: six sections, chronological timeline, four commitments, six partners identical to Home, route-safe links and images |
| Projects HTTP | Pass: three sections, seven canonical cards/order/stubs, unknown-slug 404, Home card equality, decorative pagination, identical shared testimonials, one H1, clean routes and assets |
| Node interop | **12 tests pass, 0 failures**; lifecycle/idempotence/detach/pagehide, reduced motion, missing-Owl fallback, counters/reveals, responsive clones and Home → Projects → About → Projects |
| Live route sequence | Home → Projects → About → Projects through shell links; testimonial Next remains functional after remount, with a single control set |
| Home browser | Original hero/partners/testimonials intact; project autoplay/pause/Next/Left work; counters reach 450 / 3,000 / 1,000 / 26 |
| Services browser | Shared hero intact; FAQ opens by click and closes with Space, with visible focus |
| About browser | Hero/story anchor and all six desktop partners remain intact; detailed partner keyboard/autoplay baseline and clone tests remain covered by Phase 4 and the rerun interop suites |
| Mobile navigation | Projects active state; menu opens, Escape closes/restores visible focus, Space reopens |
| MudBlazor providers | Development-only MudSelect popover opens/selects; snackbar success message appears; dialog opens above the shell and closes |
| Interactive Server / console | Live server-side FAQ/menu/provider actions succeed; final console shows normal normalization/WebSocket-connected messages, no application errors/warnings |
| Chrome Issues | 0 page errors, 0 breaking changes; inherited advisory image-dimension and form id/name metadata warnings remain (51 accumulated improvements after cross-page checks) |
| Integrity | Approved static HTML/assets/data unchanged; staged whitespace check passes; no other full-page migration |

The new Projects HTTP test initially matched a dormant detail script filename inside the generated import map. Its assertion was narrowed to actual script loading/detail DOM, then rerun successfully; the app was not loading that script. Browser visual checks and provider checks are distinct from the dependency-free mocked interop tests; the latter do not prove layout.

Reproduce (stop a running Debug preview before rebuilding that configuration on Windows):

```powershell
dotnet build src/NexNovaCo.Web/NexNovaCo.Web.csproj --no-restore -c Debug
dotnet build src/NexNovaCo.Web/NexNovaCo.Web.csproj --no-restore -c Release
node --test scripts/Test-HomeInterop.mjs scripts/Test-ServicesInterop.mjs scripts/Test-AboutInterop.mjs scripts/Test-ProjectsInterop.mjs
# With the rebuilt app running on localhost:5138:
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
./scripts/Test-About.ps1
./scripts/Test-Projects.ps1
```

## 14. Deferred work

Project Detail, Team, Member Detail and Contact remain placeholders. No database/EF Core, persistence, authentication/authorization, CMS/admin/dashboard, CRUD, uploads, search/filter, real pagination, payments, email/newsletter service, localization, SEO overhaul or page builder was added. Owl/jQuery/CountUp replacement, fixed-height content safety and the inherited 768px testimonial contrast defect are deferred. The approved static reference remains intact.

## 15. Recommendation for Phase 6

Inspect approved `inner-project.html`, its stylesheet/interaction script and every canonical JSON detail record before implementation. Reuse `/projects/{slug}` and canonical IDs, but introduce a detail-specific typed model/provider for only the fields actually rendered by the approved detail page. Keep listing covers separate from gallery images; reuse the current shared shell/hero only where the detail composition genuinely matches. Preserve unknown-slug 404s and add direct-link, back-navigation, gallery, lifecycle, accessibility and four-width tests. Continue on this branch with explicit approval. **Phase 6 has not started.**
