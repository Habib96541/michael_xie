/* Run with Playwright installed. Set PEGRUSH_PLAYWRIGHT to its package path if needed. */
const {chromium}=require(process.env.PEGRUSH_PLAYWRIGHT || 'playwright');
const fs=require('node:fs');const path=require('node:path');
const root=path.resolve(__dirname,'..');const out=path.join(root,'SprintDoc/Polish');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const report={environment:{browser:'',renderer:'',http:'http://127.0.0.1:8080',physicalDevice:false},checks:[],performance:[],screenshots:[],errors:[],warnings:[]};
function check(name,pass,details){report.checks.push({name,pass,details});if(!pass)throw new Error(name+': '+JSON.stringify(details));}
function watch(p){p.on('pageerror',e=>report.errors.push(e.message));p.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());if(m.type()==='warning')report.warnings.push(m.text())});}
const status=p=>p.evaluate(()=>window.pegrushStatus);
async function waitState(p,state){await p.waitForFunction(s=>window.pegrushStatus?.State===s,state,{timeout:45000});await sleep(400);}
async function control(p,name){await sleep(350);await p.waitForFunction(n=>window.pegrushStatus?.Controls?.some(c=>c.Name===n),name);return (await status(p)).Controls.find(c=>c.Name===name);}
async function click(p,name){const c=await control(p,name);await p.mouse.click(c.X,c.Y,{delay:40});await sleep(300);}
async function tap(p,name){const c=await control(p,name);await p.touchscreen.tap(c.X,c.Y);await sleep(100);}
async function clear(p){await p.waitForFunction(()=>window.pegrushStatus?.Active===0,null,{timeout:45000});}
async function shot(p,name){await sleep(350);let file=path.join(out,'Screenshots',name+'.png');await p.screenshot({path:file});report.screenshots.push(file);}
(async()=>{
 const browser=await chromium.launch({headless:true,args:['--use-angle=d3d11','--enable-webgl','--ignore-gpu-blocklist']});
 report.environment.browser=await browser.version();
 const ctx=await browser.newContext({viewport:{width:1600,height:900},deviceScaleFactor:1});const p=await ctx.newPage();watch(p);
 try{
  await p.goto(report.environment.http);await waitState(p,'Home');await shot(p,'After-Home-1600x900');
  report.environment.renderer=await p.evaluate(()=>{let gl=document.createElement('canvas').getContext('webgl2');return gl.getParameter(gl.getExtension('WEBGL_debug_renderer_info').UNMASKED_RENDERER_WEBGL)});
  await p.keyboard.press('Enter');await waitState(p,'Playing');check('Keyboard Enter starts game',true);
  for(const [w,h] of [[360,800],[720,1280],[768,1024],[1280,720],[1600,900]]){await p.setViewportSize({width:w,height:h});await shot(p,`After-Gameplay-${w}x${h}`);}
  await p.setViewportSize({width:1280,height:720});await sleep(350);
  await click(p,'Drop');check('Mouse click creates one ball',(await status(p)).Drops===1,await status(p));
  await p.keyboard.down('Space');await sleep(700);await p.keyboard.up('Space');await sleep(300);check('Held Space creates one ball',(await status(p)).Drops===2,await status(p));await clear(p);
  let d=(await status(p)).Drops;const drop=await control(p,'Drop');for(let i=0;i<20;i++){await p.mouse.click(drop.X,drop.Y,{delay:20});await sleep(25)}await sleep(300);check('20 rapid mouse activations',(await status(p)).Drops===d+20,await status(p));await clear(p);
  await click(p,'Home');await waitState(p,'Home');await click(p,'Play');const keyBefore=(await status(p)).Drops;for(let i=0;i<8;i++){await p.keyboard.press('Tab');await sleep(45)}await p.keyboard.press('Enter');await sleep(350);check('Tab traversal and Enter activation',(await status(p)).Drops===keyBefore+1);await clear(p);
  const before=await status(p);await p.evaluate(()=>{for(let i=0;i<51;i++)game.SendMessage('PegRush Session','Drop')});await sleep(350);const capacity=await status(p);check('50 active and 51st rejected without charge',capacity.Active===50&&capacity.Coins===before.Coins-50000,capacity);
  for(let i=0;i<20;i++){const s=await status(p);report.performance.push({active:s.Active,ms:s.FrameMilliseconds});await sleep(75)}await shot(p,'After-Stress50-1280x720');await clear(p);check('50 balls clear pending journal',await p.evaluate(()=>JSON.parse(localStorage.getItem('PegRush.Save.v1')).Pending.length===0));
  await click(p,'Amount');await click(p,'Drop');const snapshot=await p.evaluate(()=>JSON.parse(localStorage.getItem('PegRush.Save.v1')).Pending[0]);check('Per-ball cost and table snapshot',snapshot.Amount===2500&&snapshot.Rows===12&&snapshot.Profile===0&&snapshot.Table.length===13,snapshot);
  check('Rows and profiles disabled while active',!(await control(p,'RowsPlus')).Interactable&&!(await control(p,'ProfileRed')).Interactable);
  await click(p,'Settings');await waitState(p,'Modal');const modalDrops=(await status(p)).Drops;await p.keyboard.press('Space');await sleep(300);check('Modal blocks Space drop',(await status(p)).Drops===modalDrops);
  await click(p,'Mute');await click(p,'Reduced');let settings=await p.evaluate(()=>JSON.parse(localStorage.getItem('PegRush.Save.v1')));check('Settings persisted',settings.Muted&&settings.Reduced);
  await shot(p,'After-Settings-1280x720');await click(p,'ModalClose');await clear(p);
  await click(p,'Drop');await click(p,'Home');await waitState(p,'Modal');await click(p,'Drain');const drain=(await status(p)).Drops;await p.keyboard.press('Space');await sleep(300);check('Draining rejects new drops',(await status(p)).Drops===drain);await waitState(p,'Home');check('Drain returns home after resolution',true);
  await click(p,'Play');await click(p,'Drop');let pending=await p.evaluate(()=>JSON.parse(localStorage.getItem('PegRush.Save.v1')));await p.reload();await waitState(p,'Home');const recovered=await status(p);check('Reload refunds pending once',recovered.Coins===pending.Coins+pending.Pending.reduce((s,x)=>s+x.Amount,0),recovered);await p.reload();await waitState(p,'Home');check('Repeat reload preserves balance',(await status(p)).Coins===recovered.Coins);
  await click(p,'Play');await waitState(p,'Playing');for(let i=0;i<4;i++)await click(p,'RowsPlus');await click(p,'ProfileRed');check('16 rows and Red selected',(await status(p)).Rows===16&&(await status(p)).Profile===2);
  for(const [w,h] of [[360,800],[720,1280],[768,1024],[1280,720],[1600,900]]){await p.setViewportSize({width:w,height:h});await shot(p,`After-Rows16-Red-${w}x${h}`);}
  await p.setViewportSize({width:360,height:800});await sleep(450);await click(p,'Settings');await shot(p,'After-Settings-360x800');await click(p,'ModalClose');await click(p,'Home');await waitState(p,'Home');await click(p,'HowToPlay');await shot(p,'After-HowToPlay-360x800');await click(p,'ModalClose');
  const touch=await browser.newContext({viewport:{width:360,height:800},isMobile:true,hasTouch:true,deviceScaleFactor:1});const t=await touch.newPage();watch(t);await t.goto(report.environment.http);await waitState(t,'Home');await tap(t,'Play');await waitState(t,'Playing');const touchDrop=await control(t,'Drop');for(let i=0;i<10;i++){await t.touchscreen.tap(touchDrop.X,touchDrop.Y);await sleep(80)}await sleep(350);check('10 emulated taps without duplicate mouse events',(await status(t)).Drops===10,await status(t));await shot(t,'After-Touch-360x800');await t.setViewportSize({width:800,height:360});await sleep(350);check('Rotation preserves accepted drops',(await status(t)).Drops===10);await clear(t);await touch.close();
  await p.bringToFront();await p.setViewportSize({width:1280,height:720});await sleep(450);await click(p,'Play');await click(p,'Drop');const beforeFreeze=await status(p);const cdp=await ctx.newCDPSession(p);await cdp.send('Page.setWebLifecycleState',{state:'frozen'});await sleep(1000);await cdp.send('Page.setWebLifecycleState',{state:'active'});await p.bringToFront();await sleep(350);check('Controlled freeze/resume retains drop',(await status(p)).Drops===beforeFreeze.Drops);await clear(p);
  const full=p.locator('#fullscreen');await full.click();await sleep(400);check('Fullscreen user gesture',await p.evaluate(()=>!!document.fullscreenElement));await p.evaluate(()=>document.exitFullscreen());
  // Separate isolated save fixtures, not the player-facing test wallet.
  await p.evaluate(()=>localStorage.setItem('PegRush.Save.v1',JSON.stringify({Version:1,Coins:50,Muted:false,Reduced:false,Pending:[]})));await p.reload();await waitState(p,'Home');await click(p,'Play');await p.keyboard.press('Space');await sleep(350);check('Insufficient funds do not charge',(await status(p)).Coins===50&&(await status(p)).Drops===0);await click(p,'FreeRefill');check('Free refill when clear and below minimum',(await status(p)).Coins===100000);
  check('No console errors',report.errors.length===0,report.errors);check('No missing font glyph warnings',!report.warnings.some(x=>/character.*not found|Unicode.*not found/i.test(x)),report.warnings);
  report.complete=true;
 }catch(e){report.failure=e.stack;throw e;}finally{
  report.performanceMeanMs=report.performance.reduce((s,x)=>s+x.ms,0)/Math.max(1,report.performance.length);
  report.coverageLimits=['Physical devices, Safari and Firefox untested.','CDP freeze is controlled suspension, not proof of genuine tab hiding.','Short frame-time sample, not a long soak benchmark.'];
  fs.writeFileSync(path.join(out,'BrowserVerification.json'),JSON.stringify(report,null,2));await browser.close();
 }
})().catch(e=>{console.error(e);process.exitCode=1});




