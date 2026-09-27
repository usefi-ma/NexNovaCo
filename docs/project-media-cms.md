# Phase 21 — Project media CMS

## Pre-implementation audit and ownership

Started on `feature/dashboard-cms` with a clean working tree at `21ee509`.

| Role | Canonical owner | Consumers |
| --- | --- | --- |
| Cover/card image | `ProjectEntity.ImagePath` | Home featured cards, Projects listing, detail OG image |
| Ordered detail images | `ProjectGalleryImage.Source`, `Alt`, `DisplayOrder`, `ProjectId` | `ProjectDetail.Gallery` → `ProjectGallery` |
| Page hero decoration | Existing shared hero CSS / Projects page settings | Not a new per-project media slot |
| Features | Existing ordered ProjectFeature children | Unchanged; existing <=1200px hiding rule |

The cover is explicitly separate from the first gallery image (NexConnect already demonstrates this). Gallery alt text exists and is required. EF has cascading Project/gallery ownership, ordered children and 200-character paths: sufficient for generated upload paths. No migration or new media entity is needed.

Before this phase, the editor only offered approved static asset selectors. `ProjectContentService` validated those paths, and the list preview did the same. Public cards consume the cover; detail consumes ordered gallery records. The manual Razor gallery uses Bootstrap fade CSS, keyboard/pointer navigation and controls only with multiple images; zero images already hides the gallery. No carousel redesign is needed.

## Implementation decisions

- Reuse `CmsImageField` per cover/gallery row and `IMediaStorageService`; add multiple images through repeated Add gallery image, with individual upload/preview and existing Up/Down/Remove.
- Stage uploads during create or edit; save generated files before the existing entity transaction. No temporary Project IDs. Retry reuses staged saved paths.
- Add a Project media policy using `uploads/projects/`, the existing 3 MiB entity-image limit, and the exact approved static project allowlist.
- Preserve the existing one-time Project initialization marker; no seed edits or emptiness-based initialization.
- Validate upload availability on writes. Missing uploaded covers use the approved project fallback; missing uploaded gallery files are omitted from the public projection, without rewriting stored data or inventing gallery placeholders.
- Removing a relationship does not delete physical files. Orphan cleanup remains deferred.
- Keep the existing snapshot/NavigationLock guard, including pending image selections and ordered gallery changes.

## Git

- Branch remains `feature/dashboard-cms`; no new branch, push, merge or history rewrite.
- `a73d30c` — Add secure Project cover and ordered gallery uploads.
- `82ec54b` — Verify Project media security persistence and browser workflows.
- Final documentation is a separate focused `Document Project media CMS verification` commit.
- Only Phase 21 files changed. Working-tree status is checked after the documentation commit. All runtime artifacts are ignored.

## Database and initialization

No migration: existing `Projects.ImagePath` and `ProjectGalleryImages` already support all required data. No duplicate media table, cover flag, gallery field or temporary entity ID was introduced. Existing Project initialization still uses its persistent marker and never replenishes an initialized Project's gallery. Exact static defaults, other Project content and Home featured selection remain intact.

## CMS workflow

Shared Content → Projects → Add/Edit now has:

1. A distinct Cover image field using the shared upload/preview component.
2. Add gallery image, repeated as needed; each row provides upload, preview, visible position and required existing alt text (200 characters).
3. Up/Down and Remove with staged changes, persisted by Save project.
4. Safe bundled-image selection retained as a useful alternative. Uploaded images display “Current uploaded image,” not a raw-path entry field.

This is multi-image management through repeated per-row upload, not a new bulk upload subsystem. There is no four-image maximum. Both new and existing Projects support uploads before Save. A successful create navigates to the persistent edit route, so refresh does not create duplicates.

The shared component stages validated bytes until Save, writes unique files, caches their generated paths for retry, and only accepts/clears pending selections after the Project save succeeds. On a failed save the editor restores previous model paths and retains pending selections. Existing normal validation and error feedback remain in place; IO failures receive safe feedback too.

