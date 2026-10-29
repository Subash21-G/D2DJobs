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
      if (suffix === 'Second') listing.WalkInDate = dateOnly(7);
      if (suffix === 'Third') { listing.SourcePostedDate = ''; listing.SourceType = 'OfficialEmployer'; }
      if (suffix === 'Fourth') { listing.SourceType = 'GovernmentPortal'; listing.WalkInStartDate = dateOnly(7) + 'T09:00'; listing.WalkInEndDate = dateOnly(8) + 'T17:00'; }
      if (suffix === 'Fifth') { listing.SourceType = 'JobPortal'; listing.ApplyLink = 'https://example.invalid/?jobId=isolated-123'; }
      await create(listing);
    }
    await create({ ...job('Original'), Title: 'QUALITY Duplicate' });
    await create({ ...job('Thin'), ApplicationInstructions: '', EditorialNote: '', SourcePostedDate: '' });
    await create({ ...job('Stale'), LastVerifiedUtc: date(-60) });
    await create({ ...job('Unverified'), LastVerifiedUtc: '' });
    await create({ ...job('Senior'), Experience: '5+ years of professional development experience' });
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
    check(importHtml.includes('Accepted 2; rejected 0; imported 2 active listing(s)'), 'Import summary confirms rows are active');
    check(importHtml.includes('View imported jobs'), 'Import links to the job overview');
    check(importHtml.includes('Imported rows are active'), 'Import confirms automatic activation');
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
    check(await page.locator('[name="IsActive"][type="checkbox"]').isChecked(), 'Imported jobs are active without separate approval');
    await page.goto(base + '/Admin?search=' + encodeURIComponent('QUALITY Imported Date Test'));
    const importedSupportedEdit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
    await page.goto(base + importedSupportedEdit);
    check(await page.locator('[name="SourceType"]').inputValue() === 'OfficialEmployer', 'Missing Excel source type is detected from a company-matching source domain');
    check(await page.locator('[name="LastVerifiedUtc"]').inputValue() !== '', 'Admin upload records its publication check date');
    const rows = JSON.parse(execFileSync('powershell.exe', ['-NoProfile', '-File', 'tests/Set-QualityFixtures.ps1'], { encoding: 'utf8' }));
    const path = title => '/job/' + rows.find(row => row.title === 'QUALITY ' + title).slug;
    const get = async url => {
      const response = await context.request.get(base + url);
      return { status: response.status(), html: await response.text() };
    };
    const hasJobSchema = html => html.includes('"@type":"JobPosting"');
    const hasMonetag = html => html.includes('https://nap5k.com/tag.min.js') || html.includes('https://quge5.com/88/tag.min.js');
    const hasAd = html => html.includes('class="adsbygoogle"') || html.includes('pagead/js/adsbygoogle.js') || hasMonetag(html);
    const excluded = ['Original', 'Duplicate', 'Thin', 'Stale', 'Unverified', 'Senior', 'GenericApplyUrl', 'QualificationPlaceholder', 'InternalFields', 'BoilerplateDescription', 'TemplateEditorial', 'InvalidApplyUrl', 'InvalidAuthorityUrl', 'Xelentor A', 'Xelentor B', 'Expired'];
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
    check(valid.html.includes(displayDate(dateOnly(7))) && !valid.html.includes('12:00 AM IST'), 'Date-only walk-in values do not render a midnight time');
    check(!hasAd(valid.html), 'Job detail pages keep programmatic ads disabled');
    const queryApply = await get(path('Fifth'));
    check(queryApply.status === 200 && queryApply.html.includes('Continue to application source') && queryApply.html.includes('content="index, follow"'), 'Vacancy-specific query parameter application URL remains publishable');
    const queryRedirect = await context.request.get(base + '/Home/ApplyClick?id=' + rows.find(row => row.title === 'QUALITY Fifth').id, { maxRedirects: 0 });
    check(queryRedirect.status() === 302 && queryRedirect.headers().location === 'https://example.invalid/?jobId=isolated-123', 'Approved application redirect preserves its vacancy query parameter');
    const legacyApply = await context.request.get(base + '/Home/ApplyClick?id=' + rows.find(row => row.title === 'QUALITY Original').id, { maxRedirects: 0 });
    check(legacyApply.status() === 404, 'Direct ApplyClick request cannot bypass review and duplicate checks');
    const legacyDetails = await get(path('Original'));
    check(legacyDetails.html.includes('This listing is unavailable') && !legacyDetails.html.includes('class="primary-button" href="/Home/ApplyClick'), 'Legacy excluded detail page explains unavailability and has no Apply action');
    const placeholderPage = await get(path('QualificationPlaceholder'));
    check(!placeholderPage.html.includes('verify the full qualification requirements') && placeholderPage.html.includes('Not specified in the source posting'), 'Public historical page replaces qualification placeholder with a neutral factual message');
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
    check(home.html.includes('<script src="https://nap5k.com/tag.min.js" data-zone="00000000" async data-cfasync="false"></script>'), 'Monetag In-Page Push uses the supplied head tag and isolated test zone');
    check(home.html.indexOf('https://nap5k.com/tag.min.js') > home.html.indexOf('<head>') && home.html.indexOf('https://nap5k.com/tag.min.js') < home.html.indexOf('</head>'), 'Primary Monetag tag is in the document head');
    check(home.html.includes('src="https://quge5.com/88/tag.min.js" data-zone="00000001"'), 'Second supplied tag uses the isolated test zone on content pages');
    check(!home.html.includes('pagead/js/adsbygoogle.js') && !home.html.includes('class="adsbygoogle"'), 'Unverified second format cannot run beside Google ad units');
    const cleanJobs = (await get('/jobs')).html;
    check((cleanJobs.match(/https:\/\/nap5k\.com\/tag\.min\.js/g) || []).length === 1, 'Clean jobs URL emits primary Monetag tag once');
    check((cleanJobs.match(/https:\/\/quge5\.com\/88\/tag\.min\.js/g) || []).length === 1, 'Clean jobs URL emits secondary Monetag tag once');
    const monetagRequests = [];
    const monetagFailures = [];
    page.on('request', request => { if (/^(https:\/\/nap5k\.com\/tag\.min\.js|https:\/\/quge5\.com\/88\/tag\.min\.js)$/.test(request.url())) monetagRequests.push(request.url()); });
    page.on('requestfailed', request => { if (request.url().includes('/tag.min.js')) monetagFailures.push(request.url()); });
    await page.goto(base + '/jobs');
    await page.waitForTimeout(300);
    check(monetagRequests.includes('https://nap5k.com/tag.min.js') && monetagRequests.includes('https://quge5.com/88/tag.min.js'), 'Browser attempts both rendered Monetag requests');
    check(monetagFailures.includes('https://nap5k.com/tag.min.js') && monetagFailures.includes('https://quge5.com/88/tag.min.js'), 'Blocked external requests are observable as network failures');
    const worker = await get('/sw.js');
    check(worker.status === 200 && worker.html.includes('zoneId": 11867691'), 'Supplied Push Notifications service worker is hosted without registering it');
    check(!hasMonetag((await get('/jobs?search=absent-quality-fixture')).html), 'Monetag is absent from filtered results');
    const trackedJobs = (await get('/jobs?utm_source=chatgpt.com')).html;
    check((trackedJobs.match(/https:\/\/nap5k\.com\/tag\.min\.js/g) || []).length === 1 && (trackedJobs.match(/https:\/\/quge5\.com\/88\/tag\.min\.js/g) || []).length === 1, 'Tracking-only jobs URL emits each Monetag tag once');
    check(hasMonetag((await get('/jobs?gclid=test&fbclid=test')).html), 'Click attribution parameters retain Monetag tags');
    check(!hasMonetag((await get('/jobs?utm_source=chatgpt.com&search=absent-quality-fixture')).html), 'Tracking parameter does not enable Monetag on filtered results');
    check(!hasMonetag((await get('/jobs?page=2')).html), 'Monetag is absent from pagination URLs');
    check(!hasMonetag((await get('/jobs/internships?utm_source=chatgpt.com')).html), 'Tracking parameter does not bypass the five-job minimum');
    check(home.html.includes('content="index, follow"'), 'Homepage remains indexable when it contains curated jobs and editorial content');
    check(home.html.includes('Latest Fresher Jobs, Off-Campus Drives &amp; Internships in India | D2DJobs'), 'Homepage keeps its descriptive SEO title');
    const sitemap = (await get('/sitemap.xml')).html;
    const feed = (await get('/jobs/feed.xml')).html;
    const secondSitemapIndex = sitemap.indexOf(path('Second'));
    const secondSitemapBlock = sitemap.slice(sitemap.lastIndexOf('<url>', secondSitemapIndex), sitemap.indexOf('</url>', secondSitemapIndex));
    const sitemapUrls = [...sitemap.matchAll(/<loc>(.*?)<\/loc>/g)].map(match => new URL(match[1]));
    check(sitemapUrls.some(url => url.pathname === '/') && sitemapUrls.some(url => url.pathname === '/jobs') && !sitemapUrls.some(url => url.pathname === '/jobs/work-from-home'), 'Populated listings stay in the sitemap and empty categories are excluded');
    check((await get('/jobs/work-from-home')).html.includes('content="noindex, follow"'), 'Empty category page is noindexed');
    check(!valid.html.includes('APPLICATION & INTERVIEW GUIDE'), 'Job detail does not repeat a category article');
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
    for (const url of ['/?search=absent-quality-fixture', '/Home/About', '/Home/PrivacyPolicy', '/resources', '/definitely-missing']) {
      check(!hasAd((await get(url)).html), 'No ads on ' + url);
    }
    const readiness = (await get('/Readiness')).html;
    check(/\d+ active listing\(s\); \d+ would benefit from more complete source-specific detail\./.test(readiness), 'Readiness counts report active listings and incomplete source-specific details');
    await page.goto(base + '/Admin');
    const unavailableCard = page.locator('[data-metric-filter="application"]');
    check((await unavailableCard.innerText()).includes('Apply action unavailable'), 'Admin exposes the unavailable application-action card');
    await unavailableCard.click();
    await page.waitForURL('**/Admin?metric=application');
    const unavailableRows = await page.locator('.admin-job-table tbody tr').allInnerTexts();
    check(unavailableRows.length > 0, 'Unavailable application-action card filters to affected jobs');
    check(unavailableRows.some(row => row.includes('QUALITY Thin')) && !unavailableRows.some(row => row.includes('QUALITY Second')), 'Admin publication status and unavailable-action filter match the shared content gate');
    await page.goto(base + '/Admin');
    await page.locator('[data-metric-filter="duplicate"]').click();
    await page.waitForURL('**/Admin?metric=duplicate');
    const duplicateRows = await page.locator('.admin-job-table tbody tr').allInnerTexts();
    check(duplicateRows.some(row => row.includes('QUALITY Xelentor A')) && duplicateRows.some(row => row.includes('QUALITY Xelentor B')), 'Duplicate card shows likely duplicate roles with company suffix variations');
    await page.goto(base + '/resources');
    const resourceLinks = [...new Set(await page.locator('a[href^="/resources/"]').evaluateAll(links => links.map(link => link.getAttribute('href'))))];
    check(resourceLinks.length > 0, 'Career guides discoverable');
    for (const url of resourceLinks) check((await get(url)).status === 200 && sitemap.includes(url), 'Guide route and sitemap: ' + url);
    check(hasMonetag((await get(resourceLinks[0])).html), 'Monetag loads on a substantive career guide');
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
