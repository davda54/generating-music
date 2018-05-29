using System;
using Midi;

namespace MidiModel
{
    /// <summary>
    /// Midi instrument that can be played by a channel
    /// </summary>
    public abstract class Instrument
    {
        public abstract int Id();
        public abstract InstrumentCluster Cluster();

        public const int NumberOfClusters = 11;

        public static Instrument TypicalInstrument(InstrumentCluster cluster)
        {
            switch (cluster)
            {
                case InstrumentCluster.Piano:
                    return new MusicalInstrument(Midi.Instrument.AcousticGrandPiano);
                case InstrumentCluster.AcusticGuitar:
                    return new MusicalInstrument(Midi.Instrument.AcousticGuitarSteel);
                case InstrumentCluster.Orchestra:
                    return new MusicalInstrument(Midi.Instrument.StringEnsemble1);
                case InstrumentCluster.Bass:
                    return new MusicalInstrument(Midi.Instrument.ElectricBassFinger);
                case InstrumentCluster.Brass:
                    return new MusicalInstrument(Midi.Instrument.Flute);
                case InstrumentCluster.Percussion:
                    return new Percussion();
                case InstrumentCluster.Warm:
                    return new MusicalInstrument(Midi.Instrument.Pad2Warm);
                case InstrumentCluster.ElectricGuitar:
                    return new MusicalInstrument(Midi.Instrument.DistortionGuitar);
                case InstrumentCluster.Organ:
                    return new MusicalInstrument(Midi.Instrument.RockOrgan);
                case InstrumentCluster.ClappingBox:
                    return new MusicalInstrument(Midi.Instrument.Vibraphone);
                case InstrumentCluster.Violin:
                    return new MusicalInstrument(Midi.Instrument.Cello);
                case InstrumentCluster.Nonmusical:
                    return new MusicalInstrument(Midi.Instrument.Seashore);
                default:
                    throw new ArgumentOutOfRangeException(nameof(cluster), cluster, null);
            }
        }
    }

    /// <summary>
    /// Special case of midi instruments that is played on the 10th channel
    /// </summary>
    public class Percussion : Instrument
    {
        public const int MinNoteNumber = 35;
        public const int MaxNoteNumber = 82;
        public const int Range = MaxNoteNumber - MinNoteNumber + 1;

        public override string ToString()
        {
            return "Percussion";
        }

        public override int Id() => 128;
        public override InstrumentCluster Cluster() => InstrumentCluster.Percussion;
    }

    /// <summary>
    /// Ordinary instruments that are set by an instrument change message
    /// </summary>
    public class MusicalInstrument : Instrument, IEquatable<MusicalInstrument>
    {
        public Midi.Instrument Type { get; }
        public InstrumentBundle Bundle { get; }

        public MusicalInstrument(byte value)
        {
            if (value > 127) throw new FormatException("instrument should be <= 127");

            Type = (Midi.Instrument) value;
            Bundle = (InstrumentBundle) (value >> 3);
        }

        internal MusicalInstrument(Midi.Instrument type)
        {
            Type = type;
            Bundle = (InstrumentBundle)((int)type >> 3);
        }

        public override string ToString()
        {
            return Type.Name();
        }

