# Dashboard Phase 14 — Complete Team Page CMS

## Git

Implemented on `feature/dashboard-cms`, starting from clean HEAD `a3f9789`. No branch switch, history rewrite, push or merge. Page data/public integration and Dashboard/focused verification are separate logical commits. The normal developer database was not modified or reset.

## Team Dashboard IA

Content → Team is an expandable MudNavGroup with Hero and Team Section. Root `/dashboard/content/team` redirects to `/hero`. The intro editor is `/overview`. Direct entry expands the group and highlights the active child. Responsive drawer, keyboard Enter toggling, mobile close-on-child-navigation and existing focus handling are preserved.

The requested root previously hosted Shared Team Members. That canonical manager and its new/edit/home-featured routes now live at `/dashboard/content/shared-team`. The old new, integer-ID and home-featured routes redirect to their corresponding new routes. The old root intentionally becomes the Team page-settings entry point.

## Actual section inventory

| Public section | Real content | CMS treatment |
| --- | --- | --- |
| InnerPageHero | Title, description, CTA label/route, photograph | Hero singleton |
| TeamIntroSection | Eyebrow, title, introduction, highlighted sentence, description, CTA label/route, photograph | Team Section singleton |
| TeamGridSection | Member cards and shared ordering | Canonical Shared Team Members; no duplicate page fields |
| Card/profile content | Identity, role, introduction, photo, biography, skills and social destinations | Existing shared member entities and Member Detail |
| Shapes, typography, colors, spacing, breakpoints | Presentation | Unchanged; not editable |

The approved Hero uses the same title, “Our Team”, on desktop and mobile. One title field intentionally drives both; no distinct mobile copy was invented. The two real page photographs were verified in team.css: `image/team/ourteam-header.jpg` and `image/team/our-team.jpg`.

## Shared vs page-specific content

Page services own only the two settings records. IMemberCatalog/MemberContentService still supply Team cards, Member Detail and Home Featured data. No member CRUD, ordering or selection operations were copied into the page service. Shared TeamMemberCard, SocialLinks and MemberProfileContent are unchanged.

## Data models / migrations

Migration `20260923170320_AddTeamPageCms` creates only `TeamHeroSettings` and `TeamSectionSettings`. Both enforce singleton ID 1, required bounded fields and the established UpdatedAtUtc concurrency-token pattern.

ITeamPageCmsService uses short-lived DbContexts and separates public reads from authorized editing. No generic repository or duplicate member table.

## Initialization

TeamPageInitializer inserts exact approved defaults only when each singleton is absent. Existing rows, timestamps and Admin edits are retained across startup. Shared member seed markers are unchanged; intentionally empty member catalogs remain empty.

Public failures are logged and fall back to approved defaults only for the unavailable settings slice, with no writes. Tests rename each settings table and verify saved values and timestamps are unchanged after fallback. Dashboard load errors do not display defaults as if they were stored content.

## Hero

A single desktop/mobile title, description, CTA text, local public route and existing photograph are editable. The approved link remains `team#Team`. Page settings are read fresh, without the old static settings cache.

The public component receives a base-aware absolute image URL through a narrowly scoped CSS variable. Existing crop, geometry and responsive behavior stay intact.

## Team Section

Edits only the actual eyebrow, title, introduction, highlight, description, CTA and existing intro image. Includes a clear “Manage Team Members” link to the shared manager.

Home's Team introduction is independently managed and is not modified by Team page edits. Intro image visibility below 1200px remains the existing CSS behavior; it is not a new setting or removal.

## Shared Team Members reuse/link

The grid remains a read-only consumer of the shared catalog. Shared member Dashboard routes and links were relocated without changing their CRUD logic. Home Featured Members still selects/orders the same shared members. Team page settings never mutate member, skill or Home Featured tables.

## Media reuse

Reuses CmsImageField, IMediaStorageService, existing raster validation and safe public handler. TeamHero and TeamSection kinds use `uploads/team-page/`, with 5 MiB limits and their original bundled photographs as fallbacks.

Per-member media remains `uploads/team/` with its existing policy. No per-member image behavior changed. Both page image handlers were tested for image-only dirty state, failed database save retention, retry, safe serving and public path rendering. Browser upload/restoration was also verified for the intro photograph.

## Unsaved changes / validation

