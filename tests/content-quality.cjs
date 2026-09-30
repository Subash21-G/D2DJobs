const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');

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
      ApplyLink: 'https://example.invalid/apply', OfficialSourceUrl: 'https://example.invalid/careers',
      LastVerifiedUtc: date(-1), IsActive: 'true', IsFeatured: 'false'
    });
    const create = async form => {
      const r = await context.request.post(base + '/Admin/Create', { form, maxRedirects: 0 });
      check(r.status() === 302, 'Create ' + form.Title);
    };
    for (const suffix of ['Original', 'Second', 'Third', 'Fourth', 'Fifth']) await create(job(suffix));
    await create({ ...job('Original'), Title: 'QUALITY Duplicate' });
    await create({ ...job('Thin'), ApplicationInstructions: '', EditorialNote: '' });
    await create({ ...job('Stale'), LastVerifiedUtc: date(-60) });
    await create({ ...job('Expired'), ExpiryDate: date(-2).slice(0, 10) });
    await create(job('Scheduled'));

    for (const title of ['Duplicate', 'Thin', 'Stale']) {
      await page.goto(base + '/Admin?search=' + encodeURIComponent('QUALITY ' + title));
      const edit = await page.locator('a[href^="/Admin/Edit/"]').first().getAttribute('href');
      await page.goto(base + edit);
      check(!await page.locator('[name="IsActive"][type="checkbox"]').isChecked(), title + ' held as draft');
    }
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
    check(hasAd(valid.html), 'Positive control: eligible page renders fake ad configuration');
    const home = await get('/');
    check(hasAd(home.html), 'Populated listing page renders fake ad configuration');
    const sitemap = (await get('/sitemap.xml')).html;
    const feed = (await get('/jobs/feed.xml')).html;
    for (const title of [...excluded, 'Scheduled']) {
      check(!home.html.includes(path(title)), title + ' excluded from discovery');
      check(!sitemap.includes(path(title)), title + ' excluded from sitemap');
      check(!feed.includes(path(title)), title + ' excluded from RSS');
    }
    check(sitemap.includes(path('Second')) && feed.includes(path('Second')), 'Eligible listing in sitemap and RSS');
    for (const url of ['/?search=absent-quality-fixture', '/Home/About', '/resources', '/definitely-missing']) {
      check(!hasAd((await get(url)).html), 'No ads on ' + url);
    }
    const readiness = (await get('/Readiness')).html;
    check(readiness.includes('8 active listing(s); 4 need substantial publishing details or fresh verification.'), 'Readiness counts match discoverable candidates');
    await page.goto(base + '/Admin?status=Needs%20review');
    check(await page.locator('.admin-job-table tbody tr').count() === 4, 'Review queue includes legacy duplicate, thin and stale jobs');
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
