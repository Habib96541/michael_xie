"""Exercise the real compiled player through mouse, keys and emulated touch.

No seed, result or currency injection; failed rounds are replayed normally.
Telemetry is opt-in and read-only; future outcomes are never exposed.
"""
import argparse
import asyncio
import json
from pathlib import Path
import statistics
from playwright.async_api import async_playwright

ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'QA/ReferenceRevision';OUT.mkdir(exist_ok=True)
parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8783);parser.add_argument('--quick',action='store_true');args=parser.parse_args()
CASES=[(1920,1080,'Desktop',1,False),(1600,900,'Desktop',1,False),(1366,768,'Desktop',1.25,False),(1280,720,'Desktop',1,False),
       (390,844,'Portrait',2,True),(360,800,'Portrait',2,True),(844,390,'Landscape',2,True),(768,1024,'Tablet',2,True)]
async def state(page):return await page.evaluate('window.chickenRushState')
async def until(page,condition,timeout=20000):
    await page.wait_for_function('window.chickenRushState && ('+condition+')',timeout=timeout);return await state(page)
async def click(page,name,touch=False):
    s=await state(page);r=next(r for r in s['ui']['controls'] if r['name']==name);box=await page.locator('#unity-canvas').bounding_box()
    x=box['x']+r['x']+r['width']/2;y=box['y']+r['y']+r['height']/2
    if touch:await page.touchscreen.tap(x,y)
    else:await page.mouse.click(x,y)
def overlaps(a,b):return a['x']<b['x']+b['width']-.5 and a['x']+a['width']>b['x']+.5 and a['y']<b['y']+b['height']-.5 and a['y']+a['height']>b['y']+.5
async def layout(page,w,h,mode):
    await until(page,f'window.chickenRushState.ui.layout==="{mode}" && Math.abs(window.chickenRushState.ui.width-{w})<2')
    await page.wait_for_timeout(200);s=await state(page);assert s['ui']['overflowingTexts']==0,('Clipped text',s['ui'])
    box=await page.locator('#unity-canvas').bounding_box();assert abs(box['width']-w)<1 and abs(box['height']-h)<1
    scroll=await page.evaluate('({w:document.documentElement.scrollWidth,h:document.documentElement.scrollHeight})');assert scroll['w']<=w+1 and scroll['h']<=h+1
    rects={r['name']:r for r in s['ui']['controls']}
    for name in ['hop','cash','settings','difficulty']:
        r=rects[name];assert min(r['width'],r['height'])>=43.5,(name,r)
        assert r['x']>=-.5 and r['y']>=-.5 and r['x']+r['width']<=w+.5 and r['y']+r['height']<=h+.5,(name,r)
    assert not overlaps(rects['hop'],rects['cash'])
    assert not overlaps(rects['hop'],s['ui']['viewport']) and not overlaps(rects['cash'],s['ui']['viewport'])
    return s
async def start(page,touch):
    s=await until(page,'window.chickenRushState.hopEnabled')
    if s['balance']==0:
        await click(page,'hop',touch);await until(page,'window.chickenRushState.balance===10')
    await click(page,'hop',touch);paid=await until(page,'window.chickenRushState.state==="Preparing"')
    await click(page,'hop',touch);s=await until(page,'window.chickenRushState.state==="Safe"')
    assert s['row']==0 and s['balance']==paid['balance'],'Duplicate start'
    return s
