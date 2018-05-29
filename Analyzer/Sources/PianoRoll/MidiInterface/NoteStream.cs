using System;
using System.Collections.Generic;
using System.Linq;
using MidiModel;

namespace PianoRoll.MidiInterface
{

    /// <summary>
    /// Container for the NoteOn midi events
    /// It is used to gather all the notes that should appear on the screen
    /// </summary>
    class NoteStream
    {
        private readonly NoteOn[] _allNotes;

        // the index of the note, that was the first note returned in the last call of $GetFrame
        private int _lastFirstNoteIndex;

        public NoteStream(Model midi)
        {
            _allNotes = midi.EventsOfType<NoteOn>().OrderBy(n => n.AbsoluteRealTime).ToArray();

            _lastFirstNoteIndex = 0;
        }

        /// <summary>
        /// Returns all notes that are played in the time interval [time; time + frameLength]
        /// 
        /// Successive calls should not go back in time, in order to achieve this you should create
        /// a new NoteStream.
        /// This is bacause of optimization for the most common case - midi files are overall not
        /// very efficient for random accesses into different times of the file, so they are usually 
        /// played only forward.
        /// </summary>
        public IEnumerable<NoteOn> GetFrame(TimeSpan time, TimeSpan frameLength)
        {
            bool firstNoteSet = false;
            var end = time + frameLength;

            for (int i = _lastFirstNoteIndex; i < _allNotes.Length; i++)
            {
                var note = _allNotes[i];

                if (note.End < time) continue;
                if (note.AbsoluteRealTime > end) yield break;

                if (!firstNoteSet)
                {
                    _lastFirstNoteIndex = i;
                    firstNoteSet = true;
                }

                yield return note;
            }
        }
    }
}