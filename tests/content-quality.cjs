const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const nodePath = require('node:path');

(async () => {
  const base = process.env.TEST_BASE_URL;
  assert.ok(base && process.env.TEST_ISOLATED_DATABASE && process.env.TEST_ADMIN_USER && process.env.TEST_ADMIN_PASSWORD, 'Use the isolated test runner');
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  let checks = 0;
  const check = (condition, message) => { assert.ok(condition, message); checks++; };
  try {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    // Never load real ads or other external resources with fake test settings.
    await context.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
    const page = await context.newPage();
    await page.goto(base + '/Admin/Login');
    await page.locator('[name="userName"]').fill(process.env.TEST_ADMIN_USER);
    await page.locator('[name="password"]').fill(process.env.TEST_ADMIN_PASSWORD);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL('**/Admin');
    await page.goto(base + '/Admin/Create');
    const token = await page.locator('[name="__RequestVerificationToken"]').first().inputValue();
    const date = days => new Date(Date.now() + days * 86400000).toISOString().slice(0, 16);
    const dateOnly = days => new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);
    const displayDate = value => { const d = new Date(value + 'T00:00:00Z'); return `${String(d.getUTCDate()).padStart(2, '0')} ${d.toLocaleString('en-US', { month: 'short', timeZone: 'UTC' })} ${d.getUTCFullYear()}`; };
    const job = suffix => ({
      __RequestVerificationToken: token, Title: 'QUALITY ' + suffix, CompanyName: 'Isolated Test Employer ' + suffix,
      Category: 'IT Jobs', Role: 'Graduate software engineer ' + suffix, Location: 'Chennai', Qualification: 'Bachelor of Engineering in Computer Science',
      Skills: 'C#, SQL, debugging and automated testing', Experience: 'Fresher', JobType: 'Full time', Salary: 'Not disclosed',
      Description: 'Isolated regression fixture, not a real vacancy. The graduate engineer works on a small web application, investigates reproducible defects, writes tests for changed behavior and explains implementation decisions during a supervised code review.',
      Eligibility: 'Computer science engineering graduates who can demonstrate a working web project and explain their own contribution.',
      SelectionProcess: 'A code exercise followed by a discussion of the submitted solution with an engineer.',
      ApplicationInstructions: `For test opening ${suffix}, use the employer careers link, select the graduate engineering vacancy, attach the requested resume and retain the portal confirmation.`,
      EditorialNote: `Preparation for test opening ${suffix}: choose one project and be ready to explain a defect you diagnosed, the evidence you collected and how you verified the correction.`,
      SourceType: 'OfficialEmployer', SourcePostedDate: date(-5).slice(0, 10),
      ApplyLink: 'https://example.invalid/apply/' + encodeURIComponent(suffix), OfficialSourceUrl: 'https://example.invalid/careers/' + encodeURIComponent(suffix),
      LastVerifiedUtc: date(-1), IsActive: 'true', IsFeatured: 'false'
    });
    const create = async form => {
      const r = await context.request.post(base + '/Admin/Create', { form, maxRedirects: 0 });
      check(r.status() === 302, 'Create ' + form.Title);
    };
    for (const suffix of ['Original', 'Second', 'Third', 'Fourth', 'Fifth']) {
      const listing = job(suffix);
      if (suffix === 'Second') listing.WalkInDate = '2026-10-20';
      if (suffix === 'Third') { listing.SourcePostedDate = ''; listing.SourceType = 'OfficialEmployer'; }
      if (suffix === 'Fourth') { listing.SourceType = 'GovernmentPortal'; listing.WalkInStartDate = dateOnly(7) + 'T09:00'; listing.WalkInEndDate = dateOnly(8) + 'T17:00'; }
      if (suffix === 'Fifth') { listing.SourceType = 'JobPortal'; listing.ApplyLink = 'https://example.invalid/?jobId=isolated-123'; }
      await create(listing);
    }
    await create({ ...job('Original'), Title: 'QUALITY Duplicate' });
    await create({ ...job('Thin'), ApplicationInstructions: '', EditorialNote: '', SourcePostedDate: '' });
    await create({ ...job('Stale'), LastVerifiedUtc: date(-60) });
    await create({ ...job('Expired'), ExpiryDate: date(-2).slice(0, 10) });
    await create(job('Scheduled'));
    const importWorkbook = nodePath.resolve('artifacts/quality-import-fixture.xlsx');
    execFileSync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', 'tests/Create-ImportWorkbook.ps1', '-OutputPath', importWorkbook], { encoding: 'utf8' });
    await page.goto(base + '/Admin/Import');
    const importToken = await page.locator('[name="__RequestVerificationToken"]').first().inputValue();
    const importedResponse = await context.request.post(base + '/Admin/Import', {
      maxRedirects: 0,
      multipart: {
        __RequestVerificationToken: importToken,
        File: { name: 'quality-import-fixture.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', buffer: fs.readFileSync(importWorkbook) }
      }
    });
    check(importedResponse.status() === 200, 'Excel import returns its automatic publication summary');
    const importHtml = await importedResponse.text();
    check(importHtml.includes('Accepted 2; rejected 0; imported 2 active listing(s).'), 'Import summary confirms rows were automatically activated');
    check(importHtml.includes('By uploading, you confirm that you checked each vacancy against its source.'), 'Import records the uploader confirmation and uses server time for verification');
    check(!importHtml.includes('Review required before publishing') && !importHtml.includes('Review imported drafts'), 'Import no longer requires a separate review workflow');
    await create({ ...job('UnsupportedCategory'), Category: 'Company Wise Jobs' });
    await create({ ...job('MissingSourceType'), SourceType: '' });
    await create({ ...job('Generic'), ApplicationInstructions: 'Eligible candidates can apply online through the official website. For this particular test vacancy, select its role, upload the required resume and retain the portal confirmation number.' });
    await create({ ...job('MultiJob'), Title: 'Top 10 Fresher Jobs and Companies' });
    await create({ ...job('GenericApplyUrl'), ApplyLink: 'https://example.invalid/' });
    await create({ ...job('QualificationPlaceholder'), Qualification: 'verify the full qualification requirements in the official employer source.' });
    await create({ ...job('InternalFields'), ApplicationInstructions: 'Open ApplyLink and use OfficialSourceUrl to confirm the vacancy, submit the application, attach the requested resume and retain the confirmation.' });
    await create({ ...job('BoilerplateDescription'), Description: 'This vacancy summary is long enough for the completeness check, but contains generic material. Related role context: professionals in this type of position commonly analyse requirements, implement or support solutions, test and troubleshoot work, maintain documentation and collaborate across teams.' });
    await create({ ...job('TemplateEditorial'), EditorialNote: 'D2DJobs editorial note for this test role: this repeated note advises candidates to reconfirm the role, details, dates and application steps directly with the employer before applying.' });
    await create({ ...job('InvalidApplyUrl'), ApplyLink: 'https://example.invalid/apply' });
    await create({ ...job('InvalidAuthorityUrl'), ApplyLink: 'https://example.invalid/apply' });
    await create({ ...job('Xelentor A'), CompanyName: 'Xelentor Technologies', Title: 'QUALITY Xelentor A', Role: '.NET Developer', Location: 'Ahmedabad, Gujarat' });
    await create({ ...job('Xelentor B'), CompanyName: 'Xelentor Technologies Pvt. Ltd.', Title: 'QUALITY Xelentor B', Role: '.NET Developer', Location: 'Ahmedabad, Gujarat' });

    for (const title of ['Duplicate', 'Thin', 'Stale', 'UnsupportedCategory', 'MissingSourceType', 'Generic', 'MultiJob', 'GenericApplyUrl']) {
      await page.goto(base + '/Admin?search=' + encodeURIComponent('QUALITY ' + title));
      const edit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
      await page.goto(base + edit);
      check(await page.locator('[name="IsActive"][type="checkbox"]').isChecked(), title + ' retains the administrator selected Active status');
    }
    await page.goto(base + '/Admin?search=QUALITY%20Imported%20Unsupported');
    const importedUnsupportedEdit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
    await page.goto(base + importedUnsupportedEdit);
    check(await page.locator('[name="IsActive"][type="checkbox"]').isChecked(), 'Imported jobs are automatically active');
    await page.goto(base + '/Admin?search=' + encodeURIComponent('QUALITY Imported Date Test'));
    const importedSupportedEdit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
    await page.goto(base + importedSupportedEdit);
    check(await page.locator('[name="SourceType"]').inputValue() === 'OfficialEmployer', 'Missing Excel source type is detected from a company-matching source domain');
    check(await page.locator('[name="LastVerifiedUtc"]').inputValue() !== '', 'Imported jobs receive a server verification timestamp');
    await page.getByRole('button', { name: 'Save changes' }).click();
    await page.waitForURL('**/Admin');
    const rows = JSON.parse(execFileSync('powershell.exe', ['-NoProfile', '-File', 'tests/Set-QualityFixtures.ps1'], { encoding: 'utf8' }));
    const path = title => '/job/' + rows.find(row => row.title === 'QUALITY ' + title).slug;
    const get = async url => {
      const response = await context.request.get(base + url);
      return { status: response.status(), html: await response.text() };
    };
    const hasJobSchema = html => html.includes('"@type":"JobPosting"');
    const hasAd = html => html.includes('class="adsbygoogle"') || html.includes('pagead/js/adsbygoogle.js');
    const excluded = ['Original', 'Duplicate', 'Thin', 'Stale', 'GenericApplyUrl', 'QualificationPlaceholder', 'InternalFields', 'BoilerplateDescription', 'TemplateEditorial', 'InvalidApplyUrl', 'InvalidAuthorityUrl', 'Xelentor A', 'Xelentor B', 'Expired'];
    for (const title of excluded) {
      const { status, html } = await get(path(title));
      check(status === (title === 'Expired' ? 410 : 200), title + ' response');
      check(html.includes('content="noindex, follow"'), title + ' noindex');
      check(!hasJobSchema(html), title + ' no job schema');
      check(!hasAd(html), title + ' no ad slot or loader');
      check(!html.includes('/Home/ApplyClick'), title + ' has no active application action');
    }
    check((await get(path('Scheduled'))).status === 404, 'Scheduled listing stays private');
    const valid = await get(path('Second'));
    check(valid.status === 200 && valid.html.includes('content="index, follow"'), 'Complete unique listing indexable');
    check(hasJobSchema(valid.html), 'Complete unique listing has job schema');
    check(valid.html.includes(`"datePosted":"${job('Second').SourcePostedDate}"`), 'Job schema uses the factual source posting date');
    check(valid.html.includes('View official employer posting') && valid.html.includes('Apply on company website'), 'Official employer links are labelled accurately');
    check(valid.html.includes('20 Oct 2026') && !valid.html.includes('12:00 AM IST'), 'Date-only walk-in values do not render a midnight time');
    check(hasAd(valid.html), 'Positive control: eligible page renders fake ad configuration');
    const queryApply = await get(path('Fifth'));
    check(queryApply.status === 200 && queryApply.html.includes('Continue to application source') && queryApply.html.includes('content="index, follow"'), 'Vacancy-specific query parameter application URL remains publishable');
    const queryRedirect = await context.request.get(base + '/Home/ApplyClick?id=' + rows.find(row => row.title === 'QUALITY Fifth').id, { maxRedirects: 0 });
    check(queryRedirect.status() === 302 && queryRedirect.headers().location === 'https://example.invalid/?jobId=isolated-123', 'Approved application redirect preserves its vacancy query parameter');
    const legacyApply = await context.request.get(base + '/Home/ApplyClick?id=' + rows.find(row => row.title === 'QUALITY Original').id, { maxRedirects: 0 });
    check(legacyApply.status() === 404, 'Direct ApplyClick request cannot bypass review and duplicate checks');
    const legacyDetails = await get(path('Original'));
    check(legacyDetails.html.includes('This listing is under review') && !legacyDetails.html.includes('class="primary-button" href="/Home/ApplyClick'), 'Legacy unverified detail page explains review and has no Apply action');
    const placeholderPage = await get(path('QualificationPlaceholder'));
    check(!placeholderPage.html.includes('verify the full qualification requirements') && placeholderPage.html.includes('Qualification details are being reviewed.'), 'Public historical page replaces qualification placeholder with a neutral review message');
    const internalFieldPage = await get(path('InternalFields'));
    check(!internalFieldPage.html.includes('ApplyLink') && !internalFieldPage.html.includes('OfficialSourceUrl'), 'Internal application field names never reach the public page');
    check(!(await get(path('BoilerplateDescription'))).html.includes('Related role context: professionals in this type of position commonly'), 'Generated role boilerplate is hidden on historical detail pages');
    check(!(await get(path('TemplateEditorial'))).html.includes('D2DJobs editorial note for this test role'), 'Templated editorial notes are hidden on historical detail pages');
    const noSourceDate = await get(path('Third'));
    check(noSourceDate.html.includes('Added to D2DJobs') && !noSourceDate.html.includes('"datePosted"'), 'Missing employer posting date is not fabricated in page/schema');
    const government = await get(path('Fourth'));
    check(government.html.includes('View official recruitment notice') && government.html.includes('Apply through official recruitment portal'), 'Government source links use accurate labels');
    check(government.html.includes(displayDate(dateOnly(7))) && government.html.includes(displayDate(dateOnly(8))), 'Walk-in date range renders both dates');
    const jobPortal = await get(path('Fifth'));
    check(jobPortal.html.includes('View source posting') && jobPortal.html.includes('Continue to application source'), 'Third-party job portal is not labelled as employer source');
    const importedDateTest = await get(path('Imported Date Test'));
    check(importedDateTest.status === 200 && importedDateTest.html.includes('content="index, follow"'), 'Supported imported category alias publishes as a canonical category');
    check(importedDateTest.html.includes('Internship Programs'), 'Import normalizes the supported category alias');
    const importedDateLabels = importedDateTest.html.match(/(?:Posted|Added to D2DJobs)[^<]*/g) || [];
    check(/25 Sep(?:t)? 2026/.test(importedDateTest.html), 'Import keeps the source PostedDate: ' + importedDateLabels.join(' | '));
    check(importedDateTest.html.includes('"datePosted":"2026-09-25"'), 'Imported source PostedDate is used for JobPosting datePosted');
    const home = await get('/');
    check(['Original', 'Duplicate', 'Thin', 'Stale'].every(title => !home.html.includes(path(title))), 'Unverified, duplicate and thin legacy jobs stay out of public discovery');
    check(hasAd(home.html), 'Curated homepage ads require the isolated approved and consent-ready test configuration');
    check(home.html.includes('content="index, follow"'), 'Homepage remains indexable when it contains curated jobs and editorial content');
    const sitemap = (await get('/sitemap.xml')).html;
    const feed = (await get('/jobs/feed.xml')).html;
    const secondSitemapIndex = sitemap.indexOf(path('Second'));
    const secondSitemapBlock = sitemap.slice(sitemap.lastIndexOf('<url>', secondSitemapIndex), sitemap.indexOf('</url>', secondSitemapIndex));
    const sitemapUrls = [...sitemap.matchAll(/<loc>(.*?)<\/loc>/g)].map(match => new URL(match[1]));
    check(sitemapUrls.some(url => url.pathname === '/') && sitemapUrls.some(url => url.pathname === '/jobs/work-from-home'), 'Homepage and useful empty category pages stay in the sitemap');
    check(!secondSitemapBlock.includes('<lastmod>'), 'Job lastmod is omitted when the app has no actual content-modified timestamp');
    for (const title of [...excluded, 'Scheduled']) {
      check(!sitemap.includes(path(title)), title + ' excluded from sitemap');
      check(!feed.includes(path(title)), title + ' excluded from RSS');
    }
    check(!home.html.includes(path('Expired')) && !home.html.includes(path('Scheduled')), 'Expired and future listings stay out of the homepage');
    for (const title of ['Original', 'Duplicate', 'Thin', 'Stale'])
      check(!sitemap.includes(path(title)) && !feed.includes(path(title)), title + ' remains out of SEO discovery until reviewed');
    check(sitemap.includes(path('Second')) && feed.includes(path('Second')) && sitemap.includes(path('Imported Date Test')) && feed.includes(path('Imported Date Test')), 'Eligible listings in sitemap and RSS');
    const importedUnsupportedPath = '/job/' + rows.find(row => row.title === 'QUALITY Imported Unsupported').slug;
    check(!home.html.includes(importedUnsupportedPath) && !sitemap.includes(importedUnsupportedPath) && !feed.includes(importedUnsupportedPath), 'Unverified imported jobs stay out of public discovery and SEO feeds');
    for (const url of ['/?search=absent-quality-fixture', '/Home/About', '/resources', '/definitely-missing']) {
      check(!hasAd((await get(url)).html), 'No ads on ' + url);
    }
    const readiness = (await get('/Readiness')).html;
    check(/\d+ active listing\(s\); \d+ need substantial publishing details\./.test(readiness), 'Readiness counts report active listings and listings needing review');
    await page.goto(base + '/Admin?status=Needs%20review');
    const reviewRows = await page.locator('.admin-job-table tbody tr').allInnerTexts();
    check(reviewRows.length >= 10, 'Review queue includes quality failures and suspected duplicate jobs');
    check(!reviewRows.some(row => row.includes('QUALITY Second')), 'A public-gate eligible job is not placed in Needs Review');
    check(reviewRows.some(row => row.includes('QUALITY Xelentor A')) && reviewRows.some(row => row.includes('QUALITY Xelentor B')), 'Review queue catches duplicate roles when company names add a legal suffix');
    const reviewMetric = await page.locator('.metric').filter({ hasText: 'Needs review' }).first().innerText();
    check(Number(reviewMetric.match(/Needs review\s+(\d+)/)?.[1]) === reviewRows.length, 'Admin Needs Review metric matches the review queue, including duplicates');
    const reviewReasons = (await page.locator('.review-reasons').allInnerTexts()).join(' | ');
    check(reviewReasons.includes('supported job category') && reviewReasons.includes('source type') && reviewReasons.includes('generic or repeated') && reviewReasons.includes('single-job listing') && reviewReasons.includes('vacancy-specific application URL'), 'Needs Review shows category, source, generic copy, generic application URLs and multi-job reasons');
    check(reviewReasons.includes('qualification placeholder') && reviewReasons.includes('internal field references') && reviewReasons.includes('generic boilerplate'), 'Needs Review explains placeholder, internal-field and boilerplate problems');
    check(reviewReasons.includes('check whether the employer source states a posting date'), 'Needs Review prompts a factual source-date check');
    await page.goto(base + '/Admin?status=Expired');
    check((await page.locator('.review-reasons').allInnerTexts()).join(' ').includes('recruitment closing date has passed'), 'Expired review queue explains why the listing needs attention');
    await page.goto(base + '/Admin');
    const excludedMetric = await page.locator('.metric').filter({ hasText: 'Active jobs excluded from discovery' }).innerText();
    check(/Active jobs excluded from discovery\s+\d+/.test(excludedMetric), 'Admin dashboard reports active jobs held out of public discovery: ' + excludedMetric);
    await page.goto(base + '/resources');
    const resourceLinks = [...new Set(await page.locator('a[href^="/resources/"]').evaluateAll(links => links.map(link => link.getAttribute('href'))))];
    check(resourceLinks.length > 0, 'Career guides discoverable');
    for (const url of resourceLinks) check((await get(url)).status === 200 && sitemap.includes(url), 'Guide route and sitemap: ' + url);
    for (const width of [320, 390, 768, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const url of ['/', '/jobs/it-software', path('Second'), path('Duplicate'), '/resources', resourceLinks[0], '/Admin', '/Admin/Import']) {
        await page.goto(base + url);
        check(!await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), `No overflow: ${url} at ${width}`);
      }
    }
    console.log(JSON.stringify({ result: 'PASS', checks, note: 'Isolated database; fake ad IDs; all external browser requests blocked.' }));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
