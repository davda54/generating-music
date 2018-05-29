using System;
using System.Collections.Generic;
using System.Linq;
using MidiModel;

namespace MidiParser.ChordDetector
{
    /// <summary>
    /// notes in a chord segment and their scores
    /// </summary>
    class NotesInSegment
    {
        private TimeSpan _start;
        private TimeSpan _length;
        private byte _lowestPitch;

        private List<(NoteOn note, double weigth)> _notes;
        private double[] _scores;

        private const double START_ON_BEAT_MULTIPLE = 2;
        private const double BASE_NOTE_SUM = 0.2;

        private static readonly double[] _majorIntervalScores = { 1.0, -0.4, 0.1, -1.4,  0.7, 0.1, 0.2, 0.8, -0.3,  0.0, 0.4,  0.2 };
        private static readonly double[] _minorIntervalScores = { 1.0, -0.2, 0.0,  0.7, -1.2, 0.1, 0.2, 0.6,  0.0, -0.2, 0.4, -0.2 };

        public double TotalNoteWeight { get; private set; }

        public NotesInSegment(TimeSpan start, TimeSpan length, byte lowestPitch)
        {
            _start = start;
            _length = length;
            _lowestPitch = lowestPitch;

            _notes = new List<(NoteOn note, double weigth)>();
            _scores = new double[24];
        }

        public void Join(NotesInSegment other)
        {
            if (other._start < _start) _start = other._start;
            _length = _length + other._length;

            var thisNotes = _notes.Select(n => n.note);
            var otherNotes = other._notes.Select(n => n.note);

            _notes = thisNotes.Union(otherNotes).Select(n => (n, 0.0)).ToList();
        }

        public void AddNote(NoteOn note)    
        {
            _notes.Add((note, 0));
        }

        public void ComputeScores(bool computeStartOnBeatBonus)
        {
            ComputeWeights();

            for (var chordIndex = 0; chordIndex < 24; chordIndex++)
            {
                var chord = new Key(chordIndex);

                var score = 0.0;
                for (var i = 0; i < _notes.Count; i++)
                {
                    var interval = (_notes[i].note.NoteNumber - (int) chord.Tone + 12) % 12;
                    var intervalScore = chord.Scale == Scale.Major
                        ? _majorIntervalScores[interval]
                        : _minorIntervalScores[interval];

                    if (computeStartOnBeatBonus && Math.Abs((_notes[i].note.AbsoluteRealTime - _start).TotalMilliseconds) < 20)
                        intervalScore *= START_ON_BEAT_MULTIPLE;

                    score += intervalScore * _notes[i].weigth;
                }

                _scores[chordIndex] = score;
            }
        }

        private void ComputeWeights()
        {
            TotalNoteWeight = BASE_NOTE_SUM;
            for (var i = 0; i < _notes.Count; i++)
            {
                var length = Math.Min((_start + _length).TotalMilliseconds, _notes[i].note.End.TotalMilliseconds) -
                             Math.Max(_notes[i].note.AbsoluteRealTime.TotalMilliseconds, _start.TotalMilliseconds);
                var noteWeight = _notes[i].note.RealVolume * (length / _length.TotalMilliseconds) * 1.0/((_notes[i].note.NoteNumber - _lowestPitch)/12 + 1);

                _notes[i] = (_notes[i].note, noteWeight);
                TotalNoteWeight += noteWeight;
            }
        }

        public double GetScore(Key chord)
        {
            return _scores[chord.ToInt()];
        }
    }
}
