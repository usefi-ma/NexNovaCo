# Dashboard Phase 10 — Shared media upload foundation

## 1. Git / scope

Implemented on `feature/dashboard-cms`, starting from clean Phase 9 commit `7607458903abf2111dd56f957d0f7fdfaa654597`. Only shared raster upload infrastructure and Hero, Welcome, Member and Partner integrations. No push, merge, branch creation, About CMS or Media Library. Normal developer database and the existing Visual Studio app were not used for QA or reset.

## 2. Media service architecture

`IMediaStorageService` / `LocalMediaStorageService` own all upload validation, bounded stream reads and file persistence. `MediaUpload` has internal immutable byte ownership; external callers cannot construct or modify its payload. `MediaPolicy` centralizes formats, folders, size limits, generated-path syntax, approved bundled choices and fallbacks. `MediaFilePaths` handles filesystem containment/existence for storage and the read-only HTTP route. No four separate upload implementations, generic Media table or new dependency.

Selection validates and retains one bounded in-memory file per field. Save rechecks authorization and validation, writes a unique temporary file using CreateNew, closes it, then moves it to its final GUID filename without overwrite. Only that operation's incomplete temporary file can be deleted.

## 3. Storage paths / Git / deployment

Default physical root: `wwwroot/uploads`. Relative DB URLs:

- Hero / Welcome: `uploads/home/{guid-N}.jpg|png|webp`
- Member: `uploads/team/{guid-N}.jpg|png|webp`
- Partner: `uploads/partners/{guid-N}.jpg|png|webp`

The root is gitignored and excluded from project Content/None items, build static-asset manifests and publish payloads. GET/HEAD `/uploads/{folder}/{file}` serves only canonical generated raster paths, with fixed server MIME, `nosniff` and immutable caching. No directory listing, temporary-file route or HTTP upload endpoint. The old `/assets` alias explicitly excludes uploads.

For deployments that replace the application directory, set trusted server configuration `MediaStorage:RootPath` (environment: `MediaStorage__RootPath`) to a persistent, app-writable media directory outside the release directory. URLs stay the same. Do not point it at source assets or private data. Symlinks/junctions are deliberately unsupported. Back up the media directory together with SQLite; do not replace/delete it during deployment. Multi-instance/cloud storage is deferred.

## 4. Security validation

New uploads: JPG/JPEG, PNG and static WebP only. Filename extension, browser MIME and actual signature/container must agree. JPEG normalizes to .jpg. SVG, HTML, scripts, double extensions, traversal/separators, drive/stream syntax and control characters are rejected. Client filenames never determine storage paths.

Limits are server-enforced both through OpenReadStream and an independent actual-byte counter: 5 MiB Hero/Welcome, 3 MiB Member/Partner. Claimed/actual byte counts must match. Raster dimensions are bounded to 8192 per axis and 32 million pixels.

JPEG checks SOI/EOI, segment bounds, baseline/progressive 8-bit frame dimensions and scan structure. PNG checks signature, IHDR, legal depth/color combinations, chunk bounds/CRCs, IDAT and final IEND. WebP checks RIFF length, WEBP/chunk structure, VP8/VP8L signatures/dimensions and optional VP8X. APNG/animated WebP are rejected. Transparent PNG and WebP bytes are preserved.

These are conservative format/container checks, **not a full codec decoder, malware scanner or metadata sanitizer**. The UI also blocks a selected image if its browser preview cannot decode. Images are public; do not upload confidential material or sensitive embedded metadata. No EXIF stripping, resizing or automatic editing occurs.

