"""Actual WebGL input, geometry, orientation, progression and viewport checks.

?qa=1 only publishes read-only observations and a collision prediction. Every
action below is a real mouse, keyboard or emulated touch event; no outcomes,
currencies or Unity state are injected.
"""
import argparse
import asyncio
from collections import defaultdict
import json
from pathlib import Path
import statistics
from playwright.async_api import async_playwright

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'QA/Overhaul'
OUT.mkdir(exist_ok=True)
parser=argparse.ArgumentParser()
parser.add_argument('--port',type=int,default=8783)
parser.add_argument('--quick',action='store_true')
ARGS=parser.parse_args()
URL=f'http://127.0.0.1:{ARGS.port}/?qa=1'
CASES=[(1920,1080,'Desktop',1,False),(1366,768,'Desktop',1.25,False),(1280,720,'Desktop',1,False),
       (390,844,'Portrait',2,True),(360,800,'Portrait',2,True),(844,390,'Landscape',2,True),(768,1024,'Tablet',2,True)]

async def state(page):return await page.evaluate('window.chickenRushState')
async def until(page,condition,timeout=20000):
    await page.wait_for_function('window.chickenRushState && ('+condition+')',timeout=timeout)
    return await state(page)
async def click(page,name,touch=False):
    current=await state(page)
    rect=next(r for r in current['ui']['controls'] if r['name']==name)
    canvas=await page.locator('#unity-canvas').bounding_box()
    x=canvas['x']+rect['x']+rect['width']*.5
    y=canvas['y']+rect['y']+rect['height']*.5
    if touch:await page.touchscreen.tap(x,y)
    else:await page.mouse.click(x,y)

def overlaps(a,b):return a['x']<b['x']+b['width']-.1 and a['x']+a['width']>b['x']+.1 and a['y']<b['y']+b['height']-.1 and a['y']+a['height']>b['y']+.1

async def audit_layout(page,width,height,mode):
    s=await until(page,f'window.chickenRushState.ui.layout === "{mode}" && Math.abs(window.chickenRushState.ui.width-{width})<2')
    await page.wait_for_timeout(220)
    s=await state(page)
    assert 'clippedTextNames' in s['ui'],'Expected the final truncation-aware build'
    assert s['ui']['overflowingTexts']==0,('Overflowing UI text',width,height,s['ui'])
    box=await page.locator('#unity-canvas').bounding_box()
    assert abs(box['width']-width)<1 and abs(box['height']-height)<1,box
    scrolling=await page.evaluate('({w:document.documentElement.scrollWidth,h:document.documentElement.scrollHeight})')
    assert scrolling['w']<=width+1 and scrolling['h']<=height+1,scrolling
    rects={r['name']:r for r in s['ui']['controls']}
    for name in ['hop','cash','settings','difficulty']:
        r=rects[name]
        assert r['width']>=43.5 and r['height']>=43.5,(name,r)
        assert r['x']>=-.5 and r['y']>=-.5 and r['x']+r['width']<=width+.5 and r['y']+r['height']<=height+.5,(name,r)
    assert not overlaps(rects['hop'],rects['cash'])
    return s

def audit_traffic(before,after):
    moved_up=moved_down=False
    grouped=defaultdict(list)
    for first,second in zip(before['vehicles'],after['vehicles']):
        assert first['id']==second['id'] and abs(first['x']-second['x'])<.0001,'Horizontal vehicle movement'
        dy=second['y']-first['y']
        if abs(dy)<13 and abs(dy)>.001:
            assert dy*second['direction']>0,'Wrong vertical direction'
            moved_up|=dy>0;moved_down|=dy<0
        grouped[second['lane']].append(second)
    for cars in grouped.values():
        for i,a in enumerate(cars):
            for b in cars[i+1:]:
                assert abs(a['y']-b['y'])>(a['length']+b['length'])*.5-.01,'Overlapping cars'
    assert moved_up and moved_down,'Both traffic directions were not observed'
    lanes=after['lanes']
    assert lanes[0]['speed']<lanes[3]['speed']<lanes[6]['speed']<lanes[10]['speed']
    assert lanes[0]['count']<lanes[6]['count']<=lanes[10]['count']
    assert all(l['clearGap']>=after['hopSeconds']+.25 for l in lanes)

