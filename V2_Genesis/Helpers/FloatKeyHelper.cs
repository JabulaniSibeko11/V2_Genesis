using System.Globalization;

namespace V2_Genesis.Helpers
{
    public static class FloatKeyHelper
    {
        /// <summary>
        /// Normalises a DB float key to a plain integer string.
        /// PRIMARY FIX  → change result class properties to double?
        ///                so Dapper never produces scientific notation.
        /// SAFETY NET   → this method handles any leftover cases.
        ///
        /// "5.48365e 008" → "548365000"  (space stripped, then parsed)
        /// "5.48365e+008" → "548365000"  (standard scientific)
        /// "548365467"    → "548365467"  (fast path — no parsing)
        /// "10573515.0"   → "10573515"   (decimal stripped)
        /// null / ""      → ""
        /// </summary>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            // ── Strip ALL internal whitespace ─────────────────────
            // "5.48365e 008" → "5.48365e008"
            var cleaned = value.Replace(" ", "");

            // ── Fast path — plain integer, nothing to convert ─────
            if (!cleaned.Contains('e', StringComparison.OrdinalIgnoreCase)
                && !cleaned.Contains('.'))
                return cleaned;

            // ── Parse scientific or decimal form ──────────────────
            if (double.TryParse(cleaned,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var d))
                return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);

            return value.Trim();    // fallback
        }
    }
}
