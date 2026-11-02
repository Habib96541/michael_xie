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
  else if (name.includes('.js')) new vm.Script(decoded.toString('utf8'), { filename: name });
  console.log(`Validated ${name}: ${bytes.length.toLocaleString()} bytes`);
}
for (const name of ['logo.svg', 'favicon.svg']) assert(fs.readFileSync(path.join(web, name), 'utf8').includes('<svg'));

const viewportPath = path.join(root, 'Assets', 'WebGLTemplates', 'DropStack', 'viewport.js');
const viewportSource = fs.readFileSync(viewportPath, 'utf8');
new vm.Script(viewportSource);
assert.equal(fs.readFileSync(path.join(web, 'viewport.js'), 'utf8'), viewportSource, 'Packaged viewport helper must match the source');
assert(html.includes('<script src="viewport.js"></script>'), 'Viewport helper must load before Unity');
const { usePortraitFrame, fitFrame } = require(viewportPath);
const phoneSizes = [[320,568],[375,667],[375,812],[390,844],[393,852],[402,874],[414,896],[428,926],[430,932],[440,956]];
let frameChecks = 0;
for (const [width, height] of phoneSizes) {
  for (const insets of [{top:0,right:0,bottom:0,left:0},{top:59,right:0,bottom:34,left:0}]) {
    for (const browserBars of [0,120]) {
      const availableHeight = height - browserBars;
      const frame = fitFrame(width, availableHeight, insets, true);
      assert(Math.abs(frame.height / frame.width - 2.17) < 1e-8, 'Phone frame must stay 1:2.17');
      assert(frame.left >= insets.left && frame.top >= insets.top, 'Frame must clear the notch');
      assert(frame.left + frame.width <= width - insets.right + 1e-8, 'Frame must fit the screen width');
      assert(frame.top + frame.height <= availableHeight - insets.bottom + 1e-8, 'Frame must clear the home indicator and browser bars');
      frameChecks++;
    }
  }
  assert(usePortraitFrame(width, height, 'Mozilla/5.0 (iPhone)', true));
  assert(usePortraitFrame(height, width, 'Mozilla/5.0 (iPhone)', true), 'Rotated iPhones must retain the tall game frame');
  const landscape = fitFrame(height, width, {top:0,right:59,bottom:21,left:59}, true);
  assert(Math.abs(landscape.height / landscape.width - 2.17) < 1e-8);
  assert(landscape.left >= 59 && landscape.left + landscape.width <= height - 59 + 1e-8);
}
assert(usePortraitFrame(390,844,'',false), 'Narrow portrait previews must use the phone layout');
assert(!usePortraitFrame(1280,720,'',false), 'Desktop layout must stay responsive');
assert(!usePortraitFrame(820,1180,'Mozilla/5.0 (iPad)',true), 'Full-size iPads must keep the responsive layout');
assert.deepEqual(fitFrame(1280,720,{top:0,right:0,bottom:0,left:0},false),{width:1280,height:720,left:0,top:0});
console.log(`Portrait viewport passed ${frameChecks} size, safe-area, and browser-toolbar cases plus rotation/desktop checks.`);

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
