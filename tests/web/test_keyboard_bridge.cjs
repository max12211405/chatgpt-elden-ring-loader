const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url');const path=require('node:path');const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1000,height:600},reducedMotion:'reduce'});
 await page.goto(pathToFileURL(path.resolve(__dirname,'../../dist/AtGrace/startup.html')).href+'?capture=1');
 assert.equal(await page.evaluate(()=>typeof GraceStartup.enterFromKeyboard),'function','Native window Enter must have a direct shared entry API');
 assert.equal(await page.evaluate(()=>GraceStartup.enterFromKeyboard()),false,'Waiting must still ignore native Enter');
 await page.evaluate(()=>{GraceStartup.setReady(true);window.handoffs=0;addEventListener('grace-enter',()=>window.handoffs++);});
 assert.equal(await page.evaluate(()=>GraceStartup.enterFromKeyboard()),true);
 assert.equal(await page.locator('#enter .enter-text').textContent(),'Entering…','Input must acknowledge immediately, before the transition');
 assert(await page.locator('#enter').isDisabled());
 assert.equal(await page.evaluate(()=>GraceStartup.enterFromKeyboard()),false,'Repeated Enter must not launch another transition');
 await page.waitForFunction(()=>handoffs===1,null,{timeout:6000});
 await page.evaluate(()=>GraceStartup.resetEntry(true));
 assert.equal(await page.locator('#enter .enter-text').textContent(),'Continue journey');assert(await page.locator('#enter').isEnabled());
 console.log(JSON.stringify({nativeEnter:'shared action',acknowledgement:'immediate',repeatedEnter:'ignored',retry:'restored'}));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
