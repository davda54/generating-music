using System.Collections.Generic;
using System.Linq;

namespace MidiModel
{
    /// <summary>
    /// Model for a midi track
    /// </summary>
    public class Track
    {
        public Channel[] Channels;
        public List<MetaEvent> MetaEvents;

        /// <summary>
        /// How many channels are playable
        /// </summary>
        public int FilledChannelsCount => Channels.Sum(channel => channel.IsPlayable ? 1 : 0);

        /// <summary>
        /// How many midi control events are there in total
        /// </summary>
        public int EventCount => Channels.Sum(channel => channel.Count);

        /// <summary>
        /// How many midi meta events are there in total
        /// </summary>
        public int MetaEventCount => MetaEvents.Count;

        public Track()
        {
            Channels = new Channel[MidiModel.Model.NumberOfChannels];

            for (byte i = 0; i < Channels.Length; i++)
                Channels[i] = new Channel { Number = i };

            MetaEvents = new List<MetaEvent>();
        }

        public override string ToString()
        {
            return $"Track, playable channels: {FilledChannelsCount}";
        }
    }
}
