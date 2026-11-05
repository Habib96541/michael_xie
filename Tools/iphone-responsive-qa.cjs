const {chromium,devices}=require('playwright');const fs=require('node:fs');const path=require('node:path');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true});const results=[];const out=path.resolve(__dirname,'../TestResults/iPhone/browser');fs.mkdirSync(out,{recursive:true});
 const profiles=[['SE-small',320,568,2],['SE-2',375,667,2],['12-mini',375,812,3],['12',390,844,3],['12-Pro-Max',428,926,3],['14-Pro',393,852,3],['15-Pro-Max',430,932,3]];
 for(const [name,width,height,dpr] of profiles){
  const context=await browser.newContext({...devices['iPhone 13'],viewport:{width,height},deviceScaleFactor:dpr});const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8187/?qa=1');await page.waitForFunction(()=>window.brickRushState?.state==='Menu',null,{timeout:120000});
  const inspect=async()=>{await page.evaluate(()=>{window.brickRushLayout=null;window.unityInstance.SendMessage('Brick Rush','QACommand','inspect')});await page.waitForFunction(()=>window.brickRushLayout);return page.evaluate(()=>{const l=window.brickRushLayout,c=document.querySelector('canvas').getBoundingClientRect();const rect=r=>({x:c.x+r.x*c.width/l.width,y:c.y+(l.height-r.y-r.height)*c.height/l.height,width:r.width*c.width/l.width,height:r.height*c.height/l.height});return {...l,safe:rect(l.safe),arena:rect(l.arena),buttons:l.buttons.map(b=>({...b,rect:rect(b.rect)}))};});};
  const tap=async text=>{const layout=await inspect();const b=layout.buttons.find(b=>b.text.trim()===text.trim());if(!b)throw new Error(name+': missing '+text+' on '+layout.page);await page.touchscreen.tap(b.rect.x+b.rect.width/2,b.rect.y+b.rect.height/2);await page.waitForTimeout(300);};
  const wait=async(state)=>page.waitForFunction(s=>window.brickRushState?.state===s,state,{timeout:15000});
  const audit=async label=>{const l=await inspect();const fullscreen=await page.locator('#fullscreen').boundingBox();for(const b of l.buttons){const r=b.rect,s=l.safe;if(r.x<s.x-1||r.y<s.y-1||r.x+r.width>s.x+s.width+1||r.y+r.height>s.y+s.height+1)throw new Error(name+' '+label+': clipped '+b.text);if(r.height<43.5)throw new Error(name+' '+label+': small touch target '+r.height);if(fullscreen&&r.x<fullscreen.x+fullscreen.width&&r.x+r.width>fullscreen.x&&r.y<fullscreen.y+fullscreen.height&&r.y+r.height>fullscreen.y)throw new Error(name+': fullscreen overlaps '+b.text);}
    if(Math.abs(l.arena.width/l.arena.height-9.9/14.6)>.002)throw new Error(name+': distorted arena');await page.screenshot({path:path.join(out,name+'-'+label+'.png')});return l;};
  try{
   for(const landscape of [false,true]){
    await page.setViewportSize({width:landscape?height:width,height:landscape?width:height});await page.waitForTimeout(600);const mode=landscape?'landscape':'portrait';await audit(mode+'-menu');
    await tap("LET'S RUSH   >");await wait('Countdown');await tap('II');await wait('Paused');const remaining=await page.evaluate(()=>window.brickRushState.remaining);await audit(mode+'-pause');await page.waitForTimeout(500);if((await page.evaluate(()=>window.brickRushState.remaining))!==remaining)throw new Error('Paused timer moved');
    await tap('SETTINGS');await audit(mode+'-settings');await tap('ALL SET   >');await tap('BACK TO THE RUSH   >');await wait('Playing');
    const l=await inspect(),c=l.arena;const session=await context.newCDPSession(page);const y=c.y+c.height*.90;await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:c.x+c.width*.25,y}]});await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:c.x+c.width*.75,y}]});await page.waitForTimeout(400);await session.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    if((await page.evaluate(()=>window.brickRushState.paddle))<1)throw new Error('Touch drag failed');await tap('II');await wait('Paused');await tap('MAIN MENU');await wait('Menu');await tap('HOW TO PLAY');await audit(mode+'-help');await tap('GOT IT   >');
    // Arrange timer expiry to inspect the results layout without a 90-second wait per device.
    await page.evaluate(()=>window.unityInstance.SendMessage('Brick Rush','QACommand','start'));await wait('Playing');await page.evaluate(()=>window.unityInstance.SendMessage('Brick Rush','QACommand','expire'));await wait('Results');await audit(mode+'-results');await tap('MAIN MENU');await wait('Menu');
    results.push({device:name,orientation:mode,passed:true});console.log('PASS',name,mode);
   }
  }catch(e){results.push({device:name,passed:false,error:e.stack});console.log('FAIL',name,e.message);await page.screenshot({path:path.join(out,name+'-failure.png')});}
  if(errors.length)results.push({device:name,passed:false,errors});await context.close();
 }
 fs.writeFileSync(path.join(out,'results.json'),JSON.stringify(results,null,2));await browser.close();if(results.some(r=>!r.passed))process.exitCode=1;
})();
