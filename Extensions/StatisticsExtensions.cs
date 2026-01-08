using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KS.Foundation
{
    public class DescriptiveStatisticsResult
    {
        public DescriptiveStatisticsResult(){}
        public DescriptiveStatisticsResult(double[] values) 
        {
            if (!values.IsNullOrEmpty())
            {
                Count = values.Length;
                Min = values.Min();
                Max = values.Max();
                Average = values.Average();
                Variance = values.Variance();
                StanderdDeviation = Variance.StandardDeviation();
            }
        }
        public DescriptiveStatisticsResult(IEnumerable<double> values) : this(values.ToArray()) {}

        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Variance { get; set; }
        public double StanderdDeviation { get; set; }

        public override string ToString()
        {
            return String.Format("Count: {0}, Min: {1}, Max: {2}, Avg: {3}, Variance: {4}, Std-Dev: {5}", Count.ToString("n0"), Min, Max, Average, Variance, StanderdDeviation);
        }
    }

    public static class StatisticsExtensions
    {
        public static double Average(this double[] values)
        {
            if (values.IsNullOrEmpty())
                return 0;
            return values.Sum() / values.Length;
        }

        public static double Variance(this double[] values, bool as_sample = true)
        {
            if (values == null)
                return 0;
            if (as_sample && values.Length < 2)
                return 0;
            if (values.Length < 1)
                return 0;

            double avg = Average(values);
            double sumOfSquares = values.Select(v => Math.Pow((v - avg), 2.0)).Sum();

            if (as_sample)
                // standard deviation variance
                return sumOfSquares / (values.Length - 1);
            else
                // population standard deviation variance
                return sumOfSquares / values.Length;
        }

        public static double StandardDeviation(this double variance)
        {
            if (variance <= 0)
                return 0;
            return Math.Sqrt(variance);
        }

        public static double Variance(this IEnumerable<double> values)
        {
            if (values == null)
                return 0;
            return Variance(values.ToArray());
        }
    }
}
