const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url'),path=require('node:path'),assert=require('node:assert/strict');
const html=process.env.GRACE_PROGRESS_HTML||path.resolve(__dirname,'../../dist/AtGrace/startup.html');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true,args:['--mute-audio']});
 try {
  const page=await browser.newPage({viewport:{width:1600,height:900}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  // Only the host bridge is substituted; the installed HTML and progress runtime are real.
  await page.addInitScript(()=>{window.hostMessages=[];window.chrome={webview:{addEventListener:(type,fn)=>{if(type==='message')hostMessages.push(fn)},postMessage:()=>{}}};window.sendHost=data=>hostMessages.forEach(fn=>fn({data}));});
  // Capture mode keeps host-stage assertions deterministic; live pacing has its own test.
  await page.goto(pathToFileURL(html).href+'?capture=1');await page.evaluate(()=>document.fonts.ready);
  assert.equal(await page.locator('#load-progress').count(),1,'Waiting screen must include a progress bar');
  const meter=page.locator('#load-progress'),number=page.locator('#progress-number');
  assert.match(await number.textContent(),/^\d+%$/,'percentage must remain plain integers');
  assert(await meter.isVisible());assert.equal(await meter.getAttribute('aria-valuenow'),'10');
  await page.evaluate(()=>sendHost({type:'ready',ready:false,progress:60,revision:1}));
  await page.waitForTimeout(80);
  // Headless repaint scheduling can coalesce intermediate frames; the clock test checks easing.
  const middle=parseInt(await number.textContent(),10);assert(middle>=10&&middle<=60,'percentage must stay between reported milestones');
  await page.waitForFunction(()=>document.querySelector('#progress-number').textContent==='60%');
  await page.evaluate(()=>sendHost({type:'ready',ready:false,progress:85,revision:1}));
  await page.waitForFunction(()=>document.querySelector('#progress-number').textContent==='85%');
  await page.waitForTimeout(1400);assert.equal(await number.textContent(),'85%','elapsed time must not invent completed work');
  assert.equal(await page.evaluate(()=>GraceStartup.enterFromKeyboard()),false,'progress must not bypass readiness');
  await page.evaluate(()=>{GraceStartup.setProgress(100,1);GraceStartup.setProgress(NaN,1);GraceStartup.setProgress(-3,1)});
  await page.waitForTimeout(1300);assert(parseInt(await number.textContent(),10)<100,'100% is reserved for confirmed readiness');
  await page.evaluate(()=>sendHost({type:'ready',ready:true,progress:100,revision:1}));
  await page.waitForFunction(()=>document.querySelector('#progress-number').textContent==='100%');
  assert(await page.locator('#enter').isEnabled());assert.equal(await page.evaluate(()=>preview.state),'ready');
  await page.evaluate(()=>sendHost({type:'ready',ready:false,progress:30,revision:2}));
  await page.waitForFunction(()=>document.querySelector('#progress-number').textContent==='30%');
  assert(await page.locator('#enter').isDisabled(),'replacement target must revoke entry and reset percentage');
  await page.evaluate(()=>{GraceStartup.setProgress(85,1);GraceStartup.setProgress(10,2)});
  await page.waitForTimeout(1000);assert.equal(await number.textContent(),'30%','late window generations and regressive samples must be ignored');
  const track=await page.locator('.progress-track').evaluate(e=>({height:e.getBoundingClientRect().height}));assert(track.height>=2&&track.height<=3,'track should remain a fine visible line');
  const digits=await number.evaluate(el=>{const style=getComputedStyle(el),ctx=document.createElement('canvas').getContext('2d');ctx.font=`${style.fontSize} ${style.fontFamily}`;return {variant:style.fontVariantNumeric,widths:[...'0123456789'].map(d=>ctx.measureText(d).width)};});
  assert(digits.variant.includes('lining-nums')&&digits.variant.includes('tabular-nums'),'percent must use even-height tabular numerals');
  assert(Math.max(...digits.widths)-Math.min(...digits.widths)<.01,'all ten digits must have equal advance width');
  assert.deepEqual(errors,[]);
  const reduced=await browser.newPage({viewport:{width:1000,height:600},reducedMotion:'reduce'});
  await reduced.goto(pathToFileURL(html).href+'?capture=1');await reduced.evaluate(()=>GraceStartup.setProgress(60,1));
  assert.equal(await reduced.locator('#progress-number').textContent(),'60%','reduced motion must update immediately');
  console.log(JSON.stringify({progress:'native host bridge; deterministic captured stages',safety:'ready-only completion; stale-generation rejection; Enter guard',style:'2px track; uniform integer digits',errors}));
 } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exit(1)});
