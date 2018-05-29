using System;

namespace MidiModel
{
    /// <summary>
    /// Contains all information of a midi file
    /// </summary>
    public class Model
    {
        public const int NumberOfChannels = 16;

        /// <summary>
        /// Midi file can be in three different formats - 0, 1 and 2
        /// </summary>
        public byte FormatType;

        /// <summary>
        /// Time division from the header chunk
        /// </summary>
        public AbstractTimeDivision TimeDivision;

        /// <summary>
        /// Length from the start to the last end of track message
        /// </summary>
        public TimeSpan Length;

        /// <summary>
        /// Ith element is true if ith channel contains any playable event
        /// </summary>
        public bool[] IsChannelPlayable;

        public Track[] Tracks;

        //public List<(TimeSpan beat, byte key)> Beats = new List<(TimeSpan beat, byte key)>();

        public float MaxRealVolume;

        //public TimeSpan[] Metre;
        public bool IsNormalizedByMidiItself;
        public float GoodnessOfMetreFit;

        public Key? Key = null;
        public bool IsKeyFoundByMidiItself;
    }
}
