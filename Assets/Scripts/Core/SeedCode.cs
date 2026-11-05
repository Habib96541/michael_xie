using System;
using System.Globalization;

namespace NeonDash.Core
{
    public static class SeedCode
    {
        const string Prefix = "ND1-";
        public const int Length = 20;

        public static string Generate()
        {
            // These two parts omit the fixed version and variant bits of a version 4 UUID.
            string id = Guid.NewGuid().ToString("N");
            return Prefix + (id.Substring(0, 8) + id.Substring(24, 8)).ToUpperInvariant();
        }

        public static bool TryNormalize(string input, out string code)
        {
            code = (input ?? string.Empty).Trim();
            if (TryGetState(code, out _))
            {
                code = code.ToUpperInvariant();
                return true;
            }

            if (code.Length == 0 || code.Length > 10) return false;
            foreach (char c in code)
                if (c < '0' || c > '9') return false;
            return true;
        }

        internal static bool TryGetState(string code, out ulong state)
        {
            state = 0;
            return code.Length == Length && code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                && ulong.TryParse(code.Substring(Prefix.Length), NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out state);
        }
    }
}
