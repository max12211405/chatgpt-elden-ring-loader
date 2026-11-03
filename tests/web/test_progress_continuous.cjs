const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const source=fs.readFileSync(path.resolve(__dirname,'../../src/web/progress.js'),'utf8');
function fixture(historyMs){
 let now=0,id=0;const frames=new Map(),attrs={},elements={},data=new Map();
 if(historyMs)data.set('grace.startup-pace.v1',JSON.stringify({durationMs:historyMs,samples:3}));
 const storage={getItem:k=>data.get(k)||null,setItem:(k,v)=>data.set(k,v)};
 const root={classList:{toggle:()=>{}},querySelector:s=>elements[s]||(elements[s]={style:{},textContent:''}),setAttribute:(k,v)=>attrs[k]=String(v)};
 const context={window:{localStorage:storage},performance:{now:()=>now},requestAnimationFrame:fn=>{frames.set(++id,fn);return id},cancelAnimationFrame:id=>frames.delete(id)};
 vm.runInNewContext(source,context);const meter=context.window.GraceProgress.create(root,false,{remember:true});
 function step(ms){now+=ms;const callbacks=[...frames.values()];frames.clear();callbacks.forEach(fn=>fn(now))}
 function advance(ms,frameMs=16){for(let left=ms;left>0;){const dt=Math.min(frameMs,left);step(dt);left-=dt}}
 return {meter,step,advance,attrs,data,frames,value:()=>Number(attrs['aria-valuenow'])};
}
const f=fixture();f.meter.pause();f.meter.stage(85,1);f.advance(4800);assert.equal(f.value(),10);
f.meter.resume(true);f.advance(16);const beforeGap=f.value();f.step(1500);
assert(f.value()-beforeGap<=1,'a stalled render frame must not jump straight to the milestone');
f.advance(5000);const at5=f.value();f.advance(10000);const at15=f.value();f.advance(15000);const at30=f.value();f.advance(15000);const at45=f.value();
assert(at5>10&&at5<70,'early 85% signal must unfold gradually rather than complete in one second');
assert(at15>at5+5&&at30>at15+1&&at45>at30+.1,'long waits must retain continuous estimated movement: '+JSON.stringify({at5,at15,at30,at45}));
assert(at45<98,'unconfirmed loading must stay below its final ceiling');
const beforeUpdate=f.value();f.meter.stage(95,1);assert.equal(f.value(),beforeUpdate,'a new stage must not snap the displayed value');
f.meter.pause();assert.equal(f.frames.size,0,'hidden/entry state must cancel its animation');f.meter.resume();
f.meter.ready(true);f.advance(1800);assert.equal(f.value(),100);assert.equal(f.frames.size,0,'completed loading must stop frame work');
const history=JSON.parse(f.data.get('grace.startup-pace.v1'));assert(history.durationMs>10000&&history.samples===1,'actual completion must calibrate local pace');
const fast=fixture(6000),slow=fixture(60000);for(const x of [fast,slow]){x.meter.stage(85,1);x.advance(8000)}
assert(fast.value()>slow.value()+15,'per-device launch history must change the estimated pace');
const g=fixture();g.meter.stage(60,2);g.advance(4000);const current=g.value();g.meter.stage(85,1);g.advance(1000);assert(g.value()<83,'stale window generation must not raise the ceiling');
g.meter.ready(true);g.advance(1800);g.meter.ready(false);g.meter.stage(30,3);assert.equal(g.value(),10,'replacement after readiness resets presentation');
const lowFps=fixture(60000);lowFps.meter.stage(85,1);lowFps.advance(12000,1000/15);assert(lowFps.value()>10&&lowFps.value()<60,'low-frame-rate slow machine must keep moving gently');
lowFps.meter.pause();assert.equal(lowFps.frames.size,0);
console.log(JSON.stringify({longWait:{at5,at15,at30,at45},machinePace:{fast:fast.value(),slow:slow.value()},stalledFrameJump:'bounded',readyOnly100:true,completedFrames:0}));
