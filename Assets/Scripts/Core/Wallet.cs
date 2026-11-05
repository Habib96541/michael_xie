using System;
using System.Globalization;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BullRide.Core
{
    public sealed class Wallet
    {
        const string BankKey = "bullRideBank";
        public decimal Balance { get; private set; }
        public decimal Stake { get; private set; }
        public decimal Paid { get; private set; }
        bool active;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int BullRideReadBank(IntPtr buffer, int capacity);
        [DllImport("__Internal")] static extern int BullRideSaveBank(string balance);
#endif

        public void Load()
        {
            string saved = PlayerPrefs.GetString(BankKey);
#if UNITY_WEBGL && !UNITY_EDITOR
            const int capacity = 64;
            IntPtr buffer = Marshal.AllocHGlobal(capacity);
            try
            {
                int result = BullRideReadBank(buffer, capacity);
                if (result >= 0) saved = result == 0 ? null : Marshal.PtrToStringAnsi(buffer);
            }
            finally { Marshal.FreeHGlobal(buffer); }
#endif
            Balance = decimal.TryParse(saved, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal balance) && balance >= 0
                ? balance : RideRules.StartingBank;
        }

        public string Begin(string input)
        {
            if (!decimal.TryParse(input, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out decimal value) || value < 1)
                return "Put in at least $1.";
            if (value > Balance)
                return "You only have " + RideRules.Money(Balance) + ".";
            Stake = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
            Paid = 0;
            active = true;
            return null;
        }

        public decimal LivePayout(double factor) => active ? RideRules.PayoutFor(Stake, factor) : Paid;
        public decimal LiveBalance(double factor) => active ? Balance - Stake + LivePayout(factor) : Balance;

        public void Settle(double factor)
        {
            if (!active) return;
            Paid = LivePayout(factor);
            Balance = Balance - Stake + Paid;
            active = false;
            Save();
        }

        public void Refill()
        {
            if (active || Balance >= 1) return;
            Balance = RideRules.StartingBank;
            Save();
        }

        void Save()
        {
            string saved = Balance.ToString("0.00", CultureInfo.InvariantCulture);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (BullRideSaveBank(saved) == 1) return;
#endif
            PlayerPrefs.SetString(BankKey, saved);
            PlayerPrefs.Save();
        }
    }
}
