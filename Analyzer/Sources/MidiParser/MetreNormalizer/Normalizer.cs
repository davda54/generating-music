using System;
using System.Collections.Generic;
using System.Linq;
using MathExtension;
using MidiModel;
using MidiModel.Simplifiers;

namespace MidiParser.MetreNormalizer
{
    /// <summary>
    /// main class of MetreNormalizer, normalizes and renders the tempo
    /// </summary>
    public class Normalizer
    {
        /// <summary>
        /// Calculates the meter and saves it in BeatEvents
        /// </summary>
        /// <param name="midi"></param>
        public static void CalculateMetre(Model midi)
        {
            var offset = CalculateMeterFitnessMetric(midi);

            //use beats that are already in the midi file
            if (midi.GoodnessOfMetreFit > 0.4)
            {
                midi.IsNormalizedByMidiItself = true;
                CalculateImplicitMetre();
            }

            // else detect the meter by the detector
            else
            {
                midi.IsNormalizedByMidiItself = false;
                PadNotesToZero(midi);

                var metre = Analyze(midi).Select(TimeSpan.FromMilliseconds).ToList();
                for (var i = metre.Sum(t => t.TotalMilliseconds); i <= midi.Length.TotalMilliseconds; i += metre.Last().TotalMilliseconds)
                    metre.Add(metre.Last());

                CreateBeatEvents(metre);

                var beatStrengthAnalyzer = new BeatStrengthAnalyzer(midi);
                beatStrengthAnalyzer.Analyze();
            }

            var firstBeatTime = midi.EventsOfType<BeatEvent>().Min(b => b.AbsoluteRealTime);
            if (firstBeatTime != TimeSpan.Zero)
            {
                var beatEvent = new BeatEvent { AbsoluteRealTime = TimeSpan.Zero, Level = 2, Length = firstBeatTime };
                midi.Tracks[0].MetaEvents.Add(beatEvent);
            }

            if (!midi.IsNormalizedByMidiItself)
            {
                midi.GoodnessOfMetreFit = (float)CalculateMeterFitnessOfBeatEvents(midi);
            }


            void CalculateImplicitMetre()
            {
                var baseTick = GetTicksPerBeat(midi);

                var end = midi.EventsOfType<EndOfTrack>().Max(e => e.AbsoluteTime);
                var timeSignatures = new Queue<TimeSignature>(midi.EventsOfType<TimeSignature>().OrderBy(e => e.AbsoluteTime));
                var actualTimeSignature = new TimeSignature { AbsoluteTime = 0, Numerator = 4, Denominator = 4 };
                var tick = baseTick * 4 / actualTimeSignature.Denominator;
                var barCounter = 0;

                if (offset > 0)
                {
                    midi.Tracks[0].MetaEvents.Add(new BeatEvent { AbsoluteTime = 0, Level = 2 });
                }

                for (var time = offset; time <= end; time += tick)
                {
                    if (timeSignatures.Count > 0 && timeSignatures.Peek().AbsoluteTime <= time)
                    {
                        actualTimeSignature = timeSignatures.Dequeue();
                        barCounter = 0;
                        tick = baseTick * 4 / actualTimeSignature.Denominator;
                    }

                    var level = 2;
                    if (barCounter % actualTimeSignature.Numerator == 0)
                        level = 0;
                    else if ((actualTimeSignature.Numerator == 4 && barCounter % actualTimeSignature.Numerator == 2) ||
                             (actualTimeSignature.Numerator == 6 && barCounter % actualTimeSignature.Numerator == 3))
                        level = 1;

                    var beat = new BeatEvent { AbsoluteTime = time, Level = (byte)level };
                    midi.Tracks[0].MetaEvents.Add(beat);

                    barCounter++;
                }

                TimeCalculator.ComputeRealTimes(midi);
                CalculateBeatLengths();

                var averageLength = midi.EventsOfType<BeatEvent>().Average(b => b.Length.TotalMilliseconds);
                while (averageLength > 1000)
                {
                    var beats = midi.EventsOfType<BeatEvent>().ToArray();
                    foreach (var beat in beats)
                    {
                        midi.Tracks[0].MetaEvents.Add(new BeatEvent { AbsoluteRealTime = beat.AbsoluteRealTime + beat.Length.Divide(2), Level = 2 });
                    }
                    CalculateBeatLengths();
                    averageLength = averageLength / 2;
                }

            }


            void CalculateBeatLengths()
            {
                var beatEvents = midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime).ToArray();
                for (var i = 0; i < beatEvents.Length - 1; i++)
                {
                    beatEvents[i].Length = beatEvents[i + 1].AbsoluteRealTime - beatEvents[i].AbsoluteRealTime;
                }
                beatEvents[beatEvents.Length - 1].Length = beatEvents[beatEvents.Length - 2].Length;
            }


