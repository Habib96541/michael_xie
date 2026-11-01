mergeInto(LibraryManager.library, {
  HiLoViewportHeight: function() { return Module.canvas.clientHeight || window.innerHeight; },
  HiLoReady: function() { if (window.hiloReady) window.hiloReady(); },
  HiLoState: function(json) { window.hiloState = JSON.parse(UTF8ToString(json)); window.dispatchEvent(new CustomEvent('hilo:state', { detail: window.hiloState })); },
  HiLoFullscreen: function() { var c = document.getElementById('unity-container'); if (!document.fullscreenElement && c.requestFullscreen) c.requestFullscreen(); else if (document.exitFullscreen) document.exitFullscreen(); }
});
