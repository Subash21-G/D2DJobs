# Deployment and recovery

## Prepare
Run `powershell -File scripts/Prepare-Release.ps1` from this repository with the .NET 10 SDK and matching EF CLI installed. This produces `artifacts/publish` and `artifacts/deploy.sql`; it does not apply SQL or deploy anything. Use a fresh artifacts folder for each release.

## Before updating production
1. Verify the host can run this net10.0 application (including the ASP.NET Core hosting bundle for IIS). Keep the current application release for rollback.
2. Stop the site or enable the host's maintenance mode. Back up SQL Server using the host's database backup facility. Record the backup and confirm it can restore into a separate database.
3. Run `powershell -File scripts/Backup-SiteFiles.ps1 -SiteRoot 'C:\\sites\\JobForFresher' -Destination 'D:\\private-backups\\JobForFresher-release.zip'`. Choose a new archive outside the website and protect access. This includes App_Data security keys/settings and uploaded logos, including hidden files. Back up environment secrets separately through the host's secret manager. The ZIP is not encrypted and is not a database backup.
4. If an old deployment contains applicant documents, retain them outside the public web root and migrate them using your host's secure file-management process. This project has no applicant resume upload or admin resume-download workflow.
5. Review and apply deploy.sql against the intended database through your database administration tool. Do not enable automatic production migrations as a substitute for reviewing the script.
6. Deploy the publish output without deleting existing App_Data or uploads. Grant the application identity only the required filesystem access. Configure database credentials, admin bootstrap credentials if needed, and email credentials through deployment secrets. Disable bootstrap after initial account creation.
7. Use HTTPS. If a reverse proxy terminates TLS, configure trusted forwarded headers for that actual hosting topology before testing login. Do not trust arbitrary forwarded headers.
8. Set `SiteSettings__SiteUrl=https://d2djobs.in` in the production environment (or save the same URL in Admin > Site setup), then open Admin > Site setup for the real channel URLs. Leave advertising off until actual account approval, slot IDs and the appropriate consent platform are ready.

## Verify
- Admin > Launch readiness checks the current database migrations, configuration and incomplete active listings.
- Admin > Needs review finds active listings missing title, company, role, location, qualification, skills, description, selection process, application link, official source, eligibility or last verification date. Supply factual values; batch ranges and selection details must come from the employer.
- Check login/logout, job creation/editing, two populated listing pages, batch/category filters, saved jobs, job detail and application redirects. Use an isolated database for test listings.
- Check mobile widths 320, 390, 768 and 1440 pixels, RSS, sitemap and canonical URLs. Submit `https://d2djobs.in/sitemap.xml` in Google Search Console after the live HTTPS check.
- Verify /App_Data/site-settings.json returns no file content through the public host/CDN. IIS hidden segments and application middleware block private application data.
- /health/live is a liveness probe only; use the authenticated readiness page for database and configuration checks.
- Verify ads on the live domain only after legitimate setup. Readiness reports configuration, not Google approval or successful consent collection.

## AdSense review gate
Do not request review until every item below is true on the public domain, not only in a local build.

1. Deploy the current application and database migration together. Confirm `/resume-builder`, `/employer/register`, and `/editorial-policy` return successful pages from the main navigation.
2. Confirm the old ad-blocker dialog is absent on the home, legal, and job-detail pages.
3. Confirm `/ads.txt` contains the intended `pub-` account and the home page source contains the matching `google-adsense-account` meta tag. A publisher ID verifies ownership; it does not mean the site is approved.
4. In Admin > Launch readiness, resolve every active listing under **Needs review**. Manually verify factual accuracy and originality; field-presence checks cannot detect copied or misleading text.
5. Keep Monetag and AdSense ad placements disabled during review. In AdSense, configure Google's Privacy & messaging CMP or another Google-certified CMP, then test the live consent journey where it is required. Only then confirm the consent setting in this application.
6. Check the privacy policy, terms, disclaimer, editorial policy, About, Contact, and all primary navigation links on mobile and desktop. Confirm expired jobs and broken application links are removed or corrected.
7. Add `d2djobs.in` in AdSense, verify ownership, and request review. Do not select **Site approved** or enable ad slots until AdSense reports the site as **Ready**.
8. After approval, add the issued ten-digit slot IDs, confirm the certified CMP is live, select **Site approved**, and enable ads. Verify advertisements are labelled and remain separated from application controls.

## Restore drill
Keep the application stopped. Restore SQL into an isolated database; extract the file archive into an isolated site preserving App_Data and wwwroot/uploads paths. Restore environment configuration from your secret manager, point it at the isolated database, and restrict network access. Start the matching application release; verify login, a saved job and logo rendering. Check public private-file blocking again. Record when and where the drill succeeded. Do not overwrite a running production database during a drill.

## Features still requiring separate implementation
Email subscriptions, automatic channel posting, automatic AdSense synchronization, password recovery and two-factor authentication are optional future features. Existing RSS, channel links and manual ad reporting remain available.

## Repeat local checks
Run the browser regression script for isolated application checks; file backups are covered by the deployment backup procedure.
Run `powershell -NoProfile -File tests/Run-BrowserChecks.ps1 -PlaywrightModules .mobile-check/node_modules` with SQL Server LocalDB, the development HTTPS certificate, Node, Playwright and Edge available. The runner creates a new LocalDB database, random temporary admin credentials and a separate content root; it stops the test server afterward. It does not reuse the normal application database or modify site settings. Test databases and artifacts are retained for inspection. The PlaywrightModules argument may point to another existing installation; no dependency download is performed by this runner.
