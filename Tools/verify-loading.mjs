import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';
const loader=await readFile(new URL('../Assets/WebGLTemplates/BloomDoom/TemplateData/loading.js',import.meta.url),'utf8');
const bridge=await readFile(new URL('../Assets/Plugins/WebGL/GardenLoading.jslib',import.meta.url),'utf8');

function fixture(){
  let now=0,script,resolveEngine,progress,configuration;const frames=[],timers=[],listeners=new Map(),documentListeners=new Map(),elements=new Map();
  const element=()=>({textContent:'',hidden:true,isConnected:true,inert:false,style:{setProperty(){}},attributes:{},classes:new Set(),classList:{add(){}},addEventListener(){},setAttribute(k,v){this.attributes[k]=v;},removeAttribute(k){delete this.attributes[k];},remove(){this.isConnected=false;},focus(){this.focused=true;}});
  for(const id of ['game','loading','loading-meter','percentage','caption','failure','retry'])elements.set(id,element());
  for(const e of elements.values())e.classList.add=name=>e.classes.add(name);
  const body=element();body.classList.add=name=>body.classes.add(name);body.appendChild=s=>{script=s;};
  const window={gardenBuild:{dataUrl:'data',frameworkUrl:'framework',codeUrl:'code',loaderUrl:'loader',version:'test'},devicePixelRatio:1,addEventListener:(type,callback)=>listeners.set(type,callback),dispatchEvent:event=>listeners.get(event.type)?.(event)};
  const document={body,hidden:false,getElementById:id=>elements.get(id),createElement:()=>({}),addEventListener:(type,callback)=>documentListeners.set(type,callback),removeEventListener:type=>documentListeners.delete(type)};
  const context={window,document,performance:{now:()=>now},requestAnimationFrame:callback=>frames.push(callback),setTimeout:(callback,delay)=>{const timer={callback,at:now+delay};timers.push(timer);return timer;},clearTimeout:timer=>{if(timer)timer.cancelled=true;},location:{reload(){}},console:{error(){}},LibraryManager:{library:{}},mergeInto:Object.assign,Event:class{constructor(type){this.type=type;}},createUnityInstance:(_canvas,config,callback)=>{progress=callback;configuration=config;return new Promise(resolve=>{resolveEngine=resolve;});}};
  vm.createContext(context);vm.runInContext(bridge,context);vm.runInContext(loader,context);
  function pump(count){for(let i=0;i<count;i++){now+=16;const callback=frames.shift();if(callback)callback(now);for(const timer of [...timers])if(!timer.done&&!timer.cancelled&&timer.at<=now){timer.done=true;timer.callback();}}}
  return {elements,body,pump,begin(){script.onload();},native(){context.LibraryManager.library.BloomDoomLoadingReady();},async engine(){resolveEngine({});await Promise.resolve();await Promise.resolve();},hidden(value){document.hidden=value;documentListeners.get('visibilitychange')?.();},progress:value=>progress(value),networkFailure:()=>script.onerror(),get configuration(){return configuration;}};
}

{
  const f=fixture();f.begin();await f.engine();f.pump(160);
  assert.equal(f.elements.get('loading').isConnected,true,'Keep the loading screen until the native scene is ready');
  assert.equal(f.body.classes.has('game-ready'),false);
  f.native();f.pump(100);
  assert.equal(f.elements.get('loading').isConnected,false,'Native readiness should reveal the game');
  assert.equal(f.elements.get('game').focused,true);assert.equal(f.elements.get('percentage').textContent,'100%');
  assert.doesNotThrow(()=>f.configuration.showBanner('Later game error','error'));
}
{
  const f=fixture();f.begin();f.native();f.pump(160);
  assert.equal(f.elements.get('loading').isConnected,true,'Early native signal must not hide loading before engine initialization');
  await f.engine();f.pump(100);assert.equal(f.elements.get('loading').isConnected,false);
}
{
  const f=fixture();f.begin();f.progress(.75);f.pump(30);const before=Number(f.elements.get('loading-meter').attributes['aria-valuenow']);f.progress(.2);f.pump(30);
  assert.ok(Number(f.elements.get('loading-meter').attributes['aria-valuenow'])>=before,'Progress must never move backwards');
}
{
  const f=fixture();f.networkFailure();f.pump(10);
  assert.equal(f.elements.get('retry').hidden,false);assert.equal(f.elements.get('failure').hidden,false);assert.equal(f.body.classes.has('game-ready'),false);
}
{
  const f=fixture();f.begin();await f.engine();f.pump(2900);
  assert.equal(f.elements.get('retry').hidden,false,'A missing readiness signal must offer a retry instead of hanging forever');
}
{
  const f=fixture();f.begin();await f.engine();f.hidden(true);f.pump(4000);
  assert.equal(f.elements.get('retry').hidden,true,'A background tab must not trigger a false startup timeout');
  f.hidden(false);f.native();f.pump(100);assert.equal(f.elements.get('loading').isConnected,false);
}
console.log('PASS: loading lifecycle, early/late readiness, monotonic progress, download failure, timeout recovery, background tabs and post-start errors.');
