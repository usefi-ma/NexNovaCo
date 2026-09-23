# Dashboard Phase 11 — Complete About CMS

## 1. Git

- Branch: `feature/dashboard-cms`; work began on clean commit `d31d0ba`.
- No branch creation, history rewrite, push, or merge.
- Changes are grouped into About data/services/migration and Dashboard/public integration/tests.
- Normal development SQLite database was neither reset nor migrated by this work. All migration/startup/browser tests used isolated databases.

## 2. About Dashboard information architecture

Content → About is a collapsible MudNavGroup with six independent editors:

| Section | Route |
| --- | --- |
| Hero | /dashboard/content/about/hero |
| Story | /dashboard/content/about/story |
| Vision | /dashboard/content/about/vision |
| Timeline | /dashboard/content/about/timeline |
| Mission | /dashboard/content/about/mission |
| Partners | /dashboard/content/about/partners |

`/dashboard/content/about` redirects to Hero. Timeline Add/Edit routes are `/timeline/new` and `/timeline/{id:int}`; Mission point routes are `/mission/points/new` and `/mission/points/{id:int}`, under the same About prefix.

The group opens on About routes; prefix matching keeps Timeline/Mission active on their item editors. Existing MudBlazor keyboard controls are retained. The compact drawer closes after successful navigation (not when a dirty-form navigation is canceled).

## 3. Inspected section inventory

The existing About page contains precisely Hero, What We Do (Story), Vision, Timeline, Mission, and Partners.

| Section | Actual editable content | Kind |
| --- | --- | --- |
| Hero | Desktop/mobile titles, description, CTA label/route, existing hexagon/background photograph | Singleton |
| Story | Title, subtitle, two introduction paragraphs, two detail paragraphs, closing text | Singleton |
| Vision | Title, two separate paragraphs, CTA label/route, photograph, alternative text | Singleton |
| Timeline | Section title/description, CTA label/route; ordered year/description milestones | Singleton + collection |
| Mission | Brand title/description, section title, two paragraphs; ordered plain-text commitments | Singleton + collection |
| Partners | About-specific title/description | Singleton + existing shared Partner collection |

Story and Timeline have no content photographs. Mission's SVG background and repeated check icons are decorative design assets, not new editable image/icon fields. No fictitious eyebrow, item title, layout, color, or rich-text fields were introduced.

## 4. Data models and migration

`20260923042106_AddCompleteAboutCms` adds ten tables only:

- Six singleton settings: AboutHeroSettings, AboutStorySettings, AboutVisionSettings, AboutTimelineSettings, AboutMissionSettings, AboutPartnersSettings.
- AboutTimelineItems and AboutMissionPointItems: identity key, positive DisplayOrder, actual content, UpdatedAtUtc.
- AboutTimelineInitializationState and AboutMissionPointInitializationState.

Singleton/marker IDs are constrained to 1. Timeline years are constrained to four digits. Ordered collections have indexes; settings/items have timestamp concurrency tokens.

The migration creates tables/indexes, without modifying prior tables. The migration test compares every prior Identity, Home and shared-content row/column before and after upgrading from the previous migration.

## 5. Initialization

AboutInitializer runs in one transaction. Each missing singleton is created from the exact former typed About editorial snapshot in AboutDefaults. Existing rows are never overwritten.

Each collection has its own durable initialization marker. An unmarked empty collection gets approved items once; an unmarked nonempty collection is adopted without replacement. A marked empty collection stays empty. Marker creation and initial items commit together.

Tested fresh initialization, repeated startup, delete-one, delete-all, restart and creation after empty. Restoring QA defaults was explicit Admin CRUD, not deleting markers or relying on startup reseeding.

## 6. Hero

AboutHeroEditor manages real title variants, introduction, CTA and image. Public rendering keeps InnerPageHero unchanged. An About-scoped CSS custom property supplies the validated image URL without affecting Home or other inner pages.

The existing hero geometry, image tint, crop, desktop/mobile copy visibility and navigation are preserved.

## 7. Story

AboutStoryEditor preserves the original distinct introduction/detail paragraphs and closing statement. It does not invent an image or CTA. Public AboutStorySection markup is unchanged.

## 8. Vision

AboutVisionEditor manages the original two-paragraph copy, CTA, image and alt text. Public AboutVisionSection retains its split layout, existing decorative geometry and lazy image loading.

## 9. Timeline

AboutTimelineEditor edits the intro/CTA and hosts the milestone list. Independent Add/Edit forms manage year/description. The list supports confirmed deletion, Move Up/Down and refresh. Ordering is explicit and normalized transactionally; stale/duplicate reorder submissions fail safely.

Public rendering consumes database order. The original three visual slots are reused in groups of three, so adding a fourth item cannot hit the previous three-item-only switch. Incomplete groups omit the three-stop decorative connector. Zero items produce no list or connector; the section intro and CTA remain.

## 10. Mission

AboutMissionEditor edits the brand and section copy and hosts ordered commitments. Independent Add/Edit forms, delete confirmation, reorder and refresh use the same patterns as Timeline.

Public text is database-backed. Empty commitments omit the list and its decorative check icons; section copy remains. Existing icons and page styling are unchanged.

## 11. Partners

AboutPartnersEditor controls only About's real heading and description. It explains shared ownership and links to Shared Content → Partners.

AboutContentService composes About CMS content with IPartnerContentService. There is no second About Partner catalog/table. A shared Partner edit was verified on both Home and About, then restored.

## 12. Media

