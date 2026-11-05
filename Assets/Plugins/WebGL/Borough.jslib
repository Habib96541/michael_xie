mergeInto(LibraryManager.library, {
  BoroughReport: function (json) { window.boroughState = JSON.parse(UTF8ToString(json)); }
});
