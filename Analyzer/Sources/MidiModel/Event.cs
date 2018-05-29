using System;
using System.Collections.Generic;
using Midi;

namespace MidiModel
{
    /// <summary>
    /// Abstract model for any midi event
    /// </summary>
    public abstract class Event
    {
        public uint AbsoluteTime;

        public TimeSpan AbsoluteRealTime;
    }


    /// <summary>
    /// Abstract model for midi control event, i.e. events that are directly connected to a channel
    /// </summary>
    public abstract class MidiControlEvent : Event
    {
        public byte ChannelNumber;
    }

    public class NoteOff : MidiControlEvent
    {
        public byte NoteNumber;
        public byte Velocity;
    }

    public class NoteOn : MidiControlEvent
    {
        public byte NoteNumber;
        public byte Volume;
        public float RealVolume;
        public uint Length;
        public Instrument Instrument;
        public TimeSpan RealTimeLength;
        public TimeSpan End;
        public List<PitchBend> Bends = new List<PitchBend>();

        public bool IsPercussion => ChannelNumber == Channel.PercussionChannelNumber;
    }

    public class NoteAftertouch : MidiControlEvent
    {
        public byte NoteNumber;
        public byte AftertouchValue;
    }

    public class Controller : MidiControlEvent
    {
        public byte ControllerNumber;
        public byte ControllerValue;
        public Control? ControlEnum => NumberToControl(ControllerNumber);

        public Control? NumberToControl(byte controllerNumber)
        {
            switch (controllerNumber)
            {
                case 1: return Control.ModulationWheel;
                case 6: return Control.DataEntryMSB;
                case 7: return Control.Volume;
                case 10: return Control.Pan;
                case 11: return Control.Expression;
                case 38: return Control.DataEntryLSB;
                case 64: return Control.SustainPedal;
                case 91: return Control.ReverbLevel;
                case 92: return Control.TremoloLevel;
                case 93: return Control.ChorusLevel;
                case 94: return Control.CelesteLevel;
                case 95: return Control.PhaserLevel;
                case 98: return Control.NonRegisteredParameterLSB;
                case 99: return Control.NonRegisteredParameterMSB;
                case 100: return Control.RegisteredParameterNumberLSB;
                case 101: return Control.RegisteredParameterNumberMSB;
                case 121: return Control.AllControllersOff;
                case 123: return Control.AllNotesOff;
                default: return null;
            }
        }
    }

    public class InstrumentChange : MidiControlEvent
    {
        public MusicalInstrument Instrument;
    }

    public class ChannelAftertouch : MidiControlEvent
    {
        public byte AfterTouchValue;
    }

    public class PitchBend : MidiControlEvent
    {
        public ushort PitchValue;
        public NoteOn NoteReference;
        public byte Range;
        public float RealPitchChange => Range * (PitchValue - 8192f) / 16384f;
    }



    /// <summary>
    /// Abstract model for midi meta events
    /// </summary>
    public abstract class MetaEvent : Event
    {
    }

    public class BeatEvent : MetaEvent
    {
        public TimeSpan Length;
        public Key Chord;
        public byte Level; // 0 - strong, 1 - medium, 2 - weak
    }

    public class SequenceNumber : MetaEvent
    {
        public ushort Number;
    }

    public class TextEvent : MetaEvent
    {
        public string Text;
    }

    public class CopyrightNotice : TextEvent
    {
    }

    public class TrackName : TextEvent
    {
    }

    public class InstrumentName : TextEvent
    {
    }

    public class Lyrics : TextEvent
    {
    }

    public class Marker : TextEvent
    {
    }

    public class CuePoint : TextEvent
    {
    }

    public class MidiChannelPrefix : MetaEvent
    {
        public byte Channel;
    }

    public class EndOfTrack : MetaEvent
    {
    }

    public class SetTempo : MetaEvent
    {
        public uint MicrosecondsPerQuarterNote;
    }

    public class SmpteOffset : MetaEvent
    {
        public byte Hour;
        public byte Minute;
        public byte Second;
        public byte Frame;
        public byte SubFrame;
    }

    public class TimeSignature : MetaEvent
    {
        public byte Numerator;
        public byte Denominator;
        public byte MetronomePulse;
        public byte NumberOf32ndNotesPerMidiQuarterNote;
    }

    public class KeySignature : MetaEvent
    {
        public Key Key;
    }

    public class SequencerSpecific : MetaEvent
    {
        public byte[] Data;
    }

    public class UnknownMetaEvent : MetaEvent
    {
        public byte[] Data;
    }

    public class ImprovisationStartMetaEvent : MetaEvent
    {
    }
}
  
