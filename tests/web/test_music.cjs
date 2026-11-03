const {chromium}=require('playwright');
const {pathToFileURL}=require('node:url');const path=require('node:path');const fs=require('node:fs');const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true,args:['--autoplay-policy=no-user-gesture-required']});
 try{
 const page=await browser.newPage({viewport:{width:1000,height:600},reducedMotion:'reduce'});const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.resolve(__dirname,'../../dist/AtGrace/startup.html')).href+'?capture=1');
 assert.equal(await page.evaluate(()=>typeof window.GraceMusic?.configure),'function','Menu music lifecycle must be available');
 const source=pathToFileURL(require('./paths.cjs').testTone()).href;
 // A frame timestamp can precede the fade's performance.now() start.
 await page.evaluate(()=>{const original=requestAnimationFrame;let first=true;window.requestAnimationFrame=callback=>original(now=>{if(first){first=false;callback(performance.now()-10);}else callback(now);});});
 assert(await page.evaluate(src=>GraceMusic.configure({source:src,startSeconds:15,volume:.22,fadeInMs:60}),source),'Generated test tone must load and play');
 await page.waitForFunction(()=>GraceMusic.state.volume>.20);
 const initial=await page.evaluate(()=>GraceMusic.state);assert(initial.currentTime>=15&&initial.currentTime<17);assert(initial.duration>15);assert(!initial.paused);
 for(let i=1;i<=2;i++){
  await page.evaluate(()=>{const a=document.querySelector('#grace-menu-audio');a.currentTime=a.duration-.06;});
  await page.waitForFunction(expected=>GraceMusic.state.loops>=expected,i);
  const loop=await page.evaluate(()=>GraceMusic.state);assert(loop.currentTime>=15&&loop.currentTime<17,`Loop ${i} must restart at 15 seconds`);assert(!loop.paused);
 }
 await page.evaluate(()=>{GraceStartup.setReady(true);window.handoffs=0;window.silentAtHandoff=false;addEventListener('grace-enter',()=>{window.handoffs++;window.silentAtHandoff=GraceMusic.state.paused&&!GraceMusic.state.sourceAttached;});});
 await page.keyboard.press('Enter');await page.waitForTimeout(550);
 const fading=await page.evaluate(()=>GraceMusic.state);assert(fading.volume<.20,'Music must fade during entry');
 await page.waitForFunction(()=>handoffs===1,null,{timeout:8000});assert(await page.evaluate(()=>silentAtHandoff),'Music must be stopped and unloaded before chat handoff');
 await page.evaluate(()=>GraceStartup.resetEntry());await page.waitForFunction(()=>!GraceMusic.state.paused,null,{timeout:8000});
 const recovered=await page.evaluate(()=>GraceMusic.state);assert(recovered.currentTime>=15&&recovered.currentTime<17);
 await page.evaluate(()=>dispatchEvent(new Event('pagehide')));assert(await page.evaluate(()=>GraceMusic.state.paused&&!GraceMusic.state.sourceAttached));assert.deepEqual(errors,[]);
 const result={duration:initial.duration,startSeconds:15,loopRestarts:2,fade:'on entry',silentBeforeHandoff:true,recovery:'from 15 seconds',close:'stopped and unloaded',errors};
 fs.writeFileSync(path.join(require('./paths.cjs').artifacts,'music-verification.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
