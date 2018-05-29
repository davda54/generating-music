using System;
using System.Collections.Generic;
using System.Linq;

namespace MathExtension
{
    public static class Blur
    {
        /// <summary>
        /// An approxiamation of the gaussian blur with using three consecutive box blurs
        /// </summary>
        /// <param name="array">A part of this array will be blurred</param>
        /// <param name="fromIndex">The beggining of the blurred part (inclusive)</param>
        /// <param name="toIndex">The end of the blurred part (inclusive)</param>
        /// <param name="size">Size of the kernel</param>
        public static void Gaussian(double[] array, int fromIndex, int toIndex, int size)
        {
            Box(array, fromIndex, toIndex, size);
            Box(array, fromIndex, toIndex, size);
            Box(array, fromIndex, toIndex, size);
        }

        public static void GaussianWithPreservedSum(double[] array, int fromIndex, int toIndex, int size)
        {
            var initialSum = array.Skip(fromIndex).Take(toIndex - fromIndex + 1).Sum();
            Gaussian(array, fromIndex, toIndex, size);
            var sumAfter = array.Skip(fromIndex).Take(toIndex - fromIndex + 1).Sum();

            var diffPerItem = (sumAfter - initialSum) / (toIndex - fromIndex + 1);
            for (var i = fromIndex; i <= toIndex; i++)
            {
                array[i] -= diffPerItem;
            }
        }

        public static void Average(double[] array, int fromIndex, int toIndex)
        {
            if (toIndex - fromIndex < 1) return;

            var sum = 0.0;
            for (var i = fromIndex; i <= toIndex; i++)
                sum += array[i];

            var average = sum / (toIndex - fromIndex + 1);

            for (var i = fromIndex; i <= toIndex; i++)
                array[i] = average;
        }

        /// <summary>
        /// A standard box blur
        /// </summary>
        /// <param name="array">A part of this array will be blurred</param>
        /// <param name="fromIndex">The beggining of the blurred part (inclusive)</param>
        /// <param name="toIndex">The end of the blurred part (inclusive)</param>
        /// <param name="size">Size of the averaging box</param>
        public static void Box(double[] array, int fromIndex, int toIndex, int size)
        {
            if (toIndex - fromIndex < 1) return;
            if (size % 2 == 0) size++;

            var length = toIndex - fromIndex + 1;
            var tmp = new double[length];

            var sum = 0.0;

            var startAverage = 0.0;
            var endAverage = 0.0;
            for (var i = 0; i < Math.Min(size, length); i++)
            {
                startAverage += array[fromIndex + i];
                endAverage += array[toIndex - i];
            }
            startAverage /= Math.Min(size, length);
            endAverage /= Math.Min(size, length);

            for (var i = -size / 2; i <= size / 2; i++)
            {
                sum += GetValue(array, fromIndex, toIndex, fromIndex + i, startAverage, endAverage);
            }

            for (var i = 0; i < length; i++)
            {
                tmp[i] = sum / size;

                sum -= GetValue(array, fromIndex, toIndex, fromIndex + i - size / 2, startAverage, endAverage);
                sum += GetValue(array, fromIndex, toIndex, fromIndex + i + size / 2 + 1, startAverage, endAverage);
            }

            for (var i = 0; i < length; i++)
            {
                array[fromIndex + i] = tmp[i];
            }
        }

        /// <summary>
        /// Get value from a range in an array, use reflection if out of bounds
        /// </summary>
        private static double GetValue(double[] array, int fromIndex, int toIndex, int index)
        {
            if (toIndex - fromIndex <= 0) throw new ArgumentException("There should be at least two elements in the range");
            while (true)
            {
                if (index < fromIndex)
                {
                    index = 2 * fromIndex - index;
                    continue;
                }
                if (index > toIndex)
                {
                    index = 2 * toIndex - index;
                    continue;
                }
                return array[index];
            }
        }

        /// <summary>
        /// Get value from a range in an array, use start/end misplacement if out of bounds
        /// </summary>
        private static double GetValue(IReadOnlyList<double> array, int fromIndex, int toIndex, int index, double startMisplacement, double endMisplacement)
        {
            if (toIndex - fromIndex < 0)
                throw new ArgumentException("There should be at least one element in the range");

            if (index < fromIndex)
                return startMisplacement;
            if (index > toIndex)
                return endMisplacement;
            return array[index];
        }
    }
}
