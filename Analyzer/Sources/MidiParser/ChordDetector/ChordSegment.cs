using System;
using MidiModel;

namespace MidiParser.ChordDetector
{
    /// <summary>
    /// one beat with a chord assigned
    /// </summary>
    class ChordSegment
    {
        public BeatEvent Beat;
        public Key Key;

        public ChordSegment PreviousChordSegment;
        public int[] BestPrevious;
        public double[] Scores;
        public double[] BaseScores;

        public NotesInSegment NotesInLevel2Beats;
        public NotesInSegment NotesInLevel1Beats;
        public NotesInSegment NotesInLevel0Beats;

        private double[] _level0Scores = new double[24];
        private double[] _level1Scores = new double[24];
        private double[] _level2Scores = new double[24];
        private double[] _keyDiffScores = new double[24];
        private double[] _chordInScaleScores = new double[24];

        private double _noteSum;
        
        private static readonly bool[] _majorScale = { true, false, true, false, true, true, false, true, false, true, false, true };
        private static readonly bool[] _minorScale = { true, false, true, true, false, true, false, true, true, false, true, false };

        private static readonly double[] _differencePenalty = { 0, 1, 1.5, 3, 4, 6, 8 };

        private const double NOTES_IN_LEVEL_0 = 1.5;
        private const double NOTES_IN_LEVEL_1 = 1.5;
        private const double NOTES_IN_LEVEL_2 = 1.8;
        private const double DIFF_FROM_KEY_MULTIPLE = 0.1/4;
        private const double CHORD_IN_SCALE_MULTIPLE = 0.15/4;
        private const double DIFF_FROM_PREVIOUS_CHORD = 0.175/1;
        private const double DIFF_ON_WEAK_BEAT = 0.7;

        public ChordSegment(Key key, ChordSegment previousChordSegment, BeatEvent beat, byte lowestPitch)
        {
            Key = key;
            PreviousChordSegment = previousChordSegment;
            Beat = beat;

            NotesInLevel2Beats = new NotesInSegment(beat.AbsoluteRealTime, beat.Length, lowestPitch);

            BestPrevious = new int[24];
            Scores = new double[24];
            BaseScores = new double[24];
        }

        public void AddNote(NoteOn note)
        {
            NotesInLevel2Beats.AddNote(note);
        }

        public void ConnectToPreviousSegment()
        {
            for (var chordIndex = 0; chordIndex < 24; chordIndex++)
            {
                var chord = new Key(chordIndex);
                var bestScore = (chord: 0, score: double.MinValue);

                for (var previousChordIndex = 0; previousChordIndex < 24; previousChordIndex++)
                {
                    var previousChord = new Key(previousChordIndex);
                    var difference = DifferenceOnLineOfFifths(chord, previousChord);
                    if (difference == 0 && chord.Scale != previousChord.Scale) difference = 2; //penalty for changing between major and minor

                    var differenceScore = -DIFF_FROM_PREVIOUS_CHORD * _differencePenalty[difference] * _noteSum;
                    differenceScore += DIFF_ON_WEAK_BEAT*differenceScore*Beat.Level*Beat.Level; // penalty for difference on a weak beats
                    
                    var score = PreviousChordSegment.Scores[previousChordIndex] + BaseScores[chordIndex] + differenceScore;
                    if (bestScore.score < score) bestScore = (previousChordIndex, score);
                }

                Scores[chordIndex] = bestScore.score;
                BestPrevious[chordIndex] = bestScore.chord;
            }
        }

        public void CalculateBaseScores()
        {
            NotesInLevel2Beats.ComputeScores(true);

            _noteSum = NOTES_IN_LEVEL_2 * NotesInLevel2Beats.TotalNoteWeight +
                       NOTES_IN_LEVEL_1 * NotesInLevel1Beats.TotalNoteWeight +
                       NOTES_IN_LEVEL_0 * NotesInLevel0Beats.TotalNoteWeight;

            for (var chord = 0; chord < 24; chord++)
            {
                BaseScores[chord] = BaseScore(new Key(chord), chord);
            }
        }

        private double BaseScore(Key chord, int chordIndex)
        {
            var score = 0.0;

            _level0Scores[chordIndex] = NOTES_IN_LEVEL_0 * NotesInLevel0Beats.GetScore(chord);
            score += _level0Scores[chordIndex];

            _level1Scores[chordIndex] = NOTES_IN_LEVEL_1 * NotesInLevel1Beats.GetScore(chord);
            score += _level1Scores[chordIndex];

            _level2Scores[chordIndex] = NOTES_IN_LEVEL_2 * NotesInLevel2Beats.GetScore(chord);
            score += _level2Scores[chordIndex];
            
            var difference = DifferenceOnLineOfFifths(chord, Key);
            _keyDiffScores[chordIndex] = -DIFF_FROM_KEY_MULTIPLE * _differencePenalty[difference] * _noteSum;
            score += _keyDiffScores[chordIndex];

            var scale = Key.Scale == Scale.Major ? _majorScale : _minorScale;

            var notesInScale = 0;
            if (scale[((int) chord.Tone - (int) Key.Tone + 12) % 12]) notesInScale++;
            if (scale[((int)chord.Tone - (int)Key.Tone + 7 + 12) % 12]) notesInScale++;
            if (chord.Scale == Scale.Major && scale[((int)chord.Tone - (int)Key.Tone + 4 + 12) % 12]) notesInScale++;
            if (chord.Scale == Scale.Minor && scale[((int)chord.Tone - (int)Key.Tone + 3 + 12) % 12]) notesInScale++;

            _chordInScaleScores[chordIndex] = -CHORD_IN_SCALE_MULTIPLE * Math.Pow(2, 3-notesInScale) * _noteSum;
            score += _chordInScaleScores[chordIndex];

            return score;
        }

        private int DifferenceOnLineOfFifths(Key a, Key b)
        {
            var aPosition = (int)a.Tone + (a.Scale == Scale.Minor ? 3 : 0);
            var bPosition = (int)b.Tone + (b.Scale == Scale.Minor ? 3 : 0);
            int diff = Math.Abs(aPosition - bPosition);

            // calculates 7*x = diff mod 12
            // we use the fact that the inverse of 7 in mod 12 is 7
            int fifthDiff = (7 * diff) % 12;
            return Math.Min(fifthDiff, 12 - fifthDiff);
        }
    }
}
