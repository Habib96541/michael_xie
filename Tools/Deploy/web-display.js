(() => {
  const game = document.getElementById('game');
  const canvas = document.getElementById('unity-canvas');
  const fullscreen = document.getElementById('fullscreen');
  const enter = document.fullscreenEnabled && game.requestFullscreen
    ? game.requestFullscreen.bind(game)
    : document.webkitFullscreenEnabled && game.webkitRequestFullscreen?.bind(game);
  if (!enter) return;

  const exit = (document.exitFullscreen || document.webkitExitFullscreen)?.bind(document);
  const isFullscreen = () => (document.fullscreenElement || document.webkitFullscreenElement) === game;
  let firstInteraction = true;
  let landscapeRequested = false;

  async function syncFullscreen() {
    const active = isFullscreen();
    fullscreen.setAttribute('aria-pressed', String(active));
    fullscreen.title = active ? 'Exit fullscreen' : 'Enter fullscreen';
    if (active && !landscapeRequested && screen.orientation?.lock) {
      landscapeRequested = true;
      try { await screen.orientation.lock('landscape'); }
      catch { /* The rotate prompt covers devices that cannot lock orientation. */ }
    } else if (!active && landscapeRequested) {
      landscapeRequested = false;
      screen.orientation?.unlock?.();
    }
  }

  async function toggleFullscreen() {
    firstInteraction = false;
    try {
      if (isFullscreen()) await exit?.();
      else await enter();
    } catch {
      // Keep the game playable in the available viewport if fullscreen is denied.
    }
  }

  function enterOnFirstInteraction(event) {
    if (event.isTrusted && firstInteraction && !isFullscreen()) toggleFullscreen();
  }

  fullscreen.hidden = false;
  fullscreen.addEventListener('click', toggleFullscreen);
  // Unity can suppress the synthetic click after a touch on the canvas.
  canvas.addEventListener('pointerup', event => {
    if (event.isPrimary && event.button === 0) enterOnFirstInteraction(event);
  }, { capture: true });
  canvas.addEventListener('click', enterOnFirstInteraction);
  canvas.addEventListener('keydown', event => {
    if (event.code === 'Space' || event.code === 'Enter') enterOnFirstInteraction(event);
  });
  document.addEventListener('fullscreenchange', syncFullscreen);
  document.addEventListener('webkitfullscreenchange', syncFullscreen);
})();
