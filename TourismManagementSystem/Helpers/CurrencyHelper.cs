using System.Globalization;

namespace TourismManagementSystem.Helpers
{
    public static class CurrencyHelper
    {
        /// <summary>
        /// Formats a decimal amount with the Indian Rupee symbol using HTML entity
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <param name="includeDecimals">Whether to include decimal places</param>
        /// <returns>Formatted currency string with HTML entity</returns>
        public static string FormatIndianRupee(decimal amount, bool includeDecimals = true)
        {
            var format = includeDecimals ? "N2" : "N0";
            return $"&#8377;{amount.ToString(format, CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Formats a decimal amount with the Indian Rupee symbol using Unicode
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <param name="includeDecimals">Whether to include decimal places</param>
        /// <returns>Formatted currency string with Unicode symbol</returns>
        public static string FormatIndianRupeeUnicode(decimal amount, bool includeDecimals = true)
        {
            var format = includeDecimals ? "N2" : "N0";
            return $"?{amount.ToString(format, CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Gets the HTML entity for Indian Rupee symbol
        /// </summary>
        /// <returns>HTML entity string</returns>
        public static string GetRupeeSymbolHtml()
        {
            return "&#8377;";
        }

        /// <summary>
        /// Gets the Unicode character for Indian Rupee symbol
        /// </summary>
        /// <returns>Unicode character string</returns>
        public static string GetRupeeSymbolUnicode()
        {
            return "?";
        }

        /// <summary>
        /// Gets a fallback representation for Indian Rupee
        /// </summary>
        /// <returns>Fallback string</returns>
        public static string GetRupeeSymbolFallback()
        {
            return "Rs.";
        }
    }
}