# AdSense low-value-content remediation

Reviewed: 12 October 2026. This is a sampled content and implementation audit, not confirmation of AdSense approval.

## What the live-site sample showed

The public homepage at https://d2djobs.in displayed 135 active opportunities. The career-resource navigation and library were live, with fifteen substantial guides. The number of listings does not establish their usefulness or originality.

Two sampled live listings showed confirmed quality risks. The [Wipro Customer Support / Document Validation page](https://d2djobs.in/job/customer-support-executive-document-validation-21248b9c62) linked both its source and application action to the generic Wipro Careers homepage; its walk-in date was 9 October while the audit date was 12 October, and its source was labelled “Other.” The [WNS Associate, Operations page](https://d2djobs.in/job/wns-walk-in-drive-2026-associate-operations-3fe5a9b727) linked a Naukri listing, also labelled “Other,” included a past 18 September walk-in date, and described the application as comparing the two supplied URLs rather than giving vacancy-specific steps. Its on-page source check was dated 20 September. These samples need human source review; they do not establish the state of every active record or the reason for Google's decision.

The live homepage, resource library and editorial policy were accessible at review time. Local code changes below do not affect the production site until deployed.

## Fix each listing before publication

Use the employer's current official notice as the source. Do not fill gaps with invented requirements, dates, selection stages or salary figures.

1. Explain the actual work and responsibilities in your own clear summary.
2. State the eligible qualifications, stream, experience and any batch or other conditions that the notice supplies. Clearly identify details the employer has not specified.
3. Explain the published selection stages and any deadlines or instructions relevant to this opening.
4. Give application steps for the exact vacancy: where to find it, which job identifier to use if supplied, what files are requested and how submission is confirmed.
5. Add useful original editorial context, such as how a candidate can demonstrate the requested skills or an easily missed condition in the notice. Separate advice from employer requirements.
6. Link the exact official notice or vacancy where available. Check that the application link reaches the correct opening and that applications remain open.
7. Record the source-check date only after checking the source. Review the final page as a candidate, including on mobile.

Start with the sampled Wipro and WNS pages, then review the full active inventory using **Admin > Needs review** and **Admin > Launch readiness**. Review inactive drafts separately. Do not rely on the source URL's domain alone: confirm that the link identifies the exact vacancy and that the application action does not lead only to a company homepage.

Character minimums and the 45-day source-check window in this application are internal completeness rules, not Google's approval thresholds. Passing them does not prove originality, accuracy, usefulness or approval eligibility. Rewording a generic paragraph for each job does not supply meaningful original value.

## Technical safeguards

Existing work in this checkout adds application instructions, documents and editorial notes, publishing checks, category guidance and a career-resource library. Review that content before publication.

This remediation closes these technical gaps:

- The former discovery quality gate did not use its source-verification cutoff. Missing and stale verification dates now require review, and the admin form provides an explicit date field with instructions to record it only after checking the source.
- Generic application URLs, `Other` source classifications, reused copy and likely duplicates are review warnings and do not qualify for publication or index discovery. New and imported jobs stay inactive until editorial review.
- Direct legacy job URLs that fail current quality or duplicate checks remain available as historical pages with `noindex`, no programmatic ad slot and no `JobPosting` schema. They are removed from public job lists, RSS and the sitemap.
- The homepage and clean category routes stay indexable when their query is empty; an empty category can still offer its category guide. Search/filter URLs remain noindexed.
- Excel imports report accepted, rejected and draft rows, detect likely duplicates, and ignore workbook-supplied source-verification dates.
- Date-only walk-in values no longer display a false midnight time. A source-check date appears on a listing only when one was recorded.
- The admin overview includes draft, review, duplicate, missing-source-check, malformed-application-URL and index-eligible counts. These are triage metrics, not factual verification or approval evidence.

Launch readiness now excludes jobs scheduled for future publication from its current-content count. These checks do not delete existing job records.

## Deploy and request review

1. Follow [DEPLOYMENT.md](DEPLOYMENT.md), including backups and review of database changes. This remediation adds no database columns or migration.
2. Prepare the existing job inventory before public rollout. The stricter source and content gate can remove many current records from public discovery until a human checks and improves them. Keep useful homepage and category editorial content available while that review is in progress.
3. Keep advertising disabled while the account is unapproved. Confirm the real publisher account, working navigation, contact details, editorial policy and resource pages on the public domain.
4. Check that candidate-facing content is accurate, useful and original; remove or update expired openings and broken links. Review the guides for accuracy and actual editorial ownership before publishing them.
5. Check the live homepage, categories, job details, sitemap, feed and mobile layout after deployment. Search noindex settings manage discovery; they do not exempt pages from AdSense's content policies.
6. Only after the live-site work is complete, confirm the issues are fixed in AdSense and request review. Google makes the approval decision.

## Verification

Run the focused regression suite:

```powershell
powershell -NoProfile -File tests/Run-BrowserChecks.ps1 -PlaywrightModules .mobile-check/node_modules -TestScript tests/content-quality.cjs -VerifyAdGating
```

The runner uses a new LocalDB database and a separate content root. The test uses fake ad configuration, blocks external browser requests, and covers eligible jobs, inactive imports, ignored verification dates, generic application URLs, legacy duplicate/thin/stale content, expired and scheduled jobs, metadata, ads, discovery, RSS, sitemap, resource routes and four viewport widths for public pages and admin screens. It does not change production data or AdSense settings. On 12 October 2026, `dotnet restore` and `dotnet build JobForFresher.slnx` succeeded with zero warnings; `dotnet test JobForFresher.slnx --no-build --no-restore` completed without discovering a test project; the isolated browser suite passed 148 checks; and `git diff --check` passed. Restore and browser execution required access beyond the default sandbox for NuGet.Config and LocalDB.

## Google guidance

- [Make sure your site's pages are ready for AdSense](https://support.google.com/adsense/answer/7299563?hl=en): original content, useful navigation and visitor experience.
- [Google-served ads on screens without publisher-content](https://support.google.com/publisherpolicies/answer/11112688?hl=en): restrictions on low-value, empty and navigation-only screens.
- [AdSense account approval guidance](https://support.google.com/adsense/answer/81904?hl=en): content quality, insufficient content, navigation and landing-page issues.
- [JobPosting structured-data guidelines](https://developers.google.com/search/docs/appearance/structured-data/job-posting): apply markup only to a specific, current job page and keep required data consistent with visible content.
- [Google Search spam policies](https://developers.google.com/search/docs/essentials/spam-policies): avoid producing pages at scale without value for users. Search policies and AdSense approval are separate assessments.
