/* Local scene curl: retire lettering, gather gold fragments, settle to the native cover. */
window.GraceVortex=(()=>{
 let canvas,source,gl,program,timeUniform,prepared=false,fallback,moteCanvas,moteContext;
 let uiAnimations=[],fragments=[];
 let preparation=null,revision=0,capturedWidth=0,capturedHeight=0,cancellationEpoch=0;
 const duration=2400;
 const smooth=(a,b,t)=>{const x=Math.max(0,Math.min(1,(t-a)/(b-a)));return x*x*(3-2*x);};
 const vertex='attribute vec2 a;varying vec2 uv;void main(){uv=(a+1.0)*.5;gl_Position=vec4(a,0.,1.);}';
 const fragment=`precision highp float;varying vec2 uv;uniform sampler2D scene;uniform float progress;uniform float aspect;
 void main(){
  float t=clamp(progress,0.,1.);
  float pull=pow(smoothstep(.07,.94,t),1.45);
  vec2 center=vec2(.54,.50),p=(uv-center)*vec2(aspect,1.);
  float r=length(p),scale=exp(-1.85*pull);
  float spin=5.1*pull*exp(-r/(.52+.20*(1.-pull)));
  float a=atan(p.y,p.x)+spin;
  float radial=r/scale*(1.+.055*sin(a*3.-r*5.+t*2.)*pull*(1.-pull));
  vec2 q=vec2(cos(a),sin(a))*radial/vec2(aspect,1.)+center;
  float feather=mix(.0001,.22,smoothstep(.08,.76,t));
  float valid=smoothstep(0.,feather,q.x)*smoothstep(0.,feather,1.-q.x)*smoothstep(0.,feather,q.y)*smoothstep(0.,feather,1.-q.y);
  vec2 blur=vec2(-p.y,p.x)/vec2(aspect,1.)*(.011*sin(t*3.14159265)*pull);
  vec3 col=texture2D(scene,clamp(q,vec2(0.),vec2(1.))).rgb*.50;
  col+=texture2D(scene,clamp(q+blur,vec2(0.),vec2(1.))).rgb*.25;
  col+=texture2D(scene,clamp(q-blur,vec2(0.),vec2(1.))).rgb*.25;
  float fade=1.-smoothstep(.48,.98,t);
  gl_FragColor=vec4(mix(vec3(24./255.),col,valid*fade),1.);
 }`;
 function shader(type,text){const s=gl.createShader(type);gl.shaderSource(s,text);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error(gl.getShaderInfoLog(s));return s;}
 function releaseCanvas(){
  if(gl)gl.getExtension('WEBGL_lose_context')?.loseContext();
  canvas?.remove();moteCanvas?.remove();
  if(source){source.width=1;source.height=1;}
  canvas=source=gl=program=fallback=moteCanvas=moteContext=null;fragments=[];prepared=false;
 }
 function makeCanvas(id,z){
  const c=document.createElement('canvas');c.id=id;c.width=source.width;c.height=source.height;
  c.style.cssText=`position:fixed;inset:0;width:100%;height:100%;z-index:${z};pointer-events:none;opacity:0`;
  document.body.append(c);return c;
 }
 async function prepare(root){
  const ticket=++revision;releaseCanvas();const width=innerWidth,height=innerHeight;await document.fonts.ready;
  const states=[];
  for(const [i,el] of [...root.querySelectorAll('*')].entries()){
   const style=getComputedStyle(el);el.setAttribute('data-grace-snapshot',String(i));states.push({opacity:style.opacity,transform:style.transform,filter:style.filter});
  }
  try{
   const snapshot=await html2canvas(root,{backgroundColor:'#080b0b',scale:Math.min(1,1600/width,900/height),logging:false,useCORS:false,
    ignoreElements:e=>e.id==='scene-reveal'||e.classList?.contains('preview-bar')||['title-block','gate','quote','transition'].includes(e.id),
    onclone:doc=>{doc.querySelectorAll('[data-grace-snapshot]').forEach(el=>{const state=states[Number(el.getAttribute('data-grace-snapshot'))];if(state){el.style.opacity=state.opacity;el.style.transform=state.transform;if(!(el.namespaceURI==='http://www.w3.org/2000/svg'&&state.filter.startsWith('url(')))el.style.filter=state.filter;}});}
   });
   if(ticket!==revision){snapshot.width=1;snapshot.height=1;return null;}
   source=snapshot;
  }finally{root.querySelectorAll('[data-grace-snapshot]').forEach(el=>el.removeAttribute('data-grace-snapshot'));}
  canvas=makeCanvas('vortex-canvas',30);
  gl=canvas.getContext('webgl',{alpha:false,antialias:false,preserveDrawingBuffer:true});
  if(gl){
   try{
    program=gl.createProgram();gl.attachShader(program,shader(gl.VERTEX_SHADER,vertex));gl.attachShader(program,shader(gl.FRAGMENT_SHADER,fragment));gl.linkProgram(program);if(!gl.getProgramParameter(program,gl.LINK_STATUS))throw Error(gl.getProgramInfoLog(program));gl.useProgram(program);
    const buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,buffer);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array([-1,-1,1,-1,-1,1,-1,1,1,-1,1,1]),gl.STATIC_DRAW);
    const position=gl.getAttribLocation(program,'a');gl.enableVertexAttribArray(position);gl.vertexAttribPointer(position,2,gl.FLOAT,false,0,0);
    const texture=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,texture);gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,source);
    timeUniform=gl.getUniformLocation(program,'progress');gl.uniform1f(gl.getUniformLocation(program,'aspect'),canvas.width/canvas.height);gl.viewport(0,0,canvas.width,canvas.height);canvas.dataset.renderer='webgl';
   }catch(error){gl.getExtension('WEBGL_lose_context')?.loseContext();gl=null;canvas.remove();canvas=makeCanvas('vortex-canvas',30);}
  }
  if(!gl){fallback=canvas.getContext('2d');canvas.dataset.renderer='canvas';}
  moteCanvas=makeCanvas('vortex-motes',31);moteContext=moteCanvas.getContext('2d');
  let seed=736211;const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
  for(let i=0;i<44;i++){
   const x=i<26?.61+random()*.35:random(),y=i<26?.04+random()*.48:random();
   const dx=(x-.54)*canvas.width/canvas.height,dy=y-.50;
   fragments.push({r:Math.hypot(dx,dy),a:Math.atan2(dy,dx),size:.45+random()*.7,alpha:.25+random()*.45,delay:random()*.12});
  }
  capturedWidth=width;capturedHeight=height;prepared=true;return canvas;
 }
 function warm(root){
  if(prepared&&capturedWidth===innerWidth&&capturedHeight===innerHeight)return Promise.resolve(canvas);
  if(preparation){
   const epoch=cancellationEpoch;
   // A fresh request may be waiting on an older, cancelled capture. Retry only
   // if this caller itself has not been cancelled in the meantime.
   const resume=()=>epoch===cancellationEpoch?warm(root):null;
   return preparation.then(resume,resume);
  }
  preparation=prepare(root).finally(()=>{preparation=null;});return preparation;
 }
 function fragmentPoint(p,t){
  const pull=Math.pow(smooth(.07,.94,t),1.45),scale=Math.exp(-1.85*pull),r=p.r*scale*(1-smooth(.70,.98,t));
  const a=p.a+5.1*pull*Math.exp(-r/(.52+.20*(1-pull)));
  return{x:canvas.width*.54+Math.cos(a)*r*canvas.height,y:canvas.height*.50+Math.sin(a)*r*canvas.height};
 }
 function drawFragments(t){
  moteContext.clearRect(0,0,moteCanvas.width,moteCanvas.height);
  const fade=smooth(.10,.32,t)*(1-smooth(.72,.96,t));moteCanvas.style.opacity=String(fade);
  if(fade<=0)return;
  for(const p of fragments){
   const point=fragmentPoint(p,Math.max(0,t-p.delay)),tail=fragmentPoint(p,Math.max(0,t-p.delay-.022));
   moteContext.strokeStyle=`rgba(235,207,143,${p.alpha*.6})`;moteContext.lineWidth=p.size*.7;moteContext.beginPath();moteContext.moveTo(tail.x,tail.y);moteContext.lineTo(point.x,point.y);moteContext.stroke();
   moteContext.fillStyle=`rgba(249,228,179,${p.alpha})`;moteContext.beginPath();moteContext.arc(point.x,point.y,p.size,0,Math.PI*2);moteContext.fill();
  }
 }
 function draw(t){
  if(!prepared)return;t=Math.max(0,Math.min(1,t));canvas.style.opacity=String(Math.min(1,t/.055));
  if(gl){gl.uniform1f(timeUniform,t);gl.drawArrays(gl.TRIANGLES,0,6);}
  else{
   const w=canvas.width,h=canvas.height,pull=Math.pow(smooth(.07,.94,t),1.45),scale=Math.exp(-1.85*pull);
   fallback.fillStyle='#181818';fallback.fillRect(0,0,w,h);fallback.save();fallback.translate(w*.54,h*.50);fallback.rotate(pull*.9);fallback.scale(scale,scale);fallback.globalAlpha=1-smooth(.48,.98,t);fallback.drawImage(source,-w*.54,-h*.50);fallback.restore();
  }
  drawFragments(t);
 }
 async function play(root,reduced=false){
  uiAnimations.splice(0).forEach(a=>a.cancel());
  if(reduced){hide();await root.animate([{opacity:1},{opacity:0}],{duration,fill:'forwards',easing:'ease-in'}).finished;return;}
  uiAnimations=[...root.querySelectorAll('#title-block,#gate,#quote')].map(el=>el.animate([{opacity:getComputedStyle(el).opacity},{opacity:0}],{duration:260,fill:'forwards',easing:'ease-in'}));
  await Promise.all([warm(root),...uiAnimations.map(a=>a.finished)]);
  if(!prepared)return;
  if(capturedWidth!==innerWidth||capturedHeight!==innerHeight)await warm(root);
  if(!prepared)return;
  const ticket=revision;
  await new Promise((resolve,reject)=>{let start;function tick(now){try{if(ticket!==revision){resolve();return;}if(start===undefined)start=now;const t=Math.min(1,(now-start)/duration);draw(t);if(t<1)requestAnimationFrame(tick);else resolve();}catch(error){reject(error);}}requestAnimationFrame(tick);});
 }
 function hide(){cancellationEpoch++;revision++;uiAnimations.splice(0).forEach(a=>a.cancel());releaseCanvas();}
 return{prepare,warm,draw,play,hide};
})();
