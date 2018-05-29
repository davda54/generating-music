using System;
using System.Linq;

namespace MidiModel
{
    /// <summary>
    /// Transforms internal midi time units into real time units
    /// </summary>
    public static class TimeCalculator
    {
        /// <summary>
        /// Uses midi time division to set the real times of each midi event
        /// </summary>
        public static void ComputeRealTimes(Model midi)
        {
            double ticksPerBeat;
            double secondsPerBeat;

            if (midi.TimeDivision is TicksPerBeatTimeDivision)
            {
                var timeDivision = (TicksPerBeatTimeDivision) midi.TimeDivision;
                secondsPerBeat = 0.5;
                ticksPerBeat = timeDivision.NumberOfClockTicks;
            }
            else
            {
                var timeDivision = (FramesPerSecondTimeDivision) midi.TimeDivision;
                secondsPerBeat = 1 / (timeDivision.NumberOfFrames == 29 ? 29.97 : timeDivision.NumberOfFrames);
                ticksPerBeat = timeDivision.TicksPerFrame;
            }

            var events = midi.Events().OrderBy(e => e.AbsoluteTime).ToList();

            uint lastAbsoluteTime = 0;
            double actualAbsoluteRealTime = 0;

            // we need to traverse the events so that the set tempo events have the right effect
            foreach (var e in events)
            {
                actualAbsoluteRealTime += (e.AbsoluteTime - lastAbsoluteTime) / ticksPerBeat * secondsPerBeat;
                lastAbsoluteTime = e.AbsoluteTime;

                e.AbsoluteRealTime = TimeSpan.FromSeconds(actualAbsoluteRealTime);

                if (e is SetTempo)
                {
                    secondsPerBeat = ((SetTempo) e).MicrosecondsPerQuarterNote / 1000000.0;
                }
            }
        }

        /// <summary>
        /// Computes the lengths of notes
        /// 
        /// Note starts with an NoteOn event and could end either with a NoteOff event or another
        /// NoteOn event with volume 0
        /// </summary>
        public static void CreateNoteLengths(Model midi)
        {
            var channels = midi.Tracks.SelectMany(t => t.Channels);

            foreach (var channel in channels)
            {
                var events = channel.Events.OrderBy(e => e.AbsoluteRealTime).ToList();
                for (var i = 0; i < events.Count; i++)
                {
                    if (!(events[i] is NoteOn) || ((NoteOn) events[i]).Volume == 0) continue;

                    var note = (NoteOn) events[i];

                    // traverse events after the NoteOn event to determine its ending
                    for (var j = i + 1; j < events.Count; j++)
                    {
                        if (events[j] is NoteOff && ((NoteOff) events[j]).NoteNumber == note.NoteNumber)
                        {
                            note.Length = events[j].AbsoluteTime - note.AbsoluteTime;
                            note.RealTimeLength = events[j].AbsoluteRealTime - note.AbsoluteRealTime;
                            note.End = events[j].AbsoluteRealTime;
                            break;
                        }
                        if (events[j] is NoteOn && ((NoteOn) events[j]).NoteNumber == note.NoteNumber && ((NoteOn) events[j]).Volume == 0)
                        {
                            note.Length = events[j].AbsoluteTime - note.AbsoluteTime;
                            note.RealTimeLength = events[j].AbsoluteRealTime - note.AbsoluteRealTime;
                            note.End = events[j].AbsoluteRealTime;
                            break;
                        }
                    }
                }
            }
        }
    }
}
