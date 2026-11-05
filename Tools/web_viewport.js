(function () {
  "use strict";
  // Browser chrome and the on-screen keyboard change the visual viewport without
  // changing the phone's display resolution. Unity follows the canvas CSS size.
  var frame = 0;
  function update() {
    frame = 0;
    var viewport = window.visualViewport;
    var height = viewport ? viewport.height : window.innerHeight;
    var top = viewport ? viewport.offsetTop : 0;
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
  update();
})();
