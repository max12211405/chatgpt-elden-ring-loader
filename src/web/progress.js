/* Continuous estimated progress. Only the host's confirmed editor can complete it. */
window.GraceProgress = {
 create(root, instant, options={}) {
  const fill=root.querySelector('.progress-fill'),number=root.querySelector('#progress-number'),label=root.querySelector('#progress-label');
  const historyKey='grace.startup-pace.v1',clamp=(n,a,b)=>Math.max(a,Math.min(b,n));
  let value=10,observed=10,ceiling=28,generation=0,confirmed=false,frame=0,suspended=false,last=-1,velocity=0,lastFrame=0;
  let startedAt=performance.now(),revealedAt=startedAt,expectedMs=24000,samples=0,recorded=false,completion=null,storage=null;
  if(options.remember)try{
   storage=window.localStorage;
   const history=JSON.parse(storage.getItem(historyKey));
   if(Number.isFinite(history?.durationMs)&&history.durationMs>=4000&&history.durationMs<=120000){expectedMs=history.durationMs;samples=Number.isInteger(history.samples)?clamp(history.samples,1,100):1;}
  }catch{}
  function phaseCeiling(stage){return stage<30?28:stage<60?58:stage<85?83:97.8;}
  function paint(){
   const percent=confirmed&&value>=100?100:Math.min(confirmed?99:97,Math.floor(value+1e-7));
   fill.style.transform=`scaleX(${clamp(value/100,0,1).toFixed(6)})`;
   if(percent!==last){number.textContent=`${percent}%`;root.setAttribute('aria-valuenow',percent);root.setAttribute('aria-valuetext',percent===100?'Ready':`${percent}% — estimated`);last=percent;}
   const text=confirmed?'Ready':'Estimated loading';if(label.textContent!==text)label.textContent=text;
  }
  function stop(){if(frame)cancelAnimationFrame(frame);frame=0;lastFrame=0;}
  function remember(){
   if(recorded)return;recorded=true;
   const elapsed=performance.now()-startedAt;
   if(!storage||elapsed<1000)return;
   const actual=clamp(elapsed,4000,120000);
   expectedMs=samples?expectedMs*.65+actual*.35:actual;samples=Math.min(100,samples+1);
   try{storage.setItem(historyKey,JSON.stringify({durationMs:Math.round(expectedMs),samples}));}catch{}
  }
  function tick(now){
   frame=0;if(suspended)return;
   // A busy machine may miss a frame. Advance a small step, never repay the whole gap.
   const dt=clamp((now-lastFrame)/1000,0,.05);lastFrame=now;
   if(confirmed){
    completion.elapsed+=dt;
    const t=Math.min(1,completion.elapsed/completion.duration),m=completion.tangent;
    const eased=(m-2)*t*t*t+(3-2*m)*t*t+m*t;
    value=completion.from+(100-completion.from)*eased;
    if(t===1)value=100;
   }else{
    const visibleBudget=Math.max(6000,expectedMs-(revealedAt-startedAt));
    const budget=Math.max(visibleBudget,(now-revealedAt)*.7);
    const remaining=Math.max(0,ceiling-value),rate=clamp(90000/budget,1.2,12);
    const wanted=rate*remaining/(remaining+24);
    velocity+=(wanted-velocity)*(1-Math.exp(-dt/.35));
    value=Math.min(ceiling,value+Math.max(0,velocity)*dt);
    if(ceiling-value<.001){value=ceiling;velocity=0;}
   }
   paint();
   if(confirmed?value<100:value<ceiling)frame=requestAnimationFrame(tick);
  }
  function move(){
   if(instant||suspended||frame||(confirmed?value>=100:value>=ceiling))return;
   lastFrame=performance.now();frame=requestAnimationFrame(tick);
  }
  paint();
  return {
   stage(percent,revision=generation){
    if(!Number.isFinite(percent)||percent<0||!Number.isInteger(revision)||revision<generation||confirmed)return;
    const next=clamp(percent,0,95);
    if(revision>generation){
     if(generation>0&&next<observed){value=Math.min(value,next);velocity=0;}
     generation=revision;observed=next;ceiling=phaseCeiling(next);
    }else if(next>observed){observed=next;ceiling=phaseCeiling(next);}
    if(instant){value=observed;paint();}else move();
   },
   ready(ready){
    if(confirmed===ready)return;
    confirmed=ready;stop();
    if(ready){
     remember();
     const duration=clamp(.45+(100-value)*.01,.45,1.2);
     completion={from:value,elapsed:0,duration,tangent:clamp(velocity*duration/Math.max(.001,100-value),0,1)};
     if(instant)value=100;
    }else{value=observed=10;ceiling=28;velocity=0;completion=null;}
    paint();move();
   },
   reset(){stop();confirmed=false;value=observed=10;ceiling=28;generation=0;suspended=false;velocity=0;completion=null;recorded=false;startedAt=revealedAt=performance.now();paint();},
   pause(){suspended=true;stop();},
   resume(revealed=false){suspended=false;if(revealed&&revealedAt===startedAt)revealedAt=performance.now();move();}
  };
 }
};
