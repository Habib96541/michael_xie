mergeInto(LibraryManager.library, {
  // localStorage keeps the best score and the mute setting working in the offline
  // single-file build, where indexedDB (Unity's PlayerPrefs backend) is blocked.
  BF_PrefsGet: function (keyPtr) {
    try {
      var value = window.localStorage.getItem(UTF8ToString(keyPtr));
      if (value === null || value === undefined) return 0;
      var buffer = _malloc(lengthBytesUTF8(value) + 1);
      stringToUTF8(value, buffer, lengthBytesUTF8(value) + 1);
      return buffer;
    } catch (e) {
      return 0;
    }
  },

  BF_PrefsSet: function (keyPtr, valuePtr) {
    try {
      window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr));
    } catch (e) {}
  },

  BF_PrefsFree: function (ptr) {
    if (ptr) _free(ptr);
  }
});
