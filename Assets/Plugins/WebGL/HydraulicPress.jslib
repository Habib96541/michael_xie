mergeInto(LibraryManager.library, {
  HP_Report: function(json) { window.hydraulicStatus = JSON.parse(UTF8ToString(json)); }
});
