mergeInto(LibraryManager.library, {
    ShatterVibrate: function (milliseconds) {
        try { if (navigator.vibrate) navigator.vibrate(milliseconds); } catch (error) { /* optional */ }
    },
    ShatterBindVisibility: function (objectPointer) {
        var objectName = UTF8ToString(objectPointer);
        if (window.shatterVisibilityBound) return;
        window.shatterVisibilityBound = true;
        var pause = function () {
            if (window.shatterInstance) window.shatterInstance.SendMessage(objectName, 'BrowserVisibility', '1');
        };
        document.addEventListener('visibilitychange', function () { if (document.hidden) pause(); });
        window.addEventListener('blur', pause);
    },
    ShatterReport: function (jsonPointer) {
        var status = JSON.parse(UTF8ToString(jsonPointer));
        window.shatterStatus = status;
        var accessible = document.getElementById('game-status');
        if (accessible) accessible.textContent = status.State + ', step ' + status.Steps + ', wallet ' + status.Wallet;
    }
});
