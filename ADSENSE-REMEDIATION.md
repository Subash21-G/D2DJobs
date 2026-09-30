# AdSense low-value-content remediation

Reviewed: 30 September 2026. This is a content and implementation audit, not confirmation of AdSense approval.

## What the live-site sample showed

The public homepage at https://d2djobs.in displayed 162 active opportunities. The number of listings does not establish their usefulness or originality.

The sampled [UIIC Administrative Officer listing](https://d2djobs.in/job/administrative-officer-scale-i-b8fc6e14e2) deferred qualification, eligibility and selection details to the official notification. Its application section provided generic advice. A candidate still had to leave the page to establish several basic requirements. This is a likely contributor to low value, not a confirmed explanation from Google's reviewers.

The live navigation did not include the career-resource library present in the local project. Local changes do not affect the public review until deployed.

## Fix each listing before publication

Use the employer's current official notice as the source. Do not fill gaps with invented requirements, dates, selection stages or salary figures.

1. Explain the actual work and responsibilities in your own clear summary.
2. State the eligible qualifications, stream, experience and any batch or other conditions that the notice supplies. Clearly identify details the employer has not specified.
3. Explain the published selection stages and any deadlines or instructions relevant to this opening.
4. Give application steps for the exact vacancy: where to find it, which job identifier to use if supplied, what files are requested and how submission is confirmed.
5. Add useful original editorial context, such as how a candidate can demonstrate the requested skills or an easily missed condition in the notice. Separate advice from employer requirements.
6. Link the exact official notice or vacancy where available. Check that the application link reaches the correct opening and that applications remain open.
7. Record the source-check date only after checking the source. Review the final page as a candidate, including on mobile.

Start with the UIIC page and other listings that defer core details to the source. Then review the full active inventory using **Admin > Needs review** and **Admin > Launch readiness**. Review inactive drafts separately: newly submitted incomplete listings are saved as drafts and are not counted as active jobs needing review.

Character minimums and the 45-day source-check window in this application are internal completeness rules, not Google's approval thresholds. Passing them does not prove originality, accuracy, usefulness or approval eligibility. Rewording a generic paragraph for each job does not supply meaningful original value.

## Technical safeguards

Existing work in this checkout adds application instructions, documents and editorial notes, publishing checks, category guidance and a career-resource library. Review that content before publication.

This audit closes two additional gaps:

- Direct job URLs now use the duplicate-content check used by discovery. Repeated guidance blocks indexing, JobPosting markup and ad eligibility on the detail page as well.
- The shared ad component now checks page eligibility and the environment, preventing it from rendering ad slots on pages whose ad loader is suppressed.

Launch readiness now excludes jobs scheduled for future publication from its current-content count. These checks do not delete existing job records.

## Deploy and request review

1. Follow [DEPLOYMENT.md](DEPLOYMENT.md), including backups and review of the database migration. The application and editorial-content migration must be deployed together.
2. Prepare the existing job inventory before public rollout. New editorial columns start empty for old jobs; affected jobs will disappear from discovery until their content passes the publishing checks. Avoid launching an empty catalogue as a substitute for improving content.
3. Keep advertising disabled while the account is unapproved. Confirm the real publisher account, working navigation, contact details, editorial policy and resource pages on the public domain.
4. Check that candidate-facing content is accurate, useful and original; remove or update expired openings and broken links. Review the guides for accuracy and actual editorial ownership before publishing them.
5. Check the live homepage, categories, job details, sitemap, feed and mobile layout after deployment. Search noindex settings manage discovery; they do not exempt pages from AdSense's content policies.
6. Only after the live-site work is complete, confirm the issues are fixed in AdSense and request review. Google makes the approval decision.

## Verification

Run the focused regression suite:

```powershell
powershell -NoProfile -File tests/Run-BrowserChecks.ps1 -PlaywrightModules .mobile-check/node_modules -TestScript tests/content-quality.cjs -VerifyAdGating
```

The runner uses a new LocalDB database and a separate content root. The test uses fake ad configuration, blocks external browser requests, and covers eligible jobs, incomplete drafts, legacy duplicate/thin/stale content, expired and scheduled jobs, metadata, ads, discovery, RSS, sitemap, resource routes and four viewport widths. It does not change production data or AdSense settings.

## Google guidance

- [Make sure your site's pages are ready for AdSense](https://support.google.com/adsense/answer/7299563?hl=en): original content, useful navigation and visitor experience.
- [Google-served ads on screens without publisher-content](https://support.google.com/publisherpolicies/answer/11112688?hl=en): restrictions on low-value, empty and navigation-only screens.
- [Google Search spam policies](https://developers.google.com/search/docs/essentials/spam-policies): avoid producing pages at scale without value for users. Search policies and AdSense approval are separate assessments.
