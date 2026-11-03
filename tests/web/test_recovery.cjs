const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url');const path=require('node:path');const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true});
 const page=await browser.newPage({viewport:{width:1000,height:600},reducedMotion:'reduce'});
 await page.goto(pathToFileURL(path.resolve(__dirname,'../../dist/AtGrace/startup.html')).href+'?capture=1');
 await page.evaluate(()=>{window.events=0;addEventListener('grace-enter',()=>window.events++);GraceStartup.setReady(true);GraceVortex.play=async()=>{throw Error('simulated graphics failure');};});
 const started=Date.now();await page.keyboard.press('Enter');await page.waitForTimeout(1100);assert.equal(await page.evaluate(()=>events),0,'failed graphics must not hand off early');
 await page.waitForFunction(()=>events===1);assert(Date.now()-started>=2300);
 await page.evaluate(()=>GraceStartup.resetEntry());
 assert.equal(await page.evaluate(()=>preview.state),'waiting');assert.equal(await page.locator('#transition').evaluate(e=>e.classList.contains('visible')),false);
 await page.evaluate(()=>GraceStartup.setRecovery(true));assert(await page.locator('#open-direct').isVisible());
 console.log(JSON.stringify({fallback:'2.4-second fade',failedHandoff:'returns to waiting',recovery:'available'}));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
