// Run with: node Tools/test_web_viewport.cjs
const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
const path = require("node:path");
const source = fs.readFileSync(path.join(__dirname, "web_viewport.js"), "utf8");

function create(viewport) {
  const properties = {};
  const events = {};
  const viewportEvents = {};
  const frames = [];
  const window = {
    innerHeight: 680,
    visualViewport: viewport && { ...viewport, addEventListener: (name, callback) => viewportEvents[name] = callback },
    requestAnimationFrame: callback => frames.push(callback),
    addEventListener: (name, callback) => events[name] = callback,
  };
  const document = { documentElement: { style: { setProperty: (name, value) => properties[name] = value } } };
  vm.runInNewContext(source, { window, document });
  return { window, properties, events, viewportEvents, flush: () => { while (frames.length) frames.shift()(); } };
}

const phone = create({ height: 680, offsetTop: 0 });
assert.equal(phone.properties["--game-height"], "680px");
phone.window.visualViewport.height = 740;
phone.viewportEvents.resize();
phone.flush();
assert.equal(phone.properties["--game-height"], "740px", "follow expanding browser bars");
phone.window.visualViewport.height = 340;
phone.window.visualViewport.offsetTop = 22;
phone.viewportEvents.resize();
phone.viewportEvents.scroll();
phone.flush();
assert.equal(phone.properties["--game-height"], "340px", "fit above the keyboard");
assert.equal(phone.properties["--game-top"], "22px", "follow keyboard viewport pan");
phone.window.visualViewport.height = 390;
phone.window.visualViewport.offsetTop = 0;
phone.events.orientationchange();
phone.flush();
assert.equal(phone.properties["--game-height"], "390px", "follow rotation");
const fallback = create(null);
assert.equal(fallback.properties["--game-height"], "680px");
fallback.window.innerHeight = 480;
fallback.events.resize();
fallback.flush();
assert.equal(fallback.properties["--game-height"], "480px", "support browsers without visualViewport");
console.log("PASS: initial sizing, browser bars, keyboard resize/pan, rotation and viewport fallback");
