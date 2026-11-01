mergeInto(LibraryManager.library, {
  PegRushReport: function(pointer) {
    window.pegrushStatus = JSON.parse(UTF8ToString(pointer));
  },
  PegRushRead: function() {
    var value = '';
    try { value = localStorage.getItem('PegRush.Save.v1') || ''; } catch(e) {}
    var size = lengthBytesUTF8(value) + 1;
    var pointer = _malloc(size);
    stringToUTF8(value, pointer, size);
    return pointer;
  },
  PegRushWrite: function(pointer) {
    try { localStorage.setItem('PegRush.Save.v1', UTF8ToString(pointer)); return 1; }
    catch(e) { return 0; }
  }
});
