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
