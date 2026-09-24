# Phase 19 — Production Contact email delivery

## Status and Git

Implemented on `feature/dashboard-cms`, starting from clean commit `8590b09`. Implementation and tests: `715bc17` (`Add secure server-side Contact email delivery`); this report and README follow in a separate documentation commit. No branch change, merge, push or history rewrite. Final comprehensive CMS QA was not started.

The implementation is ready for deliberate business mailbox/provider configuration. **Real delivery has not been tested.** All automated and browser success paths used fakes; no external SMTP connection or email was sent during verification.

No database migration, submission table, Dashboard inbox, delivery queue or Newsletter integration was added. The normal developer database was not reset; CMS integration and browser tests used isolated databases.

## Previous flow and new architecture

Previously `DemoContactFormService` validated the form and returned an honest demo-only response. It is now replaced by:

`ContactForm` → scoped `ContactFormService` → `IContactEmailSender` → `SmtpContactEmailSender` → configured authenticated SMTP server.

The server service independently validates and normalizes the existing first name, last name, email, optional subject and message, checks the honeypot, enforces submission budgets/concurrency and snapshots the input before asynchronous delivery. The SMTP adapter independently validates before constructing MIME headers. No transport configuration is sent to the browser or stored in CMS.

Contact CMS still owns headings, labels, placeholders, validation presentation and map content. `SiteContactSettings` remains public contact information; its displayed email is **not** automatically the delivery recipient. Newsletter remains disabled and non-functional, with no subscriber storage or success simulation.

## Transport and dependency

