const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url'),path=require('node:path'),assert=require('node:assert/strict');
const html=process.env.GRACE_PROGRESS_HTML||path.resolve(__dirname,'../../dist/AtGrace/startup.html');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true,args:['--mute-audio']});
 try{
  for(const fallback of [false,true]){
   const page=await browser.newPage({viewport:{width:1000,height:600}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
   await page.goto(pathToFileURL(html).href+'?capture=1');
   await page.evaluate(fallback=>{
    window.handoffs=0;addEventListener('grace-enter',()=>handoffs++);GraceStartup.setReady(true);
    GraceVortex.play=fallback?async()=>{throw Error('simulated graphics failure')}:()=>new Promise(resolve=>setTimeout(resolve,600));
    GraceStartup.enterFromKeyboard();
   },fallback);
   await page.waitForTimeout(120);await page.evaluate(()=>GraceStartup.setReady(false));await page.waitForTimeout(2700);
   assert.equal(await page.evaluate(()=>handoffs),0,'losing readiness during entry must cancel handoff');
   assert.equal(await page.evaluate(()=>preview.state),'waiting');
   assert.equal(await page.locator('#transition').evaluate(e=>e.classList.contains('visible')),false,'lost target must restore the scene instead of leaving a black cover');
   await page.evaluate(()=>GraceStartup.setReady(true));assert(await page.locator('#enter').isEnabled(),'retry must be possible after cancellation');
   assert.deepEqual(errors,[],'cancelling the fallback animation must not cause an unhandled rejection');await page.close();
  }
  console.log(JSON.stringify({lostReadiness:'cancels normal and fallback entry',handoffCount:0,retry:'enabled',unhandledErrors:0}));
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
