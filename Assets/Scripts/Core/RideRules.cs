using System;
using System.Globalization;

namespace BullRide.Core
{
    public static class RideRules
    {
        public const double Duration = 8;
        public const double SurgeTime = 3;
        public const double MaxMultiplier = 5;
        public const float CenterRadius = .16f;
        public const decimal StartingBank = 100m;
        public const decimal DefaultStake = 10m;

        public static double BaseMultiplierAt(double time) =>
            time <= SurgeTime ? .1 + .65 * (Math.Max(0, time) / SurgeTime) :
                .75 + 1.75 * ((Math.Min(Duration, time) - SurgeTime) / (Duration - SurgeTime));

        public static decimal PayoutFor(decimal stake, double multiplier) =>
            decimal.Round(stake * (decimal)multiplier, 2, MidpointRounding.AwayFromZero);

        public static string Money(decimal amount) => "$" + amount.ToString("N2", CultureInfo.InvariantCulture);

        public static string Factor(double value) => value.ToString("0.0", CultureInfo.InvariantCulture) + "x";

        public static string Time(double seconds) => seconds.ToString("0.00", CultureInfo.InvariantCulture) + " s";
    }
}
