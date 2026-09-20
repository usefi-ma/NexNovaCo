# Phase 7 — Team and Member Detail verification

Verified locally on 2026-09-19 (America/Denver). Scope: `/team` and `/team/{slug}` only. Contact remains a placeholder; Phase 8 has not started.

## 1. Git status

- Branch: `feature/blazor-public-site`; no branch switch or history rewrite.
- Starting commit: `d4850ceedcca59d6cd85c586c5c0e651045957f1`; the working tree was clean before implementation.
- Changes are separated into Team/catalog, Member Detail, and verification commits. The final handoff supplies their hashes and the final working-tree check; this record does not embed its own future commit hash.
- Original root HTML, `assets/`, and all 82 copied legacy assets remain unchanged. No dependency or lock-file changes.

## 2. Files changed

Paths below are relative to `src/NexNovaCo.Web/` unless otherwise stated.

| Group | Files |
| --- | --- |
| Team page | `Components/Pages/Team.razor` replaces its placeholder |
| Member Detail | Added `Components/Pages/MemberDetail.razor`; removed `MemberDetailPlaceholder.razor` |
| Sections | Added `Components/Sections/Team/{TeamIntroSection,TeamGridSection,MemberProfileContent}.razor` |
| Shared components | `Components/Shared/{TeamMemberCard,Breadcrumbs,SocialLinks}.razor`; `Components/_Imports.razor` |
| Models | Added `Models/TeamContent.cs`: page composition, member detail and card-context enum |
| Services/content | Added `Services/{IMemberCatalog,MemberCatalog,ITeamContentService,TeamContentService}.cs`; updated `HomeContentService.cs` and `Program.cs` |
| CSS | Added `wwwroot/css/{team-blazor,member-blazor}.css`; extended the existing route selector lists in `inner-page-blazor.css` with Team only |
| JS | Added `wwwroot/js/team.js` for shared Team/profile reveal lifecycle only |
| Tests | Added repository `scripts/{Test-Team.ps1,Test-MemberDetail.ps1,Test-TeamInterop.mjs}`; updated `Test-Foundation.ps1` and `Test-Home.ps1` to expect real Team/profile pages |
| Docs | Repository `README.md` and this verification record |

## 3. Team component tree

```text
MainLayout (existing header/navigation, main, footer, providers)
└─ Team
   ├─ InnerPageHero
   └─ section#Team
      ├─ TeamIntroSection (approved photography, copy, quote, Contact CTA)
      └─ TeamGridSection
         └─ TeamMemberCard × 6 [Context=Listing]
            └─ SocialLinks
```

The original decorative photography is hidden from assistive technology. Shared hero semantics retain one logical H1 and hide the mobile visual title duplicates from the accessibility tree.

## 4. Member Detail component tree

```text
MainLayout (existing shared shell)
└─ MemberDetail [route /team/{Slug}]
   └─ MemberProfileContent [key = canonical slug]
      ├─ Original member_header geometry
      │  └─ Breadcrumbs (Home / Team / current member)
      └─ section.member_desc
         ├─ Overlapping hexagonal portrait
         ├─ Member H1, role, rule, full biography
         ├─ Skills H2 and responsive two-column skill list
         └─ SocialLinks
```

The source member header is different from the standard inner-page hero; it was preserved instead of forcing it into `InnerPageHero`. Three small Team sections are sufficient; no separate component was added for every profile field.

## 5. Canonical member data

`IMemberCatalog` now owns one lazy, application-lifetime JSON snapshot. `MemberCatalog` reads unchanged `wwwroot/data/member.json`; Home no longer deserializes or projects a separate member catalog. Home, Team and Detail share the same summary objects.

The existing `TeamMemberSummary` contract is unchanged. `MemberDetail` adds only `Biography` and `IReadOnlyList<string> Skills` around that summary. Name, role, image, email, LinkedIn and Telegram remain JSON-owned. The six short card teasers come from approved `team.html`; the first four previously lived in the Home provider. They are editorial summaries, not competing identity/role/social records or alternate full biographies.

`ITeamContentService` combines the canonical collection with typed Team hero/intro copy. Providers remain replaceable through DI; there is no database, EF Core, persistence, CRUD or speculative metadata. Restart the app after editing the canonical JSON snapshot.

## 6. TeamMemberCard reuse

Team renders the existing component directly, without copied card markup. Its sole new API is `TeamMemberCardContext Context`, defaulting to `Featured` for existing Home callers. `Listing` changes only the portrait class from Home's `horizental_hexagon` to Team's `hexagon`, matching the two approved source silhouettes. Route CSS continues to own dimensions and layout.

The HTTP suite compares the four shared Home/Team cards byte-for-byte after normalizing only that portrait class. Content, card markup, roles and social destinations agree. No arbitrary color/spacing/style parameters were introduced.

