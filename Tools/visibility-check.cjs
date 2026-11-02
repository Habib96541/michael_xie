const { chromium } = require('playwright');
const fs = require('node:fs/promises');
const path = require('node:path');
const os = require('node:os');
const { spawn } = require('node:child_process');

(async () => {
    const report = { kind: 'Headed installed Chrome, actual tab activation', result: 'FAIL' };
    const temporaryRoot = path.resolve(os.tmpdir());
    const profile = await fs.mkdtemp(path.join(temporaryRoot, 'ShatterLeap-visibility-'));
    const chrome = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', ['--remote-debugging-port=9224', '--user-data-dir=' + profile, '--no-first-run', '--no-default-browser-check', '--window-size=1300,820', 'about:blank'], { stdio: 'ignore', windowsHide: true });
    let browser;
    for (let attempt = 0; attempt < 30; attempt++) {
        try { browser = await chromium.connectOverCDP('http://127.0.0.1:9224', { noDefaults: true, timeout: 1000 }); break; }
        catch { await new Promise(resolve => setTimeout(resolve, 200)); }
    }
    if (!browser) { chrome.kill(); throw Error('Could not connect to the dedicated Chrome visibility window'); }
    try {
        // noDefaults attaches without Playwright's forced focus/visibility override.
        const context = browser.contexts()[0];
        const game = context.pages()[0];
        await game.setViewportSize({ width: 1280, height: 720 });
        await game.bringToFront();
        await game.goto('http://127.0.0.1:8080');
        await game.waitForFunction(() => window.shatterStatus?.State === 'Home', {}, { timeout: 90000 });
        await game.mouse.click(640, 360 + 144 * 720 / 650);
        await game.waitForFunction(() => window.shatterStatus?.State === 'Playing');
        const activeSeconds = await game.evaluate(() => window.shatterStatus.Seconds);
        const other = await context.newPage();
        await other.goto('about:blank');
        await other.bringToFront();
        report.hidden = await game.evaluate(() => document.hidden);
        if (!report.hidden) throw Error('The browser did not hide the inactive document');
        await new Promise(resolve => setTimeout(resolve, 600));
        await game.bringToFront();
        await game.waitForFunction(() => window.shatterStatus?.State === 'Paused', {}, { timeout: 10000 });
        const before = await game.evaluate(() => window.shatterStatus.Seconds);
        await new Promise(resolve => setTimeout(resolve, 700));
        const after = await game.evaluate(() => window.shatterStatus);
        if (after.State !== 'Paused' || before !== after.Seconds) throw Error('Timer changed or return auto-resumed');
        if (activeSeconds - after.Seconds > 0.5) throw Error('Hidden tab consumed gameplay time');
        report.returnState = after;
        await game.screenshot({ path: path.resolve(__dirname, '../SprintDoc/Screenshots/actual-tab-return-paused.png') });
        await game.mouse.click(640, 360 - 12 * 720 / 650);
        await game.waitForFunction(() => window.shatterStatus?.State === 'Playing');
        report.result = 'PASS';
        report.assertions = ['Tab activation actually hides the game document', 'Tab change pauses the active run', 'Return keeps the timer paused', 'Explicit Resume returns to Playing'];
        console.log(JSON.stringify(report));
    } catch (error) {
        report.error = error.stack;
        process.exitCode = 1;
        console.error(error);
    } finally {
        await fs.writeFile(path.resolve(__dirname, '../SprintDoc/VisibilityCheck.json'), JSON.stringify(report, null, 2));
        try { await (await browser.newBrowserCDPSession()).send('Browser.close'); } catch {}
        await browser.close();
        chrome.kill();
        if (path.dirname(path.resolve(profile)) === temporaryRoot && path.basename(profile).startsWith('ShatterLeap-visibility-')) {
            await fs.rm(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 100 }).catch(() => {});
        }
    }
})();
