mergeInto(LibraryManager.library, {
  BrickRushIsTouchDevice: function () { return (navigator.maxTouchPoints > 0 || window.matchMedia('(pointer: coarse)').matches) ? 1 : 0; },
  BrickRushInspect: function (json) { window.brickRushLayout = JSON.parse(UTF8ToString(json)); },
  BrickRushReport: function (json) {
    try { window.brickRushState = JSON.parse(UTF8ToString(json)); }
    catch (error) { console.warn('Brick Rush diagnostics:', error); }
  }
});