Both forms use EditorSnapshot and UnsavedChangesGuard. Browser tests verify prompting on internal/shared-manager navigation and Stay retaining edits. Required-field feedback was exercised. Automated tests invoke actual save handlers, verifying dirty state survives validation/database failures and clears on success.

Fields are bounded; CTA routes reject external/protocol-relative URLs, unsafe schemes, admin paths, query strings, traversal and invalid anchors. No raw HTML or CSS editor was added.

## Authorization

All Team settings and legacy redirect pages inherit the Dashboard Admin authorization policy. Anonymous users are challenged, non-Admins denied and Admin direct/refresh access succeeds. Read-for-edit and write service methods additionally validate current database role membership and security stamp. Revoked roles/stamps are rejected. Public reads remain anonymous and no public mutation endpoint was introduced.

## Verification

Focused command:

```powershell
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --team-page
```

129 checks passed in both Release and the isolated Debug output:

- Exact fresh defaults and startup idempotence.
- Additive migration contains precisely two CreateTable operations; prior Identity/Home/About/Services/Projects/shared rows and timestamps survive the upgrade.
- Every real singleton: edit fields, actual editor save, public HTML verification, separate-host restart, persistence and restoration.
- Every intro field and Hero CTA are verified from SQLite.
- Settings/media failure and validation retain unsaved changes.
- Both image fields use safe raster storage/serving and retry without discarding selection.
- Public settings failure logs errors, falls back only the affected slice, and leaves settings/timestamps untouched.
- Page settings workflows preserve all prior tables, including shared members/skills/featured relations.
- Home intro and Member Detail remain independent of Team page intro edits.
- Deleting the isolated shared catalog leaves Home/Team without fake cards across restart; deleted detail returns 404.
- Admin authorization, role/stamp revocation, direct/refresh routes and legacy redirects.
- Shared Members and Home Featured route smoke checks.

Debug and Release builds: zero warnings and zero errors. Debug output uses `-p:OutputPath=bin/TeamDebug/net10.0/` to avoid affecting an existing developer process. The full historical site regression was not run. Older phase migration checks were updated to account for the additive schema.

Browser QA used an isolated database/media folder under ignored `artifacts/team-page-qa-a492968c450a4d3096a51834417e377b` with a temporary Admin. Edited text and the uploaded intro image were restored to approved values.

## Public Team regression

| Width | Dashboard | Public Team |
| --- | --- | --- |
| 1440 | Expanded Team group; Hero/intro forms, active link, save/guard | Desktop Hero, side-by-side image/intro, two-column member grid |
| 1024 | Desktop drawer and wrapped form copy | Desktop Hero, full-width intro, existing hidden intro image, two-column portrait-above-text cards |
| 768 | Compact drawer/form | Mobile Hero, full-width intro, two-column cards |
| 390 | Stacked fields/actions; drawer closes after child navigation | Mobile Hero, readable intro/highlight/CTA, single-column cards |

No horizontal overflow at the requested widths. Existing portrait silhouettes, spacing and social behavior retained. Email/LinkedIn destinations were inspected without contacting external services; unconfigured Telegram anchors remain decorative and unfocusable.

The original long-running QA tab recorded intermittent SignalR disconnect/negotiation failures during the session. The QA server remained running and its logs contained no application exception. These errors were not reproduced in a fresh tab: repeated Team → Member Detail → Team, Dashboard Hero/intro saves, and return-to-public navigation completed with empty warning/error logs. Their underlying cause was not established; no speculative connection changes were made.

## Member Detail regression

Inspected MemberDetail, MemberProfileContent and the shared catalog before coding. No separate editable global Member Detail content requiring a scope expansion was identified. Existing “Skills”/breadcrumb labels remain presentation labels, not newly invented CMS settings.

`/team/emilyjohnson` was verified in automated and browser smoke checks: correct identity, biography, skills, portrait and social links. Clicking the Team card, returning through its breadcrumb/back navigation and re-enhancing the Team page remain safe. No Member Detail code or CMS responsibilities changed.

## Deferred work

No duplicate member/detail management, Contact CMS, Header/Footer/Navigation CMS, SEO editor, role management, drafts/versioning, audit logs, localization or page builder. No unrelated public CSS cleanup or redesign.

## Recommendation for next whole-page CMS target

Contact page-specific CMS is the next natural target: inspect actual contact information, Hero and existing form presentation first, while keeping real message delivery/integrations a separately approved scope. No next-phase implementation has begun.
