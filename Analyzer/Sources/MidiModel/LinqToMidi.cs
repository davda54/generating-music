using System;
using System.Collections.Generic;
using System.Linq;

namespace MidiModel
{
    /// <summary>
    /// Extensions of MidiModel for easier search syntax
    /// </summary>
    public static class LinqToMidi
    {
        public static IEnumerable<MidiControlEvent> Where(this Model midi, Func<MidiControlEvent, bool> pred)
        {
            return midi.Tracks.SelectMany(t => t.Channels).SelectMany(ch => ch.Events).Where(pred);
        }

        public static IEnumerable<MetaEvent> Where(this Model midi, Func<MetaEvent, bool> pred)
        {
            return midi.Tracks.SelectMany(t => t.MetaEvents).Where(pred);
        }

        public static IEnumerable<T> EventsOfType<T>(this Model midi) where T : Event
        {
            if (typeof(T).IsSubclassOf(typeof(MidiControlEvent)))
                return midi.Tracks.SelectMany(t => t.Channels).SelectMany(ch => ch.Events).OfType<T>();
            if (typeof(T).IsSubclassOf(typeof(MetaEvent))) return midi.Tracks.SelectMany(t => t.MetaEvents).OfType<T>();
            else
                throw new ArgumentException(
                    "Template paremeter should be implmentation of either MidiControlEvent or MetaEvent");
        }

        public static IEnumerable<T> EventsOfType<T>(this Model midi, Func<T, bool> pred) where T : Event
        {
            return EventsOfType<T>(midi).Where(pred);
        }

        public static IEnumerable<MetaEvent> MetaEvents(this Model midi)
        {
            return midi.Tracks.SelectMany(t => t.MetaEvents);
        }

        public static IEnumerable<MidiControlEvent> ControlEvents(this Model midi)
        {
            return midi.Tracks.SelectMany(t => t.Channels).SelectMany(ch => ch.Events);
        }

        /// <summary>
        /// Returns all events, i.e. all objects that are successors of abstract class MidiParser.Model.Event
        /// </summary>
        public static IEnumerable<Event> Events(this Model midi)
        {
            return midi.ControlEvents().Concat<Event>(midi.MetaEvents());
        }
    }
}
