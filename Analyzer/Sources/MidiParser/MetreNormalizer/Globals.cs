using System;

namespace MidiParser.MetreNormalizer
{
    /// <summary>
    /// Global parameters of the meter detector
    /// </summary>
    public static class Globals
    {
        public const int MaxEffectiveLength = 1000; // maximal length of a note that is counted
        public const int PipTime = 35; // quantization time (milliseconds)

        public const double NoteFactor = 1.0; // weight of a note score
        public const double BeatIntervalFactor = 10.0;  // multiplier on the penalty for the difference between successive beats
        public const double NoteBonus = 0.2; // bonus for each note in a pip

        public const double PercussionMultiple = 1.0; // bonus for percussion

        public const int BeatSlop = PipTime; // the beat interval pentalty is zero if the times of the beats differ by less than this //

        public const double TactusMin = 400; // minimal length of a beat
        public const double TactusMax = 1200; // maximal length of a beat
        public const double TactusWidth = 1.8; // maximal relative difference between the longest and shortest beat
        public const double TactusStep = 1.1; // approximation step when choosing the right beat min and max

        public static readonly int MinLength = Tactus.Quantize(TactusMin);
        public static readonly int MaxLength = Tactus.Quantize(TactusMax);

        public const double LengthPower = 2.0;

        public static readonly double DefaultScore = Math.Log((TactusMin + TactusMin) / 2.0 + 1, LengthPower);
    }
}
