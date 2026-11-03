const {chromium}=require('playwright');
const sharp=require('sharp');
const {pathToFileURL}=require('node:url');const path=require('node:path');const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({...(process.env.CHROME_PATH?{executablePath:process.env.CHROME_PATH}:{}),headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:720}});const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{window.chrome=window.chrome||{};window.chrome.webview={postMessage:()=>{},addEventListener:(kind,callback)=>{window.nativeMessage=callback;}};});
 await page.goto(pathToFileURL(path.resolve(__dirname,'../../dist/AtGrace/startup.html')).href+'?capture=1');
 await page.evaluate(()=>preview.seek(7,{state:'ready'}));
 await page.evaluate(async()=>{await GraceVortex.prepare(document.querySelector('#composition'));GraceVortex.draw(1);});
 const endpoint=await sharp(await page.screenshot()).stats();
 assert(endpoint.channels.slice(0,3).every(c=>Math.abs(c.mean-24)<1&&c.stdev<1),'Last frame must match the native charcoal bridge, not cut to pure black');
 await page.evaluate(()=>GraceVortex.draw(.45));
 const middle=await sharp(await page.screenshot({path:path.join(require('./paths.cjs').artifacts,'soft-entry-middle.png')})).stats();
 assert(middle.channels.slice(0,3).some(c=>c.stdev>8),'The gentler vortex must retain the scene while fading');
 for(const time of [.12,.35,.62,.86,1]){
  await page.evaluate(t=>GraceVortex.draw(t),time);await page.screenshot({path:path.join(require('./paths.cjs').artifacts,`soft-entry-${time}.png`)});
 }
 await page.evaluate(()=>{GraceVortex.hide();document.querySelector('#transition').classList.add('visible');document.querySelector('#transition').style.opacity='1';});
 assert(!await page.locator('#transition p').isVisible(),'Production handoff must not flash an intermediate title card');
 assert.equal(await page.locator('#transition').evaluate(e=>getComputedStyle(e).backgroundColor),'rgb(24, 24, 24)');
 await page.evaluate(()=>window.nativeMessage({data:{type:'reset-entry',ready:true}}));
 assert.equal(await page.evaluate(()=>preview.state),'ready','A failed handoff with a still-ready chat must restore the normal entry control');
 assert(await page.locator('#enter').isEnabled());
 assert.deepEqual(errors,[]);console.log(JSON.stringify({endpoint:'uniform #181818',middle:'scene retained',titleFlash:false,errors}));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
