const {chromium,devices}=require('playwright');const fs=require('node:fs');const path=require('node:path');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true});const context=await browser.newContext({...devices['iPhone 13'],deviceScaleFactor:3});const page=await context.newPage();const errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'||/NullReferenceException|MissingReferenceException|RuntimeError|Exception:/.test(m.text()))errors.push(m.text());});
 await page.goto('http://127.0.0.1:8187/?qa=1');await page.waitForFunction(()=>window.brickRushState?.state==='Menu',null,{timeout:120000});
 await page.evaluate(()=>window.unityInstance.SendMessage('Brick Rush','QACommand','inspect'));const layout=await page.evaluate(()=>window.brickRushLayout);const c=await page.locator('canvas').boundingBox();const p=layout.buttons.find(b=>b.text.startsWith("LET'S RUSH")).rect;
 await page.touchscreen.tap(c.x+(p.x+p.width/2)*c.width/layout.width,c.y+(layout.height-p.y-p.height/2)*c.height/layout.height);await page.waitForFunction(()=>window.brickRushState?.state==='Playing');
 await page.evaluate(()=>{window.unityInstance.SendMessage('Brick Rush','QACommand','auto');window.qaFrames=[];let before=performance.now();function sample(now){if(window.brickRushState?.state==='Playing')window.qaFrames.push(now-before);before=now;requestAnimationFrame(sample);}requestAnimationFrame(sample);});
 const samples=[];
 for(let n=0;n<20;n++){
  if(n%4===0)await page.evaluate(()=>{for(const cmd of ['power:0','power:0','power:1','power:2','power:3','power:6'])window.unityInstance.SendMessage('Brick Rush','QACommand',cmd);});
  await page.waitForTimeout(1000);samples.push(await page.evaluate(()=>window.brickRushState));
 }
 const report=await page.evaluate(()=>{const frames=window.qaFrames.slice(120).sort((a,b)=>a-b),gl=document.querySelector('canvas').getContext('webgl2'),debug=gl.getExtension('WEBGL_debug_renderer_info');return {frames:frames.length,averageFPS:1000/(frames.reduce((a,b)=>a+b,0)/frames.length),p95FrameMs:frames[Math.floor(frames.length*.95)],p99FrameMs:frames[Math.floor(frames.length*.99)],renderer:debug?gl.getParameter(debug.UNMASKED_RENDERER_WEBGL):'unavailable',dpr:devicePixelRatio,canvasWidth:document.querySelector('canvas').width};});
 report.samples=samples;report.errors=errors;report.passed=errors.length===0&&report.averageFPS>=55&&samples.some(s=>s.balls===5);
 const out=path.resolve(__dirname,'../TestResults/iPhone');await page.screenshot({path:path.join(out,'multiball-stress.png')});fs.writeFileSync(path.join(out,'smoothness.json'),JSON.stringify(report,null,2));console.log({...report,samples:undefined});await browser.close();if(!report.passed)process.exitCode=1;
})();
