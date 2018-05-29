using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathExtension;
using MidiModel;

namespace MusParser
{
    /// <summary>
    /// Converts internal music representation into .mus file
    /// </summary>
    public static class ModelToMusicEvents
    {
        private static List<NoteOn> GetNotes(Model midi)
        {
            return midi.EventsOfType<NoteOn>()
                .Where(n => n.Volume > 0 && n.Instrument.Cluster() != InstrumentCluster.Nonmusical)
                .OrderBy(note => note.AbsoluteRealTime).ToList();
        }

        /// <summary>
        /// Returns pairs of notes and deltas (time difference between previous and current note) ordered by note onsets
        /// </summary>
        /// <param name="notes">Ordered collection of notes</param>
        /// <param name="frameRateMillis"></param>
        /// <param name="offset"></param>
        /// <returns>pairs of notes and deltas (time difference between previous and current note) ordered by note onsets</returns>
        private static (List<(NoteOn note, float delta)>, double error) GetPairs(IReadOnlyCollection<NoteOn> notes, int frameRateMillis, double offset)
        {
            var pairs = new List<(NoteOn note, float delta)>();
            double error = 0;

            var time = 0.0;
                
            foreach (NoteOn note in notes)
            {
                var diff = (note.AbsoluteRealTime.TotalMilliseconds - offset - time) / frameRateMillis + 0.5;
                error += Math.Abs(diff - (int)diff - 0.5);
                time += (int) diff * frameRateMillis;

                pairs.Add((note, (int)diff));
            }

            return (pairs, error / (notes.Count - 1));
        }

        private static void WriteNoteOn(BinaryWriter writer, byte pitch, double volume, Instrument instrument, byte channel)
        {
            writer.Write((byte) ((byte)EventType.NoteOnEvent | (channel << 4)));
            writer.Write((byte) (pitch - ClusterRanges.Min((InstrumentCluster)channel)));
            writer.Write((byte) Calc.Clamp((int)((Math.Sign(volume) * Math.Pow(Math.Abs(volume), 1.0/2.0) + 1) * 128 + 0.5), 0, 255));
            writer.Write((byte) instrument.Id());
        }

        private static void WriteNoteOff(BinaryWriter writer, byte pitch, byte channel)
        {
            writer.Write((byte) EventType.NoteOffEvent);
            writer.Write((byte) (pitch - ClusterRanges.Min((InstrumentCluster)channel)));
            writer.Write((byte) channel);
            writer.Write((byte) 0);
        }

        private static void WritePercussionOn(BinaryWriter writer, byte type, double volume)
        {
            writer.Write((byte) EventType.PercussionOnEvent);
            writer.Write((byte) (type - Percussion.MinNoteNumber));
            writer.Write((byte) Calc.Clamp((int) ((Math.Sign(volume) * Math.Pow(Math.Abs(volume), 1.0 / 2.0) + 1) * 128 + 0.5), 0, 255));
            writer.Write((byte) 0);
        }

        private static void WritePercussionOff(BinaryWriter writer, byte type)
        {
            writer.Write((byte) EventType.PercussionOffEvent);
            writer.Write((byte) (type - Percussion.MinNoteNumber));
            writer.Write((byte) 0);
            writer.Write((byte) 0);
        }

        private static void WriteSmallSpace(BinaryWriter writer, ushort tempo, int chord)
        {
            writer.Write((byte) EventType.SmallSpaceEvent);
            writer.Write((ushort) tempo);
            writer.Write((byte) chord);
        }

        private static void WriteBigSpace(BinaryWriter writer, ushort tempo, int chord)
        {
            writer.Write((byte) EventType.BigSpaceEvent);
            writer.Write((ushort) tempo);
            writer.Write((byte) chord);
        }

        private static void WriteSongEnd(BinaryWriter writer)
        {
            writer.Write((byte) EventType.EndEvent);
            writer.Write((byte) 0);
            writer.Write((byte) 0);
            writer.Write((byte) 0);
        }

