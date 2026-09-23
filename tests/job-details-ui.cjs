const {chromium}=require('../artifacts/ui-check/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
const browser=await chromium.launch({channel:'msedge',headless:true});
try {
 const context=await browser.newContext({ignoreHTTPSErrors:true,permissions:['clipboard-read','clipboard-write']});
 const page=await context.newPage(); const errors=[]; page.on('pageerror',e=>errors.push(e.message));
 const base='https://localhost:7000'; const path='/job/credit-analyst-c-ic-mmg-s-ii-c4724d7c07';
 const response=await page.goto(base+path); assert.equal(response.status(),200);
 await page.getByRole('heading',{name:'At a glance'}).waitFor();
 assert.equal(await page.locator('script[src*="nap5k"]').count(),0);
 const copy=page.getByRole('button',{name:'Copy share text'});
 const text=await copy.getAttribute('data-copy-text');
 const target=new URL(text.match(/Apply here: (https:\/\/\S+)/)[1]);
 assert.equal(target.origin,'https://d2djobs.in'); assert.equal(target.pathname,'/job/resolve');
 const resolved=await context.request.get(base+target.pathname+target.search,{maxRedirects:0});
 assert.equal(resolved.status(),302); assert.equal(resolved.headers().location,path);
 assert.equal((await context.request.get(base+'/job/resolve?company=missing&title=missing')).status(),404);
 const apply=page.getByRole('link',{name:'Apply on company website'});
 // Relative action URLs must resolve against the tested app.
 const applyResponse=await context.request.get(new URL(await apply.getAttribute('href'),base).href,{maxRedirects:0});
 assert.equal(applyResponse.status(),302);assert.equal(applyResponse.headers().location,'https://ibpsreg.ibps.in/bonwejul26/');
 await copy.click(); assert.equal(await page.evaluate(()=>navigator.clipboard.readText()),text);
 for (const name of ['WhatsApp','Telegram']) {
  const link=page.locator('.share-links').getByRole('link',{name,exact:true});
  assert.equal(new URL(await link.getAttribute('href')).searchParams.get('text'),text);
 }
 for(const width of [320,390,768,1440]){
  await page.setViewportSize({width,height:1000});
  await page.evaluate(()=>window.scrollTo(0,0));
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'overflow at '+width);
  await page.screenshot({path:'artifacts/details-'+width+'.png',fullPage:true});
 }
 await page.getByRole('button',{name:'Toggle dark mode'}).click();
 assert.ok(await page.locator('body').evaluate(e=>e.classList.contains('dark-mode')));
 await page.screenshot({path:'artifacts/details-dark.png',fullPage:true,animations:'disabled'});
 assert.equal(await page.locator('.share-links>a').first().evaluate(e=>getComputedStyle(e).backgroundColor),'rgb(35, 55, 85)');
 await page.keyboard.press('Tab');
 assert.ok(await page.evaluate(()=>document.activeElement!==document.body));
 assert.deepEqual(errors,[]);
 console.log('PASS: resolver, missing job, application redirect, clipboard, WhatsApp/Telegram payloads, 4 viewport widths, dark mode, keyboard focus, no page errors.');
} finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
