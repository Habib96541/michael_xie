(() => {
  'use strict';

  const root = document.documentElement;
  const canvas = document.getElementById('game');
  const launch = document.getElementById('game-launch');
  const play = document.getElementById('launch-play');
  const heading = document.getElementById('launch-heading');
  const message = document.getElementById('launch-message');
  const install = document.getElementById('launch-install');
  const rotate = document.getElementById('launch-rotate');
  const ua = navigator.userAgent || '';
  const shortSide = Math.min(screen.width, screen.height);
  const iphone = /iPhone|iPod/i.test(ua) ||
    (/Macintosh/i.test(ua) && navigator.maxTouchPoints > 1 && shortSide > 0 && shortSide <= 500);
  const mobile = iphone || /Android|iPad/i.test(ua);
  const installed = navigator.standalone || window.matchMedia?.('(display-mode: standalone)')?.matches ||
    window.matchMedia?.('(display-mode: fullscreen)')?.matches;
  const requestFullscreen = document.fullscreenEnabled && root.requestFullscreen;
  const requestWebKitFullscreen = document.webkitFullscreenEnabled && root.webkitRequestFullscreen;
  const canFullscreen = Boolean(requestFullscreen || requestWebKitFullscreen);
  let launched = installed || !canFullscreen;
  let ready = false;
  let scheduled = false;

  if (mobile) root.classList.add('phone-layout');
  const safe = document.createElement('div');
  safe.setAttribute('aria-hidden', 'true');
  safe.style.cssText = 'position:fixed;visibility:hidden;pointer-events:none;padding:env(safe-area-inset-top,0px) env(safe-area-inset-right,0px) env(safe-area-inset-bottom,0px) env(safe-area-inset-left,0px)';
  if (mobile) document.body.appendChild(safe);

  function fit() {
    scheduled = false;
    const viewport = window.visualViewport;
    const width = viewport?.width || window.innerWidth;
    const height = viewport?.height || window.innerHeight;
    if (mobile) {
      const padding = getComputedStyle(safe);
      const inset = side => parseFloat(padding['padding' + side]) || 0;
      const left = inset('Left');
      const top = inset('Top');
      root.style.setProperty('--garden-frame-width', Math.max(1, width - left - inset('Right')) + 'px');
      root.style.setProperty('--garden-frame-height', Math.max(1, height - top - inset('Bottom')) + 'px');
      root.style.setProperty('--garden-frame-left', ((viewport?.offsetLeft || 0) + left) + 'px');
      root.style.setProperty('--garden-frame-top', ((viewport?.offsetTop || 0) + top) + 'px');
    }

    const needsRotation = mobile && width <= height;
    const blocked = ready && (needsRotation || !launched);
    const wasBlocked = window.bloomDoomPlayBlocked;
    const canvasHadFocus = document.activeElement === canvas;
    window.bloomDoomPlayBlocked = blocked;
    root.classList.toggle('launch-blocked', blocked);
    launch.hidden = !blocked;
    canvas.inert = blocked;
    if (blocked) {
      rotate.toggleAttribute('hidden', !needsRotation);
      heading.textContent = needsRotation ? 'Turn your phone sideways' : 'Ready to grow?';
      message.textContent = needsRotation ?
        'The garden opens in landscape.' : 'Open the garden in fullscreen to play.';
      play.hidden = launched;
      install.hidden = !iphone || installed || canFullscreen;
      if (!wasBlocked && canvasHadFocus) heading.focus({ preventScroll: true });
    } else if (wasBlocked) {
      canvas.focus({ preventScroll: true });
    }
  }

  function schedule() {
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(fit);
  }

  play.addEventListener('click', async () => {
    if (launched) return;
    launched = true;
    try {
      // Fullscreen must be requested in the user's tap.
      if (requestFullscreen) await requestFullscreen.call(root, { navigationUI: 'hide' });
      else await requestWebKitFullscreen.call(root);
      if (mobile && (document.fullscreenElement || document.webkitFullscreenElement)) {
        await screen.orientation?.lock?.('landscape');
      }
    } catch {
      // Keep the landscape view usable when the browser declines either request.
    } finally {
      fit();
    }
  });

  fit();
  window.addEventListener('bloomdoom-play-ready', () => { ready = true; fit(); }, { once: true });
  window.addEventListener('resize', schedule, { passive: true });
  window.addEventListener('orientationchange', () => { schedule(); setTimeout(schedule, 350); }, { passive: true });
  document.addEventListener('fullscreenchange', schedule);
  document.addEventListener('webkitfullscreenchange', schedule);
  window.visualViewport?.addEventListener('resize', schedule, { passive: true });
  window.visualViewport?.addEventListener('scroll', schedule, { passive: true });
})();
