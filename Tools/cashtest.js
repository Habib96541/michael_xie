// Focused check: cash out at a known time (no state polling before the tap),
// so a slow software-rendered page cannot race the round.
// Usage: node Tools/cashtest.js <url>
const fs = require('fs');
const path = require('path');
const http = require('http');
const os = require('os');
const { spawn } = require('child_process');

const url = process.argv[2];
const PORT = 9300 + Math.floor(Math.random() * 400);
const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const sleep = ms => new Promise(r => setTimeout(r, ms));

function getJson(p) {
  return new Promise((resolve, reject) => {
    const req = http.get({ host: '127.0.0.1', port: PORT, path: p, timeout: 2000 }, res => {
      let d = ''; res.on('data', c => (d += c)); res.on('end', () => { try { resolve(JSON.parse(d)); } catch (e) { reject(e); } });
    });
    req.on('error', reject); req.on('timeout', () => req.destroy(new Error('timeout')));
  });
}

(async () => {
  const profile = path.join(os.tmpdir(), 'crashcash-' + Date.now());
  const proc = spawn(CHROME, ['--headless=new', '--disable-gpu', '--enable-unsafe-swiftshader',
    '--remote-debugging-port=' + PORT, '--window-size=1280,800', '--user-data-dir=' + profile,
    '--no-first-run', '--no-default-browser-check', '--autoplay-policy=no-user-gesture-required', 'about:blank'],
    { stdio: 'ignore' });

  let targets = null;
  for (let i = 0; i < 80; i++) {
    try { targets = await getJson('/json/list'); if (targets.some(t => t.type === 'page')) break; } catch (e) {}
    await sleep(250);
  }
  const page = targets.find(t => t.type === 'page');
  const ws = new WebSocket(page.webSocketDebuggerUrl);
  let idc = 0; const pending = new Map();
  ws.addEventListener('message', ev => {
    let m; try { m = JSON.parse(ev.data); } catch (e) { return; }
    if (m.id && pending.has(m.id)) { const p = pending.get(m.id); pending.delete(m.id); m.error ? p.reject(new Error(JSON.stringify(m.error))) : p.resolve(m.result); }
  });
  await new Promise((res, rej) => { ws.addEventListener('open', res); ws.addEventListener('error', rej); });
  const send = (method, params) => new Promise((resolve, reject) => { const id = ++idc; pending.set(id, { resolve, reject }); ws.send(JSON.stringify({ id, method, params: params || {} })); });
  const ev = async e => { const r = await send('Runtime.evaluate', { expression: e, returnByValue: true }); return r.result ? r.result.value : undefined; };
  const st = async () => { try { return JSON.parse(await ev("window.__crashState || ''")); } catch (x) { return {}; } };

  await send('Page.navigate', { url });
  for (let i = 0; i < 200; i++) { const s = await st(); if (s.phase) break; await sleep(400); }
  console.log('menu ' + JSON.stringify(await st()));

  await ev("window.__unity.SendMessage('CGDebug','Play','')");
  // countdown is 2.4 s; the earliest bust is 1.05 s into the flight, so a tap at
  // 2.75 s after launch is always inside a live flight.
  await sleep(2750);
  await ev("window.__unity.SendMessage('CGDebug','Cash','')");
  await sleep(500);
  const s1 = await st();
  console.log('after_tap ' + JSON.stringify(s1));
  const ok = s1.phase === 'cashedout' && s1.total > 0;
  console.log(ok ? 'PASS cash out banks points at a known time' : 'FAIL cash out did not bank');
  ws.close(); proc.kill();
  process.exit(ok ? 0 : 1);
})().catch(e => { console.log('FAIL ' + (e && e.message)); process.exit(1); });