## Media security and physical files

`MediaKind.Project` extends the existing policy, not the storage system:

- JPG/JPEG (normalized to `.jpg`), PNG and WebP only; 3 MiB per image, matching the entity-image policy.
- Existing bounded stream, actual/claimed size, MIME, extension and file signature checks.
- GUID filenames in `uploads/projects/`; existing controlled-path, traversal and reparse-point protection; no overwrites of source files.
- Service writes require an active Admin with current security stamp and database role; uploads recheck authorization at selection and commit.
- No anonymous/public upload endpoint. Public media requests remain read-only with fixed raster content type and `nosniff`.
- Entity writes require an approved bundled path or an existing validated upload from the Project folder. Gallery paths get the same checks as covers.
- Removing images or deleting Projects does not remove physical files. Canceled/failed post-upload saves may leave safe orphan files, consistent with the current foundation. Cleanup is deferred.

Project-page hero uploads already use the same controlled Projects directory; no new directory hierarchy was needed. Team Member media code and behavior were not changed.

## Public rendering, cover and SEO

`ProjectContentService` remains the canonical reader. Home and listing cards still use the explicit cover, independent of gallery order. Project Detail's title, description and canonical slug remain entity-owned; its existing OG strategy consumes that same cover.

Approved static paths remain allowed without copying or reseeding. Missing generated covers resolve to the approved first bundled project image (`image/project/nexconnect.jpg`). Missing generated gallery files are omitted from the public projection: no broken image and no substitute gallery photograph. Stored records are not rewritten by read fallback. Database/read-validation failures retain the pre-existing approved static-catalog fallback.

The public gallery component, public page components and public CSS/JS were unchanged. Existing manual fade, previous/next, keyboard navigation and pointer behavior remain. Zero images hides the gallery; one image has no navigation controls; multiple images retain ordered slides and controls. This phase does not apply the separate testimonial-controls policy to Project galleries.

Gallery-specific alt text is preserved and editable; card alt remains derived from Project name. Existing carousel roles, current-slide state and live status are unchanged. Features and metadata remain separate; Features still hide at <=1200px, explicitly checked at 1200 and 1201.

## Dirty state and authorization

The existing `EditorSnapshot` / `UnsavedChangesGuard` / `NavigationLock` is reused. The snapshot contains the ordered gallery and all text fields. Pending cover/gallery selections also mark dirty, including validation-in-progress. Stable row keys retain their pending uploads when reordered. Removed rows release their component references. Stay preserves edits; Leave discards staged edits. No second navigation-guard system was introduced.

Existing list/new/edit/featured routes retain Admin authorization. Automated checks cover anonymous login challenge, non-Admin denial and Admin direct navigation/refresh. Service and upload operations reject anonymous/non-Admin principals; existing live-role/security-stamp checks remain enforced.

## Verification results

All database writes and browser edits used isolated SQLite databases and upload roots. The normal developer database and original source files were not reset or edited.

| Verification | Result |
| --- | --- |
| `--project-media` | 804 checks passed: Project media, shared media security, Project CRUD and one-time initialization |
| `--public-quality` | 215 checks passed, including existing SEO behavior |
| Project Detail / Projects / Home interop tests | 12 passed |
| Existing `Test-ProjectDetail.ps1` on fresh isolated defaults | Passed: all 7 detail routes, 8 ordered gallery images, separate covers, controls, metadata, features and safe 404s |
| Debug solution build | 0 warnings / 0 errors; separate output folder avoids the developer's running Debug app |
| Release solution build | 0 warnings / 0 errors |
| EF model | No pending model changes; no migration required |

The static-source Project Detail script is intentionally run against fresh approved defaults, not the modified CMS upload fixture (which correctly differs from source JSON).

### End-to-end persistence and media

