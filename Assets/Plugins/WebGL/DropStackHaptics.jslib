mergeInto(LibraryManager.library, {
  DS_RequestFullscreen: function () {
    if (typeof window.dropStackRequestFullscreen === 'function') window.dropStackRequestFullscreen();
  },
  DS_HapticsAvailable: function () { return typeof navigator.vibrate === 'function' ? 1 : 0; },
  DS_ReducedMotion: function () { return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 1 : 0; },
  DS_Vibrate: function (duration) {
    try {
      if (typeof navigator.vibrate !== 'function') return;
      if (duration > 0 && (document.hidden || (navigator.userActivation && !navigator.userActivation.hasBeenActive))) return;
      navigator.vibrate(duration);
    } catch (e) { /* Unsupported hardware is a silent no-op. */ }
  }
});
