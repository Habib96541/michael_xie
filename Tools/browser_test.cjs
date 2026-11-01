const { chromium, firefox } = require('playwright');
const fs = require('fs');
const path = require('path');
const root=path.resolve(__dirname,'..');
const out=path.join(root,'Validation');
const executable=process.env.HILO_BROWSER || 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const browserName=process.env.HILO_ENGINE==='firefox'?'Firefox':executable.toLowerCase().includes('chrome')?'Chrome':'Edge';
const base=process.env.HILO_URL || 'http://127.0.0.1:8765';
const sizes=[[1920,1080],[1366,768],[1280,720],[390,844],[360,800],[844,390],[768,1024],[1024,768]];
function geometry(w,h){const p=w/h<1.05,c=!p&&h<520,s=p?w/Math.max(420,800*w/h):h/(c?420:800);return {p,c,s};}
async function state(page){return page.evaluate(()=>window.hiloState);}
async function waitReady(page){await page.waitForFunction(()=>window.unityInstance&&window.hiloState,null,{timeout:120000});await page.waitForTimeout(3000);}
async function primary(page){let {width:w,height:h}=page.viewportSize();let {s,c}=geometry(w,h);await page.mouse.click(w/2,h/2+154*s*(c?.85:1));await page.waitForTimeout(450);}
async function predict(page,hi,touch=false){let {width:w,height:h}=page.viewportSize();let {p,c,s}=geometry(w,h);let x,y;if(p){let sw=Math.min(w/s-36,420);x=w/2+(hi?-1:1)*(sw+10)/4*s;y=h-(157+16)*s;}else{x=w/2+(hi?(c?-220:-248):0)*s;y=h-(c?65:110)*s;}if(touch)await page.touchscreen.tap(x,y);else await page.mouse.click(x,y);}
async function settle(page){await page.waitForTimeout(1000);await page.waitForFunction(()=>window.hiloState?.state!=='Revealing');}
(async()=>{
  fs.mkdirSync(path.join(out,'screenshots'),{recursive:true});
  const browser=browserName==='Firefox'?await firefox.launch({headless:true,firefoxUserPrefs:{'webgl.force-enabled':true}}):await chromium.launch({headless:true,executablePath:executable,args:['--enable-webgl','--ignore-gpu-blocklist']});
  const context=await browser.newContext({viewport:{width:1280,height:720},deviceScaleFactor:1,hasTouch:true});
  const page=await context.newPage();const errors=[],warnings=[];
  page.on('pageerror',e=>errors.push(String(e)));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());if(m.type()==='warning')warnings.push(m.text());});
  page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url());});
  const started=Date.now();await page.goto(base);await waitReady(page);const loadMs=Date.now()-started;
  await page.screenshot({path:path.join(out,'screenshots',browserName+'-welcome.png')});
  await primary(page);
  if((await state(page)).state!=='Decision')throw Error('Start button did not begin a round');
  const captures=[];
  for(const [w,h] of sizes){await page.setViewportSize({width:w,height:h});await page.waitForTimeout(350);await page.screenshot({path:path.join(out,'screenshots',browserName+`-${w}x${h}.png`)});const dimensions=await page.evaluate(()=>({scroll:document.documentElement.scrollWidth>innerWidth,canvas:[document.querySelector('canvas').width,document.querySelector('canvas').height],css:[innerWidth,innerHeight]}));if(dimensions.scroll)throw Error('Horizontal overflow at '+w+'x'+h);captures.push({width:w,height:h,...dimensions});}
  await page.setViewportSize({width:1280,height:720});await page.waitForTimeout(300);
  const checks=[];function check(name,passed,detail){checks.push({name,passed,detail});if(!passed)throw Error(name+' failed: '+JSON.stringify(detail));}
  await page.setViewportSize({width:390,height:844});await page.waitForTimeout(200);
  const beforeTouch=await state(page);await predict(page,beforeTouch.higher>=beforeTouch.lower,true);await settle(page);const afterTouch=await state(page);
  check('Phone touch prediction',BigInt(afterTouch.predictions)===BigInt(beforeTouch.predictions)+1n,afterTouch.state);
  if(afterTouch.modal)await primary(page);
  await page.touchscreen.tap(195,844-88*(390/420));await page.waitForTimeout(250);
  check('Phone touch cash out',(await state(page)).outcome==='CashOut'&&(await state(page)).modal);
  await page.screenshot({path:path.join(out,'screenshots',browserName+'-phone-result.png')});await primary(page);
  await page.setViewportSize({width:1280,height:720});await page.waitForTimeout(200);
  console.log(browserName+': viewport and touch checks passed');
  // Exercise the actual UI and production shuffle. No deck, balance or streak mutations.
  let outcomes=new Set(),maxStreak=0,attempts=0;
  while(attempts<160 && (!outcomes.has('Push') || maxStreak<5 || !outcomes.has('Wrong'))){
    let s=await state(page);if(s.modal){await primary(page);s=await state(page);if(s.modal){await primary(page);s=await state(page);}}
    if(s.state!=='Decision')throw Error('Round did not enter decision');
    const hi=s.higher>=s.lower;await predict(page,hi);await settle(page);const n=await state(page);attempts++;outcomes.add(n.outcome);maxStreak=Math.max(maxStreak,Number(n.streak));
    if(n.state==='Revealing')throw Error('Reveal never completed');
  }
  check('Higher/lower predictions and losing round',outcomes.has('Correct')&&outcomes.has('Wrong'),[...outcomes]);
  check('Natural equal-rank push',outcomes.has('Push'),[...outcomes]);
  check('Progression to timed stage',maxStreak>=5,{maxStreak,attempts});
  let s=await state(page);if(s.modal)await primary(page);
  // Settings pause and preferences.
  await page.mouse.click(1280/2+(Math.min(1280/(720/800)-90,1060)/2-20)*(720/800),44*(720/800));await page.waitForTimeout(150);
  check('Settings opens', (await state(page)).modal===true);
  for(let i=0;i<4;i++){await page.mouse.click(640,360-(80-i*57)*.9);await page.waitForTimeout(100);}
  const preferences=await state(page);check('Audio, reduced motion and timer settings toggle',!preferences.sound&&!preferences.music&&preferences.reducedMotion&&!preferences.pressure,preferences);
  await page.screenshot({path:path.join(out,'screenshots',browserName+'-settings.png')});
  await page.keyboard.press('Escape');await page.waitForTimeout(100);
  check('Settings closes',(await state(page)).modal===false);
  await page.keyboard.press('Space');await page.waitForTimeout(150);s=await state(page);
  check('Keyboard cash out',s.outcome==='CashOut'&&s.state==='Ended',s);
  await page.screenshot({path:path.join(out,'screenshots',browserName+'-cashout.png')});
  const savedBalance=s.balance;await page.reload();await waitReady(page);check('Balance persists on reload',(await state(page)).balance===savedBalance,{expected:savedBalance,actual:(await state(page)).balance});
  check('Preferences persist on reload',!(await state(page)).pressure&&(await state(page)).reducedMotion&&!(await state(page)).sound&&!(await state(page)).music);
  await primary(page);s=await state(page);
  // Rapid UI input must not queue multiple predictions across the card animation.
  const direction=s.higher>=s.lower;await predict(page,direction);await predict(page,direction);await predict(page,direction);await settle(page);
  const afterRapid=await state(page);check('Rapid clicking stays in valid round state',['Decision','Ended'].includes(afterRapid.state),afterRapid);
  check('Rapid clicking accepts exactly one prediction',BigInt(afterRapid.predictions)===BigInt(s.predictions)+1n,{before:s.predictions,after:afterRapid.predictions});
  if(afterRapid.modal)await primary(page);
  // Re-enable pressure, reach the real timed stage, and exercise timeout and pause.
  const gearX=1280/2+(Math.min(1280/(720/800)-90,1060)/2-20)*(720/800);
  await page.mouse.click(gearX,44*.9);await page.waitForTimeout(100);await page.mouse.click(640,360-(80-3*57)*.9);await page.keyboard.press('Escape');await page.waitForTimeout(150);
  let timedAttempts=0;
  while((await state(page)).stage<1&&timedAttempts<160){let current=await state(page);if(current.modal){await primary(page);current=await state(page);if(current.modal){await primary(page);current=await state(page);}}await predict(page,current.higher>=current.lower);await settle(page);timedAttempts++;}
  let timed=await state(page);check('Timed window reached in production deck',timed.stage>=1&&!timed.modal,timed);
  await page.screenshot({path:path.join(out,'screenshots',browserName+'-timed.png')});
  await page.mouse.click(gearX,44*.9);await page.waitForTimeout(18000);check('Settings pauses the live decision timer',(await state(page)).state==='Decision'&&(await state(page)).modal);
  await page.keyboard.press('Escape');await page.waitForTimeout(17000);check('Timeout ends the round without auto-predicting',(await state(page)).outcome==='Timeout'&&(await state(page)).state==='Ended',await state(page));
  await page.screenshot({path:path.join(out,'screenshots',browserName+'-timeout.png')});
  const frameStats=await page.evaluate(()=>new Promise(resolve=>{const times=[];let last=performance.now();function tick(now){times.push(now-last);last=now;if(times.length<120)requestAnimationFrame(tick);else resolve({samples:times.length,averageMs:times.reduce((a,b)=>a+b)/times.length,maxMs:Math.max(...times),heap:performance.memory?.usedJSHeapSize});}requestAnimationFrame(tick);}));
  const buildHash=require('crypto').createHash('sha256').update(fs.readFileSync(path.join(root,'WebGL_Build/Build/WebGL_Build.wasm.unityweb'))).digest('hex');
  const report={testedAt:new Date().toISOString(),buildHash,browser:browserName,version:browser.version(),executable,loadMs,captures,checks,frameStats,errors,warnings:[...new Set(warnings)],outcomes:[...outcomes],maxStreak,predictions:attempts};
  fs.writeFileSync(path.join(out,'browser-'+browserName.toLowerCase()+'.json'),JSON.stringify(report,null,2));
  await browser.close();console.log(JSON.stringify(report,null,2));if(errors.length)process.exitCode=1;
})().catch(e=>{console.error(e);process.exit(1);});
