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
  const documentEvents = {};
  const classes = new Set();
  const frames = [];
  const window = {
    innerHeight: 680,
    innerWidth: 360,
    visualViewport: viewport && { ...viewport, addEventListener: (name, callback) => viewportEvents[name] = callback },
    requestAnimationFrame: callback => frames.push(callback),
    addEventListener: (name, callback) => events[name] = callback,
  };
  const size = () => window.visualViewport || { width: window.innerWidth, height: window.innerHeight };
  const canvas = {
    get clientWidth() { return classes.has("game-sideways") ? size().height : size().width; },
    get clientHeight() { return classes.has("game-sideways") ? size().width : size().height; },
    getBoundingClientRect: () => ({ left: 10, top: 20, width: size().width, height: size().height,
      right: 10 + size().width, bottom: 20 + size().height }),
  };
  const document = {
    getElementById: () => canvas,
    addEventListener: (name, callback) => documentEvents[name] = callback,
    documentElement: {
      style: { setProperty: (name, value) => properties[name] = value },
      classList: { toggle: (name, enabled) => enabled ? classes.add(name) : classes.delete(name) },
    },
  };
  vm.runInNewContext(source, { window, document });
  return { window, canvas, classes, properties, events, viewportEvents, documentEvents,
    flush: () => { while (frames.length) frames.shift()(); } };
}

const phone = create({ width: 360, height: 680, offsetTop: 0 });
assert.equal(phone.properties["--game-height"], "680px");
assert.ok(phone.classes.has("game-sideways"), "portrait automatically presents landscape");
assert.equal(phone.canvas.getBoundingClientRect().width, 680, "Unity sees logical landscape width");
assert.equal(phone.canvas.getBoundingClientRect().height, 360, "Unity sees logical landscape height");
const corner = { target: phone.canvas, clientX: 370, clientY: 20 };
phone.documentEvents.mousedown(corner);
assert.equal(corner.clientX, 10, "visual top-right maps to logical top-left");
assert.equal(corner.clientY, 20);
const opposite = { target: phone.canvas, clientX: 10, clientY: 700 };
phone.documentEvents.pointerdown(opposite);
assert.equal(opposite.clientX, 690, "visual bottom-left maps to logical bottom-right");
assert.equal(opposite.clientY, 380);
const touches = [{ identifier: 7, clientX: 370, clientY: 20 }, { identifier: 9, clientX: 10, clientY: 700 }];
const touchEvent = { target: phone.canvas, touches, changedTouches: touches, targetTouches: touches };
phone.documentEvents.touchstart(touchEvent);
assert.equal(touchEvent.touches[0].clientX, 10);
assert.equal(touchEvent.changedTouches[1].clientX, 690);
assert.equal(touchEvent.targetTouches[1].identifier, 9, "preserve multitouch identity");
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
assert.ok(phone.classes.has("game-sideways"), "keyboard must not flip orientation");
phone.window.visualViewport.width = 680;
phone.window.visualViewport.height = 390;
phone.window.visualViewport.offsetTop = 0;
phone.events.orientationchange();
phone.flush();
assert.equal(phone.properties["--game-height"], "390px", "follow rotation");
assert.ok(!phone.classes.has("game-sideways"), "physical landscape removes the extra CSS rotation");
const landscapeClick = { target: phone.canvas, clientX: 100, clientY: 70 };
phone.documentEvents.mousedown(landscapeClick);
assert.equal(landscapeClick.clientX, 100, "leave native landscape coordinates unchanged");
const fallback = create(null);
assert.equal(fallback.properties["--game-height"], "680px");
fallback.window.innerHeight = 480;
fallback.events.resize();
fallback.flush();
assert.equal(fallback.properties["--game-height"], "480px", "support browsers without visualViewport");
console.log("PASS: default landscape, mouse/pointer and multitouch mapping, physical rotation, keyboard, browser bars and fallback");
