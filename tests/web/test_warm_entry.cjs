const {chromium}=require('playwright');
const path=require('node:path'),assert=require('node:assert/strict'),fs=require('node:fs'),{pathToFileURL}=require('node:url');
(async()=>{
 const b=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true,args:['--mute-audio']});
 try{
  const p=await b.newPage({viewport:{width:1280,height:720}}),errors=[];p.on('pageerror',e=>errors.push(e.message));
  await p.goto(pathToFileURL(path.resolve(__dirname,'../../dist/AtGrace/startup.html')).href+'?capture=1');await p.evaluate(()=>preview.seek(7,{state:'ready'}));
  const warm=await p.evaluate(async()=>{const root=document.querySelector('#composition'),start=performance.now();window.warmed=await GraceVortex.warm(root);const firstMs=performance.now()-start,again=performance.now(),second=await GraceVortex.warm(root);return{firstMs,reuseMs:performance.now()-again,sameCanvas:warmed===second};});
  assert(warm.sameCanvas,'Entry must reuse its prepared scene');assert(warm.reuseMs<50);
  await p.evaluate(()=>{window.handoffs=0;addEventListener('grace-enter',()=>handoffs++);GraceStartup.enterFromKeyboard();});
  await p.waitForTimeout(400);
  assert(await p.evaluate(()=>warmed===document.querySelector('#vortex-canvas')),'Pressing Enter must not capture the scene again');
  assert(await p.locator('#title-block').evaluate(e=>Number(getComputedStyle(e).opacity)<.01));
  await p.waitForFunction(()=>handoffs===1,null,{timeout:8000});assert.equal(await p.locator('#vortex-canvas,#vortex-motes').count(),0);
  await p.evaluate(()=>GraceStartup.resetEntry(true));assert(await p.locator('#title-block').evaluate(e=>Number(getComputedStyle(e).opacity)>.99));
  await p.evaluate(async()=>{const pending=GraceVortex.warm(document.querySelector('#composition'));GraceVortex.hide();await pending;});
  assert.equal(await p.locator('#vortex-canvas,#vortex-motes').count(),0,'Cancelling preparation must not resurrect either canvas');assert.deepEqual(errors,[]);
  const result={warm,retiredText:true,handoff:'once',renderersReleased:true,cancelledWarmReleased:true,errors};fs.writeFileSync(path.join(require('./paths.cjs').artifacts,'warm-entry-verification.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await b.close();}
})().catch(e=>{console.error(e);process.exit(1)});
