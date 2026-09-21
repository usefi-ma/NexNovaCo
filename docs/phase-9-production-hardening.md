# Phase 9 — Public-site production hardening lite

Reviewed September 20, 2026. This is a bounded hardening pass, not a production launch or Dashboard implementation.

## Git status and scope

Started from clean `main` at `7c74767054f230e2de41ffe338ebe5aab7644fe5`, matching fetched `origin/main`. Created only `feature/public-site-hardening`. The initial `index.html` status was a Windows line-ending/index-cache discrepancy: its normalized working blob exactly matched HEAD; refreshing its index entry produced no staged content change. No reference HTML was rewritten.

Changes are left uncommitted for review. No push, merge, history rewrite, deployment, or additional branch was performed. No CSS, JavaScript runtime implementation, image artwork, package version, canonical JSON, or component architecture was changed.

## Publish audit

Clean Release build/publish and locked restore passed. The explicit Release build reports **0 warnings and 0 errors**. Output: `artifacts/phase-9-publish/`, run directly in Production, not through `dotnet run`/launch settings. The output is framework-dependent and needs an appropriate .NET 10 runtime on its eventual host.

| Finding | Action/result |
| --- | --- |
| Six unused legacy scripts were being shipped | Set `CopyToPublishDirectory="Never"` for `script.js`, `inner-project.js`, `member.js`, `contact.js`, `bootstrap.min.js`, and `aos.js`. Source/reference copies remain. |
| Development configuration and application symbols shipped | Exclude `appsettings.Development.json`; set `CopyOutputSymbolsToPublishDirectory=false`. Local build symbols remain available. |
| Static HTML reference deployment | No `.html` files in publish; root references remain outside the web project. |
| Source/IDE/test/private artifacts | No source/test/IDE folders, Razor/C# sources, certificates, `.env`, logs, or user settings found in output. |
| Framework resources | Blazor framework JS, scoped CSS/reconnect module, MudBlazor resources/assembly, canonical data, and active enhancements remain present. |
| Asset manifests | Excluded scripts and their compressed copies are absent; no obsolete endpoints remain for them. Requests to both normal and `/assets` alias paths return 404. |
| Necessary generated files | Runtime/dependency/endpoint JSON, assemblies, app host, `appsettings.json`, and SDK-generated `web.config` are runtime/deployment material, not accidental source leakage. |

Baseline: 260 files / 20,901,505 bytes. Hardened output: 240 files / 20,538,505 bytes, a 363,000-byte reduction. These are complete server deployment sizes, not per-page browser transfer sizes. A package-provided 255,470-byte `MudBlazor.min.js.map` remains; it describes public vendor JavaScript, not application/server secrets. CountUp's license is deliberately retained.

Deploy **only the fresh publish output**, never the repository root. Publish exclusions do not clean files left by an older deployment; use a new/empty output and a deployment strategy that removes obsolete deployed files. The new `scripts/Test-Publish.ps1` fails if excluded files are left behind.

## Runtime hardening

Existing Production behavior is appropriate for this provider-neutral phase: generic `/Error` handling, HSTS and HTTPS redirection outside Development, safe 404 reexecution, antiforgery, static assets, and Interactive Server endpoints. No development exception page or detailed circuit-error setting is enabled. Normal logs use Information with ASP.NET warnings; submitted Contact values are not logged.

`FoundationDiagnostics` is mounted only when **both** `Environment.IsDevelopment()` and `verify=foundation` are true. The published Production `/?verify=foundation` renders no diagnostic UI. No standalone diagnostic endpoint was found.

The published browser connected using a WebSocket and processed server-side FAQ/form events. Existing reconnect/resume UI and handlers remain published. Failover, long idle periods, circuit recovery after server replacement, and load balancing are staging/provider checks, not certified by this local smoke run.

The local smoke host used loopback HTTP, intentionally without configuring certificates or a reverse proxy. Its only settled server warning was “Failed to determine the https port for redirect.” This is expected for that HTTP-only invocation and is **not** a valid public-host configuration. TLS/HSTS redirects must be verified on staging; they were not disabled in code. Initial sandbox attempts could not access the ordinary NuGet configuration/data-protection key store; rerunning with normal host permissions resolved that environmental restriction without code changes.

Provider-specific requirements, deliberately not hardcoded:

- Configure TLS termination and the correct external scheme/host. When using a reverse proxy, process forwarded headers before scheme-sensitive middleware and trust only the actual proxies/networks; do not accept forwarded headers from arbitrary callers.
- Restrict `AllowedHosts` to approved production hosts (the current portable default is `*`). Validate redirect behavior and avoid proxy HTTPS loops.
- Support WebSocket upgrades and appropriate timeouts. Validate session affinity for the chosen multi-instance Interactive Server topology.
- Provide persistent, access-controlled data-protection keys appropriate to the host and scaling topology. Do not ship development keys/certificates.
- Keep Production environment selection, credentials, logging destinations, health/operational controls, and runtime patching in deployment configuration.

References: [Microsoft proxy/forwarded-header guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0), [SignalR hosting/scaling](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0), and [static-file publish exclusions](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0).

