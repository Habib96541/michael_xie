mergeInto(LibraryManager.library, {
  CG_Push: function (ptr) {
    try {
      window.__crashState = UTF8ToString(ptr);
      var el = document.getElementById('crash-state');
      if (el) el.textContent = window.__crashState;
    } catch (e) {}
  },
  CG_Event: function (ptr) {
    try {
      var name = UTF8ToString(ptr);
      window.__crashEvents = window.__crashEvents || [];
      window.__crashEvents.push({ t: performance.now(), name: name });
      if (window.__crashEvents.length > 400) window.__crashEvents.shift();
      console.log('[crash] ' + name);
    } catch (e) {}
  }
});