        private static void WriteClusters(BinaryWriter writer, List<NoteCluster> clusters)
        {
            var pause = 0;

            for(var i = 0; i < clusters.Count; i++)
            {
                var noteStart = clusters[i].NoteStarts.OrderBy(s => s.Channel).ThenBy(s => s.Pitch).ToArray();
                var noteEnd = clusters[i].NoteEnds.OrderBy(s => s.Channel).ThenBy(s => s.Pitch).ToArray();
                var percussionStart = clusters[i].PercussionStarts.OrderBy(s => s.Type).ToArray();
                var percussionEnd = clusters[i].PercussionEnds.OrderBy(s => s.Type).ToArray();

                if (noteStart.Any() || noteEnd.Any() || percussionStart.Any() || percussionEnd.Any())
                {
                    while (pause > 0)
                    {
                        var chordInNextBeat = i - pause + 12 >= clusters.Count ? 24 : clusters[i - pause + 12].Chord.ToInt();

                        if (pause >= 6 && clusters[i - pause].OrderInBeat % 6 == 0)
                        {
                            WriteBigSpace(writer, (ushort) 1, chordInNextBeat);
                            pause -= 6;
                        }
                        else
                        {
                            WriteSmallSpace(writer, (ushort) 1, chordInNextBeat);
                            pause -= 1;
                        }
                    }
                }

                foreach (var beat in percussionEnd) WritePercussionOff(writer, beat.Type);
                foreach (var note in noteEnd) WriteNoteOff(writer, note.Pitch, note.Channel);
                foreach (var beat in percussionStart) WritePercussionOn(writer, beat.Type, beat.Volume);
                foreach (var note in noteStart) WriteNoteOn(writer, note.Pitch, note.Volume, note.Instrument, note.Channel);
               
                pause++;
            }
            
            WriteSongEnd(writer);
        }

        public static void Parse(Model midi, string filename)
        {
            Parse(midi, filename, 50, 0, 0);
        }

