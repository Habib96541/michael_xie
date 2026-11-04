"""Exercise the compiled game through HTTP using real browser input.
Requires: pip install playwright; installed Google Chrome.
"""
import json
import time
import sys
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'SprintDoc' / 'Tests'
OUT.mkdir(parents=True, exist_ok=True)
results = []
errors = []
def record_failure(kind, value, traceback):
    try:
        state = page.evaluate('window.blitzTelemetry')
        page.screenshot(path=str(OUT / 'failure.png'))
        (OUT / 'FailureState.json').write_text(json.dumps(state,indent=2),encoding='utf-8')
    except Exception:
        pass
    (OUT / 'Browser.txt').write_text('\n'.join(dict.fromkeys(results)) + '\nERROR: ' + str(value) + '\nConsole errors: ' + json.dumps(errors), encoding='utf-8')
    sys.__excepthook__(kind, value, traceback)
sys.excepthook = record_failure

def check(condition, name):
    if not any(r.endswith(name) for r in results):
        print(('PASS: ' if condition else 'FAIL: ') + name, flush=True)
    results.append(('PASS: ' if condition else 'FAIL: ') + name)
    if not condition:
        raise AssertionError(name)

def snap(page):
    return page.evaluate('window.blitzTelemetry')

def wait_state(page, state, timeout=30000):
    page.wait_for_function('(s) => window.blitzTelemetry?.State === s', arg=state, timeout=timeout)
    return snap(page)

def click_relative(page, x, y):
    box = page.locator('canvas').bounding_box()
    page.mouse.click(box['x'] + box['width'] * x, box['y'] + box['height'] * y, delay=60)
    page.wait_for_timeout(65)

def action(page, row=0):
    # Menu content uses 2% vertical margins and buttons anchored from the bottom.
    click_relative(page, .5, .02 + (1 - (.4225 - row * .095)) * .96)

def target(page, bomb=False, timeout=8000):
    page.wait_for_function('(b) => window.blitzTelemetry?.Slots.some(t => t.State === "Exposed" && t.Bomb === b)', arg=bomb, timeout=timeout)
    return next(t for t in snap(page)['Slots'] if t['State'] == 'Exposed' and t['Bomb'] == bomb)

