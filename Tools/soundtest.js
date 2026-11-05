// Checks the rocket (engine) loop starts with a flight and stops on cash out and on a bust.
// Usage: node Tools/soundtest.js <url>
const path = require('path');
const http = require('http');
const os = require('os');
const { spawn } = require('child_process');

const url = process.argv[2];
const PORT = 9700 + Math.floor(Math.random() * 200);
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
  const profile = path.join(os.tmpdir(), 'crashsnd-' + Date.now());
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
  const call = (m, a) => ev(`window.__unity.SendMessage('CGDebug','${m}','${a}')`);

  let fails = 0;
  const check = (ok, name, extra) => { console.log((ok ? 'PASS ' : 'FAIL ') + name + (extra ? ' :: ' + extra : '')); if (!ok) fails++; };
  const show = s => 'phase=' + s.phase + ' engineOn=' + s.engineOn + ' level=' + s.engineLevel;

  await send('Page.navigate', { url });
  for (let i = 0; i < 200; i++) { const s = await st(); if (s.phase) break; await sleep(400); }
  check((await st()).engineOn !== true, 'engine is silent on the menu', show(await st()));

  await call('Play', '');
  await sleep(2750);                       // countdown 2.4 s, earliest bust 1.05 s later
  const flying = await st();
  check(flying.phase === 'flying' && flying.engineOn === true, 'engine loop runs during the flight', show(flying));
  check(flying.engineLevel > 0.05, 'engine loop is audible during the flight', show(flying));

  await call('Cash', '');
  await sleep(1000);
  const cashed = await st();
  check(cashed.phase === 'cashedout', 'cash out ends the round', show(cashed));
  check(cashed.engineOn === false, 'engine stops on cash out', show(cashed));

  // next round: bust it and check the loop stops there too
  for (let i = 0; i < 40; i++) { const s = await st(); if (s.phase === 'flying') break; await sleep(250); }
  for (let i = 0; i < 8; i++) {
    const s = await st();
    if (s.phase === 'crashed') break;
    if (s.phase === 'flying') await call('ForceCrash', '1.6');
    await sleep(500);
  }
  await sleep(300);
  const crashed = await st();
  check(crashed.phase === 'crashed', 'the round busts', show(crashed));
  check(crashed.engineOn === false, 'engine stops on a bust', show(crashed));

  ws.close(); proc.kill();
  console.log(fails === 0 ? 'SUMMARY ALL PASS' : 'SUMMARY ' + fails + ' FAILURES');
  process.exit(fails === 0 ? 0 : 1);
})().catch(e => { console.log('FAIL ' + (e && e.message)); process.exit(1); });
