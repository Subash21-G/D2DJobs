# D2DJobs production-readiness checklist

Reviewed: 13 October 2026. This is a sampled public-site review plus a code audit. It is not an AdSense approval or a full live-database audit.

## Confirmed live content requiring an editor

The live homepage showed eleven openings when reviewed. Do not treat this count as the number of publishable records. Review each active row in **Admin -> Needs review**, then check the linked source and correct or close the row manually.

- [Xelentor Technologies - .NET Developer](https://d2djobs.in/job/net-developer-e72f55b4c3): qualification field contains a verification placeholder; application action is unavailable; the description contains generic role-context wording and the editorial field is a generated template. Source link points to a job-portal posting. Check whether this is the same opening as the next row.
- [Xelentor Technologies Pvt. Ltd. - .NET Developer](https://d2djobs.in/job/net-developer-9ec1e644e1): qualification is “Not Specified”; application action is unavailable; the description and editorial note contain generated boilerplate. It has the same role, location and experience as the other Xelentor page. Compare vacancy IDs and dates with an employer source before deciding whether these are duplicates.
- [Hiringlobal - 6 Engineering Roles Open](https://d2djobs.in/job/6-engineering-roles-open-d40d045009): one page combines six distinct roles with different experience and technology requirements, has no available application action, and includes templated role-context and editorial text. Verify each vacancy separately; do not invent missing requirements or links.
- The previous 12 October sampled audit in [ADSENSE-REMEDIATION.md](ADSENSE-REMEDIATION.md) also records Wipro Customer Support / Document Validation and WNS Associate, Operations as requiring source, date, application-link and source-type review. Recheck whether those records are still present before working on them.

These findings come from public page content. The production database was not queried, edited or used to infer record IDs.

## Manual correction workflow

For every active and imported draft, an editor should:

1. Open the linked source independently and confirm the exact vacancy, employer, source type, source posting date if one is shown, current application status and any closing or walk-in date.
2. Replace qualification placeholders only with source-supported details. If the source gives no qualification, say that the employer has not specified it.
3. Write a concise job-specific responsibilities summary. Remove role boilerplate, repeated editorial notes and instructions that mention internal field names.
4. Confirm the exact application destination and write steps that match that vacancy. If no working application route can be verified, keep the record out of public discovery.
5. Split multi-role adverts only when each resulting record can be verified independently. Compare company, role, location, vacancy ID, application URL and dates before resolving a suspected duplicate.
6. Record a verification date only after a person checks the source. Update status through the editor; do not bulk change active flags.

## Code and SEO audit

- ReadyForIndex is the public per-job quality predicate. NeedsReview is now its logical complement, so both database queries use the same SQL-translatable rule set.
- Duplicate pairs remain a contextual review check. Company comparison ignores common legal suffixes such as “Pvt. Ltd.”; duplicate detection only flags records for human review and does not delete or deactivate existing records in bulk.
- Homepage cards, category and search results, counts, related jobs, suggestions, RSS, sitemap job URLs, apply redirects and JobPosting schema all use the public discovery gate. Historical active detail URLs stay available with noindex, no JobPosting schema and no programmatic ads. Known qualification placeholders, generated description/editorial text and internal application field names are suppressed from the public detail output and metadata.
- Canonical tags, query-string noindex behavior, AdSense ownership meta, dynamic ads.txt, robots, sitemap, navigation, page links and ad gating were checked in source and the isolated browser suite. The web review tool could not fetch the live text endpoints /robots.txt, /ads.txt or /sitemap.xml; check those endpoints directly after deployment.
- The layout only loads the AdSense library when the page explicitly allows ads and the approval, consent and publisher settings are all ready. The repository configuration supplies a publisher ID but does not enable ads; keep ads disabled until approval and consent configuration are verified.
- No database schema change is part of this remediation. No migration was rewritten or applied, no live SQL was run, and no job status was changed.

## Career-resource review

The sampled software-interview and fresher-resume guides provide concrete preparation steps, such as project walkthroughs, test strategy, evidence in resumes and document privacy. The resource library is useful as general guidance, but it remains broad advice rather than reporting tied to a particular employer or vacancy. The sampled articles share a repeated closing reminder and the job detail pages repeat category-preparation guidance. Review the full library for freshness, repeated passages and original editorial ownership before requesting AdSense review; add examples or tools only where the publisher can substantiate them.

## Before production release

- [ ] Back up the production database, App_Data data-protection keys, uploaded logos and deployment configuration using the existing release procedure.
- [ ] Confirm the release artifact is built from this feature branch and inspect the diff. Do not change job data through a deployment script.
- [ ] Verify the production database migration level. This code change adds no migration; do not run automatic migrations for it. The existing pending-migration safety script passed and confirms the existing imported-job migration does not mutate Jobs or reactivate imported rows.
- [ ] Deploy only after review and approval. Then check canonical host redirects, homepage and category content, a noindex historical job, an eligible job, application redirects, robots.txt, ads.txt, sitemap, RSS, AdSense ownership metadata and ad absence on under-review pages.
- [ ] In Admin, correct or close all records in Needs review and resolve duplicate candidates manually. Confirm the active excluded-from-discovery count reflects the intended result.
- [ ] Recheck the career-resource library and all public navigation at mobile and desktop widths. Keep advertising disabled until the account and consent platform are confirmed ready.
- [ ] Submit the sitemap and request AdSense review only after the manual content work is complete. Approval is Google's decision.

## Verification run in this branch

- Release build: passed, zero warnings and errors.
- Migration safety verification: passed for the existing 20261001082905_ActivateImportedJobs migration; its ID is preserved and generated SQL has no Jobs operations.
- Isolated browser regression: passed 233 checks with a fresh disposable LocalDB database, fake AdSense configuration and all external browser requests blocked.
