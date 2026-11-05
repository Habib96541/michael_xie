const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '..');
const web = path.join(root, 'Builds', 'WebGL');
const html = fs.readFileSync(path.join(web, 'index.html'), 'utf8');
assert(!html.includes('{{{'), 'Unexpanded Unity template macro');
for (const [, script] of html.matchAll(/<script\b[^>]*>([\s\S]*?)<\/script>/gi)) new vm.Script(script);
const referenced = [...html.matchAll(/buildUrl\s*\+\s*'\/([^']+)'/g)].map(match => match[1]);
assert.equal(referenced.length, 4, 'Expected data, framework, wasm, and loader references');
for (const name of referenced) {
  const bytes = fs.readFileSync(path.join(web, 'Build', name));
  const decoded = bytes[0] === 0x1f && bytes[1] === 0x8b ? zlib.gunzipSync(bytes) : bytes;
  if (name.includes('.wasm')) assert(WebAssembly.validate(decoded), 'Invalid WebAssembly module');
  else if (name.includes('.js')) {
    const script = decoded.toString('utf8');
    new vm.Script(script, { filename: name });
    if (name.includes('.framework.js')) {
      assert(script.includes('dropStackRequestFullscreen'), 'The WebGL player must link to the page fullscreen handler');
    }
  }
  console.log(`Validated ${name}: ${bytes.length.toLocaleString()} bytes`);
}
for (const name of ['logo.svg', 'favicon.svg']) assert(fs.readFileSync(path.join(web, name), 'utf8').includes('<svg'));

const viewportPath = path.join(root, 'Assets', 'WebGLTemplates', 'DropStack', 'viewport.js');
const viewportSource = fs.readFileSync(viewportPath, 'utf8');
new vm.Script(viewportSource);
assert.equal(fs.readFileSync(path.join(web, 'viewport.js'), 'utf8'), viewportSource, 'Packaged viewport helper must match the source');
assert(html.includes('<script src="viewport.js"></script>'), 'Viewport helper must load before Unity');
async function verifyFullscreen() {
  const events = {};
  let requests = 0;
  const viewport = {
    style: {},
    requestFullscreen() {
      assert.equal(this, viewport);
      requests++;
      return Promise.reject(new Error('Fullscreen denied by embedder'));
    }
  };
  const fullscreenButton = {};
  const document = {
    getElementById: id => id === 'viewport' ? viewport : fullscreenButton,
    addEventListener: (event, handler) => { events[event] = handler; }
  };
  const navigator = { userActivation: { isActive: false } };
  const visualViewport = {
    width: 390, height: 669, offsetLeft: 0, offsetTop: 0,
    addEventListener: (event, handler) => { events[event] = handler; }
  };
  const window = {
    visualViewport,
    addEventListener() {},
    matchMedia: () => ({ matches: false }),
    requestAnimationFrame: handler => handler()
  };
  vm.runInNewContext(viewportSource, { window, document, navigator });
  assert.equal(viewport.style.width, '390px');
  assert.equal(viewport.style.height, '669px');
  visualViewport.height = 844;
  events.resize();
  assert.equal(viewport.style.height, '844px', 'The game follows expanding browser space');
  await window.dropStackRequestFullscreen();
  assert.equal(requests, 0, 'Fullscreen requires a user gesture');
  navigator.userActivation.isActive = true;
  await window.dropStackRequestFullscreen();
  assert.equal(requests, 1, 'Play requests fullscreen and tolerates a rejected request');
  navigator.standalone = true;
  await window.dropStackRequestFullscreen();
  assert.equal(requests, 1, 'Home Screen mode already fills the available area');
}
verifyFullscreen().then(() => console.log('Viewport and fullscreen checks passed.')).catch(error => {
  console.error(error);
  process.exitCode = 1;
});

const bridge = fs.readFileSync(path.join(root, 'Assets', 'Plugins', 'WebGL', 'DropStackHaptics.jslib'), 'utf8');
function context(navigator, hidden = false, reduceMotion = false) {
  const sandbox = { navigator, document: { hidden }, window: { matchMedia: () => ({ matches: reduceMotion }) }, LibraryManager: { library: {} } };
  sandbox.mergeInto = Object.assign;
  vm.runInNewContext(bridge, sandbox);
  return sandbox.LibraryManager.library;
}
const pulses = [];
const available = context({ vibrate: duration => pulses.push(duration), userActivation: { hasBeenActive: true } });
assert.equal(available.DS_HapticsAvailable(), 1);
available.DS_Vibrate(16);
assert.deepEqual(pulses, [16]);
context({ vibrate: duration => pulses.push(duration), userActivation: { hasBeenActive: false } }).DS_Vibrate(20);
context({ vibrate: duration => pulses.push(duration) }, true).DS_Vibrate(20);
assert.deepEqual(pulses, [16], 'Inactive/hidden document must not vibrate');
context({ vibrate: duration => pulses.push(duration) }, true).DS_Vibrate(0);
assert.deepEqual(pulses, [16, 0], 'Cancellation must work while hidden');
assert.equal(context({}).DS_HapticsAvailable(), 0);
context({}).DS_Vibrate(30);
context({ vibrate: () => { throw new Error('Unsupported hardware'); } }).DS_Vibrate(30);
assert.equal(context({}, false, true).DS_ReducedMotion(), 1);
assert.equal(context({}).DS_ReducedMotion(), 0);
console.log('Web assets and vibration bridge passed.');
