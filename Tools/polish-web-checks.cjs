/* Run after: python Tools/serve.py. Uses installed Windows Chrome/Edge, no browser downloads. */
const { chromium } = require('playwright');
const { PNG } = require('pngjs');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');

const root = path.resolve(__dirname, '..');
const evidence = path.join(root, 'SprintDoc', 'PolishRevision', 'Screenshots');
const checks = [];
const report = { started: new Date().toISOString(), checks, viewports: [], errors: [], warnings: [], performance: {} };
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
function check(value, label) { assert.ok(value, label); checks.push('PASS ' + label); console.log('PASS ' + label); }
function scale(w, h) { return h > w ? Math.min(w / 480, h / 800) : Math.min(w / 1100, h / 650); }
function point(p, x, y) { const { width: w, height: h } = p.viewportSize(); const s = scale(w, h); return { x: w / 2 + x * s, y: h / 2 - y * s }; }
async function click(p, x, y, touch = false) { const wide = p.viewportSize().width > p.viewportSize().height;
    if (y === -144 && x === 0) { x = wide ? 235 : 0; y = wide ? -75 : -150; }
    else if (y === -215 && (x === -100 || x === 100)) { x += wide ? 235 : 0; y = wide ? -161 : -230; }
    else if (y === -34) { x += wide ? 235 : 0; y = wide ? 68 : -39; }
    const q = point(p, x, y); await (touch ? p.touchscreen.tap(q.x, q.y) : p.mouse.click(q.x, q.y)); await sleep(230); }
