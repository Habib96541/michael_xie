import assert from 'node:assert/strict';
import {readFile,mkdir,writeFile} from 'node:fs/promises';
import vm from 'node:vm';

const source=await readFile(new URL('../Assets/WebGLTemplates/BloomDoom/TemplateData/viewport.js',import.meta.url),'utf8');
const iphone='Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1';
function fixture({ua=iphone,width=390,height=844,touch=5,screenWidth=width,screenHeight=height,visual=true,fullscreen,standalone=false}={}){
  const values={},classes=new Set(),events={},viewportEvents={},frames=[];
  const padding={paddingLeft:'0',paddingRight:'0',paddingTop:'0',paddingBottom:'0'};
  const element=()=>({hidden:true,focus(){},toggleAttribute(name,on){this[name]=on;},addEventListener(type,handler){this[type]=handler;}});
  const elements=new Map(['game','game-launch','launch-play','launch-heading','launch-message','launch-install','launch-rotate'].map(id=>[id,element()]));
  const root={
    classList:{add:name=>classes.add(name),toggle:(name,on)=>on?classes.add(name):classes.delete(name)},
    style:{setProperty:(key,value)=>values[key]=parseFloat(value)}
  };
  const document={
    documentElement:root,body:{appendChild(){}},createElement:()=>({style:{},setAttribute(){}}),
    getElementById:id=>elements.get(id),addEventListener(){},fullscreenEnabled:!!fullscreen
  };
  let fullscreenCalls=0;
  const locks=[];
  if(fullscreen)root.requestFullscreen=()=>{
    fullscreenCalls++;
    if(fullscreen==='deny')return Promise.reject(new Error('Fullscreen denied'));
    document.fullscreenElement=root;
    return Promise.resolve();
  };
  const window={innerWidth:width,innerHeight:height,addEventListener:(key,value)=>events[key]=value};
  if(visual)window.visualViewport={width,height,offsetLeft:0,offsetTop:0,addEventListener:(key,value)=>viewportEvents[key]=value};
  vm.runInNewContext(source,{navigator:{userAgent:ua,maxTouchPoints:touch,standalone},screen:{width:screenWidth,height:screenHeight,orientation:{lock:mode=>{locks.push(mode);return Promise.resolve();}}},document,window,getComputedStyle:()=>padding,requestAnimationFrame:fn=>frames.push(fn),setTimeout:fn=>frames.push(fn)});
  return {values,classes,padding,window,locks,get fullscreenCalls(){return fullscreenCalls;},ready(){events['bloomdoom-play-ready']();},click(){return elements.get('launch-play').click();},get blocked(){return !elements.get('game-launch').hidden;},get inert(){return elements.get('game').inert;},get playHidden(){return elements.get('launch-play').hidden;},get installHidden(){return elements.get('launch-install').hidden;},resize(w,h,insets={}){
    Object.assign(padding,{paddingLeft:'0',paddingRight:'0',paddingTop:'0',paddingBottom:'0'},insets);
    window.innerWidth=w;window.innerHeight=h;if(window.visualViewport)Object.assign(window.visualViewport,{width:w,height:h});
    (viewportEvents.resize||events.resize)?.();while(frames.length)frames.shift()();
  }};
}
const reports=[];
for(const [width,height] of [[320,568],[375,667],[414,736],[375,812],[390,844],[393,852],[414,896],[428,926],[430,932],[402,874],[440,956]]){
  const f=fixture({width,height});
  assert.equal(f.blocked,false,'Loading UI remains available until ready');
  f.ready();
  for(const landscape of [false,true]){
    const w=landscape?height:width,h=landscape?width:height;
    const insets=height>800?(landscape?{paddingLeft:'47px',paddingRight:'47px',paddingBottom:'21px'}:{paddingTop:'59px',paddingBottom:'34px'}):{};
    f.resize(w,h,insets);
    const v=f.values,fw=v['--garden-frame-width'],fh=v['--garden-frame-height'];
    const left=parseFloat(insets.paddingLeft)||0,right=parseFloat(insets.paddingRight)||0,top=parseFloat(insets.paddingTop)||0,bottom=parseFloat(insets.paddingBottom)||0;
    assert.ok(f.classes.has('phone-layout'));
    assert.equal(fw,w-left-right,'View fills safe-area width');
    assert.equal(fh,h-top-bottom,'View fills safe-area height');
    assert.equal(f.blocked,!landscape,'Portrait shows the rotate screen until the phone is sideways');
    assert.equal(f.inert,!landscape,'Covered game cannot receive input');
    assert.ok(v['--garden-frame-left']>=left&&v['--garden-frame-top']>=top,'Safe-area origin');
    assert.ok(v['--garden-frame-left']+fw<=w-right+1e-9&&v['--garden-frame-top']+fh<=h-bottom+1e-9,'No cropped controls');
    reports.push({viewport:[w,h],frame:[fw,fh],safeArea:insets});
  }
}
for(const height of [260,300,350]){
  const f=fixture({width:844,height});
  assert.equal(f.values['--garden-frame-width'],844,'Safari toolbars must not introduce empty side bars');
  assert.equal(f.values['--garden-frame-height'],height);
}
for(const ua of [iphone.replace('Version/18.0','CriOS/130.0'),iphone.replace('Version/18.0','FxiOS/131.0')])assert.ok(fixture({ua}).classes.has('phone-layout'));
assert.ok(fixture({ua:'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit/605.1.15',screenWidth:390,screenHeight:844}).classes.has('phone-layout'),'iPhone desktop mode');
assert.ok(fixture({visual:false}).classes.has('phone-layout'),'Older viewport API fallback');
for(const options of [
  {ua:'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit Chrome',touch:0},
  {ua:'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit',screenWidth:744,screenHeight:1133},
  {ua:'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit',touch:0}
]){
  const f=fixture(options);assert.equal(f.classes.size,0);assert.equal(Object.keys(f.values).length,0,'Other devices retain existing layout');
}
const android='Mozilla/5.0 (Linux; Android 14) AppleWebKit Chrome Mobile';
const supported=fixture({ua:android,fullscreen:'allow'});
supported.ready();
assert.equal(supported.playHidden,false);
assert.equal(supported.fullscreenCalls,0,'Fullscreen waits for a tap');
const entered=supported.click();
assert.equal(supported.fullscreenCalls,1,'Request runs in the click handler');
await entered;
assert.deepEqual(supported.locks,['landscape']);
supported.resize(844,390);
assert.equal(supported.blocked,false);
supported.resize(390,844);
assert.equal(supported.blocked,true,'Rotating back covers an active game');
assert.equal(supported.playHidden,true,'Rotation does not prompt for fullscreen a second time');

