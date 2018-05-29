using System;
using System.Collections.Generic;
using System.Linq;

namespace MidiParser.MetreNormalizer
{
    /// <summary>
    /// one segment/time unit after the time is quantized, contains notes occuring at the time and computes base scores
    /// </summary>
    class Pip
    {
        public enum State
        {
            None,
            Raising,
            Declining
        }
        
        public int Index;
        public List<NoteWrapper> Notes;
        public (double score, int prevLength)[] Score;
        public State[] States;
        public double BaseScore;
        public int BestOffset; 
        public bool IsBeat;
        public bool IsFirstBeat;

        public Pip(int index)
        {
            Index = index;
            Notes = new List<NoteWrapper>();
            IsBeat = false;
            IsFirstBeat = false;
            Score = new(double score, int prevLength)[Globals.MaxLength - Globals.MinLength + 1];
            States = new State[Globals.MaxLength - Globals.MinLength + 1];
        }

        public double ComputeBaseScore()
        {
            if (Notes.Count == 0) return 0.0;

            var averageLength = Notes.Average(n => n.EffectiveLength)/1000.0;
            var averageVolume = Notes.Average(n => Math.Sqrt(n.Volume));
            var percussionCount = Notes.Count(n => n.IsPercussion)*Globals.PercussionMultiple;

            return Globals.NoteFactor * (((Math.Sqrt(Notes.Count) + percussionCount) * averageLength * averageVolume) + Globals.NoteBonus);
        }

        public (double score, int prevLength) GetBestScore(int length)
        {
            return Score[length - Globals.MinLength];
        }

        public void SetBestScore(int tactus, double score, int prevLength)
        {
            Score[tactus - Globals.MinLength] = (score, prevLength);
        }

        public State GetState(int tactus)
        {
            return States[tactus - Globals.MinLength];
        }

        public void SetState(int tactus, State value)
        {
            States[tactus - Globals.MinLength] = value;
        }

        /// <summary>Implementation of the automaton for small tempo changes</summary>
        /// <returns>Returns true if the tempo difference should be punnished</returns>
        public bool SetState(int tactus, State prevState, int tempoDelta)
        {
            switch (prevState)
            {
                case State.None:
                    if (tempoDelta == 0)
                    {
                        SetState(tactus, State.None);
                        return false;
                    }
                    else if (tempoDelta > 0 && tempoDelta <= Globals.BeatSlop)
                    {
                        SetState(tactus, State.Raising);
                        return false;
                    }
                    else if (tempoDelta < 0 && tempoDelta >= -Globals.BeatSlop)
                    {
                        SetState(tactus, State.Declining);
                        return false;
                    }
                    else
                    {
                        SetState(tactus, State.None);
                        return true;
                    }

                case State.Raising:
                    if (tempoDelta == 0)
                    {
                        SetState(tactus, State.Raising);
                        return false;
                    }
                    else if (tempoDelta < 0 && tempoDelta >= -Globals.BeatSlop)
                    {
                        SetState(tactus, State.None);
                        return false;
                    }
                    else
                    {
                        SetState(tactus, State.None);
                        return true;
                    }
                case State.Declining:
                    if (tempoDelta == 0)
                    {
                        SetState(tactus, State.Declining);
                        return false;
                    }
                    else if (tempoDelta > 0 && tempoDelta <= Globals.BeatSlop)
                    {
                        SetState(tactus, State.None);
                        return false;
                    }
                    else
                    {
                        SetState(tactus, State.None);
                        return true;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(prevState), prevState, null);
            }
        }
    }
}