        public static void Parse(Model midi, string filename, int frameRateMillis, int pitchChange = 0, int volumeChange = 0)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
               Parse(midi, writer, frameRateMillis, pitchChange, volumeChange);
            }
        }
        
        public static void Parse(Model midi, BinaryWriter writer, int frameRateMillis, int pitchChange = 0, int volumeChange = 0)
        {
            var metre = midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime).ToArray();
            if (!metre.Any()) throw new NonanalyzedMidiException();

            var noteHold = new (int length, int startIndex, double volume)[Model.NumberOfChannels, 128];
            var percussionHold = new int[128];

            var notes = GetNotes(midi);
            if (notes.Count == 0) return;

            var averageVolume = notes.Average(n => n.RealVolume);

            var msPerBeat = 600;
            var ticksPerBeat = msPerBeat / frameRateMillis;
            var offset = ((int)notes.First().AbsoluteRealTime.TotalMilliseconds / msPerBeat) * msPerBeat;

            var (pairs, _) = GetPairs(notes, frameRateMillis, offset);

            var clusters = new List<NoteCluster> { new NoteCluster(metre[0].Length, metre[0].Chord, 0) };
            var frames = offset / frameRateMillis;

            foreach (var (note, delta) in pairs)
            {
                for (var j = 0; j < delta; j++)
                {
                    frames++;
                    var index = Math.Min(frames / ticksPerBeat, metre.Length - 1);
                    clusters.Add(new NoteCluster(metre[index].Length, metre[index].Chord, frames % ticksPerBeat));

                    for (byte ch = 0; ch < Model.NumberOfChannels; ch++)
                    {
                        for (byte i = 0; i < noteHold.GetLength(1); i++)
                        {
                            if (noteHold[ch, i].length == 1) clusters.Last().NoteEnds.Add(new NoteEnd(i, ch));
                            if (noteHold[ch, i].length >= 1) noteHold[ch, i].length--;
                        }
                    }
                    for (byte i = 0; i < percussionHold.Length; i++)
                    {
                        if (percussionHold[i] == 1) clusters.Last().PercussionEnds.Add(new PercussionEnd(i));
                        if (percussionHold[i] >= 1) percussionHold[i]--;
                    }
                }

                if (note.IsPercussion)
                {
                    var type = note.NoteNumber;

                    if (type < Percussion.MinNoteNumber || type > Percussion.MaxNoteNumber) continue;
                    if (clusters.Last().PercussionStarts.Select(s => s.Type).Contains(type)) continue;

                    var length = (int)(note.RealTimeLength.TotalMilliseconds / frameRateMillis + 0.5);
                    if (length > 4800 / frameRateMillis) length = 4800 / frameRateMillis;
                    if (length <= 0) length = 1;

                    if (percussionHold[type] > 0) clusters.Last().PercussionEnds.Add(new PercussionEnd(type));
                    percussionHold[type] = length;

                    clusters.Last().PercussionStarts.Add(new PercussionStart(type, note.RealVolume - averageVolume));
                }

                else
                {
                    var cluster = note.Instrument.Cluster();
                    var channel = (byte)cluster;
                    var pitch = note.NoteNumber + pitchChange;

                    while (pitch > ClusterRanges.Max(cluster))
                        pitch -= 12;
                    while (pitch < ClusterRanges.Min(cluster))
                        pitch += 12;
                    
                    if (noteHold[channel, pitch].length > 0)
                    {
                        var prevNote = noteHold[channel, pitch];
                        if (clusters.Count - 1 - prevNote.startIndex >= 400/frameRateMillis)
                        {
                            clusters.Last().NoteEnds.Add(new NoteEnd((byte) pitch, channel));
                        }
                        else if (note.RealVolume <= prevNote.volume)
                        {
                            continue;
                        }
                        else
                        {
                            var prevNoteStart = clusters[prevNote.startIndex].NoteStarts.Find(n => n.Channel == channel && n.Pitch == pitch);
                            clusters[prevNote.startIndex].NoteStarts.Remove(prevNoteStart);
                        }
                    }

                    var length = (int) (note.RealTimeLength.TotalMilliseconds / frameRateMillis + 0.5);
                    if (length > 4800 / frameRateMillis) length = 4800 / frameRateMillis;
                    if (length <= 0) length = 1;
                    
                    noteHold[channel, pitch] = (length, clusters.Count - 1, note.RealVolume);
                    clusters.Last().NoteStarts.Add(new NoteStart((byte) pitch, note.RealVolume - averageVolume, note.Instrument, channel));
                }
            }

            bool anyNonZero;
            do
            {
                anyNonZero = false;

                frames++;
                var index = Math.Min(frames / ticksPerBeat, metre.Length - 1);
                clusters.Add(new NoteCluster(metre[index].Length, metre[index].Chord, frames % 12));

                for (byte ch = 0; ch < Model.NumberOfChannels; ch++)
                {
                    for (byte i = 0; i < noteHold.GetLength(1); i++)
                    {
                        if (noteHold[ch, i].length == 1) clusters.Last().NoteEnds.Add(new NoteEnd(i, ch));
                        if (noteHold[ch, i].length >= 1)
                        {
                            noteHold[ch, i].length--;
                            anyNonZero = true;
                        }
                    }
                }
                for (byte i = 0; i < percussionHold.Length; i++)
                {
                    if (percussionHold[i] == 1) clusters.Last().PercussionEnds.Add(new PercussionEnd(i));
                    if (percussionHold[i] >= 1)
                    {
                        percussionHold[i]--;
                        anyNonZero = true;
                    }
                }
            } while (anyNonZero);
            

            WriteClusters(writer, clusters);
        }

        public static bool IsMultiinstrumental(Model midi)
        {
            var count = midi
                .EventsOfType<InstrumentChange>()
                .Where(e => e.ChannelNumber != Channel.PercussionChannelNumber)
                .Select(e => e.Instrument.Cluster())
                .Distinct().Count();

            return count > 1;
        }

        public static bool HasPercussion(Model midi)
        {
            return midi.EventsOfType<NoteOn>().Any(e => e.ChannelNumber == Channel.PercussionChannelNumber);
        }
    }
}
