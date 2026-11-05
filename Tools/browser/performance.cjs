const {chromium}=require('playwright');
const fs=require('fs');
const {boot,play,tap}=require('./helpers.cjs');
(async()=>{
  const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
  const viewport=process.env.HP_PHONE?{width:390,height:844}:{width:1280,height:720};
  const dpr=process.env.HP_DPR?Number(process.env.HP_DPR):process.env.HP_PHONE?3:1;
  const page=await browser.newPage({viewport,deviceScaleFactor:dpr});
  await page.addInitScript(()=>{
    const original=window.requestAnimationFrame.bind(window);
    window.hpFrames=[];window.hpMeasuring=false;
    window.requestAnimationFrame=callback=>original(time=>{if(window.hpMeasuring)window.hpFrames.push(time);callback(time)});
  });
  await boot(page);await play(page);await tap(page,'Crush');
  await page.waitForTimeout(2500);
  await page.evaluate(()=>{window.hpFrames=[];window.hpMeasuring=true});
  let outcomes=[];
  for(let i=0;i<6;i++){
    const state=await page.evaluate(()=>window.hydraulicStatus);
    if(state.state==='Failed') {await tap(page,'Replay');await page.waitForTimeout(400);}
    await tap(page,'Crush');await page.waitForTimeout(2400);
    outcomes.push(await page.evaluate(()=>window.hydraulicStatus));
  }
  const timing=await page.evaluate(()=>{
    window.hpMeasuring=false;const raw=window.hpFrames;const frames=[...new Set(raw)];const intervals=frames.slice(1).map((t,i)=>t-frames[i]).filter(v=>v>0).sort((a,b)=>a-b);
    const gl=document.querySelector('canvas').getContext('webgl2');const debug=gl?.getExtension('WEBGL_debug_renderer_info');
    return {callbackCount:raw.length,uniqueFrameTimestamps:frames.length,durationSeconds:(frames.at(-1)-frames[0])/1000,meanFramePacing:(frames.length-1)*1000/(frames.at(-1)-frames[0]),p50Ms:intervals[Math.floor(intervals.length*.5)],p95Ms:intervals[Math.floor(intervals.length*.95)],maxMs:intervals.at(-1),renderer:debug?gl.getParameter(debug.UNMASKED_RENDERER_WEBGL):'unavailable'};
  });
  const result={browser:await browser.version(),viewport,deviceScaleFactor:dpr,playerPixelRatio:Math.min(dpr,2),method:'Unity requestAnimationFrame callback intervals during six live crush/retry inputs, default graphics. Browser frame pacing, not a physical-device GPU benchmark.',timing,outcomes};
  fs.writeFileSync('Reports/'+(process.env.HP_REPORT_DIR||'.')+'/browser-performance'+(process.env.HP_PHONE?'-phone':'')+(dpr===2?'-dpr2':'')+'.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
