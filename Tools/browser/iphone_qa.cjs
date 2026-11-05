const {chromium,devices}=require('playwright');
const fs=require('fs'),path=require('path');
const {sleep,status,wait,tap,boot,play}=require('./helpers.cjs');
const output=path.resolve(__dirname,'../../Reports/2026-11-05');fs.mkdirSync(output,{recursive:true});
const profiles=[
  {name:'se2',width:375,height:667,dpr:2},
  {name:'12mini',width:375,height:812,dpr:3},
  {name:'12',width:390,height:844,dpr:3},
  {name:'12promax',width:428,height:926,dpr:3},
];
async function inspect(page,profile,label){
  await sleep(180);
  const result=await page.evaluate(()=>{
    const c=document.querySelector('canvas'),r=c.getBoundingClientRect();
    return {status:window.hydraulicStatus,canvas:{width:c.width,height:c.height,cssWidth:r.width,cssHeight:r.height},scroll:document.documentElement.scrollHeight>innerHeight||document.documentElement.scrollWidth>innerWidth};
  });
  const renderDpr=Math.min(profile.dpr,2);
  if(result.scroll)throw Error(label+': page scrolls');
  if(Math.abs(result.canvas.width-result.canvas.cssWidth*renderDpr)>2||Math.abs(result.canvas.height-result.canvas.cssHeight*renderDpr)>2)throw Error(label+': canvas resolution mismatch');
  result.renderDpr=renderDpr;
  for(const b of result.status.buttons){
    if(b.x-b.width/2<-.002||b.x+b.width/2>1.002||b.y-b.height/2<-.002||b.y+b.height/2>1.002)throw Error(label+': clipped '+b.name);
    if(b.width*result.canvas.cssWidth<43.9||b.height*result.canvas.cssHeight<43.9)throw Error(label+': small touch target '+b.name);
  }
  await page.screenshot({path:path.join(output,'web-'+label+'.png')});return result;
}
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.HP_BROWSER||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
  const report={browser:await browser.version(),method:'Chrome touch/DPR viewport emulation; Unity simulator supplies notch/safe-area coverage. This is not iOS Safari or physical-device performance.',scenarios:[],errors:[],warnings:[]};
  try{
    for(const profile of profiles){
      const context=await browser.newContext({userAgent:devices['iPhone 12'].userAgent,viewport:{width:profile.width,height:profile.height},deviceScaleFactor:profile.dpr,isMobile:true,hasTouch:true});
      const page=await context.newPage();
      page.on('pageerror',e=>report.errors.push(String(e)));
      page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());if(m.type()==='warning'&&!report.warnings.includes(m.text()))report.warnings.push(m.text());});
      await boot(page);const scenario={profile,views:[]};
      scenario.views.push(await inspect(page,profile,profile.name+'-portrait-main'));
      if((await status(page)).timeScale!==0)throw Error('Menu boot did not pause gameplay');
      await tap(page,'NavigationSettings',true);await wait(page,()=>window.hydraulicStatus.settings);
      scenario.views.push(await inspect(page,profile,profile.name+'-portrait-settings'));
      await tap(page,'CloseSettings',true);await wait(page,()=>!window.hydraulicStatus.settings);await play(page,true);
      scenario.views.push(await inspect(page,profile,profile.name+'-portrait-gameplay'));
      await tap(page,'Crush',true);await wait(page,()=>window.hydraulicStatus.busy);
      await tap(page,'PauseButton',true);await wait(page,()=>window.hydraulicStatus.navigation==='Paused');
      const paused=await status(page);await sleep(2000);const frozen=await status(page);
      if(!frozen.busy||frozen.state!=='Crushing'||frozen.crushed!==paused.crushed||frozen.timeScale!==0)throw Error('Mid-crush pause did not freeze');
      scenario.pause={paused,frozen};scenario.views.push(await inspect(page,profile,profile.name+'-portrait-pause'));
      await tap(page,'NavigationSettings',true);await wait(page,()=>window.hydraulicStatus.settings);
      const nativeBefore=await page.evaluate(()=>({w:document.querySelector('canvas').width,h:document.querySelector('canvas').height}));
      await tap(page,'PowerSaving',true);await sleep(350);
      const nativeAfter=await page.evaluate(()=>({w:document.querySelector('canvas').width,h:document.querySelector('canvas').height}));
      if(JSON.stringify(nativeBefore)!==JSON.stringify(nativeAfter))throw Error('Power saving blurred the canvas');
      await tap(page,'PowerSaving',true);await tap(page,'CloseSettings',true);
      if((await status(page)).navigation!=='Paused'||(await status(page)).timeScale!==0)throw Error('Closing settings resumed paused game');
      await tap(page,'MainMenu',true);await wait(page,()=>window.hydraulicStatus.navigation==='MainMenu');
      await tap(page,'PlayOrResume',true);await wait(page,()=>window.hydraulicStatus.navigation==='Playing'&&!window.hydraulicStatus.busy);
      const resolved=await status(page);scenario.resolved=resolved;
      if(resolved.bank!==paused.bank||resolved.crushed>1)throw Error('Resume changed bank or overlapped crushes');
      if(resolved.state==='Failed'){await tap(page,'Replay',true);await wait(page,()=>window.hydraulicStatus.state==='Ready'&&!window.hydraulicStatus.busy);}
      else if(resolved.state==='Ready'){
        await tap(page,'Collect',true);await wait(page,()=>window.hydraulicStatus.state==='Collected');
        if((await status(page)).bank!==paused.bank+112)throw Error('Incorrect cashout');
        await tap(page,'Replay',true);await wait(page,()=>window.hydraulicStatus.state==='Ready'&&!window.hydraulicStatus.busy);
      }else throw Error('Unexpected post-resume state');
      await page.setViewportSize({width:profile.height,height:profile.width});await sleep(450);
      scenario.views.push(await inspect(page,profile,profile.name+'-landscape-gameplay'));
      await tap(page,'PauseButton',true);await wait(page,()=>window.hydraulicStatus.navigation==='Paused');
      scenario.views.push(await inspect(page,profile,profile.name+'-landscape-pause'));
      await tap(page,'NavigationSettings',true);await wait(page,()=>window.hydraulicStatus.settings);
      scenario.views.push(await inspect(page,profile,profile.name+'-landscape-settings'));
      await tap(page,'CloseSettings',true);await tap(page,'MainMenu',true);await wait(page,()=>window.hydraulicStatus.navigation==='MainMenu');
      scenario.views.push(await inspect(page,profile,profile.name+'-landscape-main'));
      const bank=(await status(page)).bank;await page.reload();await wait(page,()=>window.hydraulicStatus.navigation==='MainMenu',90000);
      if((await status(page)).bank!==bank)throw Error('Save not restored after reload');
      report.scenarios.push(scenario);console.log(profile.name+': passed portrait/landscape, menus, mid-crush pause, resume, cashout/retry, native DPI and persistence');
      await context.close();
    }
    if(report.errors.length)throw Error(report.errors.join('\n'));
    report.passed=true;
  }catch(e){report.passed=false;report.failure=String(e);throw e;}
  finally{fs.writeFileSync(path.join(output,'browser-iphone.json'),JSON.stringify(report,null,2));await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
