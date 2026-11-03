const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1000,height:600}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
  const html=process.env.GRACE_PROGRESS_HTML||path.resolve(__dirname,'../../dist/AtGrace/startup.html');
  await page.goto(pathToFileURL(html).href+'?capture=1');await page.evaluate(()=>document.fonts.ready);
  await page.evaluate(()=>{
   const real=html2canvas;let calls=0;
   window.html2canvas=(...args)=>++calls===1?new Promise(resolve=>{window.releaseSnapshot=()=>real(...args).then(resolve)}):real(...args);
   window.oldWarm=GraceVortex.warm(document.querySelector('#composition'));
  });
  await page.waitForFunction(()=>typeof releaseSnapshot==='function');
  const result=await page.evaluate(async()=>{
   GraceVortex.hide();const fresh=GraceVortex.warm(document.querySelector('#composition'));releaseSnapshot();
   const renderer=await fresh,old=await oldWarm;
   return {oldCancelled:old===null,freshPrepared:!!renderer,canvasCount:document.querySelectorAll('#vortex-canvas,#vortex-motes').length};
  });
  assert(result.oldCancelled,'original cancelled request must stay cancelled');
  assert(result.freshPrepared,'a new warm request after cancellation must build a new renderer');assert.equal(result.canvasCount,2);
  await page.evaluate(()=>GraceVortex.hide());assert.equal(await page.locator('#vortex-canvas,#vortex-motes').count(),0);assert.deepEqual(errors,[]);
  console.log(JSON.stringify({...result,finalCanvases:0,errors}));
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
