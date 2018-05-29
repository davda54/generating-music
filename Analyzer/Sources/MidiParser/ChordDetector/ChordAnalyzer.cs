using System;
using System.Collections.Generic;
using System.Linq;
using MathExtension;
using MidiModel;

namespace MidiParser.ChordDetector
{
    /// <summary>
    /// the main class coordiniting the computation
    /// </summary>
    public class ChordAnalyzer
    {
        private Model _midi;
        private ChordSegment[] _segments;
        private BeatEvent[] _beats;

        private byte _lowestPitch;

        public ChordAnalyzer(Model midi)
        {
            _midi = midi;
            _beats = _midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime).ToArray();
            _segments = new ChordSegment[_beats.Length];

            _lowestPitch = midi.EventsOfType<NoteOn>(n => !n.IsPercussion && n.Volume > 0).Min(n => n.NoteNumber);

            var keyChanges = midi.IsKeyFoundByMidiItself
                ? new Queue<KeySignature>(midi.EventsOfType<KeySignature>().OrderBy(k => k.AbsoluteRealTime))
                : new Queue<KeySignature>();
            var actualKey = midi.IsKeyFoundByMidiItself ? keyChanges.Dequeue().Key : midi.Key.Value;

            _segments[0] = new ChordSegment(actualKey, null, _beats[0], _lowestPitch);
            for (var i = 1; i < _segments.Length; i++)
            {
                while (keyChanges.Count > 0 && keyChanges.Peek().AbsoluteRealTime <= _beats[i].AbsoluteRealTime)
                    actualKey = keyChanges.Dequeue().Key;

                _segments[i] = new ChordSegment(actualKey, _segments[i-1], _beats[i], _lowestPitch);
            }
        }

        public void Analyze()
        {
            AddNotesToSegments();
            ConnectSegments();
            ChooseBestChords();
        }


        /// <summary>
        /// play the detected chords
        /// </summary>
        /// <param name="volume"></param>
        public void AddChordNotesToModel(int volume = 127)
        {
            var channel = _midi.Tracks[0].Channels[11];
            
            foreach (var beat in _beats)
            {
                var baseNote = (int)beat.Chord.Tone + 4 * 12;
                var notes = new[] { baseNote - 12, baseNote, baseNote + (beat.Chord.Scale == Scale.Major ? 4 : 3), baseNote + 7 };

                foreach (var pitch in notes)
                {
                    var note = new NoteOn
                    {
                        ChannelNumber = 11,
                        NoteNumber = (byte)pitch,
                        Volume = (byte)(volume*(1 - beat.Level*0.125)),
                        AbsoluteTime = beat.AbsoluteTime,
                        AbsoluteRealTime = beat.AbsoluteRealTime,
                        RealTimeLength = beat.Length,
                        End = beat.AbsoluteRealTime + beat.Length
                    };
                    channel.Events.Add(note);
                }
            }

            var collector = new VolumeChangeCollector(_midi);
            collector.DetermineVolumes();
        }

        private void ChooseBestChords()
        {
            var (index, _) = _segments.Last().Scores.MaxWithIndex();
            _segments.Last().Beat.Chord = new Key(index);

            for (var i = _segments.Length - 2; i >= 0; i--)
            {
                index = _segments[i + 1].BestPrevious[index];
                _segments[i].Beat.Chord = new Key(index);
            }
        }

        private void ConnectSegments()
        {
            _segments[0].CalculateBaseScores();
            _segments[0].Scores = _segments[0].BaseScores;

            for (var i = 1; i < _segments.Length; i++)
            {
                _segments[i].CalculateBaseScores();
                _segments[i].ConnectToPreviousSegment();
            }
        }

        private void AddNotesToSegments()
        {
            var current = 0;
            var notes = _midi.EventsOfType<NoteOn>().Where(n => !n.IsPercussion && n.Volume > 0).OrderBy(n => n.AbsoluteRealTime);

            foreach (var note in notes)
            {
                // while the segment does not contain this note
                while (_segments[current].Beat.AbsoluteRealTime + _segments[current].Beat.Length <= note.AbsoluteRealTime)
                {
                    current++;
                }

                for(var span = current; span < _segments.Length && _segments[span].Beat.AbsoluteRealTime < note.End; span++)
                {
                    _segments[span].AddNote(note);
                }
            }

            var notesInLevel0 = new NotesInSegment(TimeSpan.Zero, TimeSpan.Zero, _lowestPitch);
            var notesInLevel1 = new NotesInSegment(TimeSpan.Zero, TimeSpan.Zero, _lowestPitch);

            foreach (var segment in _segments)
            {
                if (segment.Beat.Level == 1 || segment.Beat.Level == 0)
                {
                    notesInLevel1.ComputeScores(true);
                    notesInLevel1 = new NotesInSegment(segment.Beat.AbsoluteRealTime, TimeSpan.Zero, _lowestPitch);
                }
                if (segment.Beat.Level == 0)
                {
                    notesInLevel0.ComputeScores(false);
                    notesInLevel0 = new NotesInSegment(segment.Beat.AbsoluteRealTime, TimeSpan.Zero, _lowestPitch);
                }

                segment.NotesInLevel0Beats = notesInLevel0;
                segment.NotesInLevel1Beats = notesInLevel1;

                notesInLevel0.Join(segment.NotesInLevel2Beats);
                notesInLevel1.Join(segment.NotesInLevel2Beats);
            }

            notesInLevel0.ComputeScores(false);
            notesInLevel1.ComputeScores(true);
        }
    }
}