async def start_round(page,touch):
    s=await state(page)
    if s['state'] in ['Lost','CashedOut']:
        await until(page,'window.chickenRushState.hopEnabled')
    await click(page,'hop',touch)
    s=await until(page,'["Preparing","Menu","Lost","CashedOut"].includes(window.chickenRushState.state)')
    if s['state']!='Preparing':
        await click(page,'hop',touch)
        s=await until(page,'window.chickenRushState.state === "Preparing"')
    paid=s['balance']
    await click(page,'hop',touch)
    s=await until(page,'window.chickenRushState.state === "Safe"')
    assert s['balance']==paid,'Double-start charged twice'
    return s

async def safe_hop(page,touch,keyboard=False,road=False,guards=False):
    old=await until(page,'window.chickenRushState.state === "Safe" && window.chickenRushState.nextLaneSafe',30000)
    if keyboard:await page.keyboard.press('ArrowRight')
    elif road:
        canvas=await page.locator('#unity-canvas').bounding_box();v=old['ui']['viewport']
        x=canvas['x']+v['x']+v['width']*.56;y=canvas['y']+v['y']+v['height']*.50
        if touch:await page.touchscreen.tap(x,y)
        else:await page.mouse.click(x,y)
    else:await click(page,'hop',touch)
    hopping=await until(page,'window.chickenRushState.state === "Hopping"')
    if guards:
        if touch:await click(page,'cash',True)
        else:await page.keyboard.press('c')
        await click(page,'hop',touch)
        assert (await state(page))['balance']==old['balance'],'Mid-hop payout'
    result=await until(page,'["Safe","Lost","CashedOut"].includes(window.chickenRushState.state)')
    assert abs(result['y'])<.001,'Chicken moved vertically'
    assert result['state'] in ['Safe','CashedOut'],('Predicted safe crossing failed',old['row'],old['nextVehicleSeconds'],result)
    assert result['row']==old['row']+1,'One input advanced multiple lanes'
    assert result['x']>old['x']+1.54,'Chicken did not advance horizontally'
    return result

async def cash_out(page,touch):
    previous=await state(page);await click(page,'cash',touch)
    paid=await until(page,'window.chickenRushState.state === "CashedOut"')
    await page.wait_for_timeout(140)
    paid=await state(page)
    assert paid['animation']=='Celebrate',paid
    assert paid['balance']==previous['balance']+paid['reward'],'Wrong reward'
    await click(page,'cash',touch)
    if not touch:await page.keyboard.press('c')
    await page.wait_for_timeout(160)
    assert (await state(page))['balance']==paid['balance'],'Duplicate cash-out'
    return paid

