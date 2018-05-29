using System;
using System.Collections.Generic;
using System.Linq;

namespace MidiModel
{
    /// <summary>
    /// Sets the right values to putch bends
    /// </summary>
    public static class PitchBendCalculator
    {

        /// <summary>
        /// Structure representing a change of pitch bend behaviour that is caused by a midi 
        /// control change event
        /// </summary>
        private struct PitchBendChange
        {
            /// <summary>
            /// When does the change occur
            /// </summary>
            public readonly TimeSpan AbsoluteRealTime;

            /// <summary>
            /// What should be the new range of the pitch bend - i.e. (max bend - min bend)
            /// </summary>
            public readonly byte Range;

            public PitchBendChange(TimeSpan absoluteRealTime, byte range)
            {
                AbsoluteRealTime = absoluteRealTime;
                Range = range;
            }
        }

        /// <summary>
        /// Sets the right ranges to each pitch bend according to received midi control change events
        /// </summary>
        public static void DeterminePitchRanges(Model midi)
        {
            var changes = new List<PitchBendChange>[Model.NumberOfChannels];
            for (var i = 0; i < changes.Length; i++)
            {
                changes[i] = new List<PitchBendChange>();
            }

            foreach (var channel in midi.Tracks.SelectMany(t => t.Channels))
            {
                changes[channel.Number].AddRange(CollectChangesFromChannel(channel));
            }

            var pitchBends = midi.EventsOfType<PitchBend>();

            foreach (var bend in pitchBends)
            {
                var change = changes[bend.ChannelNumber].Where(ch => ch.AbsoluteRealTime <= bend.AbsoluteRealTime)
                    .DefaultIfEmpty(new PitchBendChange(TimeSpan.Zero, 4))
                    .Aggregate((prev, act) => prev.AbsoluteRealTime < act.AbsoluteRealTime ? act : prev); // select max

                bend.Range = change.Range;
            }
        }

        public static void JoinPitchBends(Model midi)
        {
            foreach (var channel in midi.Tracks.SelectMany(t => t.Channels))
            {
                JoinPitchBendsInChannel(channel);
            }
        }

        public static void DiscretizeBends(Model midi)
        {
            var bendedNotes = midi.EventsOfType<NoteOn>(n => n.Bends != null && n.Bends.Count > 0).ToArray();

            // divide note into not-bended ones
            foreach (var note in bendedNotes)
            {
                var tmpNote = note;
                var pitch = note.NoteNumber;
                var end = note.End;

                var bends = note.Bends.OrderBy(b => b.AbsoluteRealTime).ToArray();
                note.Bends = new List<PitchBend>();
                
                foreach (var bend in bends)
                {
                    tmpNote.End = bend.AbsoluteRealTime;
                    tmpNote.RealTimeLength = tmpNote.End - tmpNote.AbsoluteRealTime;

                    var bendedPitch = (byte) (pitch + bend.RealPitchChange + 0.5);
                    if (bendedPitch != tmpNote.NoteNumber)
                    {
                        tmpNote = new NoteOn
                        {
                            AbsoluteRealTime = bend.AbsoluteRealTime,
                            AbsoluteTime = bend.AbsoluteTime,
                            ChannelNumber = note.ChannelNumber,
                            Instrument = note.Instrument,
                            NoteNumber = bendedPitch,
                            Volume = note.Volume,
                            RealVolume = note.RealVolume,
                            Bends = new List<PitchBend>()
                        };
                        midi.Tracks[0].Channels[tmpNote.ChannelNumber].Events.Add(tmpNote);
                    }
                }

                tmpNote.End = end;
                tmpNote.RealTimeLength = tmpNote.End - tmpNote.AbsoluteRealTime;
            }

            // delete all pitch bends
            var channels = midi.Tracks.SelectMany(t => t.Channels);
            foreach (var channel in channels)
            {
                channel.Events.RemoveAll(e => e is PitchBend);
            }
        }

        private static void JoinPitchBendsInChannel(Channel channel)
        {
            foreach (var bend in channel.Events.OfType<PitchBend>())
            {
                foreach (var noteOn in channel.Events.OfType<NoteOn>()
                    .Where(n => n.AbsoluteRealTime <= bend.AbsoluteRealTime &&
                                n.AbsoluteRealTime + n.RealTimeLength >= bend.AbsoluteRealTime))
                {

                    noteOn.Bends.Add(bend);
                }
            }
        }

        private enum State
        {
            None,
            After101,
            After100
        };

        private static IEnumerable<PitchBendChange> CollectChangesFromChannel(Channel channel)
        {
            var changes = new List<PitchBendChange>();
            var state = State.None;

            foreach (var e in channel.Events)
            {
                if (!(e is Controller))
                {
                    state = State.None;
                    continue;
                }

                var control = e as Controller;

                switch (state)
                {
                    case State.None:
                        if (IsRight101(control)) state = State.After101;
                        break;

                    case State.After101:
                        if (IsRight100(control)) state = State.After100;
                        else if (IsRight101(control)) state = State.After101;
                        break;

                    case State.After100:
                        if (IsRightPitchChange(control))
                        {
                            var change = new PitchBendChange(control.AbsoluteRealTime, control.ControllerValue);
                            changes.Add(change);
                            state = State.None;
                        }

                        else if (IsRight100(control)) state = State.After100;
                        else if (IsRight101(control)) state = State.After101;
                        break;
                }
            }

            return changes;
        }

        static bool IsRight101(Controller control) => control.ControllerNumber == 101 && control.ControllerValue == 0;
        static bool IsRight100(Controller control) => control.ControllerNumber == 100 && control.ControllerValue == 0;
        static bool IsRightPitchChange(Controller control) => control.ControllerNumber == 6;
    }
}