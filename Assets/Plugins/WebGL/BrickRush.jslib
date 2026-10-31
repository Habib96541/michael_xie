mergeInto(LibraryManager.library, {
  BrickRushReport: function (json) {
    try { window.brickRushState = JSON.parse(UTF8ToString(json)); }
    catch (error) { console.warn('Brick Rush diagnostics:', error); }
  }
});