## Asset and dependency audit

| Asset group | Classification |
| --- | --- |
| Root `index.html`, `service.html`, `about.html`, `project.html`, `inner-project.html`, `team.html`, `member.html`, `contact.html`, original `assets/` | Reference only; useful for design comparisons; not deployed by this project. Retained. |
| Legacy content-binding/form scripts and Bootstrap/AOS JS | Unused by Blazor; safe publish exclusions now applied. Still available in source/reference mode. |
| Original page CSS and shared Bootstrap/AOS/Owl CSS | Actively used for approved geometry, breakpoints, grids, and reveals. Still required; not excluded or rewritten. |
| jQuery 3.1.0 / Owl Carousel 2.3.4 | Still required for scoped carousels. Retained without modernization. |
| CountUp 2.0.8 | Still used for Home counters; library and license retained. |
| `public-shell.js`, route modules, `carousels.js`, `reveal.js` | Active scoped enhancements; lifecycle tests pass. |
| Canonical project/member JSON | Public content and required server catalog input. Retained; not private configuration. |
| MudBlazor 9.10.0 / Blazor | Required. Exact locked version unchanged, one script/style reference, four single shared providers, correct CSS precedence. Public page components remain theme-owned HTML. |

NuGet vulnerability check including transitive packages reported no vulnerable .NET packages from the configured feed at review time. This does **not** cover vendored JavaScript. jQuery 3.1.0 predates documented DOM-manipulation XSS fixes; resolve its security posture before production and before introducing untrusted CMS HTML. No exploitable path was demonstrated in this bounded pass; existing content is Razor-encoded and Contact does not feed user HTML into jQuery. An upgrade is intentionally not bundled into this phase. See the [official jQuery security-fix announcement](https://blog.jquery.com/2020/04/10/jquery-3-5-0-released/).

## SEO / metadata

All nineteen valid concrete routes have exactly one unique page title and one nonempty unique meta description. Added descriptions to the five missing top-level pages and dynamic Project/Member Detail pages. Replaced Home's template description and removed template keyword metadata. Project descriptions derive from the canonical project summary; member descriptions use canonical name/role. Dynamic titles were already correct and remain unchanged.

Error and not-found pages now include `noindex`. No production domain was invented. Canonical URLs, sitemap/robots policy, redirect strategy, OG/Twitter sharing metadata, and business/schema/content validation remain a separate SEO pass; no existing shared SEO abstraction justified adding one here.

## Fonts / favicons / performance

- Actual PNG dimensions: `favicon-16x16.png` = **16x12**, `favicon-32x32.png` = **32x23**, `apple-touch-icon.png` = **180x129**. Corrected the two advertised favicon sizes to match the files. All icon URLs resolve in publish. Square, production-quality favicon/touch artwork is still needed; no replacement artwork was fabricated.
- Lato/Poppins families and sans-serif fallbacks remain correct in computed styles and visual smoke checks. Poppins is requested both by the theme import and a head link (the latter includes italic faces); Lato uses the theme import. Consolidation/subsetting is recommended, but the unchanged shared theme/weights/italic coverage were preserved in this lite pass.
- Largest images include `vision.png` (585,515 bytes), `partner-bg.svg` (402,905), `asset4.svg` (339,301), and `service-bg-gradient.svg` (240,770). Some SVGs embed raster data. Optimize/trace usage separately; do not blindly delete or recompress design assets.
- No duplicate Blazor or MudBlazor script/style references were found. Classic vendor scripts remain in their dependency order near the body end; no speculative async/defer changes were made. Published fingerprinting and compression remain enabled. No application PDB or development settings are deployed.

## Contact / newsletter / map

Added a visible, form-associated notice **before entry**: “Demo only: this form does not send or store messages. Please do not enter sensitive information.” Existing valid submission still states “Demo form submitted successfully. No message was sent.” Required/whitespace/email validation and all-field reset remain intact.

The demo service performs no email, external API request, persistent storage, or value logging. Input and events necessarily traverse the site's own Interactive Server connection and exist temporarily in circuit memory; this is not a client-only form.

Newsletter now visibly says signup is unavailable; input and button are natively disabled, in addition to accessible explanatory text. No backend was added.

Map URL still embeds Calgary Tower, with a meaningful title, lazy loading, referrer policy, and the existing responsive container. The response CSP is `frame-ancestors 'self'`; this controls who can embed the site, not whether the site can load Google Maps. No obvious app CSP `frame-src` restriction or browser console block was found. The in-app browser still showed an `about:blank` frame rather than verified map tiles. Verify map rendering, location accuracy, and any deployment CSP in a real staging browser. No Maps API integration or key was added.

## Accessibility / security

One H1 per route; shared banner/main/footer/navigation landmarks; existing skip link; labeled form controls; image alt attributes; named active controls; visible form focus outline; mobile Escape/focus return; keyboard navigation and FAQ activation all checked. Existing reduced-motion handling remains covered by the JS tests. New notices inherit readable theme colors and fit phone/desktop layouts.

Known inherited member-menu contrast, Project Detail Features clipping, and fixed-height/editorial constraints remain for an approved accessibility/content pass. A small initial Home reveal-related overflow (5px on phone / 2px on desktop) was observed before visiting unrevealed sections; it cleared after the section reveals. Changed Contact/footer layouts showed no overflow. No blanket overflow rule or redesign was added. This is not a WCAG/screen-reader certification.

Targeted source/configuration/filename scans found no obvious credentials, private API keys, connection-string secrets, certificates, `.env` files, or private deployment URLs. Two key-pattern matches were false positives within embedded SVG image data. Launch settings contain only local development addresses and are not published. Server configuration/assemblies, legacy HTML, and excluded scripts are not web-accessible in the published smoke test. Existing antiforgery and `frame-ancestors 'self'` / `X-Frame-Options: SAMEORIGIN` behavior remains. This is a sanity review, not a penetration test or a complete historical secret scan.

`.gitignore` now also covers local `/publish/`, logs/temp/OS noise, `.env` files (except an example), and private certificate/key formats. No useful source/docs were removed.

## Published-app smoke test

- Clean Release, locked restore, Release publish, and explicit Release build: pass; **0 build warnings/errors**.
- New `Test-Publish.ps1`: pass — 19 route title/description/H1 checks, 66 rendered assets, active modules, manifest/output exclusions, private URLs, proper 404/noindex, Production diagnostics guard, truthful favicon declarations, and demo notices.
- Existing Home and Contact HTTP suites against publish: pass.
- JS lifecycle/reduced-motion tests: **20 pass**. Contact C# checks: **21 pass**.
- Browser sequence from publish in Production: Home -> Services -> About -> Projects -> NexConnect -> Team -> Emily Johnson -> Contact -> Home. Direct HTTP checks also cover every canonical detail slug.
- Mobile menu/Escape, keyboard route links, FAQ server event, Contact invalid submission then valid demo submission/reset, and route reentry: pass. Browser console has no app warnings/errors; WebSocket connected. Desktop/phone checks focused on changed Contact/footer notices; full historical visual QA was not rerun because no shared visual CSS or artwork changed.
- Saved ignored evidence: `artifacts/phase-9-qa/contact-demo-desktop.png` and `artifacts/phase-9-qa/newsletter-mobile.png`.

### Reproduce with a fresh output

From the repository root, in PowerShell, stop on a failing command:

```powershell
dotnet clean NexNovaCo.sln -c Release
dotnet restore NexNovaCo.sln --locked-mode
# Choose a new, unused output directory for each verification.
dotnet publish src/NexNovaCo.Web/NexNovaCo.Web.csproj -c Release --no-restore -o artifacts/release-smoke-new
Push-Location artifacts/release-smoke-new
dotnet NexNovaCo.Web.dll --urls http://localhost:5158 --environment Production
# After stopping the local server:
Pop-Location
```

While the server runs, from a second terminal at the repository root:

```powershell
pwsh -NoProfile -File scripts/Test-Publish.ps1 -PublishPath artifacts/release-smoke-new -BaseUrl http://localhost:5158
node --test scripts/Test-*Interop.mjs
dotnet run --project tests/NexNovaCo.Contact.Tests --no-restore
```

Use normal local permissions for ASP.NET data protection. This loopback HTTP command is for smoke testing only, not a public hosting recipe. The original `Test-Foundation.ps1` intentionally expects every source-reference asset to be served in Development; use `Test-Publish.ps1` for the hardened publish contract instead.

## A. Fixed in this phase

Publish exclusions for six dormant scripts/development config/application symbols; meaningful route descriptions; removal of template SEO copy; error/404 noindex; truthful favicon dimensions; upfront Contact demo notice; visible/native-disabled newsletter state; additional ignore rules; reproducible publish audit and deployment notes.

## B. Required before production

Real Contact/newsletter delivery decision or removal/explicit unavailable presentation; approved business/contact/member/marketing/pricing information; actual map/location verification; staging TLS/forwarded-header/host/WebSocket/session-affinity/data-protection checks; known accessibility/contrast/clipping review; and the security disposition of vendored jQuery before public launch or untrusted content ingestion. No production readiness approval is implied by this phase.

## C. Recommended before production

Square favicon/touch artwork, consolidated/subset fonts, image/SVG optimization, appropriate vendor source-map cleanup, broader performance/cross-browser checks, dependency modernization with regression coverage, SEO/social/canonical/sitemap/robots polish, and eventual reference cleanup in a dedicated scope.

## D. Later Dashboard / product work

Authentication/authorization, roles/permissions, database, CMS, admin dashboard, uploads/media controls, and real workflows remain unimplemented. Establish the threat model and trusted/untrusted content boundaries before connecting CMS data to the public UI.

## Recommendation

The codebase is ready to begin a separately approved Dashboard/CMS planning and development phase after review of this branch. Resolve the documented security/content boundaries before introducing untrusted editing or uploads. It is **not** ready for an unqualified production launch. No Dashboard implementation was started; stop for approval.
