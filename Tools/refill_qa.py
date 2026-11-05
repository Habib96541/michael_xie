"""Touch-only risk selection, actual loss, free refill and restart on 360px UI."""
import asyncio
import json
from playwright.async_api import async_playwright
from overhaul_qa import URL,OUT,state,until,click,audit_layout

async def main():
    async with async_playwright() as p:
        browser=await p.chromium.launch(channel='chrome',headless=True,args=['--enable-webgl','--ignore-gpu-blocklist','--enable-unsafe-swiftshader'])
        context=await browser.new_context(viewport={'width':360,'height':800},device_scale_factor=2,is_mobile=True,has_touch=True)
        page=await context.new_page();errors=[]
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.on('console',lambda m:errors.append(m.text) if m.type=='error' else None)
        try:
            await page.goto(URL,wait_until='domcontentloaded');await until(page,'window.chickenRushState.version === "2.0.0"',120000)
            await audit_layout(page,360,800,'Portrait')
            for value in range(2,11):
                await click(page,'plus',True);await until(page,f'window.chickenRushState.stake === {value}')
            await click(page,'hop',True);await until(page,'window.chickenRushState.state === "Preparing"');await until(page,'window.chickenRushState.state === "Safe"')
            assert (await state(page))['balance']==0
            for attempt in range(6):
                await until(page,'window.chickenRushState.nextVehicleSeconds>.12 && window.chickenRushState.nextVehicleSeconds<.26',20000)
                await click(page,'hop',True);await until(page,'window.chickenRushState.state === "Hopping"')
                result=await until(page,'["Lost","Safe"].includes(window.chickenRushState.state)')
                if result['state']=='Lost':break
            assert result['state']=='Lost'
            await until(page,'window.chickenRushState.hopEnabled')
            await audit_layout(page,360,800,'Portrait')
            await page.screenshot(path=str(OUT/'360x800-FreeCoins.png'))
            await click(page,'hop',True);refilled=await until(page,'window.chickenRushState.balance === 10')
            assert refilled['state']=='Lost' and refilled['stake']==1
            await click(page,'hop',True);await until(page,'window.chickenRushState.state === "Preparing"');await until(page,'window.chickenRushState.state === "Safe"')
            assert (await state(page))['balance']==9
            history=await page.evaluate('window.chickenRushHistory')
            clipped=sorted(set(h['ui']['clippedTextNames'] for h in history if h['ui']['overflowingTexts']))
            assert not clipped,clipped
            assert not errors,errors
            result=dict(resolution='360x800',input='emulated touch',riskSelected=10,actualCollision=True,freeRefill=10,restartStake=1,
                noTruncatedText=True,consoleErrors=errors)
            (OUT/'RefillQA.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
        except Exception:
            await page.screenshot(path=str(OUT/'Refill-FAILED.png'))
            (OUT/'Refill-failure.json').write_text(json.dumps(dict(state=await state(page),errors=errors),indent=2))
            raise
        finally:await browser.close()

if __name__=='__main__':asyncio.run(main())
