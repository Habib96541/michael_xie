// End-to-end playtest for the WebGL build (no dependencies).
// Usage: node Tools/verify.js <url> [outPrefix] [width] [height]
// Drives real rounds through the in-game debug API and asserts the rules.

const fs = require('fs');
const path = require('path');
const http = require('http');
const os = require('os');
const { spawn } = require('child_process');

const url = process.argv[2];
const outPrefix = process.argv[3] || 'shots/verify';
const W = parseInt(process.argv[4] || '1280', 10);
const H = parseInt(process.argv[5] || '800', 10);
const PORT = 9600 + Math.floor(Math.random() * 300);
const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

const sleep = ms => new Promise(r => setTimeout(r, ms));
const fails = [];
function check(ok, name, detail) {
  console.log((ok ? 'PASS ' : 'FAIL ') + name + (detail ? ' :: ' + detail : ''));
  if (!ok) fails.push(name);
}

function getJson(p) {
  return new Promise((resolve, reject) => {
    const req = http.get({ host: '127.0.0.1', port: PORT, path: p, timeout: 2000 }, res => {
      let d = '';
      res.on('data', c => (d += c));
      res.on('end', () => { try { resolve(JSON.parse(d)); } catch (e) { reject(e); } });
    });
    req.on('error', reject);
    req.on('timeout', () => req.destroy(new Error('timeout')));
  });
}

