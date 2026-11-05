(function () {
  "use strict";
  var canvas = document.getElementById("unity-canvas");
  var measureCanvas = canvas.getBoundingClientRect.bind(canvas);
  var sideways = false;
  var previousWidth = 0;
  var maximumHeight = 0;
  var frame = 0;

  // Unity uses this rect for both render sizing and input. A CSS-rotated canvas
  // has swapped visual bounds, so report its unrotated, logical game bounds.
  canvas.getBoundingClientRect = function () {
    var rect = measureCanvas();
    if (!sideways) return rect;
    var logical = {
      x: rect.left, y: rect.top, left: rect.left, top: rect.top,
      width: canvas.clientWidth, height: canvas.clientHeight,
      right: rect.left + canvas.clientWidth, bottom: rect.top + canvas.clientHeight
    };
    logical.toJSON = function () {
      return { x: logical.x, y: logical.y, left: logical.left, top: logical.top,
        width: logical.width, height: logical.height, right: logical.right, bottom: logical.bottom };
    };
    return logical;
  };

  function gamePoint(x, y) {
    var rect = measureCanvas();
    return {
      x: rect.left + (y - rect.top) * canvas.clientWidth / rect.height,
      y: rect.top + (rect.right - x) * canvas.clientHeight / rect.width
    };
  }

  function mapInput(event) {
    if (!sideways || event.target !== canvas) return;
    if (event.changedTouches) {
      ["touches", "changedTouches", "targetTouches"].forEach(function (name) {
        var mapped = Array.from(event[name], function (touch) {
          var point = gamePoint(touch.clientX, touch.clientY);
          return { identifier: touch.identifier, target: touch.target,
            clientX: point.x, clientY: point.y, pageX: point.x, pageY: point.y,
            screenX: point.x, screenY: point.y, force: touch.force,
            radiusX: touch.radiusY, radiusY: touch.radiusX, rotationAngle: touch.rotationAngle };
        });
        Object.defineProperty(event, name, { value: mapped, configurable: true });
      });
    } else {
      var point = gamePoint(event.clientX, event.clientY);
      var movementX = event.movementX || 0;
      var movementY = event.movementY || 0;
      Object.defineProperties(event, {
        clientX: { value: point.x, configurable: true }, clientY: { value: point.y, configurable: true },
        pageX: { value: point.x, configurable: true }, pageY: { value: point.y, configurable: true },
        movementX: { value: movementY, configurable: true }, movementY: { value: -movementX, configurable: true }
      });
    }
  }
  // Capture before Unity's handlers. Keep the original trusted event and its
  // preventDefault behavior, adapting only coordinates/lists consumed by Unity.
  ["pointerdown", "pointerup", "pointermove", "mousedown", "mouseup", "mousemove",
    "touchstart", "touchmove", "touchend", "touchcancel"].forEach(function (name) {
    document.addEventListener(name, mapInput, { capture: true, passive: false });
  });

  function update() {
    frame = 0;
    var viewport = window.visualViewport;
    var width = viewport ? viewport.width : window.innerWidth;
    var height = viewport ? viewport.height : window.innerHeight;
    var top = viewport ? viewport.offsetTop : 0;
    if (Math.abs(width - previousWidth) > 2) maximumHeight = height;
    maximumHeight = Math.max(maximumHeight, height);
    // Preserve the orientation while the native keyboard covers the lower screen.
    var keyboardOpen = width === previousWidth && height < maximumHeight * 0.7;
    if (!keyboardOpen) sideways = height > width;
    previousWidth = width;
    document.documentElement.classList.toggle("game-sideways", sideways);
    document.documentElement.style.setProperty("--game-width", Math.max(1, width) + "px");
    document.documentElement.style.setProperty("--game-height", Math.max(1, height) + "px");
    document.documentElement.style.setProperty("--game-top", Math.max(0, top) + "px");
  }
  function schedule() {
    if (!frame) frame = window.requestAnimationFrame(update);
  }
  window.addEventListener("resize", schedule);
  window.addEventListener("orientationchange", schedule);
  if (window.visualViewport) {
    window.visualViewport.addEventListener("resize", schedule);
    window.visualViewport.addEventListener("scroll", schedule);
  }
  if (window.screen && window.screen.orientation)
    window.screen.orientation.addEventListener("change", schedule);
  update();
})();
