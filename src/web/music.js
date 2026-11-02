/* Local menu music: every pass starts at the requested source timestamp. */
window.GraceMusic=(()=>{
 let audio=null,config=null,ticket=0,frame=0,finishFade=null,cancelLoad=null,exiting=true,loops=0;
 function cancelFade(){if(frame)cancelAnimationFrame(frame);frame=0;if(finishFade){const done=finishFade;finishFade=null;done();}}
 function stop(){
  ++ticket;exiting=true;cancelFade();
  if(cancelLoad){const cancel=cancelLoad;cancelLoad=null;cancel();}
  if(audio){audio.pause();audio.volume=0;audio.removeAttribute('src');audio.load();audio.remove();audio=null;}
 }
 function fadeTo(volume,duration){
  cancelFade();const element=audio,id=ticket;if(!element)return Promise.resolve();
  const from=element.volume,start=performance.now();
  return new Promise(resolve=>{
   finishFade=resolve;
   const step=now=>{
    if(id!==ticket||audio!==element){finishFade=null;resolve();return;}
    const progress=Math.max(0,Math.min(1,(now-start)/Math.max(1,duration)));element.volume=from+(volume-from)*progress;
    if(progress<1)frame=requestAnimationFrame(step);else{frame=0;finishFade=null;resolve();}
   };
   frame=requestAnimationFrame(step);
  });
 }
 function configure(settings){
  stop();
  if(!settings?.source){config=null;return Promise.resolve(false);}
  config={source:settings.source,startSeconds:Math.max(0,Number(settings.startSeconds)||0),volume:Math.min(1,Math.max(0,Number(settings.volume??.22))),fadeInMs:Math.max(0,Number(settings.fadeInMs??1000))};
  const element=new Audio();audio=element;const id=ticket;exiting=false;loops=0;
  element.id='grace-menu-audio';element.hidden=true;element.preload='auto';element.loop=false;element.volume=0;
  document.body.append(element);
  element.addEventListener('ended',()=>{
   if(id!==ticket||audio!==element||exiting)return;
   element.currentTime=config.startSeconds;++loops;
   element.play().catch(()=>{if(id===ticket)stop();});
  });
  const loaded=new Promise(resolve=>{
   let settled=false;
   const done=ok=>{if(settled)return;settled=true;clearTimeout(timeout);element.removeEventListener('loadedmetadata',ready);element.removeEventListener('error',failed);if(id===ticket)cancelLoad=null;resolve(ok);};
   const failed=()=>{done(false);if(id===ticket)stop();};
   const ready=()=>{
    if(id!==ticket||exiting){done(false);return;}
    if(!Number.isFinite(element.duration)||element.duration<=config.startSeconds){failed();return;}
    element.currentTime=config.startSeconds;
    element.play().then(()=>{
     if(id!==ticket||exiting){element.pause();done(false);return;}
     done(true);fadeTo(config.volume,config.fadeInMs);
    }).catch(failed);
   };
   const timeout=setTimeout(failed,10000);cancelLoad=()=>done(false);
   element.addEventListener('loadedmetadata',ready);element.addEventListener('error',failed);
   element.src=config.source;element.load();
  });
  return loaded;
 }
 function fadeOut(duration=1800){
  const id=ticket;exiting=true;
  return fadeTo(0,duration).then(()=>{if(id===ticket&&exiting)stop();});
 }
 function resume(){return config?configure(config):Promise.resolve(false);}
 function dispose(){stop();config=null;}
 addEventListener('pagehide',dispose);addEventListener('beforeunload',dispose);
 document.addEventListener('visibilitychange',()=>{
  if(!audio||exiting)return;
  const element=audio,id=ticket;
  if(document.hidden)element.pause();else element.play().then(()=>{if(id!==ticket||exiting)element.pause();}).catch(()=>{});
 });
 return{configure,fadeOut,stop,resume,dispose,get state(){return{paused:!audio||audio.paused,currentTime:audio?.currentTime||0,duration:audio?.duration||0,volume:audio?.volume||0,sourceAttached:!!audio?.getAttribute('src'),startSeconds:config?.startSeconds||0,loops};}};
})();
