const {chromium}=require('playwright');
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'),executable=process.env.HILO_BROWSER||'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const name=executable.includes('Chrome')?'Chrome':'Edge';
(async()=>{
  const browser=await chromium.launch({executablePath:executable,headless:true});
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',e=>errors.push(String(e)));
  await page.goto('http://127.0.0.1:8765');await page.waitForFunction(()=>window.unityInstance&&window.hiloState);await page.waitForTimeout(500);
  const state=()=>page.evaluate(()=>window.hiloState);
  const amount=s=>{const [m,e]=s.balance.split('|').map(Number);return m*10**e;};
  const primary=async()=>{await page.mouse.click(640,498.6);await page.waitForTimeout(100);};
  await primary();await page.mouse.click(1099,39.6);await page.waitForTimeout(100);await page.mouse.click(640,390.6);await page.waitForTimeout(100);await page.keyboard.press('Escape');await page.waitForTimeout(100);
  let rounds=1,predictions=0;
  for(let i=0;i<200;i++){
    let s=await state();if(s.state==='Ended'){if(amount(s)<10)break;await primary();rounds++;s=await state();}
    const higher=s.higher>0&&(s.lower===0||s.higher<=s.lower);const before=BigInt(s.predictions);
    await page.keyboard.press(higher?'ArrowUp':'ArrowDown');await page.waitForTimeout(160);await page.waitForFunction(()=>window.hiloState.state!=='Revealing');s=await state();
    if(BigInt(s.predictions)!==before+1n)throw Error('Keyboard prediction was not accepted once');predictions++;
  }
  const broke=await state();if(amount(broke)>=10||broke.state!=='Ended')throw Error('Bankruptcy was not reached');
  await page.screenshot({path:path.join(root,`Validation/screenshots/${name}-bankrupt.png`)});
  await primary();const refilled=await state();if(Math.abs(amount(refilled)-amount(broke)-100)>1e-8)throw Error('Free refill did not add 100 coins');
  await page.screenshot({path:path.join(root,`Validation/screenshots/${name}-refilled.png`)});
  await primary();const restarted=await state();if(restarted.state!=='Decision')throw Error('Restart after refill failed');
  const buildHash=require('crypto').createHash('sha256').update(fs.readFileSync(path.join(root,'WebGL_Build/Build/WebGL_Build.wasm.unityweb'))).digest('hex');
  const report={testedAt:new Date().toISOString(),buildHash,browser:name,version:browser.version(),rounds,predictions,bankruptBalance:broke.balance,refilledBalance:refilled.balance,restartedBalance:restarted.balance,passed:errors.length===0,errors};
  fs.writeFileSync(path.join(root,`Validation/refill-${name.toLowerCase()}.json`),JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser.close();if(errors.length)process.exitCode=1;
})().catch(e=>{console.error(e);process.exit(1)});
