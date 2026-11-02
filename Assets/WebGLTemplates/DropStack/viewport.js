(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else api.attach(root);
})(typeof window === 'undefined' ? null : window, function () {
  'use strict';
  const portraitAspect = 100 / 217;

  function usePortraitFrame(width, height, userAgent, coarsePointer) {
    return /iPhone|iPod/i.test(userAgent || '') ||
      (width <= 600 && height > width) ||
      (coarsePointer && Math.min(width, height) <= 600);
  }

  function fitFrame(width, height, insets, portrait) {
    const availableWidth = Math.max(0, width - insets.left - insets.right);
    const availableHeight = Math.max(0, height - insets.top - insets.bottom);
    let frameWidth = availableWidth, frameHeight = availableHeight;
    if (portrait) {
      if (availableWidth > availableHeight * portraitAspect) frameWidth = availableHeight * portraitAspect;
      else frameHeight = availableWidth / portraitAspect;
    }
    return {
      width: frameWidth,
      height: frameHeight,
      left: insets.left + (availableWidth - frameWidth) / 2,
      top: insets.top + (availableHeight - frameHeight) / 2
    };
  }

  function attach(win) {
    const viewport = win.document.getElementById('viewport');
    const game = win.document.getElementById('game');
    let pending = false;
    function resize() {
      pending = false;
      const visible = win.visualViewport;
      const width = visible ? visible.width : win.innerWidth;
      const height = visible ? visible.height : win.innerHeight;
      viewport.style.width = width + 'px';
      viewport.style.height = height + 'px';
      viewport.style.left = (visible ? visible.offsetLeft : 0) + 'px';
      viewport.style.top = (visible ? visible.offsetTop : 0) + 'px';
      const style = win.getComputedStyle(viewport);
      const insets = {
        top: parseFloat(style.paddingTop) || 0,
        right: parseFloat(style.paddingRight) || 0,
        bottom: parseFloat(style.paddingBottom) || 0,
        left: parseFloat(style.paddingLeft) || 0
      };
      const portrait = usePortraitFrame(width, height, win.navigator.userAgent, win.matchMedia('(pointer: coarse)').matches);
      const frame = fitFrame(width, height, insets, portrait);
      game.style.width = frame.width + 'px';
      game.style.height = frame.height + 'px';
      game.dataset.frame = portrait ? 'portrait-1:2.17' : 'responsive';
    }
    function queueResize() {
      if (!pending) { pending = true; win.requestAnimationFrame(resize); }
    }
    win.addEventListener('resize', queueResize);
    win.addEventListener('orientationchange', queueResize);
    win.document.addEventListener('fullscreenchange', queueResize);
    if (win.visualViewport) {
      win.visualViewport.addEventListener('resize', queueResize);
      win.visualViewport.addEventListener('scroll', queueResize);
    }
    resize();
  }

  return { portraitAspect, usePortraitFrame, fitFrame, attach };
});
