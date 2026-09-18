# Current status - 18 September 2026

This section supersedes the historical review below. The original "Next work" list is no longer the current backlog.

## Implemented
- .NET 10 / EF Core 10, modern admin password hashing, legacy-hash migration, login throttling and account lockout, protected POST logout and password changes.
- Graduation-year filters, listing eligibility/source/verification fields, category SEO routes and RSS feeds.
- Consolidated category counts, indexes, paginated admin jobs, responsive admin screens and site/ad configuration.
- Launch readiness dashboard at /Readiness (admin only): HTTPS, domain/channels/ads, pending database migrations, legacy files and active content completeness. Reports manual checks honestly rather than treating configuration as verified production readiness.
- Needs review filter in the job library for incomplete active listings.
- IIS App_Data protection and exclusions for test/build/private files.
- Legacy applicant-file protection remains documented; there is no applicant resume upload or admin resume-download workflow.
- scripts/Backup-SiteFiles.ps1: new ZIP archives containing private data, keys and uploads, preserving relative paths and hidden files.
- scripts/Prepare-Release.ps1: build/publish plus idempotent SQL generation without deploying or applying migrations.
- Reusable isolated browser and storage regression checks under tests/. See DEPLOYMENT.md for commands and recovery instructions.

## Verified this pass
- Debug and Release project builds: zero warnings and errors.
- Release publish completed; private App_Data and local settings excluded.
- Idempotent deployment SQL generated at artifacts/deploy.sql; not applied to production.
- 154 browser assertions passed using 24 synthetic jobs in a fresh LocalDB database. Includes CRUD, input validation, populated pagination, category/batch browsing, feeds, sitemap, anonymous access protection, logout antiforgery, readiness counts, content review, saved jobs, application redirects and responsive widths 320/390/768/1440.
- Storage checks passed for preview, migration, repeated runs, collision rejection, archive contents, extraction/content restoration, backup overwrite rejection and public-destination rejection.
- Local legacy resume folder contained zero files. No real resumes were moved.
- Temporary browser server stopped; isolated test database and artifacts retained for inspection.

## Remaining external work
- Deploy to the actual host, apply reviewed migrations and verify hosting/TLS/proxy behavior.
- Back up and restore the real database and files in an isolated recovery drill; synthetic file restoration is not proof of production recovery.
- Set the production domain to `https://d2djobs.in`, configure real channel URLs, and publish verified job content. Google Search Console submission, TLS certificate/proxy behavior, AdSense approval, CMP consent collection and live ad delivery remain external checks.
- Optional future implementations: email subscriptions, automatic channel posting, AdSense API sync, admin recovery and 2FA.

---

# Historical review (superseded where noted above)

# Focused review — 17 September 2026

Reference: https://www.freshersvoice.com/

Already present: off-campus, walk-in, IT, government, bank and internship categories; location/qualification search; sorting; trending and category sections; saved jobs; expiry handling; sharing; reporting; job structured data; sitemap; legal pages; ad configuration.

## Implemented in this pass
- Block requests to /uploads/resumes before static-file middleware. Existing files are retained. Production IIS/CDN/static hosting must also prevent direct access, and resumes should ultimately move outside wwwroot.
- Explicitly register the in-memory distributed cache used by session storage.
- Only load homepage category previews on the first unfiltered page with the default sort, avoiding up to seven unnecessary preview queries on later pages and other sorts.
- Replace ambiguous pagination text with Previous and Next.

## Next work, in priority order
1. Admin authentication: replace unsalted SHA-256 with ASP.NET Core password hashing, migrate existing hashes on successful login, remove the fallback bootstrap password, add login throttling, and use POST with antiforgery for logout. Verify deployment secrets are stored outside checked-in settings.
2. Applicant resume uploads are outside the current product scope. Existing legacy files, if any, should be handled by the hosting provider separately.
3. Jobs by graduation batch: add structured eligible years, an EF migration, admin editing, public filters, and filter persistence through sorting/pagination. Freshersvoice already exposes batch browsing.
4. Job alerts: add configured Telegram/WhatsApp channel links or opt-in email subscriptions with verification/unsubscribe. Sharing a job is already implemented; subscriptions are not. Real channel URLs or delivery configuration are needed.
5. Listing quality: add official source URL, last-verified timestamp, explicit eligibility, selection process and walk-in date/venue. Preserve these through admin editing and show only supplied details.
6. SEO: category pages currently use query parameters and all query pages are noindex. Add stable category routes and unique metadata/sitemap entries while keeping arbitrary searches out of the index.
7. Runtime upgrade: currently net8.0 with EF Core 8.0.18. Plan .NET 10 LTS plus compatible EF packages and verify hosting support, database migrations, authentication and uploads. .NET 8 support ends 10 November 2026. Do not treat an SDK installed locally as proof production supports the upgrade.
8. Performance: consolidate the ten category count queries, add database indexes based on query plans, and paginate the admin list. Avoid caching browser-specific saved-job state.

## Validation and limitations
Build validation is recorded in the delivery message. No database migration or production deployment was performed. Browser behavior and production file access still need runtime verification. Existing uploaded documents were not opened.

Sources:
- https://www.freshersvoice.com/
- https://dotnet.microsoft.com/en-us/platform/support/policy
- https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/
## Mobile verification — 17 September 2026
- Build passed with zero warnings/errors.
- Headless Edge checks passed at 320, 375, 390, 768 and 1440 CSS pixels: no horizontal document overflow or JavaScript errors on home, empty search, and page=2 fallback.
- Mobile menu opened successfully at 390px.
- Local database has zero active listings: populated pagination, actual detail pages and active ad rendering were not verified. Do not interpret the page=2 fallback check as populated pagination verification.
- Screenshot captured in .mobile-check/mobile-home.png; visual inspection was blocked by the image tool sandbox.
- Browser tooling/output is excluded from application build/publish items. No test listings or advertising IDs were added.
## Admin UI redesign — 17 September 2026
Completed: dedicated admin navigation/layout, responsive overview with metrics and job library, mobile job cards, login redesign, shared Create/Edit job editor with grouped fields and logo preview, consistent analytics shell. Replaced legacy global admin styling with scoped admin.css. Existing job fields and controller actions are retained.
Validation: isolated build passed with zero warnings/errors; authenticated Edge checks passed on overview, Create, existing Edit and Analytics at 320/390/768/1440px (16 combinations), plus login responsiveness, presence of all editable fields and empty-title validation. No JavaScript errors or horizontal document overflow. No listings were created, changed or deleted during testing. Desktop/mobile dashboard screenshots captured and visually reviewed.
Changes are local. Restart/rebuild the normal app to load the updated Razor views. Temporary test server was stopped.