- Opened existing NexConnect; confirmed separate cover plus its two original gallery images.
- Staged a cover replacement, exercised Stay then Leave, and confirmed no unsaved change persisted.
- Uploaded a distinct cover and A/B/C/D gallery images, reordered pending rows to D/A/B/C, saved and refreshed.
- Confirmed exact public image order/alt, independent cover in Home/listing/OG, and byte-for-byte equality between selected source fixtures and saved files.
- Stopped and restarted the test application against the same isolated DB/upload root; editor and public page retained all four images in D/A/B/C order and the separate cover.
- Removed one image and saved; public gallery immediately reflected three images. Also exercised 5, 2, 1 and 0 images through the actual editor.
- Restarted again with zero images: editor and public gallery remained empty; cover/OG remained available. Removed files stayed readable, matching the orphan policy.
- Created another Project with uploaded cover and a single uploaded gallery image before its first save; refreshed its persisted edit route and checked its public single-image state after restart.
- Automated tests additionally cover nonexistent/unsafe/cross-folder paths, missing-file rendering without database writes, alt validation, invalid writes leaving existing data intact, static compatibility and anonymous/non-Admin upload rejection.
- Existing Shared Project tests cover Name, Slug, Tagline, Summary, full description, metadata, technologies, Features, gallery order/removal, Home featured membership/order and shared list reorder.

### Responsive and real-browser matrix

| Surface/browser | Widths | Result |
| --- | --- | --- |
| Dashboard Project editor / Chrome | 1440, 1024, 768, 390 | Upload previews/rows fit; no horizontal overflow or console errors |
| Project Detail / Chrome 153.0.8010.54 | 1440, 1201, 1200, 1024, 768, 390 | Correct four-image order, next/previous/wrap, keyboard, fade completion, resize, repeat navigation, Features boundary |
| Projects listing and Home featured / Chrome | 1440, 1201, 1200, 1024, 768, 390 | Correct uploaded cover; images load; no horizontal overflow or console errors |
| Project Detail / Edge 154.0.4258.37 | 1440, 390 | Same gallery/resize/navigation smoke passed |
| Project Detail / Firefox 156.0.1 | 1440, 390 | Same gallery/resize/navigation smoke passed |

Screenshots and result JSON are retained locally under ignored `artifacts/project-media/`. Desktop detail and mobile detail/editor screenshots were visually inspected. Frames and responsive image sizing retain the existing design. Full-page screenshots captured after scrolling can show fixed header/skip-link overlays at the capture scroll position; no layout CSS was changed.

### Reproducing focused tests

```powershell
dotnet build NexNovaCo.sln --no-restore -c Debug -p:OutputPath=bin/ProjectMediaDebug/net10.0/
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --project-media
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --public-quality
node --test scripts/Test-ProjectDetailInterop.mjs scripts/Test-ProjectsInterop.mjs scripts/Test-HomeInterop.mjs
```

Browser tests use the test executable's `--detail-browser <ignored-fixture.json> [isolated-database-path]` host on localhost:5199 and `scripts/Test-ProjectMediaBrowser.cjs <fixture.json> <playwright-module-path>`. Optional script modes: `setup`, `verify`, `states`, `empty`. Use `setup` on a fresh fixture; stop/restart the host using the same fixture/DB before `states`, and restart again before `empty`. The host validates the retained synthetic Identity session without resetting passwords. Do not point it at a normal developer or production DB. Test-session fixtures are ignored and removed after verification; screenshots/results contain no session credentials.

## Deferred work and recommendation

Project covers and ordered galleries are now manageable from the CMS without manual path entry. Recommend reviewing Team Member media as the next **separate, approved** phase. No Team Member media work was started.

Media Library/global browsing, bulk multi-file selection, crop/edit, orphan cleanup, optimization, cloud/CDN storage and final comprehensive CMS QA remain deferred. No public redesign, final QA, push or merge was performed. Stop here and wait for approval.