## 7. Team listing

All six entries render in approved JSON/static order:

1. Emily Johnson
2. Emma Williams
3. Sophia Lee
4. Daniel Kim
5. Lena Alvarez
6. James Park

Each card retains its portrait, name, canonical role, short teaser, social treatment and clean profile links. Original CSS keeps the two-column grid from 768px, single-column phone stack, and internal card stacking at the existing tablet breakpoint. The decorative intro photograph stays hidden at widths of 1200px and below. The original Contact CTA remains a link to the unmigrated `/contact` route.

## 8. Member routing

`/team/{Slug}` resolves exact canonical IDs with ordinal comparison, supports direct requests/bookmarks, and renders the corresponding title and profile. Async request/version and disposal guards prevent old lookups from replacing newer navigation. A slug key remounts the profile's visual lifecycle.

Missing IDs call `NavigationManager.NotFound()`. Both tested invalid slugs return HTTP 404 with the shared header/footer and no profile or fallback member. The browser showed the proper not-found view and working Return Home link. A direct request to an invalid URL produces Chrome's expected HTTP-404 network entry, not a JavaScript/circuit exception; successful routes have no console errors.

## 9. Member content and accessibility

- The actual name binds the page title, profile H1 and current breadcrumb item.
- The role, full biography, portrait, email, LinkedIn and all four skill strings come from the current record. All six members and 24 skill items are checked automatically.
- The source H2 member name becomes the page's H1; its role becomes a paragraph; Skills becomes an H2. Targeted CSS preserves the original H2/H4 appearance.
- Skills preserve source order and the original two-column grouping, becoming a single column below the existing `sm` breakpoint. Accessible list/list-item roles do not change visual spacing.
- Shared `SocialLinks` exposes configured email and LinkedIn as real named links. Empty or `#` destinations have no href, no keyboard focus stop and are hidden from assistive technology. Existing unconfigured Telegram artwork stays visible but inert.
- The breadcrumb remains a named navigation landmark with `aria-current`. Portraits use actual member names for alt text. Existing focus-visible styling and mobile Escape/focus restoration remain intact.

No progress bars, percentages, invented social URLs or extra profile fields were added; none exist in the approved member content.

## 10. Legacy JavaScript

The original `member.js` and all static references are retained unchanged, but no Blazor route loads that script. Member lookup, titles, images, skills and socials are Razor-owned. No new browser code fetches JSON, binds query-string IDs, writes `innerHTML` or mutates profile content.

The new 20-line `team.js` module only initializes the existing `reveal.js` helper once per route root and cleans it up on removal/pagehide. Team and Member Detail do not initialize Owl, counters, Bootstrap JavaScript or a carousel. Reduced motion remains supported.

## 11. Styling

- Unchanged `team.css` and `member.css` are loaded through route-local `HeadContent`.
- Existing `inner-page-blazor.css` selector lists now include `.team-page`; no existing declarations or breakpoints changed.
- `team-blazor.css` supplies visible prerender/no-JS content and existing reveal/reduced-motion behavior only.
- `member-blazor.css` scopes shell geometry to `.member-detail-page`: the source 80px header, logo/menu alignment, original white/orange colors, source normal-flow offset and original scrollbars/letter spacing. It also preserves typography after semantic heading changes and adds reveal fallback/reduced-motion rules.
- `Breadcrumbs.RevealDelay` defaults to its previous 100ms; Member passes the original 0ms. Optional `SocialLinks.RevealDelay` preserves the member page's 700/800/900ms icon timing without changing other callers.

No broad global override, new breakpoint system, mass `!important`, public MudBlazor redesign, source stylesheet rewrite or shared carousel change was introduced.

## 12. Visual verification

Method: actual Chrome responsive viewport comparisons through the Computer Use skill against the loopback static reference (`team.html`, `member.html?id=emilyjohnson`) and the running Blazor app. These are human-style rendered UI inspections, not automated pixel diffs or numeric DOM overflow assertions. Screenshot observations are in the task history, not saved as repository artifacts.

| Width | Team comparison | Emily Member Detail comparison |
| --- | --- | --- |
| 1440px | Pass: desktop hero, decorative intro/photo, two-column card grid, portrait geometry and typography | Pass: clipped header, overlap/crop, name/role, centered biography, two-column skills, socials and footer transition |
| 1366px | Pass: compact navigation, intro photography, card dimensions/copy wrapping and socials | Pass: source header/profile geometry, biography, skills and footer; inherited menu-icon contrast limitation below |
| 768px | Pass: mobile hero title, hidden intro photography, two-column cards with centered stacked content | Pass: portrait/title/biography, two-column skills, socials and footer transition |
| 390px | Pass: single-column intro/cards, quote/CTA, portrait crops and social spacing through the last members | Pass: narrow biography wrapping, single-column skills, social/contact icons and footer transition |

