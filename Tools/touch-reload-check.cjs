const { chromium } = require('playwright');
const { PNG } = require('pngjs');
const fs = require('node:fs/promises');
const path = require('node:path');

(async () => {
    const report = { result: 'FAIL', kind: 'Chrome touch emulation, actual world-panel raycast and interrupted reload' };
    const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
    try {
        const context = await browser.newContext({ viewport: { width: 360, height: 800 }, hasTouch: true, isMobile: true });
        const p = await context.newPage();
        await p.goto('http://127.0.0.1:8080');
        await p.waitForFunction(() => window.shatterStatus?.State === 'Home', {}, { timeout: 90000 });
        await p.touchscreen.tap(180, 400 + 144 * .75);
        await p.waitForFunction(() => window.shatterStatus?.State === 'Preview');
        const image = PNG.sync.read(await p.screenshot());
        let side;
        for (let y = 640; y > 150 && side === undefined; y--) {
            const xs = [];
            for (let x = 110; x < 250; x++) {
                const i = (y * image.width + x) * 4;
                const [r, g, b] = image.data.subarray(i, i + 3);
                if (r > 100 && g > 180 && g > r * 1.15 && b < 150) xs.push(x);
            }
            if (xs.length > 2) side = xs.reduce((a,b)=>a+b) / xs.length < 180 ? 0 : 1;
        }
        if (side === undefined) throw Error('Could not read the visible first preview marker');
        await p.waitForFunction(() => window.shatterStatus?.State === 'Playing');
        await new Promise(resolve => setTimeout(resolve, 400));
        const forwardLength = Math.hypot(7.7, 13.5), fy = -7.7 / forwardLength, fz = 13.5 / forwardLength;
        const depth = -7.8 * fy + 12.7 * fz, up = -7.8 * fz + 12.7 * (-fy), tangent = Math.tan(29 * Math.PI / 180);
        await p.touchscreen.tap(180 + (side ? 1.1 : -1.1) / depth / tangent * 400, 400 - up / depth / tangent * 400);
        await p.waitForFunction(() => window.shatterStatus?.Steps === 1, {}, { timeout: 5000 });
        await new Promise(resolve => setTimeout(resolve, 800));
        report.landed = await p.evaluate(() => window.shatterStatus);
        if (report.landed.Steps !== 1 || report.landed.RoundCoins !== 10 || report.landed.Wallet !== 0) throw Error('Touch action duplicated or did not resolve correctly');
        await p.screenshot({ path: path.resolve(__dirname, '../SprintDoc/Screenshots/360x800-world-touch-landed.png') });
        await p.reload();
        await p.waitForFunction(() => window.shatterStatus?.State === 'Home');
        report.reloaded = await p.evaluate(() => window.shatterStatus);
        if (report.reloaded.Wallet !== 0 || report.reloaded.Steps !== 0 || report.reloaded.RoundCoins !== 0) throw Error('Interrupted run restored or paid');
        report.result = 'PASS';
        report.assertions = ['Touch on the actual valid next panel lands safely', 'Touch plus mouse simulation does not duplicate a step', 'Reload during an earned active round abandons the provisional coins and run'];
        console.log(JSON.stringify(report));
    } catch (error) { report.error = error.stack; process.exitCode = 1; console.error(error); }
    finally { await fs.writeFile(path.resolve(__dirname, '../SprintDoc/TouchReloadCheck.json'), JSON.stringify(report, null, 2)); await browser.close(); }
})();
