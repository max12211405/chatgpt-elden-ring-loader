const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true});
 try {
  const page=await browser.newPage({viewport:{width:1000,height:600}});
  await page.addInitScript(()=>{window.hostMessages=[];window.chrome={webview:{addEventListener:()=>{},postMessage:m=>hostMessages.push(m)}}});
  const html=process.env.GRACE_PROGRESS_HTML||path.resolve(__dirname,'../../dist/AtGrace/startup.html');
  await page.goto(pathToFileURL(html).href+'?capture=1');
  await page.evaluate(()=>{GraceStartup.setRecovery(true);window.vortexCalls=0;GraceVortex.play=async()=>{vortexCalls++;return new Promise(()=>{})}});
  await page.locator('#open-direct').click();await page.waitForTimeout(120);
  assert.deepEqual(await page.evaluate(()=>hostMessages),[{type:'retry-readiness'}],'recovery must ask for a readiness check immediately, without waiting on graphics');
  assert.equal(await page.evaluate(()=>vortexCalls),0,'unready recovery must not start the entry effect');
  assert.equal(await page.evaluate(()=>preview.state),'waiting');
  assert.equal(await page.locator('#transition').evaluate(e=>e.classList.contains('visible')),false);
  await page.evaluate(()=>GraceStartup.setReady(true));
  assert(!(await page.locator('#open-direct').isVisible()),'confirmed ready must remove the recovery alternative');
  assert(await page.locator('#enter').isEnabled());
  console.log(JSON.stringify({unreadyRecovery:'recheck only',graphicsDependency:false,readyEntry:'normal Press Enter'}));
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
