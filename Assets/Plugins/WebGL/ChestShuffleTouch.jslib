mergeInto(LibraryManager.library, {
    ChestShuffleUsesTouch: function () {
        return (window.matchMedia('(pointer: coarse)').matches ||
            /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
            (navigator.maxTouchPoints > 1 && /Macintosh/i.test(navigator.userAgent))) ? 1 : 0;
    }
});
