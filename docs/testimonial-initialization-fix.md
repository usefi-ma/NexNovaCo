# One-time testimonial initialization correction

Verified 2026-09-22. Scope: initialization semantics only, on `feature/dashboard-cms`, starting clean at `886422c`. No Partners CMS, CRUD redesign, public CSS/JS change, push, or merge.

## Root cause

`TestimonialInitializer` previously treated `!Testimonials.Any()` as the complete initialization decision. Admin deletion of every row made that condition true again, so the next controlled startup restored the approved defaults. Collection membership did not preserve whether initialization had already happened.

## Fix

A dedicated `TestimonialInitializationState` singleton (Id 1) records completion independently of testimonial rows. Controlled startup now:

1. Begins a transaction and checks the marker. If present, leaves all content alone, including an empty collection.
2. If unmarked, adopts existing content unchanged; otherwise inserts the exact approved defaults in their existing order.
3. Saves the marker and any seeded rows in the same transaction. A failure rolls back both, allowing a safe retry.

Admin CRUD never deletes/resets the marker. Public reads never initialize content. The last-item delete dialog's explanation now says sections remain hidden until an Admin adds another testimonial; only that explanatory text changed, not UI structure or delete behavior.

## Migration and legacy upgrade

Additive migration `20260922212453_AddTestimonialInitializationState` adds only the singleton marker table/constraint. It does not modify testimonial rows, Identity, or any Home settings schema.

An existing database from the prior release may already have had every testimonial deleted before this marker existed. The previous Testimonials migration uses SQLite AUTOINCREMENT IDs. SQLite retains the table's insertion high-water mark in `sqlite_sequence` after normal row deletion. The new migration backfills the marker if testimonial rows exist **or** the Testimonials sequence has insertion history. Thus an initialized-but-empty legacy collection stays empty; a freshly migrated/never-inserted table remains unmarked and receives defaults at controlled startup. This does not infer initialization from a particular row count or copy match.

The sequence check is a one-time compatibility bridge in the migration. The permanent initialization decision uses the explicit marker, not the collection's row count. No developer database was reset, inspected for content, or used for test mutations.

## Verification

All database scenarios ran against disposable file-backed AuthFactory databases or isolated in-memory SQLite databases. Restart checks start new application hosts against the same test database and execute the real migration/startup path.

| Scenario | Result |
| --- | --- |
| Fresh database | Exact two approved testimonials, paragraph structure/order, one marker |
| Restart after fresh initialization | Same content, IDs, order and timestamps; no duplicates |
| Delete one through Admin service, restart | Only the remaining item survives; deleted item not recreated |
| Delete all through Admin service, repeated restarts | Collection stays empty; marker persists |
| Add to initialized empty collection, restart | Only the new Admin-created item remains, unchanged |
| Upgrade never-initialized prior schema | Defaults seeded once |
| Upgrade prior schema after delete-one/delete-all | Remaining content or intentional emptiness preserved |
| Upgrade customized prior collection | Existing content/order/IDs/timestamps untouched |
| Simulated marker insert failure | Seed and marker roll back together; retry succeeds |
| Public Home/Projects with zero items | HTTP 200, empty shared data, no testimonial carousel root |
| Empty-root interop | Home/Projects initialize, repeat initialization, and dispose without throwing or creating Owl instances |

- Auth/CMS harness: **935 checks passed**, including existing authorization, all eight Home settings, shared CRUD/reorder, and the new initialization/restart/upgrade checks.
- Home/Projects/Dashboard interop suites: **9 tests passed**.
- Debug and Release builds: **zero warnings and zero errors**.
- `git diff --check`: passed.

The existing `Testimonials.Count > 0` guards in Home and Projects already safely omit empty sections; they were tested and left unchanged. This focused correction did not require a browser visual redesign/regression pass.

## Git / scope

One focused commit accompanies this report: `Fix one-time testimonial initialization`. No unrelated schema, service CRUD, carousel, public markup, or styling edits. Partners CMS has not started. Stop for approval.