async def run_case(browser,case,advanced=False):
    width,height,mode,dpr,touch=case
    context=await browser.new_context(viewport={'width':width,'height':height},device_scale_factor=dpr,is_mobile=touch,has_touch=touch)
    page=await context.new_page();errors=[];http_errors=[]
    page.on('pageerror',lambda e:errors.append(str(e)))
    page.on('console',lambda m:errors.append(m.text+' '+str(m.location)) if m.type=='error' else None)
    page.on('response',lambda r:http_errors.append(str(r.status)+' '+r.url) if r.status>=400 else None)
    name=f'{width}x{height}'
    try:
        await page.goto(URL,wait_until='domcontentloaded')
        await page.wait_for_function('window.chickenRushState || getComputedStyle(document.getElementById("error")).display !== "none"',timeout=120000)
        s=await state(page);assert s and s['version']=='2.0.0',('Wrong build or startup error',s,errors)
        s=await audit_layout(page,width,height,mode)
        await page.screenshot(path=str(OUT/f'{name}-Menu.png'))
        await page.wait_for_timeout(300);audit_traffic(s,await state(page))
        await start_round(page,touch)
        await click(page,'settings',touch);frozen=await until(page,'window.chickenRushState.paused')
        await page.wait_for_timeout(250);still=await state(page)
        assert abs(frozen['animationTime']-still['animationTime'])<.02 and frozen['x']==still['x']
        assert abs(frozen['vehicles'][0]['y']-still['vehicles'][0]['y'])<.001
        assert still['ui']['overflowingTexts']==0,('Modal text overflow',still['ui'])
        await click(page,'resume',touch);await until(page,'!window.chickenRushState.paused')
        await safe_hop(page,touch,guards=True)
        await page.wait_for_timeout(300)
        s=await audit_layout(page,width,height,mode)
        assert .25<(s['x']-s['cameraX']+s['worldWidth']*.5)/s['worldWidth']<.40,'Poor follow composition'
        assert s['worldWidth']*.68/1.55>=2,'Too little hazard look-ahead'
        await page.screenshot(path=str(OUT/f'{name}-Gameplay.png'))
        if name=='390x844':
            old=s;await page.set_viewport_size({'width':844,'height':390});await audit_layout(page,844,390,'Landscape')
            assert (await state(page))['row']==old['row'] and (await state(page))['balance']==old['balance']
            await page.screenshot(path=str(OUT/'Rotation-Landscape.png'))
            await page.set_viewport_size({'width':390,'height':844});await audit_layout(page,390,844,'Portrait')
        await safe_hop(page,touch,road=True)
        await cash_out(page,touch)
        if advanced:
            await page.screenshot(path=str(OUT/'Desktop-Celebrate.png'))
            await start_round(page,touch)
            for i in range(12):await safe_hop(page,False,keyboard=True)
            await page.wait_for_timeout(300)
            await page.screenshot(path=str(OUT/'Desktop-HighDanger.png'))
            advanced_state=await state(page)
            assert advanced_state['row']>=11 and advanced_state['cameraX']>15
            await cash_out(page,False)
            await page.screenshot(path=str(OUT/'Desktop-BigCashOut.png'))
            await start_round(page,False)
            for attempt in range(6):
                await until(page,'window.chickenRushState.state === "Safe" && window.chickenRushState.nextVehicleSeconds>.12 && window.chickenRushState.nextVehicleSeconds<.26',20000)
                await click(page,'hop',False)
                await until(page,'window.chickenRushState.state === "Hopping"')
                result=await until(page,'["Lost","Safe"].includes(window.chickenRushState.state)')
                if result['state']=='Lost':break
            assert result['state']=='Lost','A real collision was not observed'
            await page.wait_for_timeout(420);lost=await state(page);assert lost['animation']=='Hit'
            await page.keyboard.press('c');await page.keyboard.press('Space');await page.wait_for_timeout(120)
            assert (await state(page))['state']=='Lost'
            await page.screenshot(path=str(OUT/'Desktop-Hit.png'))
            await start_round(page,False)
        history=await page.evaluate('window.chickenRushHistory')
        observed=sorted(set(h['animation'] for h in history))
        clipped=sorted(set(h['ui']['clippedTextNames'] for h in history if h['ui']['overflowingTexts']))
        assert not clipped,('Text was clipped during a state transition',clipped)
        assert {'Idle','Walk','Hop','Celebrate'}.issubset(observed),observed
        if advanced:assert 'Hit' in observed
        assert len(set(len(h['vehicles']) for h in history))==1,'Vehicle pool grew during play'
        values=[h['fps'] for h in history if not h['paused'] and h['fps']<200]
        assert not errors and not http_errors,(errors,http_errors)
        return dict(resolution=name,layout=mode,dpr=dpr,browserVersion=browser.version,input='emulated touch' if touch else 'mouse and keyboard',
            checks=['full viewport','no scrolling','no overflowing UI text','44px primary targets','no button overlap','vertical traffic both directions',
            'no vehicle overlap','horizontal chicken hops','camera follows horizontally','pause/resume','one hop per input','cash-out guard','duplicate payout guard','celebration'],
            advancedLanes=12 if advanced else 2,animations=observed,poolSize=len(s['vehicles']),
            medianReportedFPS=round(statistics.median(values),1),nearMissesObserved=max(h['nearMisses'] for h in history),consoleErrors=errors,httpErrors=http_errors,worldWidth=s['worldWidth'],
            performanceNote='Headless workstation Chrome; not a physical-device performance certification.')
    except Exception:
        await page.screenshot(path=str(OUT/f'{name}-FAILED.png'))
        (OUT/f'{name}-failure.json').write_text(json.dumps(dict(state=await state(page),errors=errors,httpErrors=http_errors),indent=2))
        raise
    finally:await context.close()

async def main():
    async with async_playwright() as p:
        browser=await p.chromium.launch(channel='chrome',headless=True,args=['--enable-webgl','--ignore-gpu-blocklist','--enable-unsafe-swiftshader'])
        results=[]
        try:
            for index,case in enumerate(CASES[:1] if ARGS.quick else CASES):
                result=await run_case(browser,case,advanced=index==0)
                results.append(result);(OUT/'BrowserQA.json').write_text(json.dumps(results,indent=2));print(json.dumps(result),flush=True)
        finally:await browser.close()

if __name__=='__main__':asyncio.run(main())
