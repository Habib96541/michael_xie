const {chromium}=require('playwright');const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'),executable=process.env.HILO_BROWSER||'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',name=executable.includes('Chrome')?'Chrome':'Edge';
(async()=>{
 const browser=await chromium.launch({executablePath:executable,headless:true}),checks=[],errors=[];
 for(const [w,h,dpr] of [[844,390,2],[390,844,2],[768,1024,2],[1024,768,2],[844,390,3],[390,844,3]]){
  const context=await browser.newContext({viewport:{width:w,height:h},deviceScaleFactor:dpr,hasTouch:true}),page=await context.newPage();page.on('pageerror',e=>errors.push(String(e)));
  await page.goto('http://127.0.0.1:8765');await page.waitForFunction(()=>window.unityInstance&&window.hiloState);await page.waitForTimeout(300);
  const p=w/h<1.05,c=!p&&h<520,s=p?w/Math.max(420,800*w/h):h/(c?420:800);
  await page.touchscreen.tap(w/2,h/2+154*s*(c?.85:1));await page.waitForTimeout(250);
  const before=await page.evaluate(()=>window.hiloState);if(before.state!=='Decision')throw Error(name+' DPR '+dpr+' start failed');
  await page.screenshot({path:path.join(root,`Validation/screenshots/${name}-${w}x${h}-DPR${dpr}.png`)});
  const hi=before.higher>=before.lower,sw=Math.min(w/s-36,420),x=p?w/2+(hi?-1:1)*(sw+10)/4*s:w/2+(hi?(c?-220:-248):0)*s,y=p?h-173*s:h-(c?65:110)*s;
  await page.touchscreen.tap(x,y);await page.waitForTimeout(1000);await page.waitForFunction(()=>window.hiloState.state!=='Revealing');
  const after=await page.evaluate(()=>window.hiloState);if(BigInt(after.predictions)!==BigInt(before.predictions)+1n)throw Error(`${name} ${w}x${h} DPR${dpr}: touch control missed`);
  const canvas=await page.evaluate(()=>[document.querySelector('canvas').width,document.querySelector('canvas').height]);if(canvas[0]>w*2+2||canvas[1]>h*2+2)throw Error('Render pixel ratio cap failed');
  checks.push({width:w,height:h,dpr,canvas,passed:true});await context.close();
 }
 const buildHash=require('crypto').createHash('sha256').update(fs.readFileSync(path.join(root,'WebGL_Build/Build/WebGL_Build.wasm.unityweb'))).digest('hex');
 const report={testedAt:new Date().toISOString(),buildHash,browser:name,version:browser.version(),checks,errors};fs.writeFileSync(path.join(root,`Validation/density-${name.toLowerCase()}.json`),JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser.close();if(errors.length)process.exitCode=1;
})().catch(e=>{console.error(e);process.exit(1)});