            void CreateBeatEvents(IEnumerable<TimeSpan> metre)
            {
                var time = TimeSpan.Zero;
                foreach (var beat in metre)
                {
                    var beatEvent = new BeatEvent { AbsoluteRealTime = time, Level = 0, Length = beat };
                    midi.Tracks[0].MetaEvents.Add(beatEvent);
                    time += beat;
                }
            }
        }

        public static IEnumerable<double> Analyze(Model midi)
        {
            var tactus = new Tactus(midi);
            return tactus.Compute();
        }

        // If the meter is already detected, normalize it to 100 BPM
        public static void Normalize(Model midi, bool prolongSustainedNotes = false)
        {
            var beatLength = 600;

            var events = midi.Events().OrderBy(k => k.AbsoluteRealTime).GetEnumerator();
            
            var normalizedTime = TimeSpan.Zero;

            if(!events.MoveNext()) throw new Exception("no events");
            var e = events.Current;

            var beatsCopy = midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime).Select(b => new BeatEvent {AbsoluteRealTime = b.AbsoluteRealTime, Length = b.Length});

            foreach (var beat in beatsCopy)
            {
                while (e != null && e.AbsoluteRealTime < beat.AbsoluteRealTime + beat.Length)
                {
                    var delta = (e.AbsoluteRealTime - beat.AbsoluteRealTime).TotalMilliseconds * beatLength / beat.Length.TotalMilliseconds;
                    e.AbsoluteRealTime = normalizedTime + TimeSpan.FromMilliseconds(delta);

                    if(!events.MoveNext()) break;
                    e = events.Current;
                }

                normalizedTime += TimeSpan.FromMilliseconds(beatLength);
            }

            foreach (var beat in midi.EventsOfType<BeatEvent>())
            {
                beat.Length = TimeSpan.FromMilliseconds(beatLength);
            }

            events.Dispose();
            
            TimeCalculator.CreateNoteLengths(midi);
            
            var collector = new VolumeChangeCollector(midi);
            collector.DetermineVolumes();

