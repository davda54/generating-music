using System;
using System.Collections.Generic;
using System.Linq;
using MathExtension;
using MidiModel;

namespace MidiParser
{
    /// <summary>
    /// original Krumhansl's algorithm for key detection
    /// </summary>
    public class KeyFinder
    {
        private Model _midi;

        /// <summary>
        /// key profile for C major
        /// </summary>
        private double[] _majorKeyProfile =
        {
            //0.084, 0.0043, 0.06475, 0.00705, 0.06745, 0.05965, 0.00625, 0.1014, 0.009, 0.0402, 0.0031, 0.05285
            0.08874125044, 0.007118282121, 0.05789536125, 0.009728318899, 0.07948748369, 0.05457349626,
            0.01138925139, 0.08482619528, 0.01233835568, 0.04342152094, 0.006762368015, 0.04745521414
        };

        /// <summary>
        /// key profile for C minor
        /// </summary>
        private double[] _minorKeyProfile =
        {
            //0.0908, 0.00345, 0.06495, 0.0667, 0.00535, 0.05575, 0.0069, 0.10535, 0.03745, 0.00765, 0.0046, 0.05105
            0.08447028117, 0.00996559497, 0.05623442876, 0.07331830585, 0.005813263732, 0.05457349626,
            0.01245699371, 0.08862261241, 0.04792976628, 0.007948748369, 0.0157788587, 0.03915055167
        };

        public KeyFinder(Model midi)
        {
            _midi = midi;
        }

        // Krumhansl algorithm
        public void Analyze()
        {
            var notes = _midi.EventsOfType<NoteOn>().Where(n => n.RealVolume > 0 && !n.IsPercussion).ToArray();
            var occurenciesDictionary = notes.GroupBy(n => n.NoteNumber % 12).ToDictionary(g => (int)g.Key, g => (double)g.Count() / notes.Length);

            var occurencies = new double[12];
            for (int i = 0; i < 12; i++)
                if (occurenciesDictionary.ContainsKey(i)) occurencies[i] = occurenciesDictionary[i];
            
            var majorProb = new double[12];
            var minorProb = new double[12];

            var occurencyQueue = new Queue<double>(occurencies);
            for (var i = 0; i < majorProb.Length; i++)
            {
                majorProb[i] = Statistics.Correlation(occurencyQueue.ToArray(), _majorKeyProfile);
                minorProb[i] = Statistics.Correlation(occurencyQueue.ToArray(), _minorKeyProfile);

                occurencyQueue.Enqueue(occurencyQueue.Dequeue());
            }

            var majorMax = majorProb.MaxWithIndex();
            var minorMax = minorProb.MaxWithIndex();

            if (majorMax.max_value > minorMax.max_value)
            {
                _midi.Key = new Key { Scale = Scale.Major, Tone = (Tone)majorMax.max_index };
            }
            else
            {
                _midi.Key = new Key { Scale = Scale.Minor, Tone = (Tone)minorMax.max_index };
            }
        }

        // Krumhansl algorithm
        public void AlternativeAnalyze()
        {
            var notes = _midi.EventsOfType<NoteOn>().Where(n => n.RealVolume > 0 && !n.IsPercussion).OrderBy(n => n.AbsoluteRealTime).ToArray();
            var occurencies = new double[12];

            var nextBeat = TimeSpan.FromMilliseconds(1200);
            var beatSpan = TimeSpan.FromMilliseconds(1200);
            var occuredInBeat = new bool[12];
            foreach (var note in notes)
            {
                if (note.AbsoluteRealTime >= nextBeat)
                {
                    for (var i = 0; i < 12; i++)
                    {
                        if (!occuredInBeat[i]) continue;

                        occurencies[i]++;
                        occuredInBeat[i] = false;
                    }
                    nextBeat += beatSpan;
                }

                occuredInBeat[note.NoteNumber % 12] = true;
            }

            var sum = occurencies.Sum();
            for (var i = 0; i < 12; i++) occurencies[i] /= sum;
            

            var majorProb = new double[12];
            var minorProb = new double[12];

            var occurencyQueue = new Queue<double>(occurencies);
            for (var i = 0; i < majorProb.Length; i++)
            {
                majorProb[i] = Statistics.Correlation(occurencyQueue.ToArray(), _majorKeyProfile);
                minorProb[i] = Statistics.Correlation(occurencyQueue.ToArray(), _minorKeyProfile);

                occurencyQueue.Enqueue(occurencyQueue.Dequeue());
            }

            var majorMax = majorProb.MaxWithIndex();
            var minorMax = minorProb.MaxWithIndex();

            if (majorMax.max_value > minorMax.max_value)
            {
                _midi.Key = new Key { Scale = Scale.Major, Tone = (Tone)majorMax.max_index };
            }
            else
            {
                _midi.Key = new Key { Scale = Scale.Minor, Tone = (Tone)minorMax.max_index };
            }
        }
    }
}
