mergeInto(LibraryManager.library, {
    BullRideReadBank: function (buffer, capacity) {
        try {
            var saved = localStorage.getItem("bullRideBank");
            if (saved === null) return 0;
            stringToUTF8(saved, buffer, capacity);
            return 1;
        } catch (error) {
            return -1;
        }
    },
    BullRideSaveBank: function (balance) {
        try {
            localStorage.setItem("bullRideBank", UTF8ToString(balance));
            return 1;
        } catch (error) {
            return 0;
        }
    }
});
