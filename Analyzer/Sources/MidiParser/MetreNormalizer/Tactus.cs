using System;
using System.Collections.Generic;
using System.Linq;
using MidiModel;

namespace MidiParser.MetreNormalizer
{
    /// <summary>
    /// computes total scores and normalize the tempo
    /// </summary>
    class Tactus
    {
        private Pip[] _pips;
        private int _minPip, _maxPip;

        public Tactus(Model midi)
        {
            _pips = CreatePips(midi);
        }

        public IEnumerable<double> Compute()
        {
            var tmin = Globals.TactusMin;

            var best = (value: double.MinValue, min: 0, max: 0);

            for (var tmax = Globals.TactusMin * Globals.TactusWidth; tmax <= Globals.TactusMax; tmax *= Globals.TactusStep, tmin *= Globals.TactusStep)
            {
                _minPip = Quantize(tmin);
                _maxPip = Quantize(tmax);

                ComputeTactusScores();
                var score = EvaluateSolution(false);
                                 
                if (score > best.value) best = (score, _minPip, _maxPip);
            }

            _minPip = best.min;
            _maxPip = best.max;
            ComputeTactusScores();
            EvaluateSolution(true);

            var lastTime = 0.0;

            foreach (var pip in _pips.Skip(1))
            {
                if (pip.IsBeat)
                {
                    var relativePipTime = pip.Notes.Any() ? pip.Notes.Average(n => n.Start) : pip.Index*Globals.PipTime;
                    yield return relativePipTime - lastTime;
                    lastTime = relativePipTime;
                }
            }
        }
        
        public static int Quantize(double t)
        {
            return (int)((t / Globals.PipTime) + 0.5);
        }

        private double EvaluateSolution(bool computeBeats)
        {
            var best = (score: double.MinValue, length: -1, index: -1);

            for (var i = _pips.Length - 1; i >= _pips.Length - _maxPip && i >= 0; i--)
            {
                for (var length = _minPip; length <= _maxPip; length++)
                {
                    var current = _pips[i].GetBestScore(length);
                    if (current.score > best.score) best = (current.score, current.prevLength, i);
                }
            }

            if (best.index == -1) throw new Exception("Error: no scores to look at.");

            if (computeBeats) LabelBeats(best.index, best.length);

            return best.score;
        }

        private void LabelBeats(int index, int length)
        {
            while (index >= 0)
            {
                _pips[index].IsBeat = true;
                _pips[index].BestOffset = length;

                (_, var prevLength) = _pips[index].GetBestScore(length);
                index -= length;
                length = prevLength;
            }
        }

        private void ComputeTactusScores()
        {
            for (var i = 0; i < _pips.Length; i++)
            {
                for (var length = _minPip; length <= _maxPip; length++)
                {
                    var best = CalculateBestScore(i, length);
                    _pips[i].SetBestScore(length, best.score, best.prevLength);
                }
            }
        }

        private (double score, int prevLength) CalculateBestScore(int index, int length)
        {
            var baseScore = _pips[index].BaseScore;

            if (index - length < 0)
            {
                return (baseScore * Globals.DefaultScore, -1);
            }

            (double score, int index) max = (double.MinValue, -1);

            for (var prevLength = _minPip; prevLength <= _maxPip; prevLength++)
            {
                var syncopationScore = _pips[index - length / 2].BaseScore / 4 +
                                       _pips[index - length / 3].BaseScore / 12 +
                                       _pips[index - length * 2 / 3].BaseScore / 12 +
                                       _pips[index - length / 4].BaseScore / 16 +
                                       _pips[index - length * 3 / 4].BaseScore / 16;

                (var score, _) = _pips[index - length].GetBestScore(prevLength);

                if (index - length - prevLength < 0)
                {
                    _pips[index].SetState(length, Pip.State.None);

                    var lengthMultiple = Math.Log(((length + length) * Globals.PipTime) / 2.0 + 1, Globals.LengthPower);
                    score += (syncopationScore + baseScore) * lengthMultiple;
                }
                else
                {
                    var lengthMultiple = Math.Log((length + prevLength) * Globals.PipTime / 2.0 + 1, Globals.LengthPower);
                    score += (syncopationScore + baseScore) * lengthMultiple - DeviationPenalty(index, prevLength, length);
                }

                if (score > max.score) max = (score, prevLength);
            }

            return max;
        }

        private double DeviationPenalty(int pipIndex, int prevOffset, int offset)
        {
            var diff = Math.Abs(offset - prevOffset)*Globals.PipTime;
            var isPunnishment = _pips[pipIndex].SetState(offset, _pips[pipIndex - offset].GetState(prevOffset), diff);

            if (!isPunnishment) return 0.0;

            return Globals.BeatIntervalFactor * Math.Sqrt(diff/1000.0);
        }


        private static Pip[] CreatePips(Model midi)
        {
            var notes = CreateNoteWrappers(midi).ToArray();

            var lastTime = notes.Max(n => n.Start + n.Length);

            var pipsLength = Quantize(lastTime) + 1;
            var pips = new Pip[pipsLength];
            for (var i = 0; i < pips.Length; i++) pips[i] = new Pip(i);

            foreach (var note in notes)
            {
                var index = Quantize(note.Start);
                pips[index].Notes.Add(note);
            }

            foreach (var pip in pips)
            {
                pip.BaseScore = pip.ComputeBaseScore();
            }

            return pips;
        }

        private static IEnumerable<NoteWrapper> CreateNoteWrappers(Model midi)
        {
            var noteOns = midi.EventsOfType<NoteOn>().Where(n => n.Volume > 0 && n.NoteNumber < 128).OrderBy(n => n.AbsoluteRealTime).ToArray();
            var averageVolume = noteOns.Average(n => n.RealVolume);

            for (var i = 0; i < noteOns.Length; i++)
            {
                var note = new NoteWrapper(noteOns[i], (noteOns[i].RealVolume / averageVolume));

                // compute rioi
                for (var j = i + 1; j < noteOns.Length; j++)
                {
                    if ((!noteOns[i].IsPercussion && note.Note.ChannelNumber == noteOns[j].ChannelNumber && Math.Abs(noteOns[j].NoteNumber - note.NoteNumber) <= 9) ||
                        (noteOns[i].IsPercussion && noteOns[j].IsPercussion && note.NoteNumber == noteOns[j].NoteNumber))
                    {
                        note.Rioi = (int)(noteOns[j].AbsoluteRealTime - noteOns[i].AbsoluteRealTime).TotalMilliseconds;
                        break;
                    }

                    if ((int)noteOns[j].AbsoluteRealTime.TotalMilliseconds - note.Start >= Globals.MaxEffectiveLength)
                        break;
                }

                yield return note;
            }
        }
    }
}