with sync_playwright() as p:
    browser = p.chromium.launch(channel='chrome', headless=True, args=['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--disable-background-timer-throttling'])
    context = browser.new_context(viewport={'width':1280,'height':720}, device_scale_factor=1)
    page = context.new_page()
    page.add_init_script("""(() => {
      window.qaAudioContexts = [];
      if (window.AudioContext) {
        window.AudioContext = new Proxy(window.AudioContext, {
          construct(target,args) { const context = new target(...args); window.qaAudioContexts.push(context); return context; }
        });
      }
    })();""")
    page.on('pageerror', lambda e: errors.append(str(e)))
    page.on('console', lambda m: errors.append(m.text) if m.type == 'error' else None)
    page.goto('http://127.0.0.1:8765/?qa=1', wait_until='domcontentloaded')
    wait_state(page, 'Home', 180000)
    check(True, 'Compiled Web build loads through HTTP')
    for width, height in [(360,800),(720,1280),(768,1024),(1280,720),(1600,900)]:
        page.set_viewport_size({'width':width,'height':height})
        page.wait_for_timeout(350)
        page.screenshot(path=str(OUT / f'home-{width}x{height}.png'))
    page.set_viewport_size({'width':1280,'height':720})
    page.evaluate("""() => {
      window.originalStorageSet = Storage.prototype.setItem;
      Storage.prototype.setItem = function(k,v) {
        if (k === 'BullseyeBlitz.Save.v1') throw new DOMException('QA storage failure','QuotaExceededError');
        return window.originalStorageSet.call(this,k,v);
      };
    }""")
    action(page)
    wait_state(page, 'Countdown')
    check(page.evaluate('window.qaAudioContexts.some(c => c.state === "running")'),'Browser audio activates after Play gesture')
    click_relative(page,.5,.5)
    check(snap(page)['Pending'] == 0, 'Countdown input does not reward coins')
    wait_state(page,'Playing')
    start = time.monotonic()
    initial = snap(page)
    check(sum(t['State'] != 'Hidden' for t in initial['Slots']) == 2 and not any(t['Bomb'] for t in initial['Slots']), 'Round starts with two coin targets')
    samples = []
    while snap(page)['State'] == 'Playing':
        state = snap(page)
        active = [t for t in state['Slots'] if t['State'] != 'Hidden']
        check(len(active) <= 5 and sum(t['Bomb'] for t in active) <= 2, 'Spawn capacity respected')
        check(all(t['Collider'] == (t['State'] == 'Exposed') for t in state['Slots']), 'Collider eligibility matches exposed state')
        samples.append(state['Fps'])
        coins = [t for t in active if not t['Bomb'] and t['State'] == 'Exposed']
        if coins:
            t = coins[0]
            click_relative(page, t['X'], t['Y'])
        if 2 < state['Time'] < 3:
            page.screenshot(path=str(OUT / 'gameplay-1280x720.png'))
        page.wait_for_timeout(130)
    state = wait_state(page,'Results')
    check(state['Wallet'] == 0 and state['Completed'] == 0 and state['Pending'] > 0, 'Storage failure does not claim or apply payout')
    page.screenshot(path=str(OUT / 'save-failure.png'))
    page.evaluate('() => { Storage.prototype.setItem = window.originalStorageSet; }')
    action(page)
    page.wait_for_function('window.blitzTelemetry.Completed === 1')
    state = snap(page)
    check(state['Time'] == 20 and state['Wallet'] == state['Pending'] and state['Completed'] == 1, 'Successful 20-second round banks exactly once')
    check(19.0 <= time.monotonic() - start <= 24, 'Browser round duration matches 20-second clock')
    wallet = state['Wallet']
    completed = state['Completed']
    page.screenshot(path=str(OUT / 'success.png'))
    click_relative(page,.5,.5)
    page.wait_for_timeout(250)
    check(snap(page)['Wallet'] == wallet, 'Post-deadline input cannot add payouts')
    action(page)
    wait_state(page,'Playing')
    t = target(page)
    before = snap(page)['Pending']
    box = page.locator('canvas').bounding_box()
    click_relative(page,t['X'],t['Y'] + t['Diameter']*.485/box['height'])
    page.wait_for_timeout(150)
    check(snap(page)['Pending'] == before,'Shelf foreground blocks a shot at the concealed target edge')
    t = target(page)
    click_relative(page,t['X'] + t['Diameter']*.65/box['width'],t['Y'])
    page.wait_for_timeout(150)
    check(snap(page)['Pending'] == before,'Space outside the tight circular hit area gives no reward')
    t = target(page)
    before = snap(page)['Pending']
    box = page.locator('canvas').bounding_box()
    page.mouse.move(t['X']*box['width'],t['Y']*box['height'])
    page.mouse.down()
    page.wait_for_timeout(550)
    page.mouse.up()
    page.wait_for_timeout(120)
    check(snap(page)['Pending'] == before + t['Reward'], 'Held mouse produces one reward')
    click_relative(page,.9,.055)
    page.wait_for_function('window.blitzTelemetry?.Paused')
    paused = snap(page)
    page.wait_for_timeout(1000)
    check(snap(page)['Time'] == paused['Time'], 'Pause freezes authoritative clock')
    page.screenshot(path=str(OUT / 'pause.png'))
    for width, height in [(360,800),(720,1280),(768,1024),(1280,720),(1600,900)]:
        page.set_viewport_size({'width':width,'height':height})
        page.wait_for_timeout(300)
        current = snap(page)
        check([(t['Activation'],t['Reward'],t['Bomb']) for t in current['Slots']] == [(t['Activation'],t['Reward'],t['Bomb']) for t in paused['Slots']], 'Rotation preserves target identity')
    action(page)
    page.wait_for_function('!window.blitzTelemetry.Paused')
    page.wait_for_timeout(150)
    check(snap(page)['Pending'] == paused['Pending'], 'Resume interaction does not fire')
    check(snap(page)['State'] == 'Playing','Resume returns to the current round')
    print('Focus test before blur: ' + json.dumps({k:snap(page)[k] for k in ['State','Paused','Time']}),flush=True)
    page.evaluate("window.dispatchEvent(new Event('blur'))")
    page.wait_for_timeout(350)
    print('Focus test after blur: ' + json.dumps({k:snap(page)[k] for k in ['State','Paused','Time']}),flush=True)
    if not snap(page)['Paused']:
        page.screenshot(path=str(OUT / 'focus-failure.png'))
        (OUT / 'FocusFailure.json').write_text(json.dumps(snap(page),indent=2))
    page.wait_for_function('window.blitzTelemetry.Paused',timeout=2000)
    check(True,'Browser blur automatically pauses')
    page.evaluate("window.dispatchEvent(new Event('focus'))")
    page.wait_for_timeout(150)
    action(page)
    page.wait_for_function('!window.blitzTelemetry.Paused')
    for attempt in range(4):
        page.wait_for_function('window.blitzTelemetry.State === "Results" || window.blitzTelemetry.Slots.some(t => t.State === "Exposed" && t.Bomb)',timeout=25000)
        current = snap(page)
        if current['State'] == 'Results':
            action(page)
            wait_state(page,'Playing')
            continue
        t = next(t for t in current['Slots'] if t['State'] == 'Exposed' and t['Bomb'])
        wallet = current['Wallet']
        completed = current['Completed']
        click_relative(page,t['X'],t['Y'])
        page.wait_for_timeout(700)
        if snap(page)['State'] == 'Results':
            break
    else:
        raise AssertionError('Unable to hit a random bomb in four opportunities')
    state = wait_state(page,'Results')
    check(state['Pending'] == 0 and state['Wallet'] == wallet and state['Completed'] == completed,'Bomb discards round earnings and preserves wallet')
    page.screenshot(path=str(OUT / 'bomb-results.png'))
    action(page)
    wait_state(page,'Playing')
    t = target(page)
    click_relative(page,t['X'],t['Y'])
    page.wait_for_timeout(200)
    page.reload(wait_until='domcontentloaded')
    state = wait_state(page,'Home',90000)
    check(state['Wallet'] == wallet and state['Pending'] == 0 and state['Completed'] == completed,'Reload preserves banked coins and discards unfinished round')
    action(page,1)
    page.wait_for_timeout(150)
    click_relative(page,.5 + min(.96,.92*page.viewport_size['height']/page.viewport_size['width'])*.20,.548)
    click_relative(page,.5 + min(.96,.92*page.viewport_size['height']/page.viewport_size['width'])*.10,.6344)
    action(page,2)
    page.wait_for_timeout(150)
    settings = page.evaluate("JSON.parse(localStorage.getItem('BullseyeBlitz.Save.v1'))")
    check(abs(settings['Music'] - .35) > .05 and abs(settings['Sound'] - .7) > .05 and settings['ReducedEffects'],'Music, sound, and reduced effects settings are saved')
    page.screenshot(path=str(OUT / 'settings.png'))
    page.reload(wait_until='domcontentloaded')
    wait_state(page,'Home',90000)
    check(page.evaluate("JSON.parse(localStorage.getItem('BullseyeBlitz.Save.v1'))") == settings,'Settings survive reload together with wallet')
    # New context: actual Chromium touch emulation, not a physical device.
    touch_context = browser.new_context(viewport={'width':360,'height':800},has_touch=True,is_mobile=True,device_scale_factor=1)
    touch_page = touch_context.new_page()
    touch_page.on('pageerror',lambda e: errors.append(str(e)))
    touch_page.goto('http://127.0.0.1:8765/?qa=1')
    wait_state(touch_page,'Home',90000)
    touch_page.touchscreen.tap(180,800*.5744)
    wait_state(touch_page,'Playing')
    t = target(touch_page)
    touch_page.touchscreen.tap(t['X']*360,t['Y']*800)
    touch_page.wait_for_timeout(200)
    check(snap(touch_page)['Pending'] == t['Reward'],'Emulated touch uses actual target position without mouse duplicate')
    touch_page.wait_for_function('window.blitzTelemetry.Slots.filter(t => t.State === "Exposed" && !t.Bomb).length >= 2')
    coins = [t for t in snap(touch_page)['Slots'] if t['State'] == 'Exposed' and not t['Bomb']][:2]
    before = snap(touch_page)['Pending']
    cdp = touch_context.new_cdp_session(touch_page)
    cdp.send('Input.dispatchTouchEvent',{'type':'touchStart','touchPoints':[{'x':t['X']*360,'y':t['Y']*800,'id':i+1} for i,t in enumerate(coins)]})
    touch_page.wait_for_timeout(700)
    cdp.send('Input.dispatchTouchEvent',{'type':'touchEnd','touchPoints':[]})
    touch_page.wait_for_timeout(200)
    check(snap(touch_page)['Pending'] - before in [t['Reward'] for t in coins],'Concurrent fingers and held touch produce only one shot')
    for width,height in [(360,800),(720,1280),(768,1024),(1280,720),(1600,900)]:
        touch_page.set_viewport_size({'width':width,'height':height})
        touch_page.wait_for_timeout(250)
        state = snap(touch_page)
        check(all(0 < t['X'] < 1 and 0 < t['Y'] < 1 for t in state['Slots'] if t['State'] != 'Hidden'),'Active slots remain in viewport')
        check(all(t['Diameter'] >= 48 for t in state['Slots']),'Target hit diameter at least 48 CSS pixels at DPR 1')
        touch_page.screenshot(path=str(OUT / f'gameplay-{width}x{height}.png'))
    check(not errors,'No browser JavaScript or console errors')
    (OUT / 'Browser.txt').write_text('\n'.join(dict.fromkeys(results)) + '\n\nFPS samples (headless software rendering): ' + json.dumps(samples) + '\nErrors: ' + json.dumps(errors),encoding='utf-8')
    browser.close()
print('\n'.join(dict.fromkeys(results)))
