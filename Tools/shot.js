// Headless playtest harness (no dependencies).
// Usage: node Tools/shot.js <url> <outPrefix> [width] [height]
// Loads the game, plays a full run through the debug API, saves screenshots and
// a state log, then reloads to verify the best score persisted.

const fs = require('fs');
const path = require('path');
const http = require('http');
const { spawn } = require('child_process');

const url = process.argv[2];
const outPrefix = process.argv[3] || 'shots/run';
const W = parseInt(process.argv[4] || '1280', 10);
const H = parseInt(process.argv[5] || '800', 10);
const PORT = 9333 + Math.floor(Math.random() * 200);
const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

const sleep = ms => new Promise(r => setTimeout(r, ms));

function getJson(pathname) {
  return new Promise((resolve, reject) => {
    const req = http.get({ host: '127.0.0.1', port: PORT, path: pathname, timeout: 2000 }, res => {
      let d = '';
      res.on('data', c => (d += c));
      res.on('end', () => {
        try { resolve(JSON.parse(d)); } catch (e) { reject(e); }
      });
    });
    req.on('error', reject);
    req.on('timeout', () => { req.destroy(new Error('timeout')); });
  });
}

async function main() {
  if (!url) { console.error('usage: node Tools/shot.js <url> [outPrefix] [w] [h]'); process.exit(2); }
  const profile = path.join(require('os').tmpdir(), 'crashshot-' + Date.now());
  const proc = spawn(CHROME, [
    '--headless=new', '--disable-gpu', '--enable-unsafe-swiftshader', '--hide-scrollbars',
    '--remote-debugging-port=' + PORT, '--window-size=' + W + ',' + H,
    '--user-data-dir=' + profile, '--no-first-run', '--no-default-browser-check',
    '--autoplay-policy=no-user-gesture-required', '--disable-extensions', 'about:blank'
  ], { stdio: 'ignore' });

  let targets = null;
  for (let i = 0; i < 80; i++) {
    try {
      targets = await getJson('/json/list');
      if (targets && targets.some(t => t.type === 'page')) break;
    } catch (e) { /* not up yet */ }
    await sleep(250);
  }
  const page = (targets || []).find(t => t.type === 'page');
  if (!page) { console.error('chrome did not start'); proc.kill(); process.exit(3); }

  const ws = new WebSocket(page.webSocketDebuggerUrl);
  let idc = 0;
  const pending = new Map();
  ws.addEventListener('message', ev => {
    let msg;
    try { msg = JSON.parse(ev.data); } catch (e) { return; }
    if (msg.id && pending.has(msg.id)) {
      const p = pending.get(msg.id);
      pending.delete(msg.id);
      if (msg.error) p.reject(new Error(JSON.stringify(msg.error)));
      else p.resolve(msg.result);
    }
  });
  await new Promise((res, rej) => { ws.addEventListener('open', res); ws.addEventListener('error', rej); });

  const send = (method, params) => new Promise((resolve, reject) => {
    const id = ++idc;
    pending.set(id, { resolve, reject });
    ws.send(JSON.stringify({ id, method, params: params || {} }));
  });

  const evalJs = async (expr, awaitPromise = false) => {
    const r = await send('Runtime.evaluate', { expression: expr, returnByValue: true, awaitPromise });
    if (r && r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 300));
    return r && r.result ? r.result.value : undefined;
  };

  const shot = async name => {
    const r = await send('Page.captureScreenshot', { format: 'png' });
    const file = outPrefix + '_' + name + '.png';
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, Buffer.from(r.data, 'base64'));
    return file;
  };

  const log = [];
  const state = async tag => {
    const s = await evalJs('window.__crashState || ""');
    log.push(tag + ' ' + s);
    try { return JSON.parse(s || '{}'); } catch (e) { return {}; }
  };

  await send('Page.enable');
  await send('Runtime.enable');
  await send('Emulation.setDeviceMetricsOverride', { width: W, height: H, deviceScaleFactor: 1, mobile: false });
  await send('Page.navigate', { url });

  let ready = false;
  const deadline = Date.now() + 90000;
  while (Date.now() < deadline) {
    ready = await evalJs('!!window.__crashReady').catch(() => false);
    if (ready) break;
    const err = await evalJs("(document.getElementById('err')||{}).textContent||''").catch(() => '');
    if (err) log.push('LOADER_ERROR ' + err);
    await sleep(500);
  }
  log.push('ready=' + ready);
  if (!ready) {
    await shot('00_failed');
    console.log(log.join('\n'));
    proc.kill();
    process.exit(4);
  }

  await sleep(1200);
  await shot('01_menu');
  await state('menu');

  await evalJs("window.__unity.SendMessage('CGDebug','Play','')");
  await sleep(900);
  await shot('02_countdown');
  await state('countdown');

  await sleep(2400);
  await shot('03_flying');
  await state('flying');

  await evalJs("window.__unity.SendMessage('CGDebug','Cash','')");
  await sleep(500);
  await shot('04_cashout');
  await state('cashout');

  await sleep(4300);

  let over = false;
  for (let i = 0; i < 6 && !over; i++) {
    const s = await state('pre_crash_' + i);
    if (s.phase === 'gameover') { over = true; break; }
    if (s.phase !== 'flying') { await sleep(1500); continue; }
    await evalJs("window.__unity.SendMessage('CGDebug','ForceCrash','" + ((s.mult || 1) + 0.35).toFixed(2) + "')");
    await sleep(450);
    const c = await state('crash_' + i);
    if (i === 0) await shot('05_crash');
    if (c.shields === 0) over = true;
    await sleep(3800);
  }

  await sleep(2200);
  await shot('06_gameover');
  const final = await state('gameover');

  // reload and check the best score survived (PlayerPrefs -> IndexedDB)
  await send('Page.navigate', { url });
  const dl2 = Date.now() + 90000;
  let again = false;
  while (Date.now() < dl2) {
    again = await evalJs('!!window.__crashReady').catch(() => false);
    if (again) break;
    await sleep(500);
  }
  await sleep(1000);
  await shot('07_reloaded');
  await state('after_reload');

  console.log(log.join('\n'));
  console.log('shots=' + outPrefix + '_*.png');
  ws.close();
  proc.kill();
  process.exit(0);
}

main().catch(e => { console.error('HARNESS ERROR ' + (e && e.message)); process.exit(1); });
