mergeInto(LibraryManager.library, {
    NeonDash_CopySeed: function (valuePtr) {
        var previous = document.activeElement;
        var field = document.createElement('textarea');
        field.value = UTF8ToString(valuePtr);
        field.readOnly = true;
        field.style.cssText = 'position:fixed;left:0;top:0;opacity:0;pointer-events:none';
        document.body.appendChild(field);
        try {
            field.select();
            return document.execCommand('copy') ? 1 : 0;
        } catch (error) {
            return 0;
        } finally {
            field.remove();
            if (previous) previous.focus();
        }
    },

    NeonDash_InstallSeedPaste: function (targetPtr) {
        var target = UTF8ToString(targetPtr);
        document.addEventListener('paste', function (event) {
            if (!event.clipboardData || (event.target !== Module.canvas && event.target !== document.body)) return;
            var value = event.clipboardData.getData('text/plain');
            if (value) SendMessage(target, 'PasteSeed', value);
        });
    }
});
