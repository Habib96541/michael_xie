(function () {
  'use strict';
  const viewport = document.getElementById('viewport');
  const fullscreenButton = document.getElementById('fullscreen');
  const visual = window.visualViewport;
  const request = viewport.requestFullscreen || viewport.webkitRequestFullscreen;
  const exit = document.exitFullscreen || document.webkitExitFullscreen;
  const fullscreenElement = () => document.fullscreenElement || document.webkitFullscreenElement;
  let pending = false;

  function resize() {
    pending = false;
    viewport.style.width = (visual ? visual.width : window.innerWidth) + 'px';
    viewport.style.height = (visual ? visual.height : window.innerHeight) + 'px';
    viewport.style.left = (visual ? visual.offsetLeft : 0) + 'px';
    viewport.style.top = (visual ? visual.offsetTop : 0) + 'px';
  }

  function queueResize() {
    if (pending) return;
    pending = true;
    window.requestAnimationFrame(resize);
  }

  window.dropStackRequestFullscreen = async function () {
    if (!request || fullscreenElement() || navigator.standalone || window.matchMedia('(display-mode: standalone)').matches) return;
    if (navigator.userActivation && !navigator.userActivation.isActive) return;
    try {
      await request.call(viewport);
    } catch {
      // Embedded browsers can deny fullscreen. Play continues in the visible viewport.
    }
  };

  fullscreenButton.hidden = !request || document.fullscreenEnabled === false;
  fullscreenButton.onclick = async () => {
    if (fullscreenElement() && exit) await exit.call(document);
    else await window.dropStackRequestFullscreen();
  };

  window.addEventListener('resize', queueResize);
  window.addEventListener('orientationchange', queueResize);
  document.addEventListener('fullscreenchange', queueResize);
  document.addEventListener('webkitfullscreenchange', queueResize);
  if (visual) {
    visual.addEventListener('resize', queueResize);
    visual.addEventListener('scroll', queueResize);
  }
  resize();
})();
