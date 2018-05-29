using System;
using System.Collections.Generic;
using MidiModel;

namespace MusParser
{
    struct NoteStart
    {
        public byte Pitch;
        public double Volume;
        public Instrument Instrument;
        public byte Channel;

        public NoteStart(byte pitch, double volume, Instrument instrument, byte channel)
        {
            Pitch = pitch;
            Volume = volume;
            Instrument = instrument;
            Channel = channel;
        }
    }

    struct NoteEnd
    {
        public byte Pitch;
        public byte Channel;

        public NoteEnd(byte pitch, byte channel)
        {
            Pitch = pitch;
            Channel = channel;
        }
    }

    struct PercussionStart
    {
        public byte Type;
        public double Volume;

        public PercussionStart(byte type, double volume)
        {
            Type = type;
            Volume = volume;
        }
    }

    struct PercussionEnd
    {
        public byte Type;

        public PercussionEnd(byte type)
        {
            Type = type;
        }
    }

    class NoteCluster
    {
        public List<NoteStart> NoteStarts;
        public List<NoteEnd> NoteEnds;
        public List<PercussionStart> PercussionStarts;
        public List<PercussionEnd> PercussionEnds;
        public TimeSpan Metre;
        public Key Chord;
        public int OrderInBeat;

        public NoteCluster(TimeSpan metre, Key chord, int orderInBeat)
        {
            NoteStarts = new List<NoteStart>();
            NoteEnds = new List<NoteEnd>();
            PercussionStarts = new List<PercussionStart>();
            PercussionEnds = new List<PercussionEnd>();
            Metre = metre;
            Chord = chord;
            OrderInBeat = orderInBeat;
        }
    }

}
