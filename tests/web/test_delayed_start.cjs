const {chromium}=require('playwright');
const path=require('node:path'),{pathToFileURL}=require('node:url'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROME_PATH,headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.addInitScript(()=>{window.messages=[];window.chrome={webview:{postMessage:m=>messages.push(m),addEventListener:()=>{}}}});
  await page.goto(pathToFileURL(process.env.GRACE_PROGRESS_HTML||path.resolve(__dirname,'../../dist/AtGrace/startup.html')).href+'?capture=1');
  await page.evaluate(()=>{GraceStartup.setProgress(85,1);GraceStartup.setRecovery(true);window.vortexCalls=0;GraceVortex.play=async()=>{vortexCalls++}});
  assert.equal(await page.locator('#waiting-text').textContent(),'There may be a connection issue.');
  assert.match(await page.locator('#recovery-note').textContent(),/Check your network or proxy, then try again/);
  assert(!(await page.locator('#load-progress').isVisible()),'a delayed app must not look stuck at a near-complete percentage');
  assert.equal(await page.locator('#show-app').count(),0,'network warning must not offer an unready app handoff');
  assert.doesNotMatch(await page.locator('#recovery-note').textContent(),/Open ChatGPT/);
  assert.equal(await page.evaluate(()=>GraceStartup.enterFromKeyboard()),false,'delayed recovery must not report ready');
  await page.locator('#open-direct').click();
  assert.deepEqual(await page.evaluate(()=>messages),[{type:'retry-readiness'}]);
  assert.equal(await page.evaluate(()=>vortexCalls),0,'retry must only recheck readiness');
  assert.equal(await page.evaluate(()=>preview.state),'waiting');
  await page.evaluate(()=>GraceStartup.setReady(true));
  assert(!(await page.locator('#recovery-actions').isVisible()));assert(await page.locator('#load-progress').isVisible());
  assert.equal(await page.locator('#progress-number').textContent(),'100%');
  await page.evaluate(()=>{GraceStartup.setReady(false);GraceStartup.setRecovery(true)});
  assert.equal(await page.evaluate(()=>GraceStartup.enterFromKeyboard()),false);
  assert.equal(await page.evaluate(()=>vortexCalls),0,'an unready app must not run the ready-entry transition');
  assert.equal(await page.locator('#transition').evaluate(e=>e.classList.contains('visible')),false);
  assert.deepEqual(errors,[]);
  console.log(JSON.stringify({delayed:'large network warning; estimated percentage hidden',normalReady:'restored',recovery:'retry only; no unready app handoff',errors}));
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