async def hop(page,touch,guards=False,key=False,road=False):
    old=await until(page,'window.chickenRushState.state==="Safe"')
    if key:await page.keyboard.press('ArrowRight')
    elif road:
        box=await page.locator('#unity-canvas').bounding_box();v=old['ui']['viewport'];x=box['x']+v['x']+v['width']*.6;y=box['y']+v['y']+v['height']*.55
        if touch:await page.touchscreen.tap(x,y)
        else:await page.mouse.click(x,y)
    else:await click(page,'hop',touch)
    await until(page,'window.chickenRushState.state==="Hopping"')
    if guards:
        await click(page,'cash',touch);await click(page,'hop',touch)
        if not touch:await page.keyboard.press('c')
        assert (await state(page))['balance']==old['balance'],'Mid-hop payout'
    s=await until(page,'["Safe","Lost","CashedOut"].includes(window.chickenRushState.state)')
    if s['state']=='Lost':
        assert s['row']==old['row'] and s['balance']==old['balance'] and s['impactContact'],'Loss without visible contact'
        await page.wait_for_timeout(150);assert (await state(page))['animation']=='Hit'
    else:
        assert s['row']==old['row']+1 and abs(s['x']-old['x']-2.4)<.01 and abs(s['y'])<.001,'Wrong crossing distance'
        assert s['multiplier']>old['multiplier'] and not s['impactContact'],'Winning collision'
    return s
async def bank(page,touch):
    old=await state(page);await click(page,'cash',touch);s=await until(page,'window.chickenRushState.state==="CashedOut"')
    assert s['balance']==old['balance']+s['reward'];await click(page,'cash',touch)
    if not touch:await page.keyboard.press('c')
    await page.wait_for_timeout(180);after=await state(page);assert after['balance']==s['balance'] and after['animation']=='Celebrate'
    return after