const denied=fixture({ua:android,fullscreen:'deny'});
denied.ready();
await denied.click();
assert.equal(denied.blocked,true);
denied.resize(844,390);
assert.equal(denied.blocked,false,'A denied request still allows landscape play');

const desktop=fixture({ua:'Windows Chrome',width:1280,height:720,touch:0,fullscreen:'allow'});
desktop.ready();
assert.equal(desktop.blocked,true);
await desktop.click();
assert.equal(desktop.blocked,false);
assert.deepEqual(desktop.locks,[],'Desktop fullscreen does not try to rotate the display');
const installed=fixture({standalone:true,width:844,height:390});
installed.ready();
assert.equal(installed.blocked,false,'The Home Screen app starts directly in landscape');
installed.resize(390,844);
assert.equal(installed.installHidden,true);
const moved=fixture();Object.assign(moved.window.visualViewport,{offsetLeft:12,offsetTop:20});moved.resize(390,600);
assert.equal(moved.values['--garden-frame-left'],12);assert.ok(moved.values['--garden-frame-top']>=20,'Visual viewport changes stay centered');
if(!process.argv.includes('--source-only')){
  for(const name of ['viewport.js','loading.css','garden.webmanifest'])assert.equal(await readFile(new URL('../Builds/WebGL/TemplateData/'+name,import.meta.url),'utf8'),await readFile(new URL('../Assets/WebGLTemplates/BloomDoom/TemplateData/'+name,import.meta.url),'utf8'),'Shipped template matches source');
}
const qa=new URL('../Logs/QA/',import.meta.url);await mkdir(qa,{recursive:true});await writeFile(new URL('iphone-layout.json',qa),JSON.stringify({passed:true,landscapeFillsViewport:true,portraitRotateGate:true,simulated:true,cases:reports},null,2)+'\n');
console.log('PASS: 22 phone sizes and orientations, safe areas, rotate gate, user-gesture fullscreen, landscape lock, denied fullscreen and Home Screen mode.');
