using System;
using MidiModel;

namespace MidiParser.MetreNormalizer
{
    class NoteWrapper
    {
        public NoteOn Note;
        public int Rioi;     // registal ioi

        public int EffectiveLength;
        public float Volume;
        public int NoteNumber => Note.NoteNumber;
        public bool IsPercussion => Note.IsPercussion;
        public int Length => (int)Note.RealTimeLength.TotalMilliseconds;
        public int Start => (int) Note.AbsoluteRealTime.TotalMilliseconds;

        public NoteWrapper(NoteOn note, float volume)
        {
            Note = note;
            Volume = volume;
            EffectiveLength = Math.Min(Math.Max(Length, Rioi), Globals.MaxEffectiveLength);
        }
    }
}
