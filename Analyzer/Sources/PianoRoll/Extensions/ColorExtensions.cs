using System;
using System.Windows.Media;
using SkiaSharp;

namespace PianoRoll.Extensions
{

    /// <summary>
    /// Extensions for easier manipulation with System.Windows.Media.Color
    /// </summary>
    public static class ColorExtensions
    {

        /// <summary>
        /// Converts System.Windows.Media.Color to SkiaSharp.SKColor
        /// </summary>
        public static SKColor ToSkColor(this Color color)
        {
            return new SKColor(color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Darkens color by an $amount
        /// </summary>
        /// <param name="color">input color</param>
        /// <param name="amount">should be in [0,1]; 0 means original color, 1 means black</param>
        public static Color Darken(this Color color, float amount)
        {
            if (amount < 0 || amount > 1) throw new ArgumentException("Amount should be between 0 and 1");

            amount = 1 - amount;
            return Color.FromArgb(color.A, (byte) (color.R * amount), (byte) (color.G * amount),
                (byte) (color.B * amount));
        }

        /// <summary>
        /// Color inversion; #FFFFFF -> #000000, #123456 -> #EDCBA9, etc...
        /// </summary>
        public static Color Invert(this Color color)
        {
            return Color.FromArgb(color.A, (byte) (byte.MaxValue - color.R), (byte) (byte.MaxValue - color.G),
                (byte) (byte.MaxValue - color.B));
        }

        /// <summary>
        /// Lightens color by an $amount
        /// </summary>
        /// <param name="color">input color</param>
        /// <param name="amount">should be in [0,1]; 0 means original color, 1 means white</param>
        public static Color Lighten(this Color color, float amount)
        {
            if (amount < 0 || amount > 1) throw new ArgumentException("Amount should be between 0 and 1");

            return color.Invert().Darken(amount).Invert();
        }

        /// <summary>
        /// Reduces saturation by an $amount
        /// </summary>
        /// <param name="color">input color</param>
        /// <param name="amount">should be in [0,1]; 0 means original color, 1 means grey of the same intensity</param>
        public static Color Fade(this Color color, float amount)
        {
            if (amount < 0 || amount > 1) throw new ArgumentException("Amount should be between 0 and 1");

            float average = (color.R + color.G + color.B) / 3f;
            return Color.FromArgb(color.A, (byte) (average * amount + color.R * (1 - amount)),
                (byte) (average * amount + color.G * (1 - amount)), (byte) (average * amount + color.B * (1 - amount)));
        }
    }
}