[MailKit 4.18.0](https://www.nuget.org/packages/MailKit/4.18.0) is pinned and locked. It provides an asynchronous SMTP client, MIME-safe message construction through MimeKit, authenticated delivery and modern TLS. The lockfiles also include MimeKit 4.18.0, BouncyCastle.Cryptography 2.7.0 and System.Security.Cryptography.Pkcs 10.0.0. No second provider was added; a future transactional HTTP provider can replace `IContactEmailSender`.

Each delivery uses a fresh client. TLS 1.2/1.3 is required with standard certificate validation and **no certificate bypass**. Port 465 uses implicit TLS; other configured ports use required STARTTLS, not opportunistic upgrade. `UseSsl=false` is rejected. See [MailKit secure socket options](https://mimekit.net/docs/html/T_MailKit_Security_SecureSocketOptions.htm).

The default timeout is 15 seconds, enforced both through cancellation and the SMTP client timeout; allowed range is 5–60 seconds. There is no automatic retry or background queue. Success means the SMTP server accepted the message, not that the recipient inbox has confirmed arrival. A later QUIT/disconnect failure does not undo an accepted send. A connection loss during acceptance can leave delivery uncertain; exactly-once inbox delivery is not promised and automatic retries were deliberately not added.

## Exact operational configuration

All keys are under `ContactEmail`. User Secrets use the colon form; environment variables use double underscores. Committed defaults leave delivery disabled and all addresses, host and credentials empty.

| User Secret/config key | Environment variable | Default / requirement | Classification |
| --- | --- | --- | --- |
| `ContactEmail:Enabled` | `ContactEmail__Enabled` | `false`; enable only after configuration | Non-secret |
| `ContactEmail:RecipientAddress` | `ContactEmail__RecipientAddress` | Empty; one business recipient mailbox | Operational, not CMS |
| `ContactEmail:SenderAddress` | `ContactEmail__SenderAddress` | Empty; one provider-approved sender mailbox | Operational, not CMS |
| `ContactEmail:SenderName` | `ContactEmail__SenderName` | `NexNovaCo`; max 100, no header controls | Non-secret |
| `ContactEmail:SmtpHost` | `ContactEmail__SmtpHost` | Empty; valid SMTP hostname | Non-secret operational |
| `ContactEmail:SmtpPort` | `ContactEmail__SmtpPort` | `587`; provider port, normally 587 or 465 | Non-secret |
| `ContactEmail:UseSsl` | `ContactEmail__UseSsl` | `true`; false is not supported | Non-secret |
| `ContactEmail:Username` | `ContactEmail__Username` | Empty; provider authentication identity | Credential; protect |
| `ContactEmail:Password` | `ContactEmail__Password` | Empty; SMTP password/provider token | **Secret** |
| `ContactEmail:TimeoutSeconds` | `ContactEmail__TimeoutSeconds` | `15`; range 5–60 | Non-secret |

Disabled or incomplete settings do not prevent startup or rendering other pages. Submission returns the generic failure; server logs distinguish Disabled from missing/invalid setting **names**, without printing values. Typed settings are validated at delivery time rather than crashing unrelated routes at startup.

### Production setup

1. Select one authenticated SMTP provider and verify the sending domain/address using that provider's instructions. Confirm required host, port and credentials.
2. Supply the exact keys above via development User Secrets or the deployment platform's protected configuration/secret store. For local setup, the project already has a User Secrets ID. For example, `dotnet user-secrets set "ContactEmail:RecipientAddress" "<business-recipient-address>" --project src/NexNovaCo.Web` shows the key format only: replace the placeholder privately, never commit real values. Do not paste secrets into this report, source files, screenshots or task logs. User Secrets are a development facility, not an encrypted production vault; protect local access and avoid exposing passwords through shared shell history.
3. Configure the recipient separately from the public `SiteContactSettings` email. Configure a provider-authorized From address, not a visitor address. Leave `UseSsl=true`.
4. Set `ContactEmail:Enabled=true` only when all required values are available. Restart the app after changing configuration; this implementation uses `IOptions`, not live SMTP configuration reload.
5. Complete the real-delivery checklist below in local/staging before declaring production delivery verified.

Production may require provider/domain verification and SPF, DKIM and DMARC. Obtain the precise records and alignment requirements from the chosen provider/domain administrator. No DNS records were generated or changed in this phase. Do not copy fabricated generic DNS values.

## From, To, Reply-To and content

- From: configured `SenderAddress` and `SenderName`.
- To: configured `RecipientAddress` only.
- Reply-To: validated visitor email only; it never becomes From.
- Subject: trimmed visitor subject, or `NexNovaCo Contact Request` when blank.
- Body: plain text containing the existing five fields and a server-generated UTC submission timestamp. No raw HTML.

First/last names are required, single-line and capped at 100 characters each. Email is required, capped at 254 and parsed as exactly one mailbox without a display name. Subject remains optional, single-line, capped at 200. Message is required and capped at 5,000. Required whitespace-only input is rejected. Header-bearing fields reject control characters including CR/LF; lists/display-name email input and injected headers are rejected. Surrounding whitespace is trimmed for delivery while meaningful internal message line breaks remain intact. Visible maxlength attributes supplement, but do not replace, server validation.

## Anti-spam, rate limiting and request integrity

Submission is an **Interactive Server circuit event**, not a standalone Contact HTTP POST. No unnecessary public API or separate antiforgery mechanism was introduced; existing application antiforgery/auth middleware is unchanged. All input is still treated as untrusted at the server service boundary.

The service uses supported `System.Threading.RateLimiting` fixed-window and concurrency primitives used by [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0), at the actual submission boundary rather than restricting Contact GETs or all SignalR events:

- Up to 5 attempts per 10-minute window per circuit.
- Up to 30 attempts per minute across the application process.
- At most 2 concurrent sends per process; no queued requests.
- An atomic per-service gate plus the component sending guard prevents overlapping repeated-click submissions.

Attempts reaching the service consume budget even if subsequently rejected by server validation/honeypot. Ordinary invalid-form UI validation does not invoke the service. All rate/concurrency/honeypot rejection uses the same generic failure as transport failure.

The honeypot is hidden, excluded from the accessibility tree and tab order, and never sends if populated. No form-fill timing threshold was added: autofill, assistive technology, reconnects and Blazor lifecycle timing make a minimum duration unreliable.

No client IP or forwarded header is trusted. Circuit limits reset with a new circuit, but the process budget still applies. Limits are in-memory, reset at application restart, and are not shared across instances; they are conservative single-process controls, not DDoS protection. Production edge/distributed abuse policy is a future deployment concern. Deliberate retries after a completed send are not content-deduplicated or stored.

## UX, accessibility, logging and privacy

The existing form layout/CMS labels/styles remain. A privacy notice replaces the demo-only notice. While awaiting delivery, inputs and submit are disabled, the button reads `Sending…`, and `aria-busy` plus the live status describe progress.

Only accepted delivery returns `Thank you. Your message has been sent successfully.` It resets all visible fields and the honeypot, clears the clean validation state and focuses/announces feedback. Failure returns `We couldn't send your message right now. Please try again later.`, retains values and restores submit. Label/error associations, required/optional semantics and keyboard access remain. No SMTP details reach the UI.

Structured logs contain transport acceptance, failure category, setting names, rate/concurrency and honeypot rejection reasons. SMTP exception objects/messages, body, addresses, credentials and protocol transcripts are not logged. The transport holds the message only for validation/send; the app stores no Contact submission in SQLite. On failure the live form retains values for the visitor to retry. Email provider/mailbox retention is outside the application's no-database-storage promise.

## Verification

All tests below passed using fake senders/socket-free SMTP proxies and isolated test databases where applicable:

| Check | Result |
| --- | --- |
| Contact model/service/adapter/component harness, Debug and Release | 105 checks each |
| Focused Contact page CMS regression | 221 checks |
| Focused Global Settings regression | 289 checks |
| Public quality regression | 215 checks |
| Contact HTTP/asset/markup smoke | Passed, including initial enabled fields, maxlength, hidden honeypot and privacy association |
| Contact JavaScript interop lifecycle | 2 tests |
| Solution Debug and Release builds | 0 warnings / 0 errors |
| Contact test project Debug and Release builds | 0 warnings / 0 errors |

The delivery harness covers configured headers, subject fallback, normalization, malformed/oversized/whitespace input, injection defenses, disabled/missing configuration, TLS modes, timeout and transport failures, disconnect-after-acceptance, no sensitive logging, both rate budgets, concurrency, double-submit, cancellation, failure retention and success reset. A browser-discovered Boolean `disabled` binding issue was corrected and an initial-input-enabled HTTP assertion added.

Debug outputs were isolated under `bin/ContactEmailDebug/net10.0/` to avoid overwriting the developer's running Debug application. Useful commands:

```powershell
dotnet build NexNovaCo.sln --no-restore -c Debug -p:OutputPath=bin/ContactEmailDebug/net10.0/
dotnet build NexNovaCo.sln --no-restore -c Release
dotnet run --project tests/NexNovaCo.Contact.Tests -c Release --no-restore
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --contact-page
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --global-settings
dotnet tests/NexNovaCo.Auth.Tests/bin/Release/net10.0/NexNovaCo.Auth.Tests.dll --public-quality
node scripts/Test-ContactInterop.mjs
```

### Focused browser smoke

The test executable's `--contact-browser` host binds localhost:5199 with a fresh isolated database and an injected fake sender. It cannot send email, regardless of machine SMTP secrets. `simulate-failure` as subject returns a delayed failure; other valid subjects return delayed fake success. This mechanism exists **only in the test project**, not as a production configuration switch or endpoint.

| Real browser | Version | 1440px | 390px |
| --- | --- | --- | --- |
| Chrome | 153.0.8010.53 | Passed | Passed |
| Edge | 153.0.4234.48 | Passed | Passed |
| Firefox | 156.0.1 | Passed | Passed |

All six cases verified required/invalid/whitespace validation, sending label/busy state, disabled inputs, generic failure with values retained, successful reset using the fake, repeated-Enter protection, accessible status roles/focus, honeypot absence from accessibility navigation and disabled Newsletter. Horizontal overflow: 0; console errors: 0; failed local requests: 0. Desktop Chrome and mobile Firefox screenshots were visually inspected. Local screenshots/results are in ignored `artifacts/contact-email-qa/`; they are QA artifacts, not production assets. This was focused Contact smoke, not a repeat of the entire Phase 18 matrix or final CMS QA.

## Manual real-delivery checklist — NOT YET RUN

- [ ] Configure real local/staging provider settings privately, enable delivery and restart.
- [ ] Submit one synthetic Contact message; observe sending then success.
- [ ] Confirm arrival in recipient inbox (check spam/quarantine as needed).
- [ ] Verify configured business recipient and provider-approved From.
- [ ] Verify Reply-To is the supplied visitor address; reply and confirm its destination.
- [ ] Confirm plain-text content/line breaks/optional-subject fallback and UTC timestamp.
- [ ] Inspect application logs: no credentials, message body or unnecessary personal information.
- [ ] Confirm there is no Contact submission table/row in SQLite.
- [ ] Deliberately disable transport in staging; verify generic failure retains values and other pages remain usable. Restore approved settings.
- [ ] Confirm provider/domain verification and required SPF/DKIM/DMARC configuration with the provider/domain administrator.

## Deferred / handoff

Real mailbox delivery/deliverability is pending only deliberate provider configuration and the manual checklist. Newsletter, Contact database storage/inbox, CAPTCHA, distributed rate policies, queues/retries, multiple email providers and final comprehensive CMS QA remain outside this phase. No production secrets, DNS or real mailbox were modified. Stop for approval before any next phase or merge.
