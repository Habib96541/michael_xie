mergeInto(LibraryManager.library, {
  BF_Report: function (jsonPtr) {
    try {
      var json = UTF8ToString(jsonPtr);
      var state = JSON.parse(json);
      window.__bottleFlipState = state;
      if (window.console && console.log) console.log("[bottleflip] " + json);
    } catch (e) {
      if (window.console && console.warn) console.warn("BF_Report failed: " + e);
    }
  },

  BF_Log: function (msgPtr) {
    try {
      var msg = UTF8ToString(msgPtr);
      if (window.console && console.log) console.log("[bf] " + msg);
    } catch (e) {}
  },

  // Playtest tooling: POST a PNG to the local capture server so screenshots can be
  // inspected without relying on browser capture. Harmless when the server is absent.
  BF_UploadPng: function (namePtr, dataPtr, length) {
    try {
      var name = UTF8ToString(namePtr);
      var copy = new Uint8Array(HEAPU8.subarray(dataPtr, dataPtr + length));
      var blob = new Blob([copy], { type: "image/png" });
      var base = window.__bottleFlipShotServer || "http://localhost:8124";
      fetch(base + "/shot?name=" + encodeURIComponent(name), { method: "POST", body: blob, mode: "cors" })
        .then(function (r) { return r.text(); })
        .then(function (t) { console.log("[shot] " + name + " " + t); })
        .catch(function (e) { console.warn("[shot] failed " + e); });
    } catch (e) {
      if (window.console && console.warn) console.warn("[shot] error " + e);
    }
  }
});
