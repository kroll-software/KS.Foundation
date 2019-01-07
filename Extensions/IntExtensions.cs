using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Foundation
{
    /// <summary>
    /// Integer Extensions
    /// </summary>
    public static class IntExtensions
    {
        #region PercentageOf calculations

		/// <summary>
		/// The numbers percentage
		/// </summary>
		/// <param name="number">The number.</param>
		/// <param name="percent">The percent.</param>
		/// <returns>The result</returns>
        public static decimal PercentageOf(this int number, int percent)
        {
			return (decimal)(number * percent / 100);
        }

		/// <summary>
		/// Percentage of the number.
		/// </summary>
		/// <param name="percent">The percent</param>
		/// <param name="number">The Number</param>
		/// <returns>The result</returns>
		public static decimal PercentOf(this int position, int total)
		{
            decimal result = 0;
            if (position > 0 && total > 0)
                result = (decimal)position / (decimal)total * 100;
            return result;
		}
        public static decimal PercentOf(this int? position, int total)
        {
            if (position == null) return 0;
            
            decimal result = 0;
            if (position > 0 && total > 0)
                result = (decimal)((decimal)position / (decimal)total * 100);
            return result;
        }

		/// <summary>
		/// The numbers percentage
		/// </summary>
		/// <param name="number">The number.</param>
		/// <param name="percent">The percent.</param>
		/// <returns>The result</returns>
        public static decimal PercentageOf(this int number, float percent)
        {
			return (decimal)(number * percent / 100);
        }

		/// <summary>
		/// Percentage of the number.
		/// </summary>
		/// <param name="percent">The percent</param>
		/// <param name="number">The Number</param>
		/// <returns>The result</returns>
		public static decimal PercentOf(this int position, float total)
		{
            decimal result = 0;
            if (position > 0 && total > 0)
                result = (decimal)((decimal)position / (decimal)total * 100);
            return result;
		}

		/// <summary>
		/// The numbers percentage
		/// </summary>
		/// <param name="number">The number.</param>
		/// <param name="percent">The percent.</param>
		/// <returns>The result</returns>
        public static decimal PercentageOf(this int number, double percent)
        {
			return (decimal)(number * percent / 100);
        }

		/// <summary>
		/// Percentage of the number.
		/// </summary>
		/// <param name="percent">The percent</param>
		/// <param name="number">The Number</param>
		/// <returns>The result</returns>
		public static decimal PercentOf(this int position, double total)
		{
            decimal result = 0;
            if (position > 0 && total > 0)
                result = (decimal)((decimal)position / (decimal)total * 100);
            return result;
		}

        /// <summary>
		/// The numbers percentage
		/// </summary>
		/// <param name="number">The number.</param>
		/// <param name="percent">The percent.</param>
		/// <returns>The result</returns>
		public static decimal PercentageOf(this int number, decimal percent)
        {
			return (decimal)(number * percent / 100);
        }

		/// <summary>
		/// Percentage of the number.
		/// </summary>
		/// <param name="percent">The percent</param>
		/// <param name="number">The Number</param>
		/// <returns>The result</returns>
		public static decimal PercentOf(this int position, decimal total)
		{
            decimal result = 0;
            if (position > 0 && total > 0)
                result = (decimal)position / (decimal)total * 100;
            return result;
		}

		/// <summary>
		/// The numbers percentage
		/// </summary>
		/// <param name="number">The number.</param>
		/// <param name="percent">The percent.</param>
		/// <returns>The result</returns>
		public static decimal PercentageOf(this int number, long percent)
        {
			return (decimal)(number * percent / 100);
        }

		/// <summary>
		/// Percentage of the number.
		/// </summary>
		/// <param name="percent">The percent</param>
		/// <param name="number">The Number</param>
		/// <returns>The result</returns>
		public static decimal PercentOf(this int position, long total)
		{
            decimal result = 0;
            if (position > 0 && total > 0)
                result = (decimal)position / (decimal)total * 100;
            return result;
		}

        #endregion

        public static string ToString(this int? value, string defaultvalue)
        {
            if (value == null) return defaultvalue;
            return value.Value.ToString();
        }

        public static bool ToBool(this int? value)
        {
            if (value == null) return false;
            if (value == 0)
                return false;
            else
                return true;
        }

        public static int SafeProgressValue(this int i)
        {
            return SafeProgressValue(i, 0, 100);
        }

        public static int SafeProgressValue(this int i, int min, int max)
        {
            if (i < min)
            {
                i = min;
            }
            if (i > max)
            {
                i = max;
            }
            return i;
        }
    }
}
