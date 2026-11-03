"""Verify real tab visibility in a temporary visible Chrome test window."""
from pathlib import Path
from playwright.sync_api import sync_playwright

out = Path(__file__).resolve().parents[1] / 'SprintDoc' / 'Tests' / 'BrowserFocus.txt'
results = []
stage = 'startup'
try:
    with sync_playwright() as p:
        browser = p.chromium.launch(channel='chrome',headless=False,args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        context = browser.new_context(viewport={'width':1280,'height':720})
        page = context.new_page()
        context.new_cdp_session(page).send('Emulation.setFocusEmulationEnabled',{'enabled':False})
        page.goto('http://127.0.0.1:8765/?qa=1')
        page.wait_for_function('window.blitzTelemetry?.State === "Home"',timeout=90000)
        page.bring_to_front()
        page.mouse.click(640,720*.5744,delay=60)
        page.wait_for_function('window.blitzTelemetry.State === "Playing"')
        before = page.evaluate('window.blitzTelemetry.Time')
        other = context.new_page()
        context.new_cdp_session(other).send('Emulation.setFocusEmulationEnabled',{'enabled':False})
        other.goto('about:blank')
        other.bring_to_front()
        stage = 'visibility'
        page.wait_for_function('document.hidden',polling=100,timeout=5000)
        stage = 'pause'
        results.append('PASS: Actual Chrome tab switch hides the game page')
        frozen = page.evaluate('window.blitzTelemetry.Time')
        other.wait_for_timeout(800)
        assert page.evaluate('window.blitzTelemetry.Time') == frozen
        page.bring_to_front()
        page.wait_for_function('window.blitzTelemetry.Paused',polling=100,timeout=5000)
        frozen = page.evaluate('window.blitzTelemetry.Time')
        assert frozen - before < .45
        results.append('PASS: Actual hidden-tab interval is excluded from the gameplay clock')
        results.append('PASS: Returning to the tab requires explicit resume')
        page.mouse.click(640,720*.5744,delay=60)
        page.wait_for_function('!window.blitzTelemetry.Paused')
        page.wait_for_timeout(200)
        assert page.evaluate('window.blitzTelemetry.Time') > frozen
        assert page.evaluate('window.blitzTelemetry.Pending') == 0
        results.append('PASS: Explicit resume restarts time and does not fire a shot')
        browser.close()
except Exception as error:
    results.append(('UNAVAILABLE: Chrome automation kept the game document visible when switching tabs; actual hidden-tab pause was not verified.' if stage == 'visibility' else 'FAILED: ' + str(error)))
    out.write_text('\n'.join(results),encoding='utf-8')
    raise
out.write_text('\n'.join(results),encoding='utf-8')
print('\n'.join(results))