Reference: [ASP.NET Core Blazor file uploads](https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0), [PNG specification](https://www.w3.org/TR/png-3/), [WebP container specification](https://developers.google.com/speed/webp/docs/riff_container).

## 5. Reusable upload UI / save semantics

`CmsImageField` is shared by all four editors: MudBlazor labels/select/feedback, standard InputFile, current and selected previews, filename, size/type/aspect guidance, discard-selection action and approved bundled choices. No raw-path textbox or Media Library navigation.

A pending selection activates the existing unsaved-changes guard even when no text changed. Files persist only inside the parent Save workflow. The entity service validates the generated path and file availability before updating its existing image property.

If file persistence fails, the database is unchanged. If the entity save fails, the editor restores the previous form image path, retains text and pending selection, and retries using the same already-persisted file. Only successful entity persistence clears the pending upload and dirty snapshot. New Member/Partner forms start with their first approved bundled choice, which can be replaced before saving.

## 6. Hero integration

Adds ImagePath to the existing singleton content/edit/entity contracts. The public hero uses its existing CSS background/hexagon and approved breakpoint behavior. Only the background source changes. CSS custom-property URLs are resolved through NavigationManager.ToAbsoluteUri: relative URLs otherwise resolve against the consuming stylesheet's css directory. Missing upload falls back to `image/home/header.jpg`.

## 7. Welcome integration

Same service and field; minimal ImagePath on the existing Welcome singleton. Existing image hexagon, text, crop and responsive visibility remain intact. Uses a base-aware absolute CSS background URL and `image/home/welcome.jpg` fallback.

## 8. Team integration

Member Add/Edit replaces the old bundled-only selector with CmsImageField. Existing canonical MemberEntity.ImagePath remains the single source for Home featured cards, Team listing and Member Detail; no duplicated page fields or schema change. Missing uploaded portrait uses the existing generic `image/team/our-team.jpg` asset across all consumers, retaining saved text and identity.

## 9. Partner integration

Partner Add/Edit uses the same field and existing PartnerEntity.ImagePath. Home and About continue sharing the canonical Partner service and carousel. Transparent raster files are not transformed. The white-logo-background option is retained. Missing upload uses the approved TechCo bundled logo. Existing source/static SVG assets remain unchanged; uploaded SVG is not enabled.

## 10. Database migration

`20260923030843_AddHomeImagePaths` adds exactly two required TEXT ImagePath columns (max 200) to HomeHeroSettings and HomeWelcomeSettings, each with its approved original path as the SQL default. Existing content/timestamps and initialization markers remain untouched. No Media table, data reset or unrelated schema change.

Upgrade tests now use `HistoricalHomeFixture` to insert/read frozen pre-media Home columns while testing older migrations, instead of querying old schemas with today's entities. Existing assertions about preserving credentials, content and order remain in place. The new migration has its own upgrade/data-preservation/no-model-drift checks.

## 11. Fallback / orphan behavior

All public consumers resolve unavailable generated images to existing bundled fallbacks without rewriting the database. Admin list preview components still show their existing unavailable-image placeholder. Replacing/deleting an entity never deletes source images or old uploads. Failed entity saves may leave a valid orphan; safe cleanup/reference tracking is intentionally deferred.

## 12. Authorization

Selection and persistence require an authenticated Admin claim, a current database Admin membership, a current user and matching Identity security stamp. Save repeats these checks; selecting a file before role/stamp revocation does not authorize later persistence. Existing entity-service Admin checks remain. Anonymous GET/HEAD is intentional for public images; anonymous/non-Admin writes are denied, and POST to the public read route does not upload or overwrite anything.

## 13. Verification / builds

Isolated AuthFactory databases and sibling upload roots only. Browser QA used a separate artifacts/media-qa directory and loopback host; no normal developer DB reset or application shutdown.

- Debug and Release: zero warnings/errors; Debug uses `-p:OutputPath=bin/MediaDebug/net10.0/` because the user's running Visual Studio app locks the standard Debug output. Release uses the normal output.
- 2509 Auth/CMS checks passed in both configurations, including JPG/JPEG/PNG/WebP acceptance, actual/hinted oversized files, false MIME/extension, script/HTML, traversal/local paths, truncated containers, PNG CRC mismatch, generated names, no source overwrite and authorization/role/stamp revocation.
- Actual editor save handlers for all four integrations tested with forced SQLite update failures: prior form/DB paths retained, pending upload stays dirty, retry reuses the file, success clears dirty state.
- Public HTTP checks verify uploaded paths on Home, Team, Member Detail and About, GET/HEAD MIME/nosniff, restart persistence and missing-file fallbacks. Absolute Home CSS URL regression assertion included.
- All eight existing JavaScript interop suites passed (24 tests).
- Browser: actual Hero/Welcome/Member JPG uploads and Partner PNG upload saved successfully; valid previews, persisted current-image choices after restart; SVG selection rejected and Save blocked.
- Dashboard matrix: all four editors at 1440, 1024, 768 and 390 px — upload input present, loaded current preview, no horizontal overflow.
- Public browser matrix: /, /team, /team/emilyjohnson and /about at 1440 and 390 px — uploaded shared images load, no broken images or horizontal overflow; Hero/Welcome backgrounds visually verified at desktop and approved mobile visibility retained.
- Final clean-browser public/Dashboard pass had no console warnings/errors. Repeated page loads retained functional interactive editors.
- Isolated Release publish succeeded; no uploads directory, SQLite files or temporary upload files in payload. Git ignore verified and no source image changes.

Reproduce from the repository root:

```powershell
dotnet build NexNovaCo.sln --no-restore -p:OutputPath=bin/MediaDebug/net10.0/
dotnet tests/NexNovaCo.Auth.Tests/bin/MediaDebug/net10.0/NexNovaCo.Auth.Tests.dll
dotnet build NexNovaCo.sln -c Release --no-restore
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll
Get-ChildItem scripts -Filter 'Test-*Interop.mjs' | ForEach-Object { node $_.FullName }
```

## 14. Deferred Media Library work

No library/search/metadata table, cropper, image transformation, remote/cloud provider, SVG sanitizer, deletion UI, reference-counted orphan cleanup or background job. Operational quotas, advanced scanning/decoding and metadata sanitization can be considered with the future media-management phase.

## 15. Whole-page About CMS readiness

The reusable field/storage, canonical shared images, authorization, fallback and save semantics are ready to support a separately approved About CMS phase. This phase does not implement any About content editor, Header/Footer CMS or additional page migration.
