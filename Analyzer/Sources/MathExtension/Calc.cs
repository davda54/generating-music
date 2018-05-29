using System;

namespace MathExtension
{
    public static class Calc
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value > max) return max;
            if (value < min) return min;
            return value;
        }

        public static TimeSpan Divide(this TimeSpan time, double divider)
        {
            return TimeSpan.FromMilliseconds(time.TotalMilliseconds / divider);
        }
    }
}
