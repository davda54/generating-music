using System.Collections.Generic;
using System.Linq;

namespace MidiModel
{
    /// <summary>
    /// Class modeling a channel in a midi track
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// All midi control events that belong to this channel
        /// </summary>
        public List<MidiControlEvent> Events = new List<MidiControlEvent>();

        /// <summary>
        /// Number of events in this channel
        /// </summary>
        public int Count => Events.Count;

        /// <summary>
        /// True if this channel produces any sound
        /// </summary>
        public bool IsPlayable => Events.Any(e => e is NoteOn);

        /// <summary>
        /// Number of the channel; lies in [0;15]
        /// </summary>
        public byte Number;

        /// <summary>
        /// Channel number 9 should always be reproduced as percussion
        /// </summary>
        public const int PercussionChannelNumber = 9;

        public override string ToString()
        {
            return $"Channel, events: {Count}";
        }
    }
}