        public override int Id() => (int) Type;
        public override InstrumentCluster Cluster()
        {
            switch (Type)
            {
                case Midi.Instrument.AcousticGrandPiano:
                case Midi.Instrument.BrightAcousticPiano:
                case Midi.Instrument.ElectricGrandPiano:
                case Midi.Instrument.HonkyTonkPiano:
                case Midi.Instrument.ElectricPiano1:
                case Midi.Instrument.ElectricPiano2:
                case Midi.Instrument.Harpsichord:
                case Midi.Instrument.Clavinet:
                    return InstrumentCluster.Piano;

                case Midi.Instrument.Celesta:
                case Midi.Instrument.Glockenspiel:
                case Midi.Instrument.MusicBox:
                case Midi.Instrument.Vibraphone:
                case Midi.Instrument.Marimba:
                case Midi.Instrument.Xylophone:
                case Midi.Instrument.TubularBells:
                case Midi.Instrument.Dulcimer:
                    return InstrumentCluster.ClappingBox;

                case Midi.Instrument.DrawbarOrgan:
                case Midi.Instrument.PercussiveOrgan:
                case Midi.Instrument.RockOrgan:
                case Midi.Instrument.ChurchOrgan:
                case Midi.Instrument.ReedOrgan:
                case Midi.Instrument.Accordion:
                case Midi.Instrument.Harmonica:
                case Midi.Instrument.TangoAccordion:
                    return InstrumentCluster.Organ;

                case Midi.Instrument.AcousticGuitarNylon:
                case Midi.Instrument.AcousticGuitarSteel:
                case Midi.Instrument.ElectricGuitarJazz:
                case Midi.Instrument.ElectricGuitarClean:
                case Midi.Instrument.ElectricGuitarMuted:
                    return InstrumentCluster.AcusticGuitar;

                case Midi.Instrument.OverdrivenGuitar:
                case Midi.Instrument.DistortionGuitar:
                case Midi.Instrument.GuitarHarmonics:
                    return InstrumentCluster.ElectricGuitar;

                case Midi.Instrument.AcousticBass:
                case Midi.Instrument.ElectricBassFinger:
                case Midi.Instrument.ElectricBassPick:
                case Midi.Instrument.FretlessBass:
                case Midi.Instrument.SlapBass1:
                case Midi.Instrument.SlapBass2:
                case Midi.Instrument.SynthBass1:
                case Midi.Instrument.SynthBass2:
                    return InstrumentCluster.Bass;

                case Midi.Instrument.Violin:
                case Midi.Instrument.Viola:
                case Midi.Instrument.Cello:
                case Midi.Instrument.Contrabass:
                    return InstrumentCluster.Violin;

                case Midi.Instrument.TremoloStrings:
                    return InstrumentCluster.Orchestra;

                case Midi.Instrument.PizzicatoStrings:
                    return InstrumentCluster.ClappingBox;

                case Midi.Instrument.OrchestralHarp:
                    return InstrumentCluster.AcusticGuitar;

                case Midi.Instrument.Timpani:
                    return InstrumentCluster.ClappingBox;

                case Midi.Instrument.StringEnsemble1:
                case Midi.Instrument.StringEnsemble2:
                case Midi.Instrument.SynthStrings1:
                case Midi.Instrument.SynthStrings2:
                    return InstrumentCluster.Orchestra;

                case Midi.Instrument.ChoirAahs:
                case Midi.Instrument.VoiceOohs:
                    return InstrumentCluster.Warm;

                case Midi.Instrument.SynthVoice:
                    return InstrumentCluster.Orchestra;

                case Midi.Instrument.OrchestraHit:
                    return InstrumentCluster.ClappingBox;

                case Midi.Instrument.Trumpet:
                case Midi.Instrument.Trombone:
                case Midi.Instrument.Tuba:
                case Midi.Instrument.MutedTrumpet:
                case Midi.Instrument.FrenchHorn:
                case Midi.Instrument.BrassSection:
                case Midi.Instrument.SynthBrass1:
                case Midi.Instrument.SynthBrass2:
                case Midi.Instrument.SopranoSax:
                case Midi.Instrument.AltoSax:
                case Midi.Instrument.TenorSax:
                case Midi.Instrument.BaritoneSax:
                case Midi.Instrument.Oboe:
                case Midi.Instrument.EnglishHorn:
                case Midi.Instrument.Bassoon:
                case Midi.Instrument.Clarinet:
                case Midi.Instrument.Piccolo:
                case Midi.Instrument.Flute:
                case Midi.Instrument.Recorder:
                case Midi.Instrument.PanFlute:
                case Midi.Instrument.BlownBottle:
                case Midi.Instrument.Shakuhachi:
                case Midi.Instrument.Whistle:
                case Midi.Instrument.Ocarina:
                case Midi.Instrument.Lead1Square:
                case Midi.Instrument.Lead2Sawtooth:
                case Midi.Instrument.Lead3Calliope:
                case Midi.Instrument.Lead4Chiff:
                    return InstrumentCluster.Brass;

                case Midi.Instrument.Lead5Charang:
                    return InstrumentCluster.ElectricGuitar;

                case Midi.Instrument.Lead6Voice:
                    return InstrumentCluster.Warm;

                case Midi.Instrument.Lead7Fifths:
                case Midi.Instrument.Lead8BassPlusLead:
                    return InstrumentCluster.Brass;

                case Midi.Instrument.Pad1NewAge:
                case Midi.Instrument.Pad2Warm:
                case Midi.Instrument.Pad3Polysynth:
                case Midi.Instrument.Pad4Choir:
                case Midi.Instrument.Pad5Bowed:
                case Midi.Instrument.Pad6Metallic:
                case Midi.Instrument.Pad7Halo:
                case Midi.Instrument.Pad8Sweep:
                case Midi.Instrument.FX1Rain:
                case Midi.Instrument.FX2Soundtrack:
                case Midi.Instrument.FX3Crystal:
                case Midi.Instrument.FX4Atmosphere:
                case Midi.Instrument.FX5Brightness:
                case Midi.Instrument.FX6Goblins:
                case Midi.Instrument.FX7Echoes:
                case Midi.Instrument.FX8SciFi:
                    return InstrumentCluster.Warm;

                case Midi.Instrument.Sitar:
                case Midi.Instrument.Banjo:
                case Midi.Instrument.Shamisen:
                case Midi.Instrument.Koto:
                    return InstrumentCluster.AcusticGuitar;

                case Midi.Instrument.Kalimba:
                    return InstrumentCluster.ClappingBox;

                case Midi.Instrument.Bagpipe:
                    return InstrumentCluster.Organ;

                case Midi.Instrument.Fiddle:
                    return InstrumentCluster.Violin;

                case Midi.Instrument.Shanai:
                    return InstrumentCluster.Brass;

                case Midi.Instrument.TinkleBell:
                case Midi.Instrument.Agogo:
                case Midi.Instrument.SteelDrums:
                case Midi.Instrument.Woodblock:
                case Midi.Instrument.TaikoDrum:
                case Midi.Instrument.MelodicTom:
                case Midi.Instrument.SynthDrum:
                case Midi.Instrument.ReverseCymbal:
                    return InstrumentCluster.ClappingBox;
                    
                default:
                    return InstrumentCluster.Nonmusical;
            }
        }

        public bool Equals(MusicalInstrument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MusicalInstrument) obj);
        }

        public override int GetHashCode()
        {
            return (int)Type;
        }
    }

    public enum InstrumentBundle
    {
        Piano,
        ChromaticPercussion,
        Organ,
        Guitar,
        Bass,
        Strings,
        Ensamble,
        Brass,
        Reed,
        Pipe,
        SynthLead,
        SynthPad,
        SynthEffects,
        Ethnic,
        Percussive,
        SoundEffects,
    }

    public enum InstrumentCluster
    {
        Piano,
        AcusticGuitar,
        Orchestra,
        Bass,
        Brass,
        Warm,
        ElectricGuitar,
        Organ,
        ClappingBox,
        Percussion = Channel.PercussionChannelNumber,
        Violin,
        Nonmusical,
    }
}