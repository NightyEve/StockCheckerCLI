using System.Globalization;
using System.Net;

namespace StockCheckerCLI.Helpers
{
    public static class PriceHelper
    {
        public static decimal ParsePrice(string priceString)
        {
            if (string.IsNullOrWhiteSpace(priceString))
                return 0;

            // Remove the euro symbol
            var cleaned = priceString.Replace("€", "");

            // Remove spaces and common non-breaking spaces
            cleaned = cleaned.Replace(" ", "")
                             .Replace("\u00A0", "")
                             .Replace("\u202F", "");

            // Check if there is a comma or dot as a decimal separator.
            if (!cleaned.Contains(",") && !cleaned.Contains("."))
            {
                // Assume the last two digits are cents and insert a dot before them.
                if (cleaned.Length > 2)
                    cleaned = cleaned.Insert(cleaned.Length - 2, ".");
            }
            else
            {
                // Replace comma with dot for uniformity.
                cleaned = cleaned.Replace(",", ".");
            }

            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                return value;
            return 0;
        }
    }
}
