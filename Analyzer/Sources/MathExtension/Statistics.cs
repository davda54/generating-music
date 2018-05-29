using System;
using System.Collections.Generic;
using System.Linq;

namespace MathExtension
{
    public static class Statistics
    {
        public static double StandardDeviation(IEnumerable<double> inputs)
        {
            var enumerable = inputs.ToArray();

            if (enumerable.Length == 0) return 0;
            if (enumerable.Length == 1) return enumerable[0];

            var mean = enumerable.Average();
            return Math.Sqrt(enumerable.Sum(v => (v - mean) * (v - mean)) / (enumerable.Length - 1));
        }

        public static float StandardDeviation(IEnumerable<float> inputs)
        {
            var enumerable = inputs.ToArray();

            if (enumerable.Length == 0) return 0;
            if (enumerable.Length == 1) return enumerable[0];

            var mean = enumerable.Average();
            return (float)Math.Sqrt(enumerable.Sum(v => (v - mean) * (v - mean)) / (enumerable.Length - 1));
        }

        public static double ChiSquared(IEnumerable<double> expected, IEnumerable<double> observed)
        {
            return expected.Zip(observed, (e, o) => (e - o) * (e - o) / e).Sum();
        }

        public static double Contingency(IReadOnlyList<double> expected, IReadOnlyList<double> observed)
        {
            var x2 = ChiSquared(expected, observed);
            return Math.Sqrt(x2 / (expected.Count + x2));
        }

        public static double Covariance(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            if(a.Count == 0 || a.Count != b.Count) throw new ArgumentException("Arguments should be of the same length and have at least one item.");

            var averageA = a.Average();
            var averageB = b.Average();

            return a.Zip(b, (aa, bb) => (aa - averageA) * (bb - averageB)).Sum() / (a.Count - 1);
        }

        public static double Correlation(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            if (a.Count == 0 || a.Count != b.Count) throw new ArgumentException("Arguments should be of the same length and have at least one item.");

            var averageA = a.Average();
            var averageB = b.Average();

            var varA = a.Select(aa => (aa - averageA) * (aa - averageA)).Sum();
            var varB = b.Select(bb => (bb - averageB) * (bb - averageB)).Sum();

            return a.Zip(b, (aa, bb) => (aa - averageA) * (bb - averageB)).Sum() / Math.Sqrt(varA*varB);
        }
    }
}