            if (prolongSustainedNotes)
            {
                var sustainer = new Sustainer(midi);
                sustainer.ProlongSustainedNotes();
            }
        }


        public static uint GetTicksPerBeat(Model midi)
        {
            if (midi.TimeDivision is TicksPerBeatTimeDivision)
                return ((TicksPerBeatTimeDivision)midi.TimeDivision).NumberOfClockTicks;
            else
                return ((FramesPerSecondTimeDivision)midi.TimeDivision).TicksPerFrame;
        }


        /// <summary>
        /// Calculate Meter Fitness Metric (MFM) as defines in the thesis
        /// </summary>
        /// <param name="midi"></param>
        /// <returns>best offset of the metre</returns>
        public static uint CalculateMeterFitnessMetric(Model midi)
        {
            if (!midi.EventsOfType<NoteOn>().Any()) return 0;

            var tick = GetTicksPerBeat(midi);

            var firstNoteOffset = midi.EventsOfType<NoteOn>().Min(n => n.AbsoluteTime);
            
            var zeroOffsetFit = midi.EventsOfType<NoteOn>(n => n.Volume > 0).Average(n => GoodnessOfNote(n, 0));
            var firstNoteOffsetFit = midi.EventsOfType<NoteOn>(n => n.Volume > 0).Average(n => GoodnessOfNote(n, firstNoteOffset));

            if (zeroOffsetFit > firstNoteOffsetFit)
            {
                midi.GoodnessOfMetreFit = (float) zeroOffsetFit;
                return 0;
            }
            else
            {
                midi.GoodnessOfMetreFit = (float) firstNoteOffsetFit;
                return firstNoteOffset;
            }
            
            double GoodnessOfNote(NoteOn note, uint offset)
            {
                var time = (note.AbsoluteTime - offset) % tick;
                var portion = (int)(time * 4.0 / tick + 0.5);
                var multiple = portion % 4 == 0 ? 1.0 : (portion % 2 == 0 ? 0.5 : 0.25);

                var differenceFromIdeal = (time - portion / 4.0 * tick) * 8.0 / tick;

                return Math.Exp(-differenceFromIdeal * differenceFromIdeal * 4*4) * multiple;
            }
        }


        public static double CalculateMeterFitnessOfBeatEvents(Model midi)
        {
            var beats = midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime);
            if(!beats.Any()) throw new ArgumentException("Midi model has no beat events attached");

            var notes = midi.EventsOfType<NoteOn>(n => n.Volume > 0).OrderBy(n => n.AbsoluteRealTime);
            var beatsQueue = new Queue<BeatEvent>(beats);

            var beatNotePairs = new List<(BeatEvent beat, NoteOn note)>();
            foreach (var note in notes)
            {
                var actualBeat = beatsQueue.Peek();
                while (actualBeat.AbsoluteRealTime + actualBeat.Length <= note.AbsoluteRealTime)
                {
                    beatsQueue.Dequeue();
                    actualBeat = beatsQueue.Peek();
                }

                beatNotePairs.Add((actualBeat, note));
            }

            return beatNotePairs.Average(pair => GoodnessOfNote(pair.note, pair.beat));


            double GoodnessOfNote(NoteOn note, BeatEvent beat)
            {
                var length = beat.Length.TotalMilliseconds;
                var time = (note.AbsoluteRealTime - beat.AbsoluteRealTime).TotalMilliseconds;
                var portion = (int)(time * 4.0 / length + 0.5);
                var multiple = portion % 4 == 0 ? 1.0 : (portion % 2 == 0 ? 0.5 : 0.25);

                var differenceFromIdeal = (time - portion / 4.0 * length) * 8.0 / length;

                return Math.Exp(-differenceFromIdeal * differenceFromIdeal * 4 * 4) * multiple;
            }
        }

        /// <summary>
        /// Create percussion sounds for the detected meter
        /// </summary>
        /// <param name="midi"></param>
        /// <param name="prolongSustainedNotes"></param>
        /// <param name="volume"></param>
        public static void AddBeats(Model midi, bool prolongSustainedNotes = false, int volume = 127)
        {
            var channel = midi.Tracks[0].Channels[Channel.PercussionChannelNumber];
            var beatEvents = midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime);

            foreach (var beat in beatEvents)
            {
                var length = TimeSpan.FromMilliseconds(beat.Length.TotalMilliseconds * 0.25);

                var note = new NoteOn
                {
                    ChannelNumber = Channel.PercussionChannelNumber,
                    NoteNumber = beat.Level == 0 ? (byte) 36 : (byte) 44,
                    Volume = beat.Level == 2 ? (byte)(0.75*volume) : (byte)volume,
                    AbsoluteRealTime = beat.AbsoluteRealTime,
                    AbsoluteTime = beat.AbsoluteTime,
                    RealTimeLength = length,
                    End = beat.AbsoluteRealTime + length
                };
                channel.Events.Add(note);
            }


            var collector = new VolumeChangeCollector(midi);
            collector.DetermineVolumes();

            if (prolongSustainedNotes)
            {
                var sustainer = new Sustainer(midi);
                sustainer.ProlongSustainedNotes();
            }
        }

        /// <summary>
        /// Offset the times so that the first note start exacly at 0 ms from the beginning
        /// </summary>
        /// <param name="midi"></param>
        private static void PadNotesToZero(Model midi)
        {
            var firstNoteTime = midi.EventsOfType<NoteOn>().Where(n => n.Volume > 0).Min(n => n.AbsoluteRealTime);
            foreach (var @event in midi.Events())
            {
                if (@event.AbsoluteRealTime < firstNoteTime) @event.AbsoluteRealTime = TimeSpan.Zero;
                else @event.AbsoluteRealTime -= firstNoteTime;
            }
        }
    }
}
