using System;
using SkiaSharp;

namespace PianoRoll.Extensions
{

    /// <summary>
    /// Extensions to make SKPoint behave like a 2D vector
    /// </summary>
    static class SKPointExtensions
    {
        /// <summary>
        /// Division with a scalar
        /// </summary>
        public static SKPoint Div(this SKPoint numerator, float denominator)
        {
            return new SKPoint(numerator.X / denominator, numerator.Y / denominator);
        }

        /// <summary>
        /// Multiplication with a scalar
        /// </summary>
        public static SKPoint Mul(this SKPoint multiplier, float multiplicant)
        {
            return new SKPoint(multiplier.X * multiplicant, multiplier.Y * multiplicant);
        }

        /// <summary>
        /// Length of the vector in the Euclidian norm
        /// </summary>
        public static float Length(this SKPoint input)
        {
            return (float) Math.Sqrt(input.X * input.X + input.Y * input.Y);
        }

        /// <summary>
        /// Normalization in the Euclidian norm
        /// </summary>
        public static SKPoint Norm(this SKPoint input)
        {
            return input.Div(input.Length());
        }

        /// <summary>
        /// Euclidian distance between two points
        /// </summary>
        public static float Distance(this SKPoint a, SKPoint b)
        {
            return (a - b).Length();
        }
    }
}