The existing CmsImageField and LocalMediaStorageService are reused. MediaKind.AboutHero and MediaKind.AboutVision use `uploads/about/`, with the existing raster validation, 5 MiB limit, generated filenames, safe read endpoint, missing-file fallback and live Admin checks.

Existing bundled image defaults remain welcome.jpg and vision.png. No new storage implementation or upload endpoint was created. Both image editor save/retry paths were tested, including database failure after upload, pending-image dirty state, retry reuse and public rendering. Hero replacement was also exercised through the browser file chooser and survived a QA server restart.

## 13. Unsaved changes and validation

All six singleton editors and both item forms reuse UnsavedChangesGuard and EditorSnapshot. Load is clean; text/image edits are dirty; successful saves clear the snapshot. Validation/database failures preserve local edits and dirty state.

Browser checks covered navigation warning, Stay/Leave, and navigation after invalid collection submission. Actual save handlers were tested for all singleton editors and both item editors; image save failures were tested for Hero and Vision.

Fields are required where the existing design requires content, with field-specific length limits. Timeline years are 1000–9999. CTA validation follows the existing public-route allowlist and additionally permits the existing `about#About` Story anchor. External schemes, admin routes, traversal, queries and arbitrary anchors are rejected. Plain strings are rendered by Razor, without raw HTML editing.

## 14. Authorization

Dashboard folder authorization protects every About route. HTTP checks cover anonymous login redirects, non-Admin access denial and Admin direct navigation/refresh.

Every Admin service read/write checks the current authenticated principal, live database Admin membership and security stamp. Tests cover anonymous, non-Admin, removed Admin role and revoked security stamp for all 24 management operations. Public reads remain anonymous; no public write API was introduced.

## 15. Verification

Focused command:

```powershell
dotnet run --project tests/NexNovaCo.Auth.Tests -c Release -- --about
```

Result: **285 checks passed**, including:

- Exact fresh defaults and persistent initialization markers.
- Upgrade safety for every prior CMS and Identity table/account.
- All six editors: actual handler failure/validation/success, public HTTP update, separate-host restart persistence and approved-value restoration.
- Timeline/Mission add, edit, reorder, delete-one, delete-all, restart and add-after-empty.
- Zero-item public HTTP rendering and collection fallback distinction.
- Per-slice database-read failure fallback, without writes.
- Hero/Vision upload integration and failure/retry behavior.
- All About route roles, direct navigation/refresh, live service authorization, and missing-item handling.
- Shared Home/About/Partners smoke checks.

The test fixture's data-protection keys now also live beside its isolated database, rather than accessing the developer's key ring.

Browser checks used a separate localhost QA database and upload directory under ignored `artifacts/about-qa-*`. They covered all six singleton save/publish/refresh/restore flows; Timeline/Mission CRUD/reorder/empty states; mobile navigation; dirty-form dialogs; field validation; Hero upload and restart; Shared Partners; and repeated Home/About navigation.

Additional focused lifecycle checks:

```powershell
node --test scripts/Test-AboutInterop.mjs
```

Result: **4 tests passed** (empty Partners, initialization/disposal, repeated Home/About remount, reduced-motion and unavailable-Owl fallback).

Build verification:

```powershell
dotnet build NexNovaCo.sln --no-restore -p:OutputPath=bin/AboutDebug/net10.0/
dotnet build NexNovaCo.sln -c Release --no-restore
```

Both configurations passed with **0 warnings and 0 errors**. The separate Debug output avoids locking the developer's running app.

The existing approved-About baseline check also passed on the restored isolated QA host:

```powershell
./scripts/Test-About.ps1 -BaseUrl http://localhost:5187
```

It verified approved copy, six-section order, one H1, three milestones, four commitments, canonical Partner markup matching Home, CTA targets and image/asset responses.

The historical full-site suite was deliberately **not** run.

## 16. Public About regression

| Viewport | Public About | Six Dashboard editors |
| --- | --- | --- |
| 1440 | Content/images, three milestones/four points, Partner enhancement; no horizontal overflow | Correct active child and expanded About group; no overflow |
| 1024 | Tablet hero/content flow and carousel controls; no broken images/overflow | No overflow |
| 768 | Existing inner-hero geometry and responsive section flow retained; no broken images/overflow | No overflow |
| 390 | Mobile hero, stacked text/images/milestones/commitments; no broken images/overflow | Compact forms/tables; drawer closes on keyboard link selection |

Measured public document widths were 1425, 1009, 753 and 375 px respectively (viewport minus scrollbar). Live-session browser console error/warning checks were empty. Deliberately stopping the QA host produced expected connection-negotiation messages in the old tab; a fresh post-restart session was verified with no warnings/errors. Carousel Next/Previous changed the stage, pause worked, and three Home→About return cycles each retained exactly one enhanced carousel and one stage. Interactive Server continued to process saves, dialogs and navigation.

The approved theme CSS and shared carousel code were not redesigned. A full pixel-diff was not performed; viewport inspections plus DOM measurements and the unchanged base styles were used. The only public CSS additions supply the About hero image and hide an inappropriate connector on partial Timeline rows.

## 17. Deferred work

No Services page CMS, Contact CMS, additional Projects/Team page-intro CMS, header/footer/navigation CMS, SEO editor, rich text, publishing workflow, version history, audit logging, localization or page builder.

## 18. Recommendation

Next whole-page target: **Services page CMS**, reusing the existing shared Services catalog and managing only actual Services-page-specific presentation content. It has not been started; await approval.
