mergeInto(LibraryManager.library, {
  RushIsQA: function () { return new URLSearchParams(window.location.search).get('qa') === '1' ? 1 : 0; },
  RushCssWidth: function () { return window.rushViewport ? window.rushViewport.width : document.getElementById('unity-canvas').getBoundingClientRect().width; },
  RushCssHeight: function () { return window.rushViewport ? window.rushViewport.height : document.getElementById('unity-canvas').getBoundingClientRect().height; },
  RushFullscreen: function () { if (window.unityInstance) window.unityInstance.SetFullscreen(1); },
  RushSnapshot: function (json) {
    window.chickenRushState = JSON.parse(UTF8ToString(json));
    window.chickenRushHistory = window.chickenRushHistory || [];
    if (window.chickenRushHistory.length > 2400) window.chickenRushHistory.shift();
    window.chickenRushHistory.push({time: performance.now(), ...window.chickenRushState});
  }
});
