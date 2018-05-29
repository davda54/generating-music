using System;
using System.IO;
using System.Linq;
using MidiModel;
using SharpLearning.InputOutput.Csv;
using SharpLearning.RandomForest.Learners;
using SharpLearning.RandomForest.Models;

namespace MidiParser
{
    /// <summary>
    /// Random forest model for key detection
    /// </summary>
    public static class MLKeyFinder
    {
        private static ClassificationForestModel model;
        private static int lastNumOfTrees = 30;

        public static void LearnModel(int trees = 30)
        {
            var keySignatureTable = Properties.Resources.keySignaturesDataset;

            var parser = new CsvParser(() => new StringReader(keySignatureTable), ',');
            var targetName = "key";

            var observations = parser.EnumerateRows(c => c != targetName).ToF64Matrix();
            var targets = parser.EnumerateRows(targetName).ToF64Vector();

            var learner = new ClassificationRandomForestLearner(trees: trees);
            model = learner.Learn(observations, targets);
        }

        public static void DetectKey(Model midi, int trees = 30)
        {
            var occurencies = GetOccurencies(midi);

            if (model == null || trees != lastNumOfTrees)
            {
                LearnModel(trees);
                lastNumOfTrees = trees;
            }
            var key = (int) model.Predict(occurencies);

            if (key < 12) midi.Key = new Key((Tone) key, Scale.Major);
            else midi.Key = new Key((Tone) (key - 12), Scale.Minor);
        }
        
        /// <summary>
        /// computes the pitch profile
        /// </summary>
        /// <param name="midi"></param>
        /// <returns>pitch profie</returns>
        public static double[] GetOccurencies(Model midi)
        {
            var notes = midi.EventsOfType<NoteOn>().Where(n => n.RealVolume > 0 && !n.IsPercussion).OrderBy(n => n.AbsoluteRealTime).ToArray();

            var occurencies = new double[12];

            var nextBeat = TimeSpan.FromMilliseconds(1200);
            var beatSpan = TimeSpan.FromMilliseconds(1200);
            var occuredInBeat = new bool[12];

            var beatSum = 0.0;

            foreach (var note in notes)
            {
                if (note.AbsoluteRealTime >= nextBeat)
                {
                    for (var i = 0; i < 12; i++)
                    {
                        if (!occuredInBeat[i]) continue;

                        occurencies[i]++;
                        beatSum++;
                        occuredInBeat[i] = false;
                    }
                    nextBeat += beatSpan;
                }

                occuredInBeat[note.NoteNumber % 12] = true;
            }

            for (var i = 0; i < 12; i++) occurencies[i] /= beatSum;

            return occurencies;
        }
    }
}
