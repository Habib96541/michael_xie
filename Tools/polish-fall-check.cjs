const { chromium } = require('playwright');
const { PNG } = require('pngjs');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const folder = path.resolve(__dirname, '../SprintDoc/PolishRevision/FallSequence');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

(async () => {
    await fs.mkdir(folder, { recursive: true });
    const report = { result: 'FAIL', frames: [], assertions: [] };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
    try {
        const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
        const p = await context.newPage();
        const state = async name => p.waitForFunction(s => window.shatterStatus?.State === s, name, { timeout: 15000 });
        const status = async () => p.evaluate(() => window.shatterStatus);
        const capture = async label => {
            await p.screenshot({ path: path.join(folder, label + '.png') });
            report.frames.push({ label, status: await status() });
        };
        await p.goto('http://127.0.0.1:8080'); await state('Home');
        await p.mouse.click(640 + 235 * 720 / 650, 360 + 75 * 720 / 650);
        await state('Preview');
        const preview = PNG.sync.read(await p.screenshot());
        let safeSide;
        for (let y = 580; y > 160 && safeSide === undefined; y--) {
            const xs = [];
            for (let x = 580; x < 700; x++) {
                const i = (y * preview.width + x) * 4;
                const [r,g,b] = preview.data.subarray(i,i+3);
                if (r > 100 && g > 180 && g > r * 1.15 && b < 150) xs.push(x);
            }
            if (xs.length > 2) safeSide = xs.reduce((a,b)=>a+b) / xs.length < 640 ? 0 : 1;
        }
        assert.notEqual(safeSide, undefined);
        await state('Playing');
        await p.keyboard.press(safeSide ? 'a' : 'd');
        await sleep(430); await capture('01-approaching-fragile-contact');
        await state('Falling'); await capture('02-glass-break-and-loss-of-balance');
        await p.keyboard.press('Escape'); await state('Paused');
        const frozen = await status(); await sleep(600);
        const still = await status();
        assert.equal(still.PlayerY, frozen.PlayerY);
        assert.equal(still.FallSeconds, frozen.FallSeconds);
        await capture('03-paused-fall');
        report.assertions.push('PASS Fall pose and physical position freeze while paused');
        await p.mouse.click(640, 360 - 12 * 720 / 650); await state('Falling');
        await capture('04-resumed-stagger');
        await sleep(250); await capture('05-transition-to-falling-pose');
        await sleep(250); await capture('06-visible-descent');
        await p.keyboard.press('a'); await p.keyboard.press('d');
        await sleep(200); await capture('07-falling-below-the-bridge');
        await state('Results'); await capture('08-results-after-fall');
        const result = await status();
        assert.equal(result.Wallet, 0); assert.equal(result.Steps, 0);
        assert.ok(result.FallSeconds >= 2.5);
        report.assertions.push('PASS One fragile fall, no reward, delayed results, rapid keys ignored');
        assert.ok(report.frames.find(frame => frame.label.startsWith('06')).status.PlayerY < frozen.PlayerY - 0.3);
        report.assertions.push('PASS Player physically descends during the animated sequence');
        await p.mouse.click(640, 360 + 141 * 720 / 650); await state('Preview');
        const reset = await status(); assert.equal(reset.FallSeconds, 0); assert.ok(Math.abs(reset.PlayerY - 0.12) < 0.01);
        await capture('09-retry-reset');
        report.assertions.push('PASS Retry resets position, fall time, and run');
        report.result = 'PASS';
        console.log(JSON.stringify(report));
        await context.close();
    } catch (error) { report.error = error.stack; process.exitCode = 1; console.error(error); }
    finally { await fs.writeFile(path.join(folder, 'Sequence.json'), JSON.stringify(report, null, 2)); await browser.close(); }
})();
