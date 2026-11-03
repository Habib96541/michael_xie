"""Additional compiled-browser checks for fullscreen and high-DPI touch."""
from pathlib import Path
import json
from playwright.sync_api import sync_playwright

out = Path(__file__).resolve().parents[1] / 'SprintDoc' / 'Tests'
passed = []
def check(value, message):
    if not value:
        raise AssertionError(message)
    passed.append('PASS: ' + message)
    print(passed[-1],flush=True)

with sync_playwright() as p:
    browser = p.chromium.launch(channel='chrome',args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
    context = browser.new_context(viewport={'width':1280,'height':720})
    page = context.new_page()
    page.goto('http://127.0.0.1:8765/?qa=1')
    page.wait_for_function('window.blitzTelemetry?.State === "Home"',timeout=90000)
    page.locator('#fullscreen').click()
    page.wait_for_function('document.fullscreenElement !== null')
    check(True,'Fullscreen enters from a user gesture')
    page.evaluate('document.exitFullscreen()')
    page.wait_for_function('document.fullscreenElement === null')
    page.wait_for_timeout(300)
    check(page.evaluate('window.blitzTelemetry.State') == 'Home','Fullscreen exit preserves game state')
    context.close()
    context = browser.new_context(viewport={'width':360,'height':800},device_scale_factor=2,has_touch=True,is_mobile=True)
    page = context.new_page()
    page.goto('http://127.0.0.1:8765/?qa=1')
    page.wait_for_function('window.blitzTelemetry?.State === "Home"',timeout=90000)
    cdp = context.new_cdp_session(page)
    cdp.send('Input.dispatchTouchEvent',{'type':'touchStart','touchPoints':[{'x':180,'y':800*.5744,'id':1}]})
    page.wait_for_timeout(80)
    cdp.send('Input.dispatchTouchEvent',{'type':'touchEnd','touchPoints':[]})
    page.wait_for_function('window.blitzTelemetry.State === "Playing"')
    page.wait_for_function('window.blitzTelemetry.Slots.some(t => t.State === "Exposed" && !t.Bomb)')
    state = page.evaluate('window.blitzTelemetry')
    target = next(t for t in state['Slots'] if t['State'] == 'Exposed' and not t['Bomb'])
    height = page.locator('canvas').bounding_box()['height']
    render_height = page.locator('canvas').evaluate('(c) => c.height')
    check(target['Diameter'] * height / render_height >= 48,'DPR 2 target collider is at least 48 CSS pixels')
    page.touchscreen.tap(target['X']*360,target['Y']*800)
    page.wait_for_timeout(200)
    check(page.evaluate('window.blitzTelemetry.Pending') == target['Reward'],'DPR 2 touch coordinates hit the rendered target')
    page.screenshot(path=str(out / 'gameplay-360x800-dpr2.png'))
    memory = page.evaluate('({jsHeap:performance.memory?.usedJSHeapSize,wasmHeap:window.blitzInstance?.Module?.HEAPU8?.byteLength})')
    (out / 'BrowserExtras.txt').write_text('\n'.join(passed) + '\nObserved memory sample: ' + json.dumps(memory),encoding='utf-8')
    browser.close()
