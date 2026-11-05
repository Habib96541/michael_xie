mergeInto(LibraryManager.library, {
  CoinPixelRatio: function () { return Module.canvas.width / Math.max(1,Module.canvas.clientWidth); },
  CoinReportState: function (json) { window.coinFlipState = JSON.parse(UTF8ToString(json)); },
  CoinUnlockAudio: function () {
    if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext && WEBAudio.audioContext.state === 'suspended')
      WEBAudio.audioContext.resume();
  }
});
