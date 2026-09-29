# Resume builder and employer workspace

## Routes

- `/resume-builder`: free browser-only resume editor, live plain-text preview, explicit local draft save/load/clear, browser print / Save as PDF.
- `/resume-builder/services`: optional ATS review (₹99–₹199), resume preparation (₹299–₹499), or resume + LinkedIn support (₹499–₹799). These are indicative ranges. Requests are stored for manual follow-up; no payment is collected and no email is automatically sent.
- `/employer/register`, `/employer/login`, `/employer`: separate employer accounts and campaign dashboard.
- `/admin/commerce`: existing admin login required. Review jobs, act on featured requests, issue invoices, confirm payments received externally, and update resume enquiry status.

The public navigation links to the builder and employer area. The admin navigation links to the request queue.

## Employer workflow

1. Employer registers, then posts an opening with start/end dates and an HTTPS application URL.
2. Admin verifies the company and details, then approves or rejects the submission. Registration alone does not verify the employer.
3. Approved jobs appear in the existing public catalogue on their start date. Future jobs cannot be opened or applied to early. Existing expiry rules stop applications after the end date.
4. Editing a submission pauses its public listing and requires another review. Stale edits/reviews are rejected using database row versions. Listing counters and URLs survive approval of edits.
5. Employers can request featured placement. Admin agrees pricing and dates externally and explicitly activates the upgrade. Invoice creation and payment confirmation do not automatically activate placement.
6. Admin issues a uniquely numbered invoice with seller details, billing details, description, and agreed INR total. The employer can view and print it. This is a basic single-total invoice, without tax calculation, payment gateway, credit-note, or accounting integration.

Dashboard views, apply clicks, and CTR are lifetime counters from the linked job. They are not unique visitors or completed applications; automated or repeated requests may contribute. Campaign dates use the site's existing server-local date convention; audit timestamps use UTC.

Employer authentication uses a separate secure, HTTP-only cookie. Employer credentials grant no admin access. All employer campaign and invoice lookups enforce account ownership. Mutation endpoints require antiforgery tokens. Authentication and public enquiry submission are rate limited; failed employer sign-ins also trigger account lockout. Passwords are hashed. Company verification and account recovery are manual support operations in this release.

## Deployment

1. Back up the database using the normal deployment process.
2. Review and apply `scripts/employer-resume-migration.sql` before deploying the new application. The script is idempotent and contains only migration `AddEmployerWorkspaceAndResumeRequests`; prior application migrations must already be applied.
3. Deploy the application and new static assets together. Restart the application and verify the three main routes above over HTTPS.

New schema: `EmployerAccounts`, `EmployerCampaigns`, `CampaignInvoices`, `ResumeServiceRequests`, plus nullable `Jobs.AvailableFrom`. Existing listings retain their previous visibility because the new date defaults to null. Production migrations are not applied automatically unless the existing `Database:ApplyMigrations` configuration is enabled.

Use the admin queue for manual email follow-up. Before offering paid work, enter accurate seller/buyer details and confirm scope, final price, and delivery time with the customer. Resume-builder content is not uploaded; service requests store only the submitted contact details and notes.

## Verification

Run in a dedicated LocalDB database (the harness never uses production database configuration):

```powershell
./tests/Run-BrowserChecks.ps1 -PlaywrightModules .mobile-check/node_modules -Port 7241 -TestScript tests/commerce.cjs
```

The tests cover local resume drafts, plain-text rendering, print layout, request validation, secure employer authentication, cross-account isolation, admin approval, application metrics, featured upgrades, invoice access, stale edits, scheduled visibility, and responsive layouts. Test screenshots and the printed sample are generated under ignored `artifacts/`.
