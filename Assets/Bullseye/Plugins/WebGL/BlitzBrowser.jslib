mergeInto(LibraryManager.library, {
    BlitzIsQa: function () {
        return new URLSearchParams(window.location.search).has('qa') ? 1 : 0;
    },
    BlitzSave: function (key, value) {
        try {
            var k = UTF8ToString(key), v = UTF8ToString(value);
            localStorage.setItem(k, v);
            return localStorage.getItem(k) === v ? 1 : 0;
        } catch (_) { return 0; }
    },
    BlitzLoad: function (key) {
        var value = '';
        try { value = localStorage.getItem(UTF8ToString(key)) || ''; } catch (_) {}
        var length = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(length);
        stringToUTF8(value, buffer, length);
        return buffer;
    },
    BlitzTelemetry: function (json) {
        if (new URLSearchParams(window.location.search).has('qa')) {
            window.blitzTelemetry = JSON.parse(UTF8ToString(json));
        }
    },
    BlitzVibrate: function () {
        try { if (navigator.vibrate) navigator.vibrate(35); } catch (_) {}
    },
    BlitzHasVibration: function () {
        return typeof navigator.vibrate === 'function' ? 1 : 0;
    }
});