async function main() {
  const profile = path.join(os.tmpdir(), 'crashverify-' + Date.now());
  const proc = spawn(CHROME, [
    '--headless=new', '--disable-gpu', '--enable-unsafe-swiftshader', '--hide-scrollbars',
    '--remote-debugging-port=' + PORT, '--window-size=' + W + ',' + H,
    '--user-data-dir=' + profile, '--no-first-run', '--no-default-browser-check',
    '--autoplay-policy=no-user-gesture-required', '--disable-extensions', 'about:blank'
  ], { stdio: 'ignore' });

  let targets = null;
  for (let i = 0; i < 80; i++) {
    try { targets = await getJson('/json/list'); if (targets.some(t => t.type === 'page')) break; } catch (e) {}
    await sleep(250);
  }
  const page = targets.find(t => t.type === 'page');
  if (!page) { console.log('FAIL chrome'); proc.kill(); process.exit(3); }

  const ws = new WebSocket(page.webSocketDebuggerUrl);
  let idc = 0;
  const pending = new Map();
  ws.addEventListener('message', ev => {
    let m; try { m = JSON.parse(ev.data); } catch (e) { return; }
    if (m.id && pending.has(m.id)) {
      const p = pending.get(m.id); pending.delete(m.id);
      m.error ? p.reject(new Error(JSON.stringify(m.error))) : p.resolve(m.result);
    }
  });
  await new Promise((res, rej) => { ws.addEventListener('open', res); ws.addEventListener('error', rej); });

  const send = (method, params) => new Promise((resolve, reject) => {
    const id = ++idc; pending.set(id, { resolve, reject });
    ws.send(JSON.stringify({ id, method, params: params || {} }));
  });
  const evalJs = async (expr) => {
    const r = await send('Runtime.evaluate', { expression: expr, returnByValue: true });
    if (r.exceptionDetails) throw new Error(JSON.stringify(r.exceptionDetails).slice(0, 200));
    return r.result ? r.result.value : undefined;
  };
  const shot = async name => {
    const r = await send('Page.captureScreenshot', { format: 'png' });
    const file = outPrefix + '_' + name + '.png';
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, Buffer.from(r.data, 'base64'));
  };
  const state = async () => {
    const s = await evalJs("window.__crashState || ''");
    try { return JSON.parse(s); } catch (e) { return {}; }
  };
  const waitFor = async (phase, timeout = 15000) => {
    const end = Date.now() + timeout;
    while (Date.now() < end) {
      const s = await state();
      if (s.phase === phase) return s;
      await sleep(120);
    }
    return await state();
  };
  const call = name => evalJs("window.__unity.SendMessage('CGDebug','" + name + "','')");

  await send('Page.enable');
  await send('Runtime.enable');
  await send('Runtime.enable');
  await send('Emulation.setDeviceMetricsOverride', { width: W, height: H, deviceScaleFactor: 1, mobile: false });

  // collect page errors
  const consoleErrors = [];
  ws.addEventListener('message', ev => {
    let m; try { m = JSON.parse(ev.data); } catch (e) { return; }
    if (m.method === 'Runtime.exceptionThrown') consoleErrors.push('exception');
    if (m.method === 'Runtime.consoleAPICalled' && m.params.type === 'error') consoleErrors.push('console.error');
  });

  await send('Page.navigate', { url });
  let ready = false;
  const end = Date.now() + 90000;
  while (Date.now() < end) {
    ready = await evalJs('!!window.__crashReady').catch(() => false);
    if (ready) break;
    await sleep(400);
  }
  check(ready, 'loads and reaches the game', url);
  if (!ready) { await shot('00_failed'); proc.kill(); process.exit(4); }

  await sleep(4200);
  const menu = await state();
  check(menu.phase === 'menu', 'starts on the menu', JSON.stringify(menu.phase));
  check(menu.shields === 3 && menu.total === 0, 'fresh run has 3 shields and 0 points');
  await shot('01_menu');

  await call('Play');
  const cd = await waitFor('flying', 6000);
  check(cd.phase === 'flying', 'countdown leads into a flight');
  await sleep(600);
  await shot('02_flying');

  const before = await state();
  await call('Cash');
  await sleep(350);
  const cashed = await state();
  check(cashed.phase === 'cashedout', 'cashing out ends the round');
  check(cashed.total > 0, 'cash out banks points', 'total=' + cashed.total);
  check(cashed.total >= before.round * 0.9, 'banked points match the multiplier', before.round + ' -> ' + cashed.total);
  check(cashed.streak === 1, 'streak increments');
  await shot('03_cashout');

  // four cash-outs in a row restore a shield
  let s = await waitFor('flying', 12000);
  await sleep(400);
  await shot('02_flying');
  await sleep(1700);
  await shot('02b_midflight');
  if ((await state()).phase === 'flying') { await call('Cash'); await sleep(300); }
  s = await waitFor('flying', 12000);
  const shieldBefore = s.shields;
  await evalJs("window.__unity.SendMessage('CGDebug','ForceCrash','" + ((s.mult || 1) + 0.3).toFixed(2) + "')");
  await sleep(400);
  const crashed = await waitFor('crashed', 6000);
  check(crashed.phase === 'crashed', 'crash ends the round', crashed.phase);
  check(crashed.shields === shieldBefore - 1, 'a crash costs one shield', shieldBefore + ' -> ' + crashed.shields);
  check(crashed.streak === 0, 'a crash resets the streak');
  await shot('04_crash');

  // push to game over: crash every round until shields hit 0, then one more
  for (let i = 0; i < 9; i++) {
    const f = await waitFor('flying', 16000);
    if (f.phase === 'gameover') break;
    if (f.phase !== 'flying') { await sleep(400); continue; }
    await evalJs("window.__unity.SendMessage('CGDebug','ForceCrash','" + ((f.mult || 1) + 0.25).toFixed(2) + "')");
    const c = await waitFor('crashed', 8000);
    console.log('  round crash shields=' + c.shields + ' phase=' + c.phase);
    await sleep(500);
  }
  const over = await waitFor('gameover', 15000);
  check(over.phase === 'gameover', 'running out of shields ends the run', over.phase);
  check(over.total === 0, 'the final crash wipes the total', 'total=' + over.total);
  check(over.best > 0, 'best score is recorded', 'best=' + over.best);
  await shot('05_gameover');

  // reload: best score must survive
  await send('Page.navigate', { url });
  const end2 = Date.now() + 90000;
  let ready2 = false;
  let reloaded = {};
  while (Date.now() < end2) {
    const raw = await evalJs("window.__crashState || ''").catch(() => '');
    if (typeof raw === 'string' && raw.length > 2) {
      try { reloaded = JSON.parse(raw); } catch (e) { await sleep(300); continue; }
      if (reloaded.phase) { ready2 = true; break; }
    }
    await sleep(300);
  }
  check(ready2, 'reloads after a run');
  check(reloaded.best === over.best, 'best score persists across reloads', JSON.stringify(reloaded.best) + ' vs ' + JSON.stringify(over.best));
  await shot('06_reloaded');

  // a round that survives to the last column always bursts at the table end
  await call('Play');
  const f2 = await waitFor('flying', 20000);
  check(f2.phase === 'flying', 'a new run starts after reload');
  // the table end caps every round: asking for a crash past it clamps to it
  let capped = 0;
  for (let i = 0; i < 4; i++) {
    if ((await state()).phase !== 'flying') break;
    await evalJs("window.__unity.SendMessage('CGDebug','ForceCrash','999')");
    await sleep(200);
    const st = await state();
    if (st.phase === 'flying') { capped = st.crashAt; break; }
  }
  check(Math.abs(capped - 7.90) < 0.06, 'the table end caps the round', 'crashAt=' + capped);
  const edge = await waitFor('crashed', 16000);
  check(edge.phase === 'crashed' && edge.mult >= 7.5,
    'the rocket bursts at the end of the table', 'mult=' + edge.mult);
  await shot('07_tableend');

  check(consoleErrors.length === 0, 'no runtime errors in the page', consoleErrors.join(','));
  console.log('SUMMARY ' + (fails.length === 0 ? 'ALL PASS' : fails.length + ' FAILURES: ' + fails.join('; ')));
  ws.close();
  proc.kill();
  process.exit(fails.length === 0 ? 0 : 1);
}

main().catch(e => { console.log('FAIL harness ' + (e && e.message)); process.exit(1); });