async def run_case(browser,case,advanced):
    w,h,mode,dpr,touch=case;context=await browser.new_context(viewport={'width':w,'height':h},device_scale_factor=dpr,is_mobile=touch,has_touch=touch)
    page=await context.new_page();errors=[]
    page.on('pageerror',lambda e:errors.append(str(e)));page.on('console',lambda m:errors.append(m.text) if m.type=='error' else None)
    result={'width':w,'height':h,'mode':mode,'dpr':dpr,'touch':touch};name=f'{w}x{h}'
    try:
        await page.goto(f'http://127.0.0.1:{args.port}/?qa=1',wait_until='domcontentloaded');s=await until(page,'window.chickenRushState.version==="3.0.0"',120000)
        assert 'nextLaneSafe' not in s,'Future outcome leaked';await layout(page,w,h,mode);await page.screenshot(path=str(OUT/f'{name}-Menu.png'))
        before=await state(page);await page.wait_for_timeout(350);after=await state(page)
        movement=[b['y']-a['y'] for a,b in zip(before['vehicles'],after['vehicles']) if a['visible'] and b['visible'] and abs(b['y']-a['y'])<4]
        assert any(d>.01 for d in movement) and any(d<-.01 for d in movement),'Missing opposing traffic'
        for a,b in zip(before['vehicles'],after['vehicles']):
            if a['visible'] and b['visible'] and .01<abs(b['y']-a['y'])<4:assert (b['y']-a['y'])*b['direction']>0,'Vehicle moving against lane direction'
        losses=0
        for attempt in range(24):
            await start(page,touch)
            if attempt==0:
                await click(page,'settings',touch);frozen=await until(page,'window.chickenRushState.paused');await page.wait_for_timeout(250);still=await state(page)
                assert abs(still['animationTime']-frozen['animationTime'])<.02 and still['vehicles']==frozen['vehicles'],'Pause failed'
                assert still['ui']['overflowingTexts']==0,still['ui'];await click(page,'resume',touch);await until(page,'!window.chickenRushState.paused')
            s=await hop(page,touch,guards=True,key=advanced)
            if s['state']=='Lost':losses+=1;continue
            break
        else:raise AssertionError('No first-hop success after 24 rounds')
        await page.wait_for_timeout(300);s=await layout(page,w,h,mode);await page.screenshot(path=str(OUT/f'{name}-Gameplay.png'))
        assert .20<(s['x']-s['cameraX']+s['worldWidth']/2)/s['worldWidth']<.35,'Chicken framing'
        assert s['worldWidth']*.74/2.4>=2.5,'Insufficient lookahead'
        if w==390:
            previous=s;await page.set_viewport_size({'width':844,'height':390});await layout(page,844,390,'Landscape');rotated=await state(page)
            assert rotated['row']==previous['row'] and rotated['balance']==previous['balance'];await page.screenshot(path=str(OUT/'Rotation-Landscape.png'))
            await page.set_viewport_size({'width':w,'height':h});await layout(page,w,h,mode)
        paid=await bank(page,touch);await layout(page,w,h,mode);await page.screenshot(path=str(OUT/f'{name}-Celebrate.png'))
        if advanced:
            # Select Easy through real controls, then keep trying normal rounds
            # to validate danger, progression and lane recycling beyond lane 12.
            await until(page,'window.chickenRushState.hopEnabled')
            chances={}
            for _ in range(4):
                mode_state=await state(page);chances[mode_state['difficulty']]=mode_state['nextChance'];await click(page,'difficulty',False);await page.wait_for_timeout(160)
            assert set(chances)=={'EASY','MEDIUM','HARD','HARDCORE'},chances
            assert chances['EASY']>chances['MEDIUM']>chances['HARD']>chances['HARDCORE']
            for _ in range(4):
                if (await state(page))['difficulty']=='EASY':break
                await click(page,'difficulty',False);await page.wait_for_timeout(140)
            highest=0;hit_seen=False
            for attempt in range(18):
                await start(page,False)
                while True:
                    s=await hop(page,False,key=True,road=False)
                    highest=max(highest,s['row'])
                    if s['state']=='Lost':
                        hit_seen=True;await page.screenshot(path=str(OUT/'Desktop-Hit.png'));await page.wait_for_timeout(700);await page.screenshot(path=str(OUT/'Desktop-LossRest.png'));break
                    if s['row']==4:
                        await page.wait_for_timeout(180);await page.screenshot(path=str(OUT/'Desktop-Danger.png'))
                    if s['row']>=13:
                        await page.wait_for_timeout(250);await page.screenshot(path=str(OUT/'Desktop-ExtendedRoad.png'));await bank(page,False);break
                if highest>=13 and hit_seen:break
            assert highest>=13 and hit_seen,('Missing deep progression/loss coverage',highest,hit_seen)
            result.update(highestLane=highest,visibleLossVerified=hit_seen)
        samples=[]
        for _ in range(20):samples.append((await state(page))['fps']);await page.wait_for_timeout(85)
        s=await state(page);assert len(s['vehicles'])==36,'Traffic pool grew';assert not errors,errors
        # Saved coins survive an actual reload.
        saved=s['balance'];await page.reload();await until(page,'window.chickenRushState.version==="3.0.0"',120000);assert (await state(page))['balance']==saved
        heap=await page.evaluate('window.unityInstance?.Module?.HEAP8?.buffer?.byteLength || null')
        raf=await page.evaluate('''() => new Promise(resolve=>{let frames=0,start;function sample(t){if(start===undefined)start=t;frames++;if(t-start>=1800)resolve((frames-1)*1000/(t-start));else requestAnimationFrame(sample);}requestAnimationFrame(sample);})''')
        result.update(status='Passed',fpsMedian=round(statistics.median(samples),1),browserRafHz=round(raf,1),wasmHeapMiB=round(heap/1048576,1) if heap else None,firstHopLosses=losses,pooledVehicles=36,errors=errors)
    except Exception as error:
        await page.screenshot(path=str(OUT/f'{name}-FAIL.png'));result.update(status='Failed',error=str(error),state=await state(page),errors=errors);raise
    finally:
        (OUT/f'{name}-Result.json').write_text(json.dumps(result,indent=2));await context.close()
    return result
async def main():
    results=[]
    async with async_playwright() as p:
        browser=await p.chromium.launch(channel='chrome',headless=True)
        version=browser.version
        try:
            for i,case in enumerate(CASES[:1] if args.quick else CASES):
                result=await run_case(browser,case,i==0);results.append(result);print(json.dumps(result),flush=True)
        finally:
            (OUT/'BrowserQA.json').write_text(json.dumps({'browser':'Chrome '+version,'cases':results,'touchScope':'Chrome device emulation, not physical mobile hardware'},indent=2));await browser.close()
asyncio.run(main())
