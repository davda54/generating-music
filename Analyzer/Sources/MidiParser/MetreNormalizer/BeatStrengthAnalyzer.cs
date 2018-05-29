using System;
using System.Linq;
using MathExtension;
using MidiModel;

namespace MidiParser.MetreNormalizer
{
    /// <summary>
    /// Detects different beat levels of already detected beats
    /// </summary>
    public class BeatStrengthAnalyzer
    {
        private Model _midi;
        private BeatSegment[] _segments;
        private BeatEvent[] _beats;

        public BeatStrengthAnalyzer(Model midi)
        {
            _midi = midi;
            _beats = _midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime).ToArray();
            _segments = new BeatSegment[_beats.Length];

            for (var i = 0; i < _segments.Length; i++)
            {
                _segments[i] = new BeatSegment(_beats[i], i, _segments);
            }
        }

        public void Analyze()
        {
            AddNotesToSegments();
            ConnectSegments();
            ChooseStrongBeats();
        }
        
        private void ChooseStrongBeats()
        {
            foreach (var beat in _beats) beat.Level = 1;

            var (lastSegmentIndex, _) = _segments.Select(s => s.BestScore.score).MaxWithIndex();

            var segment = _segments[lastSegmentIndex];
            var interval = BeatSegment.IndexToInterval(segment.BestScore.index);
            var prevIndex = segment.GetPreviousIndex(segment.BestScore.index);

            while (interval != null && segment.Index - (int)interval > 0)
            {
                segment.Beat.Level = 0;

                if (interval == 3)
                {
                    _segments[segment.Index - 1].Beat.Level = 2;
                    _segments[segment.Index - 2].Beat.Level = 2;
                }
                else if (interval == 4)
                {
                    _segments[segment.Index - 1].Beat.Level = 2;
                    _segments[segment.Index - 3].Beat.Level = 2;
                }
                else if (interval == 6)
                {
                    _segments[segment.Index - 1].Beat.Level = 2;
                    _segments[segment.Index - 2].Beat.Level = 2;
                    _segments[segment.Index - 4].Beat.Level = 2;
                    _segments[segment.Index - 5].Beat.Level = 2;
                }

                segment = _segments[segment.Index - (int)interval];
                interval = BeatSegment.IndexToInterval(prevIndex);
                prevIndex = segment.GetPreviousIndex(prevIndex);
            }
            segment.Beat.Level = 0;
        }

        private void ConnectSegments()
        {
            foreach (var segment in _segments)
            {
                segment.CalculateBaseScore();
                segment.ConnectToPreviousSegments();
            }
        }

        private void AddNotesToSegments()
        {
            var current = 0;
            var notes = _midi.EventsOfType<NoteOn>().Where(n => n.Volume > 0).OrderBy(n => n.AbsoluteRealTime);

            foreach (var note in notes)
            {
                // while the segment does not contain this note (but we allow the notes to be 50 ms too early)
                while (current < _segments.Length - 1 && _segments[current].Beat.AbsoluteRealTime + _segments[current].Beat.Length <= note.AbsoluteRealTime + TimeSpan.FromMilliseconds(50))
                {
                    current++;
                }

                if (Math.Abs((_segments[current].Beat.AbsoluteRealTime - note.AbsoluteRealTime).TotalMilliseconds) <= 50)
                {
                    _segments[current].AddNote(note);
                }
            }
        }
    }
}
