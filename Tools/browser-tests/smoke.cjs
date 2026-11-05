const {chromium}=require('playwright');
const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const reportDir=path.resolve(__dirname,'../../Reports');
const url=process.env.BOROUGH_URL||'http://127.0.0.1:8767';
const results={started:new Date().toISOString(),checks:[],samples:[],consoleErrors:[],limitations:['Physical Android and iPhone hardware were not available. Mobile checks use Chromium touch and viewport emulation.']};
const openBrowsers=[];
fs.mkdirSync(reportDir,{recursive:true});
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function check(name,pass,detail){results.checks.push({name,pass,detail});console.log((pass?'PASS ':'FAIL ')+name+(detail?' '+JSON.stringify(detail):''));assert.ok(pass,name);}
async function state(page){return page.evaluate(()=>window.boroughState);}
async function waitStage(page,stage,timeout=30000){await page.waitForFunction(stage=>window.boroughState?.stage===stage,stage,{timeout});return state(page);}
function point(w,h,kind){const s=Math.sqrt(w/1280*h/720);const top=(h-648*s)/2;if(kind==='play')return {x:(34+30+186)*s,y:top+(391+38)*s};if(kind==='help')return{x:(34+30+89)*s,y:top+(481+30)*s};if(kind==='settings')return{x:(34+223+90)*s,y:top+(481+30)*s};}
async function clickPoint(page,p){await page.mouse.click(p.x,p.y);}
async function launch(channel){const browser=await chromium.launch({channel,headless:true,args:['--enable-webgl','--ignore-gpu-blocklist']});openBrowsers.push(browser);results.browserVersions??={};results.browserVersions[channel]=browser.version();return browser;}
async function load(context,label){const page=await context.newPage();page.on('pageerror',e=>results.consoleErrors.push({label,message:e.message}));page.on('console',m=>{if(m.type()==='error')results.consoleErrors.push({label,message:m.text()});});const started=Date.now();await page.goto(url);await waitStage(page,'Menu',120000);results.checks.push({name:label+' loads over HTTP',pass:true,loadMs:Date.now()-started});console.log(label+' LOADED '+(Date.now()-started)+'ms');return page;}
async function desktop(){
 const browser=await launch('chrome');const context=await browser.newContext({viewport:{width:1280,height:720},deviceScaleFactor:1});const page=await load(context,'Chrome desktop');
 const cityCount=(await state(page)).available;await page.screenshot({path:path.join(reportDir,'chrome-menu.png')});
 await clickPoint(page,point(1280,720,'help'));await delay(300);await page.screenshot({path:path.join(reportDir,'chrome-help.png')});await page.mouse.click(640,580);await delay(200);
 await clickPoint(page,point(1280,720,'settings'));await delay(200);await page.screenshot({path:path.join(reportDir,'chrome-settings.png')});await page.mouse.click(640,558);await delay(200);
 await clickPoint(page,point(1280,720,'play'));await waitStage(page,'Playing');check('Countdown enters live gameplay',true);
 let initial=await state(page);await page.keyboard.down('a');await delay(900);await page.keyboard.up('a');await delay(650);let moved=await state(page);check('WASD moves player',Math.abs(moved.x-initial.x)>2,{from:initial.x,to:moved.x});
 await page.keyboard.press('Escape');let paused=await waitStage(page,'Paused');await delay(1200);let still=await state(page);check('Pause freezes timer and player',paused.timeLeft===still.timeLeft&&paused.x===still.x&&paused.z===still.z);await page.keyboard.press('Escape');await waitStage(page,'Playing');
 let held=new Set();let sampleAt=0;const deadline=Date.now()+155000;
 while(Date.now()<deadline){const s=await state(page);if(s?.stage==='Results')break;if(s?.stage==='Paused'){await page.keyboard.press('Escape');await delay(500);continue;}if(s?.stage!=='Playing'){await delay(300);continue;}
   const dx=s.targetX-s.x,dz=s.targetZ-s.z;const desired=new Set();if(Math.abs(dx)>.25)desired.add(dx>0?'d':'a');if(Math.abs(dz)>.25)desired.add(dz>0?'w':'s');
   for(const key of held)if(!desired.has(key))await page.keyboard.up(key);for(const key of desired)if(!held.has(key))await page.keyboard.down(key);held=desired;
   if(Date.now()>sampleAt){sampleAt=Date.now()+5000;results.samples.push(s);console.log('MATCH '+Math.round(s.timeLeft)+'s score='+s.score+' radius='+s.radius.toFixed(2)+' fps='+s.fps.toFixed(1)+' remaining='+s.available);if(s.timeLeft<110&&s.timeLeft>100)await page.screenshot({path:path.join(reportDir,'chrome-gameplay.png')});}
   await delay(220);
 }
 for(const key of held)await page.keyboard.up(key);const final=await waitStage(page,'Results',10000);results.desktopFinal=final;
 check('Complete 120-second match reaches results',final.timeLeft===0);check('Consumption awards score and growth',final.score>100&&final.consumed>5&&final.radius>.88,final);check('Seven AI rivals score independently',final.rivalScores.length===7&&final.rivalScores.filter(x=>x>0).length===7,final.rivalScores);check('Two street drops sustain late match targets',final.streetDrops===2&&results.samples.some(s=>s.timeLeft<35&&s.available>50));
 await page.screenshot({path:path.join(reportDir,'chrome-results.png')});await page.mouse.click(434,576);const replay=await waitStage(page,'Countdown');check('Replay resets score timer radius and entire city',replay.score===0&&replay.timeLeft===120&&replay.available===cityCount&&Math.abs(replay.radius-.88)<.001,replay);
 await waitStage(page,'Playing');await page.mouse.move(580,450);await page.mouse.down();await page.mouse.move(680,450,{steps:8});await delay(800);await page.mouse.up();await delay(600);const drag=await state(page);check('Mouse drag steers on replay',drag.x>replay.x+2);await browser.close();
}
async function mobile(){
 const browser=await launch('chrome');const context=await browser.newContext({viewport:{width:844,height:390},deviceScaleFactor:1,hasTouch:true,isMobile:true,userAgent:'Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Mobile Safari/537.36'});const page=await load(context,'Android Chrome emulation');await page.screenshot({path:path.join(reportDir,'mobile-menu.png')});const p=point(844,390,'play');await page.touchscreen.tap(p.x,p.y);await waitStage(page,'Playing');
 const session=await context.newCDPSession(page);const before=await state(page);
 await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:420,y:265,id:1}]});await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:500,y:265,id:1}]});await delay(1100);await session.send('Input.dispatchTouchEvent',{type:'touchCancel',touchPoints:[]});await delay(700);const after=await state(page);check('Touch drag moves without keyboard',after.x>before.x+3,{before,after});const stop=await state(page);await delay(850);const stationary=await state(page);check('Touch cancellation releases steering',Math.abs(stop.x-stationary.x)<.4);
 const multiBefore=await state(page);await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:420,y:265,id:1}]});await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:420,y:265,id:1},{x:600,y:265,id:2}]});await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:420,y:185,id:1},{x:650,y:265,id:2}]});await delay(850);await session.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await delay(700);const multiAfter=await state(page);check('Second touch does not take over steering',multiAfter.z>multiBefore.z+2&&Math.abs(multiAfter.x-multiBefore.x)<.6,{multiBefore,multiAfter});
 await page.screenshot({path:path.join(reportDir,'mobile-gameplay.png')});await page.touchscreen.tap(807,32);await waitStage(page,'Paused');const freeze=await state(page);await delay(700);check('Touch pause works without moving player',(await state(page)).timeLeft===freeze.timeLeft);await page.screenshot({path:path.join(reportDir,'mobile-pause.png')});
 await page.setViewportSize({width:390,height:844});await delay(500);await page.screenshot({path:path.join(reportDir,'mobile-portrait.png')});check('Portrait layout has no page scrolling',await page.evaluate(()=>document.documentElement.scrollWidth===innerWidth&&document.documentElement.scrollHeight===innerHeight));
 for(const size of [{width:780,height:360},{width:800,height:360},{width:1280,height:720}]){await page.setViewportSize(size);await delay(350);await page.screenshot({path:path.join(reportDir,`layout-${size.width}x${size.height}.png`)});}results.mobileFinal=await state(page);await browser.close();
}
async function edge(){const browser=await launch('msedge');const context=await browser.newContext({viewport:{width:1280,height:720}});const page=await load(context,'Edge desktop');await clickPoint(page,point(1280,720,'play'));await waitStage(page,'Playing');const before=await state(page);await page.keyboard.down('ArrowUp');await delay(800);await page.keyboard.up('ArrowUp');await delay(600);const after=await state(page);check('Edge WebGL loads and arrow keys move',after.z>before.z+2);await page.screenshot({path:path.join(reportDir,'edge-gameplay.png')});await browser.close();}
(async()=>{try{await desktop();await mobile();await edge();check('No browser runtime errors',results.consoleErrors.length===0,results.consoleErrors);results.status='passed';}catch(error){results.status='failed';results.error=error.stack;console.error(error);process.exitCode=1;}finally{for(const browser of openBrowsers)await browser.close().catch(()=>{});results.finished=new Date().toISOString();fs.writeFileSync(path.join(reportDir,'browser-results.json'),JSON.stringify(results,null,2));}})();