async function bottom(p, side, touch = false) { const { width: w, height: h } = p.viewportSize(); const s = scale(w, h); const x = w / 2 + (side ? 110 : -110) * s; const y = h - 104 * s; await (touch ? p.touchscreen.tap(x, y) : p.mouse.click(x, y)); await sleep(230); }
async function pause(p, touch = false) { const { width: w, height: h } = p.viewportSize(); const s = scale(w, h); const x = w / 2 + 174 * s; const y = 40 * s; await (touch ? p.touchscreen.tap(x, y) : p.mouse.click(x, y)); await state(p, 'Paused'); }
async function status(p) { return p.evaluate(() => window.shatterStatus); }
async function state(p, name, timeout = 15000) { await p.waitForFunction(value => window.shatterStatus?.State === value, name, { timeout }); return status(p); }
async function shot(p, name) {
    const buffer = await p.screenshot({ path: path.join(evidence, name + '.png') });
    if (/home|help|settings|pause|confirm|results/.test(name)) {
        const image = PNG.sync.read(buffer); let white = 0;
        for (let y = Math.floor(image.height * .2); y < image.height * .85; y++) {
            for (let x = Math.floor(image.width * .2); x < image.width * .8; x++) {
                const i = (y * image.width + x) * 4;
                if (image.data[i] > 170 && image.data[i+1] > 170 && image.data[i+2] > 170) white++;
            }
        }
        check(white > 60, name + ': foreground UI text is actually rendered');
    }
}
async function frameStats(p) {
    return p.evaluate(() => new Promise(resolve => {
        const intervals = []; let last = performance.now();
        function frame(now) { intervals.push(now - last); last = now; if (intervals.length < 120) requestAnimationFrame(frame); else { intervals.shift(); intervals.sort((a,b)=>a-b); resolve({ medianMs: intervals[Math.floor(intervals.length / 2)], p95Ms: intervals[Math.floor(intervals.length * .95)], samples: intervals.length }); } }
        requestAnimationFrame(frame);
    }));
}
function readPath(buffer) {
    const image = PNG.sync.read(buffer);
    let groups = [], group;
    for (let y = Math.floor(image.height * .15); y < image.height * .81; y++) {
        const xs = [];
        for (let x = Math.floor(image.width * .35); x < image.width * .65; x++) {
            const i = (y * image.width + x) * 4;
            const [r, g, b] = image.data.subarray(i, i + 3);
            if (r > 100 && g > 180 && g > r * 1.15 && b < 150) xs.push(x);
        }
        if (xs.length > 2) { group ??= { ys: [], xs: [] }; group.ys.push(y); group.xs.push(...xs); }
        else if (group) { groups.push(group); group = undefined; }
    }
    if (group) groups.push(group);
    return groups.map(g => ({ y: g.ys.reduce((a, b) => a + b) / g.ys.length, x: g.xs.reduce((a, b) => a + b) / g.xs.length }))
        .sort((a, b) => b.y - a.y).map(g => g.x < image.width / 2 ? 0 : 1);
}
async function preview(p, name, touch = false, rows = 6) {
    await click(p, 0, -144, touch);
    await state(p, 'Preview');
    const buffer = await p.screenshot({ path: path.join(evidence, name + '-preview.png') });
    const route = readPath(buffer);
    check(route.length === rows, `${name}: all ${rows} preview markers are visible`);
    await p.keyboard.press('a');
    await p.keyboard.press('d');
    check((await status(p)).Steps === 0, name + ': keyboard choices blocked during preview');
    return route;
}
function attach(p, tag) {
    p.on('pageerror', error => report.errors.push(tag + ': ' + error.message));
    p.on('console', message => {
        if (message.type() === 'error') report.errors.push(tag + ': ' + message.text());
        if (message.type() === 'warning') report.warnings.push(message.text());
    });
}
async function load(p) {
    const started = Date.now();
    await p.goto('http://127.0.0.1:8080');
    await state(p, 'Home', 90000);
    return Date.now() - started;
}
async function safeStep(p, side, expected, method = 'key') {
    await state(p, 'Playing');
    if (method === 'button') await bottom(p, side);
    else await p.keyboard.press(side ? 'd' : 'a');
    await p.waitForFunction(step => window.shatterStatus?.Steps === step, expected, { timeout: 4000 });
}
async function desktopChecks(browser) {
    const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
    const p = await context.newPage(); attach(p, 'desktop');
    await p.addInitScript(() => {
        window.__audioContexts = [];
        if (window.AudioContext) {
            window.AudioContext = new Proxy(window.AudioContext, { construct(target, args) { const audio = Reflect.construct(target, args); window.__audioContexts.push(audio); return audio; } });
        }
    });
    report.performance.coldLoadMs = await load(p);
    await shot(p, '1280x720-home');
    if (await p.evaluate(() => document.fullscreenEnabled)) {
        await p.getByRole('button', { name: 'Full screen' }).click();
        await p.waitForFunction(() => !!document.fullscreenElement);
        check(true, 'Browser fullscreen enters through the visible user control');
        await p.evaluate(() => document.exitFullscreen());
    }
    await click(p, 100, -215); await shot(p, '1280x720-help'); await click(p, 0, -221);
    report.audioContextStates = await p.evaluate(() => window.__audioContexts.map(audio => audio.state));
    check(report.audioContextStates.includes('running'), 'Browser audio context runs after a UI user gesture');
    await click(p, -100, -215);
    await click(p, 0, -68); await click(p, 0, -145);
    await click(p, 90, 110); await click(p, -20, 8);
    await shot(p, '1280x720-settings'); await click(p, 0, -260);
    await p.reload(); await state(p, 'Home');
    await click(p, -100, -215); await shot(p, '1280x720-settings-after-reload'); await click(p, 0, -260);
    checks.push('OBSERVE Settings persisted across reload: compare settings screenshots');
    const route = await preview(p, '1280x720');
    await state(p, 'Playing'); await shot(p, '1280x720-gameplay');
    report.performance.gameplayRaf = await frameStats(p);
    check(readPath(await p.screenshot()).length === 0, 'Safe markers are hidden during gameplay');
    const key = route[0] ? 'd' : 'a';
    await p.keyboard.down(key);
    await state(p, 'Jumping', 3000);
    for (let i = 0; i < 8; i++) await p.keyboard.press(route[0] ? 'a' : 'd');
    await sleep(1050);
    check((await status(p)).Steps === 1, 'Held key and rapid extra inputs produce exactly one landing: ' + JSON.stringify(await status(p)));
    await p.keyboard.up(key);
    check((await status(p)).RoundCoins === 10 && (await status(p)).Wallet === 0, 'Safe landing earns provisional coins without banking');
    await pause(p); await shot(p, '1280x720-pause');
    const paused = (await status(p)).Seconds; await sleep(600);
    check((await status(p)).Seconds === paused, 'Manual pause freezes the browser countdown');
    await click(p, 0, -62); await shot(p, '1280x720-confirm');
    await click(p, -102, -93); check((await status(p)).State === 'Paused', 'Cancel restart confirmation preserves paused round');
    await click(p, 0, 12); await state(p, 'Playing'); await sleep(350);
    // Project the visible next-panel center using the shipped third-person camera.
    const previousX = route[0] ? 1.1 : -1.1;
    const targetX = route[1] ? 1.1 : -1.1;
    const cameraForwardLength = Math.hypot(7.7, 11.5);
    const fy = -7.7 / cameraForwardLength, fz = 11.5 / cameraForwardLength;
    const depth = (-7.8 * fy) + (10.7 * fz), up = -7.8 * fz + 10.7 * (-fy);
    const tangent = Math.tan(26 * Math.PI / 180);
    await p.mouse.click(640 + (targetX - previousX) / depth / tangent * 360, 360 - up / depth / tangent * 360);
    await p.waitForFunction(() => window.shatterStatus?.Steps === 2, {}, { timeout: 4000 });
    check((await status(p)).Steps === 2, 'Mouse raycast on the actual next panel lands safely');
    for (let i = 2; i < 6; i++) await safeStep(p, route[i], i + 1, i === 2 ? 'button' : 'key');
    await state(p, 'Results'); await shot(p, '1280x720-results-win');
    check((await status(p)).Wallet === 260, 'Compiled Web Easy win banks exactly 260 coins');
    await sleep(500); check((await status(p)).Wallet === 260, 'Results do not duplicate a payout');
    await p.reload(); await state(p, 'Home');
    check((await status(p)).Wallet === 260, 'Banked win persists after an actual browser reload');
    const abandonRoute = await preview(p, 'abandon'); await state(p, 'Playing');
    await safeStep(p, abandonRoute[0], 1);
    await pause(p); await click(p, 0, -136); await shot(p, 'abandon-confirm'); await click(p, 102, -93);
    await state(p, 'Home'); check((await status(p)).Wallet === 260, 'Confirmed Home abandons provisional earnings without changing wallet');
    // Headless Chrome does not reliably hide inactive tabs; exercise the browser's blur handler explicitly.
    await preview(p, 'visibility'); await state(p, 'Playing');
    await p.evaluate(() => window.dispatchEvent(new Event('blur')));
    await state(p, 'Paused');
    const hiddenTime = (await status(p)).Seconds;
    await p.evaluate(() => window.dispatchEvent(new Event('focus'))); await sleep(500);
    check((await status(p)).State === 'Paused' && (await status(p)).Seconds === hiddenTime, 'Browser blur/focus event simulation pauses and requires explicit resume');
    await click(p, 0, 12); await state(p, 'Playing');
    await pause(p); await click(p, 0, -62); await click(p, 102, -93); await state(p, 'Preview');
    check((await status(p)).Steps === 0 && (await status(p)).Wallet === 260, 'Confirmed Restart resets the round without charging or paying coins');
    await state(p, 'Playing');
    // Let a real countdown expire while a controlled jump is unfinished.
    await p.waitForFunction(() => window.shatterStatus?.Seconds < 0.35 && window.shatterStatus?.State === 'Playing', {}, { timeout: 35000 });
    await p.keyboard.press('a'); await state(p, 'Results', 6000); await shot(p, '1280x720-results-timeout');
    check((await status(p)).Wallet === 260 && (await status(p)).Steps === 0, 'Real browser timeout during jump pays zero');
    report.performance.resultsRaf = await frameStats(p);
    report.performance.renderer = await p.evaluate(() => { const gl = document.getElementById('unity-canvas').getContext('webgl2'); const ext = gl.getExtension('WEBGL_debug_renderer_info'); return ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : 'unknown'; });
    await context.close();
}
async function viewportChecks(browser, width, height) {
    const touch = height > width;
    const name = `${width}x${height}`;
    const context = await browser.newContext({ viewport: { width, height }, hasTouch: touch, isMobile: touch, deviceScaleFactor: 1 });
    const p = await context.newPage(); attach(p, name); const loadMs = await load(p);
    await shot(p, name + '-home');
    await click(p, 100, -215, touch); await shot(p, name + '-help'); await click(p, 0, -221, touch);
    const route = await preview(p, name, touch); await state(p, 'Playing'); await sleep(300); await shot(p, name + '-gameplay');
    await pause(p, touch); await shot(p, name + '-pause');
    await click(p, 0, -206, touch); await shot(p, name + '-settings'); await click(p, 0, -260, touch);
    await click(p, 0, -62, touch); await shot(p, name + '-confirm'); await click(p, -102, -93, touch); await click(p, 0, 12, touch);
    await state(p, 'Playing');
    await bottom(p, route[0], touch);
    await p.waitForFunction(() => window.shatterStatus?.Steps === 1, {}, { timeout: 4000 });
    check((await status(p)).RoundCoins === 10 && (await status(p)).Wallet === 0, name + ': one safe mouse/touch action earns exactly one provisional step');
    await state(p, 'Playing'); await bottom(p, 1 - route[1], touch); await state(p, 'Falling'); await p.waitForFunction(() => window.shatterStatus?.FallSeconds > 1.05 && window.shatterStatus?.State === 'Falling'); await shot(p, name + '-falling'); check((await status(p)).PlayerY < -1, name + ': animated player is descending before results'); await state(p, 'Results'); await shot(p, name + '-results-fall');
    check((await status(p)).Wallet === 0 && (await status(p)).Steps === 1, name + ': fragile landing discards the round earnings');
    await click(p, 0, -141, touch); await state(p, 'Preview');
    check((await status(p)).Wallet === 0 && (await status(p)).Steps === 0, name + ': Retry starts a free fresh round');
    await p.reload(); await state(p, 'Home'); check((await status(p)).Wallet === 0, name + ': failed round never persists provisional coins');
    report.viewports.push({ width, height, touchEmulation: touch, loadMs, minimumUnityButtonCssHeight: 64 * scale(width, height) });
    await context.close();
}
async function difficultiesAndResize(browser) {
    const context = await browser.newContext({ viewport: { width: 768, height: 1024 }, hasTouch: true, isMobile: true });
    const p = await context.newPage(); attach(p, 'difficulty-resize'); await load(p);
    await click(p, 0, -34, true); // Normal
    await p.reload(); await state(p, 'Home');
    await preview(p, 'normal', true, 10); check((await status(p)).Seconds === 45, 'Normal difficulty persists after reload and starts at 45 seconds');
    await state(p, 'Playing');
    await p.setViewportSize({ width: 1280, height: 720 }); await sleep(400); await shot(p, 'resize-portrait-to-landscape');
    check((await status(p)).State === 'Playing', 'Live orientation/viewport change retains the active run');
    await pause(p); await click(p, 0, -136); await click(p, 102, -93); await state(p, 'Home');
    await click(p, 132, -34); await preview(p, 'hard', false, 14); check((await status(p)).Seconds === 60, 'Hard preview shows all fourteen rows and a 60-second timer');
    await context.close();
}
(async () => {
    await fs.mkdir(evidence, { recursive: true });
    const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
    report.browser = await browser.version();
    try {
        await desktopChecks(browser);
        for (const [w,h] of [[360,800],[720,1280],[768,1024],[1280,720],[1600,900]]) await viewportChecks(browser,w,h);
        await difficultiesAndResize(browser);
        const edge = await chromium.launch({ executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true });
        try {
            const context = await edge.newContext({ viewport: { width: 1280, height: 720 } });
            const p = await context.newPage(); attach(p, 'Edge'); await load(p); await preview(p, 'edge'); await state(p, 'Playing'); await shot(p, 'edge-gameplay');
            check((await status(p)).State === 'Playing', 'Installed Edge loads the exact compiled build and reaches gameplay');
            report.edge = await edge.version(); await context.close();
        } finally { await edge.close(); }
        check(report.errors.length === 0, 'No JavaScript/runtime/resource errors in tested browser sessions');
        report.result = 'PASS';
    } catch (error) {
        report.result = 'FAIL'; report.failure = error.stack; console.error(error); process.exitCode = 1;
    } finally {
        report.warnings = [...new Set(report.warnings)];
        report.finished = new Date().toISOString();
        await fs.writeFile(path.join(root,'SprintDoc','PolishRevision','WebChecks.json'), JSON.stringify(report,null,2));
        await browser.close();
    }
})();
