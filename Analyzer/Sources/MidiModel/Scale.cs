using System;
using System.Runtime.Remoting.Messaging;

namespace MidiModel
{
    public struct Key : IEquatable<Key>
    {
        public Tone Tone;
        public Scale Scale;

        public enum DetectionError
        {
            NoError,
            ParallelKeys, //eg. Cmaj vs Cmin
            MajorMinor, //eg. Cmaj vs Amin
            Fifth, //eg. Cmaj vs Gmaj
            TotallyWrong
        }

        public Key(Tone tone, Scale scale)
        {
            Tone = tone;
            Scale = scale;
        }

        public Key(int keyNumber)
        {
            if (keyNumber < 12)
            {
                Scale = Scale.Major;
                Tone = (Tone) keyNumber;
            }
            else
            {
                Scale = Scale.Minor;
                Tone = (Tone) (keyNumber - 12);
            }
        }

        public Key(Scale scale, sbyte flatsOrSharpsFromMidiEvent)
        {
            Scale = scale;

            if (scale == Scale.Major)
            {
                // according to http://www.recordingblogs.com/sa/Wiki/topic/MIDI-Key-Signature-meta-message
                // and the circle of fifths from music theory :)
                Tone = (Tone) (((12 - flatsOrSharpsFromMidiEvent) * 5) % 12);
            }
            else
            {
                // according to http://www.recordingblogs.com/sa/Wiki/topic/MIDI-Key-Signature-meta-message
                // and the circle of fifths from music theory :)
                Tone = (Tone)(((12 - flatsOrSharpsFromMidiEvent) * 5 + 9) % 12);
            }
        }

        public static DetectionError HowMuchAreEqual(Key a, Key b)
        {
            if (a.Tone == b.Tone && a.Scale == b.Scale) return DetectionError.NoError;
            if (a.Tone == b.Tone) return DetectionError.ParallelKeys;
            if (a.Scale == b.Scale && (((int)a.Tone - (int)b.Tone + 12) % 12 == 7 || ((int)a.Tone - (int)b.Tone + 12) % 12 == 5)) return DetectionError.Fifth;
            if (a.Scale == Scale.Major && b.Scale == Scale.Minor && (((int) a.Tone - (int) b.Tone + 12) % 12 == 3)) return DetectionError.MajorMinor;
            if (a.Scale == Scale.Minor && b.Scale == Scale.Major && (((int) b.Tone - (int) a.Tone + 12) % 12 == 3)) return DetectionError.MajorMinor;

            return DetectionError.TotallyWrong;
        }

        public int ToInt()
        {
            return (int)Scale * 12 + (int)Tone;
        }

        public override bool Equals(object obj)
        {
            return obj is Key && Equals((Key)obj);
        }

        public bool Equals(Key other)
        {
            return Tone == other.Tone &&
                   Scale == other.Scale;
        }

        public override int GetHashCode()
        {
            var hashCode = 1131172314;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Tone.GetHashCode();
            hashCode = hashCode * -1521134295 + Scale.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Tone.ToString()} {Scale.ToString()}";
        }

        public static bool operator ==(Key key1, Key key2)
        {
            return key1.Equals(key2);
        }

        public static bool operator !=(Key key1, Key key2)
        {
            return !(key1 == key2);
        }
    }

    public enum Tone
    {
        C, Cis, D, Dis, E, F, Fis, G, Gis, A, Ais, B
    }
    public enum Scale
    {
        Major,
        Minor
    }
}
