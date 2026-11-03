const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url'),path=require('node:path'),assert=require('node:assert/strict');
const html=process.env.GRACE_PROGRESS_HTML||path.resolve(__dirname,'../../dist/AtGrace/startup.html');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true,args:['--mute-audio']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  // Snapshot iframes share storage; seed only the top-level test page.
  await page.addInitScript(()=>{if(window.top!==window)return;try{localStorage.setItem('grace.startup-pace.v1',JSON.stringify({durationMs:6000,samples:2}));}catch{}});
  await page.goto(pathToFileURL(html).href);
  await page.evaluate(()=>{
   window.progressChanges=[];
   new MutationObserver(()=>progressChanges.push({percent:parseInt(document.querySelector('#progress-number').textContent,10),opacity:Number(getComputedStyle(document.querySelector('#gate')).opacity)})).observe(document.querySelector('#progress-number'),{childList:true});
   GraceStartup.setProgress(85,1);
  });
  await page.waitForTimeout(650);
  assert.equal(await page.locator('#progress-number').textContent(),'10%','progress must not finish behind the invisible introduction');
  await page.waitForFunction(()=>parseInt(document.querySelector('#progress-number').textContent,10)>=25);
  const samples=await page.evaluate(()=>progressChanges);
  assert(samples.length>=10&&samples.every(s=>Number.isInteger(s.percent)),'live progress must pass through plain integer values');
  assert(samples.every((s,i)=>i===0||s.percent-samples[i-1].percent<=1),'waiting updates must not skip large blocks of percentages');
  assert(samples.every(s=>s.opacity>=.99),'percentage updates must begin after the gate is visible');
  await page.evaluate(()=>GraceStartup.setReady(true));
  assert(await page.locator('#enter').isEnabled(),'visual catch-up must not block actual ready entry');
  await page.waitForFunction(()=>document.querySelector('#progress-number').textContent==='100%');
  const history=await page.evaluate(()=>JSON.parse(localStorage.getItem('grace.startup-pace.v1')));
  assert.equal(history.samples,3,'real ready signal must update this device timing history once');
  const fastPage=await browser.newPage({viewport:{width:1280,height:720}});
  await fastPage.goto(pathToFileURL(html).href);await fastPage.evaluate(()=>GraceStartup.setReady(true));
  assert(await fastPage.locator('#enter').isEnabled(),'a fast machine can enter before the intro finishes');
  await fastPage.waitForFunction(()=>document.querySelector('#progress-number').textContent==='100%',null,{timeout:2500});
  assert.deepEqual(errors,[]);
  console.log(JSON.stringify({earlyHostProgress:85,firstDisplayedPercent:10,catchUp:'visible intermediate values',readyEntry:'immediate',errors}));
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
