const {chromium}=require('playwright');
const fs=require('node:fs');const path=require('node:path');
(async()=>{
  const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
  const page=await browser.newPage({viewport:{width:1440,height:960}});const errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'||/NullReferenceException|MissingReferenceException|TypeLoadException|RuntimeError|Exception:|Assertion failed|Shader error/.test(m.text()))errors.push(m.text());});
  await page.goto('http://127.0.0.1:8187/?qa=1');await page.waitForFunction(()=>window.brickRushState?.state==='Menu',null,{timeout:180000});
  await page.evaluate(()=>{
    const gl=document.querySelector('canvas').getContext('webgl2'),debug=gl.getExtension('WEBGL_debug_renderer_info');
    window.qaRenderer=debug?gl.getParameter(debug.UNMASKED_RENDERER_WEBGL):'not exposed';window.qaFrames=[];let last=performance.now();
    function frame(now){if(window.brickRushState?.state==='Playing')window.qaFrames.push(now-last);last=now;requestAnimationFrame(frame);}requestAnimationFrame(frame);
    window.unityInstance.SendMessage('Brick Rush','QACommand','auto');
  });
  await page.mouse.click(720,610,{delay:80});
  const samples=[];const started=Date.now();
  for(let i=0;i<110;i++){await page.waitForTimeout(1000);const s=await page.evaluate(()=>window.brickRushState);samples.push(s);if(i%10===0)console.log('SESSION',i,s.state,Math.round(s.remaining),s.score,s.wave,s.balls);if(s.state==='Results')break;}
  const result=await page.evaluate(()=>{
    const frames=window.qaFrames.slice(120).sort((a,b)=>a-b);let sum=frames.reduce((a,b)=>a+b,0);return {state:window.brickRushState,renderer:window.qaRenderer,frames:frames.length,averageFPS:1000/(sum/frames.length),p95FrameMs:frames[Math.floor(frames.length*.95)],p99FrameMs:frames[Math.floor(frames.length*.99)]};
  });
  result.wallSeconds=(Date.now()-started)/1000;result.samples=samples;result.errors=errors;
  const out=path.resolve(__dirname,'../TestResults');fs.writeFileSync(path.join(out,'natural-session.json'),JSON.stringify(result,null,2));await page.screenshot({path:path.join(out,'natural-session-results.png')});console.log(JSON.stringify({...result,samples:undefined},null,2));await browser.close();if(errors.length||result.state.state!=='Results'||result.state.remaining>0||result.state.score===0)process.exitCode=1;
})();
