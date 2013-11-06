using System;

namespace Utilities
{
    public static class DateTimeUtility
    {
        /// <summary>
        /// Formats the time to UTC string.
        /// </summary>
        /// <param name="originalDate">The original date.</param>
        /// <returns>System.String.</returns>
        public static string FormatTimeToUtcString(this DateTime originalDate)
        {
            return originalDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        /// <summary>
        /// Formats the time to a sortable date.
        /// </summary>
        /// <param name="originalDate">The original date.</param>
        /// <returns>System.String.</returns>
        public static string FormatTimeToSortableDate(this DateTime originalDate)
        {
            return originalDate.ToString("yyyy-MM-dd");
        }
    }
}
