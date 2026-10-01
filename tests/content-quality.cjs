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
    const job = suffix => ({
      __RequestVerificationToken: token, Title: 'QUALITY ' + suffix, CompanyName: 'Isolated Test Employer',
      Category: 'IT Jobs', Role: 'Graduate software engineer', Location: 'Chennai', Qualification: 'Bachelor of Engineering in Computer Science',
      Skills: 'C#, SQL, debugging and automated testing', Experience: 'Fresher', JobType: 'Full time', Salary: 'Not disclosed',
      Description: 'Isolated regression fixture, not a real vacancy. The graduate engineer works on a small web application, investigates reproducible defects, writes tests for changed behavior and explains implementation decisions during a supervised code review.',
      Eligibility: 'Computer science engineering graduates who can demonstrate a working web project and explain their own contribution.',
      SelectionProcess: 'A code exercise followed by a discussion of the submitted solution with an engineer.',
      ApplicationInstructions: `For test opening ${suffix}, use the employer careers link, select the graduate engineering vacancy, attach the requested resume and retain the portal confirmation.`,
      EditorialNote: `Preparation for test opening ${suffix}: choose one project and be ready to explain a defect you diagnosed, the evidence you collected and how you verified the correction.`,
      SourceType: 'OfficialEmployer', SourcePostedDate: date(-5).slice(0, 10),
      ApplyLink: 'https://example.invalid/apply', OfficialSourceUrl: 'https://example.invalid/careers',
      LastVerifiedUtc: date(-1), IsActive: 'true', IsFeatured: 'false'
    });
    const create = async form => {
      const r = await context.request.post(base + '/Admin/Create', { form, maxRedirects: 0 });
      check(r.status() === 302, 'Create ' + form.Title);
    };
    for (const suffix of ['Original', 'Second', 'Third', 'Fourth', 'Fifth']) {
      const listing = job(suffix);
      if (suffix === 'Third') { listing.SourcePostedDate = ''; listing.SourceType = 'Other'; }
      if (suffix === 'Fourth') { listing.SourceType = 'GovernmentPortal'; listing.WalkInStartDate = '2026-10-05T09:00'; listing.WalkInEndDate = '2026-10-06T17:00'; }
      if (suffix === 'Fifth') listing.SourceType = 'JobPortal';
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
    check(importedResponse.status() === 302, 'Excel import accepts test workbook');
    await create({ ...job('UnsupportedCategory'), Category: 'Company Wise Jobs' });
    await create({ ...job('MissingSourceType'), SourceType: '' });
    await create({ ...job('Generic'), ApplicationInstructions: 'Eligible candidates can apply online through the official website. For this particular test vacancy, select its role, upload the required resume and retain the portal confirmation number.' });
    await create({ ...job('MultiJob'), Title: 'Top 10 Fresher Jobs and Companies' });

    for (const title of ['Duplicate', 'Thin', 'Stale', 'UnsupportedCategory', 'MissingSourceType', 'Generic', 'MultiJob']) {
      await page.goto(base + '/Admin?search=' + encodeURIComponent('QUALITY ' + title));
      const edit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
      await page.goto(base + edit);
      check(!await page.locator('[name="IsActive"][type="checkbox"]').isChecked(), title + ' held as draft');
    }
    await page.goto(base + '/Admin?search=QUALITY%20Imported%20Unsupported');
    const importedUnsupportedEdit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
    await page.goto(base + importedUnsupportedEdit);
    check(await page.locator('[name="IsActive"][type="checkbox"]').isChecked(), 'Every valid Excel row is activated automatically');
    const rows = JSON.parse(execFileSync('powershell.exe', ['-NoProfile', '-File', 'tests/Set-QualityFixtures.ps1'], { encoding: 'utf8' }));
    const path = title => '/job/' + rows.find(row => row.title === 'QUALITY ' + title).slug;
    const get = async url => {
      const response = await context.request.get(base + url);
      return { status: response.status(), html: await response.text() };
    };
    const hasJobSchema = html => html.includes('"@type":"JobPosting"');
    const hasAd = html => html.includes('class="adsbygoogle"') || html.includes('pagead/js/adsbygoogle.js');
    const excluded = ['Original', 'Duplicate', 'Thin', 'Stale', 'Expired'];
    for (const title of excluded) {
      const { status, html } = await get(path(title));
      check(status === (title === 'Expired' ? 410 : 200), title + ' response');
      check(html.includes('content="noindex, follow"'), title + ' noindex');
      check(!hasJobSchema(html), title + ' no job schema');
      check(!hasAd(html), title + ' no ad slot or loader');
    }
    check((await get(path('Scheduled'))).status === 404, 'Scheduled listing stays private');
    const valid = await get(path('Second'));
    check(valid.status === 200 && valid.html.includes('content="index, follow"'), 'Complete unique listing indexable');
    check(hasJobSchema(valid.html), 'Complete unique listing has job schema');
    check(valid.html.includes(`"datePosted":"${job('Second').SourcePostedDate}"`), 'Job schema uses the factual source posting date');
    check(valid.html.includes('View official employer posting') && valid.html.includes('Apply on company website'), 'Official employer links are labelled accurately');
    check(hasAd(valid.html), 'Positive control: eligible page renders fake ad configuration');
    const noSourceDate = await get(path('Third'));
    check(noSourceDate.html.includes('Added to D2DJobs') && !noSourceDate.html.includes('"datePosted"'), 'Missing employer posting date is not fabricated in page/schema');
    const government = await get(path('Fourth'));
    check(government.html.includes('View official recruitment notice') && government.html.includes('Apply through official recruitment portal'), 'Government source links use accurate labels');
    check(government.html.includes('05 Oct 2026') && government.html.includes('06 Oct 2026'), 'Walk-in date range renders both dates');
    const jobPortal = await get(path('Fifth'));
    check(jobPortal.html.includes('View source posting') && jobPortal.html.includes('Continue to application source'), 'Third-party job portal is not labelled as employer source');
    const importedDateTest = await get(path('Imported Date Test'));
    check(importedDateTest.status === 200 && importedDateTest.html.includes('content="index, follow"'), 'Supported imported category alias publishes as a canonical category');
    check(importedDateTest.html.includes('Internship Programs'), 'Import normalizes the supported category alias');
    const importedDateLabels = importedDateTest.html.match(/(?:Posted|Added to D2DJobs)[^<]*/g) || [];
    check(/25 Sep(?:t)? 2026/.test(importedDateTest.html), 'Import keeps the source PostedDate: ' + importedDateLabels.join(' | '));
    check(importedDateTest.html.includes('"datePosted":"2026-09-25"'), 'Imported source PostedDate is used for JobPosting datePosted');
    const home = await get('/');
    check(['Original', 'Duplicate', 'Thin', 'Stale'].every(title => home.html.includes(path(title))), 'Active legacy jobs remain visible on the homepage');
    check(!hasAd(home.html), 'Homepage containing unreviewed listings stays ad-free');
    check(home.html.includes('content="noindex, follow"'), 'Homepage stays noindex while any displayed active listing needs review');
    const sitemap = (await get('/sitemap.xml')).html;
    const feed = (await get('/jobs/feed.xml')).html;
    for (const title of [...excluded, 'Scheduled']) {
      check(!sitemap.includes(path(title)), title + ' excluded from sitemap');
      check(!feed.includes(path(title)), title + ' excluded from RSS');
    }
    check(!home.html.includes(path('Expired')) && !home.html.includes(path('Scheduled')), 'Expired and future listings stay out of the homepage');
    for (const title of ['Original', 'Duplicate', 'Thin', 'Stale'])
      check(!sitemap.includes(path(title)) && !feed.includes(path(title)), title + ' remains out of SEO discovery until reviewed');
    check(sitemap.includes(path('Second')) && feed.includes(path('Second')) && sitemap.includes(path('Imported Date Test')) && feed.includes(path('Imported Date Test')), 'Eligible listings in sitemap and RSS');
    const importedUnsupportedPath = '/job/' + rows.find(row => row.title === 'QUALITY Imported Unsupported').slug;
    check(home.html.includes(importedUnsupportedPath) && !sitemap.includes(importedUnsupportedPath) && !feed.includes(importedUnsupportedPath), 'Imported job is visible to viewers while incomplete metadata stays out of SEO feeds');
    for (const url of ['/?search=absent-quality-fixture', '/Home/About', '/resources', '/definitely-missing']) {
      check(!hasAd((await get(url)).html), 'No ads on ' + url);
    }
    const readiness = (await get('/Readiness')).html;
    check(readiness.includes('10 active listing(s); 5 need substantial publishing details.'), 'Readiness counts match active listings');
    await page.goto(base + '/Admin?status=Needs%20review');
    check(await page.locator('.admin-job-table tbody tr').count() === 9, 'Review queue includes legacy and newly held quality cases');
    const reviewReasons = (await page.locator('.review-reasons').allInnerTexts()).join(' | ');
    check(reviewReasons.includes('supported job category') && reviewReasons.includes('source type') && reviewReasons.includes('generic or repeated') && reviewReasons.includes('single-job listing'), 'Needs Review shows category, source type, generic text and multi-job reasons');
    check(reviewReasons.includes('check whether the employer source states a posting date'), 'Needs Review prompts a factual source-date check');
    await page.goto(base + '/resources');
    const resourceLinks = [...new Set(await page.locator('a[href^="/resources/"]').evaluateAll(links => links.map(link => link.getAttribute('href'))))];
    check(resourceLinks.length > 0, 'Career guides discoverable');
    for (const url of resourceLinks) check((await get(url)).status === 200 && sitemap.includes(url), 'Guide route and sitemap: ' + url);
    for (const width of [320, 390, 768, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const url of [path('Second'), path('Duplicate'), '/resources', resourceLinks[0]]) {
        await page.goto(base + url);
        check(!await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), `No overflow: ${url} at ${width}`);
      }
    }
    console.log(JSON.stringify({ result: 'PASS', checks, note: 'Isolated database; fake ad IDs; all external browser requests blocked.' }));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