Emily and **Lena Alvarez** were verified dynamically. The actual Team name link loaded Lena at `/team/lenaalvarez`, with her own portrait, breadcrumb/title, full biography, four ML/data skills and canonical email/LinkedIn links. Lena's browser check was at 390px; the full four-width static comparison was Emily's. All six profiles additionally pass canonical HTTP content checks.

No horizontal page scrollbar or horizontal content spill was observed in the inspected responsive views. Typography, photo treatments, background shapes and spacing retain the static design rather than substitute a component-library layout.

Meaningful differences/limitations:

- Shared accessible menu/footer behavior, clean links, one logical H1 and honest inert social placeholders intentionally replace legacy semantics; appearance remains consistent.
- At 1366px, the source Member header's white hamburger falls on a pale portion outside its clipped dark shape. The same weak contrast is present in both source and Blazor. It is documented, not redesigned as part of migration. A targeted follow-up needs approval.
- The profile's decorative fixed geometry and the original card sizing remain source constraints. No claim is made about arbitrarily long future editable content or other browsers.

## 13. Runtime and regression verification

### Automated checks

Both `dotnet build NexNovaCo.sln --no-restore` and `dotnet build NexNovaCo.sln --no-restore --configuration Release` pass with **0 warnings, 0 errors**.

All eight PowerShell 7 HTTP suites pass:

```powershell
./scripts/Test-Foundation.ps1
./scripts/Test-Home.ps1
./scripts/Test-Services.ps1
./scripts/Test-About.ps1
./scripts/Test-Projects.ps1
./scripts/Test-ProjectDetail.ps1
./scripts/Test-Team.ps1
./scripts/Test-MemberDetail.ps1
node --test scripts/Test-*Interop.mjs
```

All **18 Node interop tests pass**, including three new Team/profile tests for idempotent setup, route removal/remount, reduced motion and pagehide without jQuery. The existing suites retain service/partner/project/testimonial checks, seven project detail routes, gallery data, safe 404s and the Projects 768px CSS guard. Foundation verifies 82 unchanged copied assets and 58 rendered asset URLs; Team and Member tests additionally request their portraits and new route assets. No broken referenced image or asset was found.

### Interactive browser checks

| Area | Result |
| --- | --- |
| Home | Hero intact; testimonial pause/next works and switches readable Daniel/Olivia content; featured members remain canonical |
| About | Hero intact; partner carousel pause/next works with one control set |
| Services | Hero/cards intact; FAQ toggles by click and keyboard with visible focus |
| Projects | Listing and clean NexConnect link work; at exactly 768px the testimonial remains white text on the approved dark background; pause/next works |
| Project Detail | NexConnect opens from listing; next-image control advances the manual gallery; existing detail HTTP suite passes |
| Team/member navigation | Home → Team → Emily → Team → Lena completed; repeated mounts show the correct member without duplicate handlers or stale content |
| Mobile navigation | At 390px, menu opens, Escape closes/restores focus, Space reopens, and Team selection navigates and closes it |
| Invalid member | Shared-shell 404 and Return Home work; no fallback profile or application exception |
| MudBlazor | Development-only provider select/popover, snackbar, dialog open and dialog close all work |
| Interactive Server | Browser reports a connected `_blazor` WebSocket; server-bound controls remain responsive; no disconnect UI or circuit exception observed |
| Console | Final successful-route console has two normal Blazor connection info messages, **no errors and no warnings** |

Chrome's separate Issues panel still lists nonfatal form-field metadata and lazy-image explicit-dimension advisories (2 and 32 instances on Home diagnostics). These are not JavaScript errors or new Phase 7 behavior; broad unrelated cleanup remains deferred. The deliberate invalid-document request has the expected HTTP 404 entry described above. No console code was executed for verification.

`git diff --check` passes. HTTP/fixture tests supplement, rather than replace, the interactive visual/navigation checks. The Computer Use skill was used for actual rendering and input verification; no DOM-injection or screenshot-generation substitute was used.

## 14. Deferred work

Contact migration, member editing/uploads, database/EF Core, authentication/authorization, CMS, CRUD, persistence, admin/dashboard, SEO overhaul, localization, page building, email delivery and newsletter integration remain out of scope. No dependency upgrade or CSS cleanup was bundled. The inherited member-header contrast limitation and existing browser advisories are documented for separate decisions.

## 15. Recommendation for Phase 8

After explicit approval, inspect the static Contact page/form and migrate it with the existing shell, appropriate shared hero and semantic Razor markup. Preserve its approved responsive styling and real contact links. Keep the form clearly demo-only unless a submission destination, validation/data-handling requirements and backend/email scope are separately approved; never imply successful delivery or storage without integration. Extend the existing tests and re-run all seven migrated-page regressions. **Do not start Phase 8 automatically.**
