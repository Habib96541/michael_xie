using System;
using System.Globalization;
using System.Numerics;

namespace HiLo.Core
{
    // A bounded-size significand and an arbitrary-size exponent keep endless runs finite in memory.
    public readonly struct ScientificNumber : IComparable<ScientificNumber>
    {
        public readonly double Mantissa;
        public readonly BigInteger Exponent;
        public static ScientificNumber Zero => new ScientificNumber(0);
        public static ScientificNumber One => new ScientificNumber(1);
        public ScientificNumber(double value) : this(value, BigInteger.Zero) { }
        public ScientificNumber(double mantissa, BigInteger exponent)
        {
            if (double.IsNaN(mantissa) || double.IsInfinity(mantissa) || mantissa < 0) throw new ArgumentOutOfRangeException(nameof(mantissa));
            if (mantissa == 0) { Mantissa = 0; Exponent = 0; return; }
            int shift = (int)Math.Floor(Math.Log10(mantissa));
            Mantissa = Math.Round(mantissa / Math.Pow(10, shift), 14, MidpointRounding.ToEven);
            Exponent = exponent + shift;
            if (Mantissa >= 10) { Mantissa /= 10; Exponent++; }
        }
        public ScientificNumber Multiply(double factor) => new ScientificNumber(Mantissa * factor, Exponent);
        public ScientificNumber Add(ScientificNumber other)
        {
            if (Mantissa == 0) return other;
            if (other.Mantissa == 0) return this;
            var high = CompareTo(other) >= 0 ? this : other;
            var low = CompareTo(other) >= 0 ? other : this;
            BigInteger difference = high.Exponent - low.Exponent;
            return difference > 16 ? high : new ScientificNumber(high.Mantissa + low.Mantissa * Math.Pow(10, -(int)difference), high.Exponent);
        }
        public ScientificNumber Subtract(ScientificNumber other)
        {
            if (CompareTo(other) < 0) throw new InvalidOperationException("Insufficient balance");
            if (other.Mantissa == 0) return this;
            BigInteger difference = Exponent - other.Exponent;
            if (difference > 16) return this;
            return new ScientificNumber(Math.Max(0, Mantissa - other.Mantissa * Math.Pow(10, -(int)difference)), Exponent);
        }
        public int CompareTo(ScientificNumber other)
        {
            if (Mantissa == 0 || other.Mantissa == 0) return Mantissa.CompareTo(other.Mantissa);
            int exponentComparison = Exponent.CompareTo(other.Exponent);
            return exponentComparison == 0 ? Mantissa.CompareTo(other.Mantissa) : exponentComparison;
        }
        public string Encode() => Mantissa.ToString("R", CultureInfo.InvariantCulture) + "|" + Exponent.ToString(CultureInfo.InvariantCulture);
        public static ScientificNumber Decode(string value, double fallback = 0)
        {
            try { var p = value.Split('|'); return new ScientificNumber(double.Parse(p[0], CultureInfo.InvariantCulture), BigInteger.Parse(p[1], CultureInfo.InvariantCulture)); }
            catch { return new ScientificNumber(fallback); }
        }
        public string Format(bool multiplier = false)
        {
            string suffix = multiplier ? "×" : "";
            if (Mantissa == 0) return "0" + suffix;
            if (Exponent >= 0 && Exponent < 3) return (Mantissa * Math.Pow(10, (int)Exponent)).ToString(multiplier ? "0.00" : "0.##", CultureInfo.InvariantCulture) + suffix;
            string[] units = { "", "K", "M", "B", "T", "Q" };
            if (Exponent >= 3 && Exponent < 18) { int group = (int)Exponent / 3; return (Mantissa * Math.Pow(10, (int)Exponent % 3)).ToString("0.##", CultureInfo.InvariantCulture) + units[group] + suffix; }
            return Mantissa.ToString("0.##", CultureInfo.InvariantCulture) + "e" + Exponent.ToString(CultureInfo.InvariantCulture) + suffix;
        }
    }
}
