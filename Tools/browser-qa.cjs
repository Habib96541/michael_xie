const {chromium}=require('playwright');
const fs=require('node:fs');
const path=require('node:path');
const out=path.resolve(__dirname,'../TestResults');fs.mkdirSync(out,{recursive:true});
const mode=process.argv[2]||'chrome';
const exe=mode==='edge'?'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe':'C:/Program Files/Google/Chrome/Application/chrome.exe';
(async()=>{
  const browser=await chromium.launch({executablePath:exe,headless:true,args:['--enable-unsafe-swiftshader']});
  const context=await browser.newContext({viewport:{width:1440,height:960},deviceScaleFactor:1});
  await context.addInitScript(()=>{window.qaAudioContexts=[];const Native=window.AudioContext;if(Native)window.AudioContext=new Proxy(Native,{construct(Target,args){const instance=new Target(...args);window.qaAudioContexts.push(instance);return instance;}});});
  const page=await context.newPage();let errors=[],warnings=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{const value=m.text();if(m.type()==='error'||/NullReferenceException|MissingReferenceException|TypeLoadException|DllNotFoundException|EntryPointNotFoundException|RuntimeError|Exception:|Assertion failed|Shader error/.test(value))errors.push(value);if(m.type()==='warning'||/ERROR: Shader|shader is not supported/.test(value))warnings.push(value);});
  const mouseClick=page.mouse.click.bind(page.mouse);page.mouse.click=(x,y,options={})=>mouseClick(x,y,{delay:80,...options});
  const checks=[];let s;
  const check=(name,pass,details)=>{checks.push({name,pass:!!pass,details});console.log(`${pass?'PASS':'FAIL'} ${name}`,pass?'':details||'');if(!pass)throw new Error(name);};
  const state=()=>page.evaluate(()=>window.brickRushState);
  const command=async cmd=>{await page.evaluate(c=>window.unityInstance.SendMessage('Brick Rush','QACommand',c),cmd);await page.waitForTimeout(300);};
  const waitState=async s=>page.waitForFunction(s=>window.brickRushState?.state===s,s,{timeout:15000});
  try{
    await page.goto('http://127.0.0.1:8187/?qa=1');await page.waitForFunction(()=>window.brickRushState?.state==='Menu',null,{timeout:180000});
    check('WebGL menu loaded',(await state()).remainingBricks===54);
    await page.screenshot({path:path.join(out,mode+'-menu.png')});
    await page.mouse.click(624,755);await page.waitForTimeout(300);await page.screenshot({path:path.join(out,mode+'-help.png')});await page.mouse.click(720,791);await page.waitForTimeout(300);
    const settingsBefore=await state();await page.mouse.click(816,755);await page.waitForTimeout(300);
    await page.mouse.click(600,391);await page.waitForTimeout(300);s=await state();check('Independent music volume',Math.abs(s.musicVolume-settingsBefore.musicVolume)>.1&&s.sfxVolume===settingsBefore.sfxVolume,s);
    await page.mouse.click(840,496);await page.waitForTimeout(300);s=await state();check('Independent SFX volume',Math.abs(s.sfxVolume-settingsBefore.sfxVolume)>.05,s);
    await page.mouse.click(720,586);await page.waitForTimeout(350);await page.mouse.click(720,658);await page.waitForTimeout(400);s=await state();check('Screen shake and reduced-effects settings',s.shake!==settingsBefore.shake&&s.reduced!==settingsBefore.reduced,s);await page.screenshot({path:path.join(out,mode+'-settings.png')});
    await page.mouse.click(720,765);await page.waitForTimeout(300);
    // Real pointer interaction starts the game; diagnostic commands only arrange edge scenarios.
    await page.mouse.click(720,610);await waitState('Countdown');s=await state();check('Start button and 3-second countdown',s.remaining===90,s);
    await waitState('Playing');await page.mouse.move(600,700);await page.waitForTimeout(300);s=await state();check('Mouse paddle control',s.paddle<-1,s.paddle);
    const leftPaddle=s.paddle;await page.keyboard.down('d');await page.waitForTimeout(250);await page.keyboard.up('d');await page.waitForTimeout(250);s=await state();check('Keyboard paddle control',s.paddle>leftPaddle+1,s.paddle);
    await command('auto');await page.waitForTimeout(7000);s=await state();check('Ball collisions score and remove bricks',s.bricks>0&&s.score>0,s);
    check('Audio resumes after real user interaction',await page.evaluate(()=>window.qaAudioContexts.some(c=>c.state==='running')));
    await page.screenshot({path:path.join(out,mode+'-playing.png')});
    await page.keyboard.press('Escape');await waitState('Paused');const paused=(await state()).remaining;await page.waitForTimeout(1000);check('Pause freezes timer',Math.abs((await state()).remaining-paused)<.05);
    await page.mouse.click(720,563);await waitState('Playing');
    const offlineTime=(await state()).remaining;await context.setOffline(true);await page.waitForTimeout(700);check('Gameplay continues offline after loading',(await state()).remaining<offlineTime-.4);await context.setOffline(false);
    for(let p=0;p<7;p++){const before=await state();await command('drop:'+p);s=await state();check('Power '+p+' collection and activation',s.collected>before.collected&&(p===0?s.balls===Math.min(5,before.balls+2):p===6?s.shield:Number(s.powers.split(',')[p])>8),s);}
    await command('power:0');await command('power:0');s=await state();check('Multiball capped',s.balls===5,s.balls);
    const shieldLives=s.lives,shieldSaves=s.shieldSaves;await page.evaluate(()=>{window.unityInstance.SendMessage('Brick Rush','QACommand','power:6');window.unityInstance.SendMessage('Brick Rush','QACommand','shield-test');});await page.waitForTimeout(400);s=await state();check('Shield reflects falling ball once',s.shieldSaves===shieldSaves+1&&!s.shield&&s.balls===1&&s.lives===shieldLives,s);
    const returns=s.paddleReturns;await command('paddle-test');s=await state();check('Paddle collision returns ball',s.paddleReturns>returns,s);
    await command('power:0');await command('power:0');const lives=s.lives;await command('lose');await waitState('Countdown');s=await state();check('Simultaneous ball loss costs one life',s.lives===lives-1&&s.balls===1,s);
    await waitState('Playing');const priorWave=(await state()).wave;await command('late');await command('wave');await command('power:1');await page.waitForTimeout(800);s=await state();check('New wave continues session',s.wave===priorWave+1&&s.completed>=1&&s.state==='Playing'&&Number(s.powers.split(',')[1])>8,s);
    await command('chain');await command('expire');await waitState('Results');s=await state();check('Timer results and life bonus',s.finalScore===s.score+s.lives*100&&s.best>=s.finalScore,s);
    const finishedPoints=s.score;await command('wave');check('No scoring after timer expires',(await state()).score===finishedPoints);
    await page.screenshot({path:path.join(out,mode+'-results.png')});const best=s.best;
    await page.mouse.click(720,646);await waitState('Countdown');s=await state();check('Replay resets state',s.score===0&&s.lives===3&&s.wave===1,s);
    await waitState('Playing');for(let life=2;life>=0;life--){await command('lose');await waitState(life>0?'Countdown':'Results');s=await state();check('Life exhaustion / '+life,s.lives===life,s);if(life>0)await waitState('Playing');}
    await page.reload();await waitState('Menu');check('Best score persists after reload',(await state()).best===best);
    check('Preferences persist after reload',(await state()).shake!==settingsBefore.shake&&(await state()).reduced!==settingsBefore.reduced);
    await page.setViewportSize({width:390,height:844});await page.waitForTimeout(1200);await page.screenshot({path:path.join(out,mode+'-portrait.png')});
    check('Portrait canvas fits viewport',await page.evaluate(()=>document.documentElement.scrollHeight===innerHeight&&document.documentElement.scrollWidth===innerWidth));
    await command('start');await waitState('Playing');await page.evaluate(()=>window.dispatchEvent(new Event('blur')));await waitState('Paused');check('Browser blur pauses safely',true);
    // Fresh context enables real browser touch events. This is emulation, not a physical device.
    const mobile=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true,deviceScaleFactor:1});const mp=await mobile.newPage();mp.on('pageerror',e=>errors.push(e.message));
    await mp.goto('http://127.0.0.1:8187/?qa=1');await mp.waitForFunction(()=>window.brickRushState?.state==='Menu',null,{timeout:180000});
    await mp.touchscreen.tap(195,501);await mp.waitForFunction(()=>window.brickRushState?.state==='Playing',null,{timeout:15000});
    const cdp=await mobile.newCDPSession(mp);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:100,y:650}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:280,y:650}]});await mp.waitForTimeout(400);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    const ms=await mp.evaluate(()=>window.brickRushState);check('Unified touch drag moves paddle',ms.paddle>1,ms);await mp.screenshot({path:path.join(out,mode+'-touch.png')});
    await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:200,y:700}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchCancel',touchPoints:[]});await mp.waitForFunction(()=>window.brickRushState?.state==='Paused');check('Touch interruption pauses safely',true);await mobile.close();
    check('No JavaScript or Unity runtime errors',errors.length===0,errors);
  }catch(error){errors.push(error.stack);await page.screenshot({path:path.join(out,mode+'-failure.png')}).catch(()=>{});}
  fs.writeFileSync(path.join(out,mode+'-qa.json'),JSON.stringify({browser:mode,version:browser.version(),checks,errors,warnings},null,2));await browser.close();if(errors.length)process.exitCode=1;
})();
