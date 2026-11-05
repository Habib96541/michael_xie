const {chromium}=require('playwright');
const fs=require('fs'),path=require('path');
const {sleep,status,wait,tap,boot,play}=require('./helpers.cjs');
const output=path.resolve(__dirname,'../../Reports',process.env.HP_REPORT_DIR||'.');fs.mkdirSync(output,{recursive:true});
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.HP_BROWSER||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
  const report={browser:await browser.version(),scenarios:[],errors:[],warnings:[]};
  const sizes=[{name:'desktop',width:1440,height:900},{name:'portrait',width:390,height:844,touch:true},{name:'narrow',width:320,height:568,touch:true},{name:'landscape',width:844,height:390,touch:true},{name:'square',width:600,height:600},{name:'tablet',width:800,height:600,touch:true},{name:'retina',width:390,height:844,touch:true,dpr:2}];
  try{
    for(const size of sizes.filter(s=>!process.env.HP_VIEWPORT||s.name===process.env.HP_VIEWPORT)){
      const context=await browser.newContext({viewport:{width:size.width,height:size.height},deviceScaleFactor:size.dpr||1,hasTouch:!!size.touch,isMobile:!!size.touch});
      const page=await context.newPage();page.on('pageerror',e=>report.errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());if(m.type()==='warning'&&!report.warnings.includes(m.text()))report.warnings.push(m.text());});
      await boot(page);await play(page,size.touch);
      const initial=await page.evaluate(()=>({status:window.hydraulicStatus,scroll:document.documentElement.scrollHeight>innerHeight,canvas:{width:document.querySelector('canvas').width,height:document.querySelector('canvas').height}}));
      if(initial.scroll)throw Error('Unexpected browser scrolling');
      if(initial.canvas.width!==size.width*(size.dpr||1)||initial.canvas.height!==size.height*(size.dpr||1))throw Error('DPI mismatch');
      await page.screenshot({path:path.join(output,size.name+'.png')});
      await tap(page,'Crush',size.touch);await wait(page,()=>window.hydraulicStatus.busy);
      // Repeat real input on disabled controls to exercise the UI lock.
      for(let i=0;i<5;i++)for(const name of ['Crush','Collect']){
        const b=(await status(page)).buttons.find(b=>b.name===name);
        if(size.touch)await page.touchscreen.tap(b.x*size.width,b.y*size.height);else await page.mouse.click(b.x*size.width,b.y*size.height);
      }
      await wait(page,()=>!window.hydraulicStatus.busy);const resolved=await status(page);
      if(resolved.crushed>1||resolved.bank!==initial.status.bank)throw Error('Input lock failed');
      if(resolved.state==='Ready'){
        await tap(page,'Collect',size.touch);await wait(page,()=>window.hydraulicStatus.state==='Collected');
        if((await status(page)).bank!==initial.status.bank+112)throw Error('Wrong payout');
      }else if(resolved.state!=='Failed')throw Error('Unexpected crush result');
      await page.screenshot({path:path.join(output,size.name+'-result.png')});
      await tap(page,'Replay',size.touch);await wait(page,()=>window.hydraulicStatus.state==='Ready'&&!window.hydraulicStatus.busy);
      await page.setViewportSize({width:size.height,height:size.width});await sleep(400);await page.screenshot({path:path.join(output,size.name+'-rotated.png')});
      await page.setViewportSize({width:size.width,height:size.height});await sleep(350);
      await tap(page,'PauseButton',size.touch);await wait(page,()=>window.hydraulicStatus.navigation==='Paused');
      await tap(page,'NavigationSettings',size.touch);await wait(page,()=>window.hydraulicStatus.settings);
      await page.screenshot({path:path.join(output,size.name+'-settings.png')});
      await tap(page,'PowerSaving',size.touch);await tap(page,'PowerSaving',size.touch);
      await tap(page,'CloseSettings',size.touch);await play(page,size.touch);
      const bankBefore=(await status(page)).bank;await page.reload();await wait(page,()=>window.hydraulicStatus.navigation==='MainMenu',90000);
      const restored=(await status(page)).bank;if(restored!==bankBefore)throw Error('Persistence mismatch');
      report.scenarios.push({size,initial,resolved,restored});console.log(size.name+': passed');await context.close();
    }
    if(report.errors.length)throw Error(report.errors.join('\n'));report.passed=true;
  }catch(e){report.passed=false;report.failure=String(e);throw e;}
  finally{fs.writeFileSync(path.join(output,'browser-smoke.json'),JSON.stringify(report,null,2));await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
