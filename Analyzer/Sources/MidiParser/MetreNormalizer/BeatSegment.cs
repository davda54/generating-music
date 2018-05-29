using System.Collections.Generic;
using System.Linq;
using MidiModel;

namespace MidiParser.MetreNormalizer
{
    /// <summary>
    /// Segment of notes for detection of strong beats
    /// </summary>
    class BeatSegment
    {
        public BeatEvent Beat;
        public double[] Scores;
        public (int? index, double score) BestScore;
        public readonly int Index;

        private int?[] _previousIndices;
        private BeatSegment[] _otherSegments;
        private double _baseScore;
        private List<NoteOn> _notes;
        
        private const double DIFFERENT_INTERVAL_PENALTY = -1.0;

        public BeatSegment(BeatEvent beat, int index, BeatSegment[] otherSegments)
        {
            Beat = beat;
            Index = index;
            _otherSegments = otherSegments;

            Scores = new double[_indexToInterval.Length];
            _previousIndices = new int?[_indexToInterval.Length];
            _notes = new List<NoteOn>();
        }

        public void AddNote(NoteOn note)
        {
            _notes.Add(note);
        }
        
        public void ConnectToPreviousSegments()
        {
            BestScore = (null, double.MinValue);
            for (var i = 0; i < _indexToInterval.Length; i++)
            {
                var interval = (int)IndexToInterval(i);

                if (interval > Index)
                {
                    Scores[i] = _baseScore;
                    _previousIndices[i] = null;
                    continue;
                }

                (int? index, double score) max = (null, double.MinValue);
                for (var j = 0; j < _indexToInterval.Length; j++)
                {
                    var score = _otherSegments[Index - interval].Scores[j] + (_baseScore + 0.1) * _intervalBonus[i] * (i != j ? DIFFERENT_INTERVAL_PENALTY : 1);
                    if (score > max.score) max = (j, score);
                }

                Scores[i] = max.score;
                _previousIndices[i] = max.index;

                if (Scores[i] > BestScore.score) BestScore = (i, Scores[i]);
            }
        }

        public void CalculateBaseScore()
        {
            _baseScore = _notes.Sum(n => n.RealVolume*n.RealTimeLength.TotalSeconds);
        }

        private static readonly int[] _indexToInterval = { 2, 3, 4, 5, 6, 7 };
        private static readonly double[] _intervalBonus = { 1.7, 3, 4, 4.7, 5.9, 6 };

        public static int? IndexToInterval(int? index)
        {
            if (index == null) return null;
            return _indexToInterval[(int)index];
        }

        public int? GetPreviousIndex(int? index)
        {
            if (index == null) return null;
            return _previousIndices[(int)index];
        }
    }
}
