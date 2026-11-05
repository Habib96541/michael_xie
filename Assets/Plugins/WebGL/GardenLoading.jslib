mergeInto(LibraryManager.library, {
  BloomDoomLoadingReady: function () {
    window.bloomDoomSceneReady = true;
    window.dispatchEvent(new Event('bloomdoom-ready'));
  },
  BloomDoomPlayBlocked: function () {
    return window.bloomDoomPlayBlocked ? 1 : 0;
  }
});
