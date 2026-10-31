const {chromium}=require('playwright');const fs=require('node:fs');const path=require('node:path');
(async()=>{
  const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-unsafe-swiftshader']});
  const context=await browser.newContext({viewport:{width:390,height:844},deviceScaleFactor:3,isMobile:true,hasTouch:true});const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8187/?qa=1');await page.waitForFunction(()=>window.brickRushState?.state==='Menu',null,{timeout:180000});
  await page.touchscreen.tap(195,501);await page.waitForFunction(()=>window.brickRushState?.state==='Playing',null,{timeout:15000});
  const cdp=await context.newCDPSession(page);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:100,y:700}]});await page.waitForTimeout(100);await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:280,y:700}]});await page.waitForTimeout(500);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
  const result=await page.evaluate(()=>({state:window.brickRushState,dpr:devicePixelRatio,canvasWidth:document.querySelector('canvas').width,canvasHeight:document.querySelector('canvas').height,scrolls:document.documentElement.scrollHeight>innerHeight}));
  result.errors=errors;result.passed=result.state.paddle>1&&result.canvasWidth===624&&!result.scrolls&&errors.length===0;
  const out=path.resolve(__dirname,'../TestResults');await page.screenshot({path:path.join(out,'high-dpi-touch.png')});fs.writeFileSync(path.join(out,'high-dpi-touch.json'),JSON.stringify(result,null,2));console.log(result);await browser.close();if(!result.passed)process.exitCode=1;
})();
