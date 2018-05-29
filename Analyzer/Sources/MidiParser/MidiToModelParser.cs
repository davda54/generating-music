using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathExtension;
using MidiModel;
using MidiModel.Simplifiers;
using MidiParser.ChordDetector;
using MidiParser.MetreNormalizer;

namespace MidiParser
{
    /// <summary>
    /// Parses a valid midi file into MidiParser.Model.MidiModel
    /// 
    /// With the help of midi specification at http://wayback.archive.org/web/20141227205754/http://www.sonicspot.com/guide/midifiles.html
    /// </summary>
    public static class MidiToModelParser
    {
        public static Model Parse(string filename, bool computeNoteLenghts = true, bool computePitchBends = true,
            bool computeRealVolumes = true, bool determineInstruments = false, bool normalizeMetre = false,
            bool prolongSustainedNotes = false, bool showBeats = false, bool analyzeKeys = false,
            bool analyzeChords = false, bool playChords = false, bool discretizeBends = false, Tone? transposeTo = null)
        {
            if (playChords) analyzeChords = true;
            if (analyzeChords) analyzeKeys = true;
            if (discretizeBends) computePitchBends = true;
            if (transposeTo != null) analyzeKeys = true;

            var midi = new Model();

            using (var reader = new MidiBigEndianReader(File.Open(filename, FileMode.Open)))
            {
                ParseHeaderChunk(reader, midi);

                foreach (var track in midi.Tracks)
                {
                    ParseTrackChunk(reader, track);
                }

                if (computeNoteLenghts || normalizeMetre || showBeats)
                {
                    TimeCalculator.ComputeRealTimes(midi);
                    TimeCalculator.CreateNoteLengths(midi);
                }
                midi.Length = midi.EventsOfType<EndOfTrack>().Max(e => e.AbsoluteRealTime);

                if (computeRealVolumes || normalizeMetre || showBeats)
                {
                    var collector = new VolumeChangeCollector(midi);
                    collector.DetermineVolumes();
                }
       

                if (prolongSustainedNotes || normalizeMetre || showBeats)
                {
                    var sustainer = new Sustainer(midi);
                    sustainer.ProlongSustainedNotes();
                }

                if (normalizeMetre)
                {
                    Normalizer.CalculateMetre(midi);
                    Normalizer.Normalize(midi, prolongSustainedNotes);
                }
                else if (showBeats)
                {
                    Normalizer.CalculateMetre(midi);
                    Normalizer.AddBeats(midi, prolongSustainedNotes);
                }

                if (computePitchBends)
                {
                    PitchBendCalculator.JoinPitchBends(midi);
                    PitchBendCalculator.DeterminePitchRanges(midi);
                    if(discretizeBends) PitchBendCalculator.DiscretizeBends(midi);
                }

                if (determineInstruments)
                {
                    var collector = new InstrumentChangeCollector(midi);
                    collector.DetermineInstruments();
                }

                if (analyzeKeys)
                {
                    if (!normalizeMetre && !showBeats) Normalizer.CalculateMetre(midi);

                    if (midi.EventsOfType<KeySignature>().Any(k => k.Key.Tone != Tone.C || k.Key.Scale != Scale.Major))
                    {
                        midi.Key = midi.EventsOfType<KeySignature>().First().Key;
                        midi.IsKeyFoundByMidiItself = true;
                    }
                    else
                    {
                        MLKeyFinder.DetectKey(midi);
                        midi.IsKeyFoundByMidiItself = false;
                    }
                }

                if (analyzeChords)
                {
                    var analyzer = new ChordAnalyzer(midi);
                    analyzer.Analyze();

                    if (playChords) analyzer.AddChordNotesToModel();
                }

                if (transposeTo != null)
                {
                    Transposer.Transpose(midi, (Tone)transposeTo);
                }

                ChannelPlayabilityChecker.Check(midi);
            }

            return midi;
        }

        private static void ParseHeaderChunk(BinaryReader reader, Model midi)
        {
            var c = reader.ReadUInt32();

            if (c == 0x52494646)
            {
                // skip the RIFF wrapper
                reader.ReadBytes(16);
                ParseHeaderChunk(reader, midi);
                return;
            }

            if (c != 0x4D546864) throw new FormatException("MIDI should start with \"MThd\"");

            var length = reader.ReadUInt32();

            c = reader.ReadUInt16();
            if (c > 2) throw new FormatException("Format type should be 0, 1 or 2");
            midi.FormatType = (byte) c;

            c = reader.ReadUInt16();
            midi.Tracks = new Track[c];
            for (var i = 0; i < midi.Tracks.Length; i++)
            {
                midi.Tracks[i] = new Track();
            }

            c = reader.ReadUInt16();
            midi.TimeDivision = ParseTimeDivision((ushort) c);

            // software which reads midi files is required to honor the length field, even if it is greater than expected. 
            // Any unexpected data must be ignored.
            reader.ReadBytes((int) (length - 6)); 
        }

        private static void ParseTrackChunk(MidiBigEndianReader reader, Track track)
        {
            uint chunkType = reader.ReadUInt32();

            uint totalSize = reader.ReadUInt32();
            uint size = 0;

            if (chunkType != 0x4D54726B)
            {
                reader.ReadBytes((int) totalSize); // skip if unexpeced type of chunk
                ParseTrackChunk(reader, track); // shouldn't recurse too deep, every chunks is usually a track chunk
                return;
            }


            byte lastEventTypeValue = 0;
            byte lastChannel = 0;
            uint absoluteTime = 0;

            while (true)
            {
                var deltaTime = reader.ReadVariableLengthValue(out byte deltaTimeLength);
                absoluteTime += deltaTime;
                size += deltaTimeLength;

                var eventType = reader.ReadByte();
                size += 1;

                if (eventType >= 0x80 && eventType <= 0xEF)
                {
                    // midi control event

                    byte eventTypeValue = (byte) (eventType >> 4);
                    byte channel = (byte) (eventType & 0x0F);

                    lastEventTypeValue = eventTypeValue;
                    lastChannel = channel;

                    ParseControlEvent(reader, track, eventTypeValue, channel, absoluteTime, ref size, reader.ReadByte());
                    size += 1;
                }

                else if (eventType >> 4 < 0x8)
                {
                    // running status control event

                    if (lastEventTypeValue < 0x8)
                        throw new FormatException("No event is saved, so running status cannot be applied");

                    ParseControlEvent(reader, track, lastEventTypeValue, lastChannel, absoluteTime, ref size, eventType);
                }


                else if (eventType == 0xFF)
                {
                    // meta event

                    byte type = reader.ReadByte();
                    size += 1;

                    var length = reader.ReadVariableLengthValue(out byte lengthLength);
                    size += lengthLength;

                    ParseMetaEvent(reader, track, type, length, absoluteTime);

                    // check end of track
                    if (type == 0x2F)
                    {
                        // if end of track event was encountered, then it should be at the end of the track
                        if (size + length == totalSize) return;
                        else throw new FormatException("Unexpected end of track");
                    }

                    size += length;
                    lastEventTypeValue = 0; // cancel the running status
                }

                else if (eventType == 0xF0 || eventType == 0xF7)
                {
                    // System exclusive events
                    // skip them

                    var length = reader.ReadVariableLengthValue(out byte lengthLength);
                    reader.ReadBytes((int) length);

                    size += lengthLength + length;
                    lastEventTypeValue = 0; // cancel the running status
                }

                else
                {
                    throw new FormatException("Unexpected event type");
                }


                if (size > totalSize)
                {
                    throw new FormatException("Track is longer than expected");
                }
            }
        }

        private static void ParseControlEvent(MidiBigEndianReader reader, Track track, byte eventType, byte channel, uint absoluteTime, ref uint size, byte nextByte)
        {
            switch (eventType)
            {
                case 0x8:
                    //note number out of bound
                    if (nextByte > 127) break;

                    track.Channels[channel].Events.Add(new NoteOff
                    {
                        AbsoluteTime = absoluteTime,
                        ChannelNumber = channel,
                        NoteNumber = nextByte,
                        Velocity = reader.ReadByte()
                    });
                    size += 1;
                    break;

                case 0x9:
                    //note number out of bound
                    if (nextByte > 127) break;

                    track.Channels[channel].Events
                        .Add(new NoteOn
                        {
                            AbsoluteTime = absoluteTime,
                            ChannelNumber = channel,
                            NoteNumber = nextByte,
                            Volume = reader.ReadByte()
                        });
                    size += 1;
                    break;

                case 0xA:
                    //note number out of bound
                    if (nextByte > 127) break;

                    track.Channels[channel].Events.Add(new NoteAftertouch
                    {
                        AbsoluteTime = absoluteTime,
                        ChannelNumber = channel,
                        NoteNumber = nextByte,
                        AftertouchValue = reader.ReadByte()
                    });
                    size += 1;
                    break;

                case 0xB:
                    track.Channels[channel].Events.Add(new Controller
                    {
                        AbsoluteTime = absoluteTime,
                        ChannelNumber = channel,
                        ControllerNumber = nextByte,
                        ControllerValue = Math.Min(reader.ReadByte(), (byte)127)
                    });
                    size += 1;
                    break;

                case 0xC:
                    var instrument = new MusicalInstrument(nextByte);
                    track.Channels[channel].Events.Add(new InstrumentChange
                    {
                        AbsoluteTime = absoluteTime,
                        ChannelNumber = channel,
                        Instrument = instrument
                    });
                    break;

                case 0xD:
                    track.Channels[channel].Events.Add(new ChannelAftertouch
                    {
                        AbsoluteTime = absoluteTime,
                        ChannelNumber = channel,
                        AfterTouchValue = nextByte
                    });
                    break;

                case 0xE:
                    var lsb = nextByte;
                    var msb = reader.ReadByte();
                    var pitchValue = (ushort) ((msb << 7) | lsb);

                    if (pitchValue > 16383) throw new FormatException("Pitch value shouldn't be greater than 16383");

                    track.Channels[channel].Events.Add(new PitchBend
                    {
                        AbsoluteTime = absoluteTime,
                        ChannelNumber = channel,
                        PitchValue = pitchValue
                    });
                    size += 1;
                    break;

                default: throw new FormatException("Unsupported event type");
            }
        }

        private static void ParseMetaEvent(MidiBigEndianReader reader, Track track, byte type, uint length, uint absoluteTime)
        {
            string text;
            byte[] data;

            switch (type)
            {
                case 0x00:
                    // sequence number
                    if (length != 2) throw new FormatException("Sequence number meta event should have length == 2");
                    track.MetaEvents.Add(new SequenceNumber
                    {
                        AbsoluteTime = absoluteTime,
                        Number = reader.ReadUInt16()
                    });
                    break;

                case 0x01:
                    // text event
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new TextEvent
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x02:
                    // copyright notice
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new CopyrightNotice
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x03:
                    // track name
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new TrackName
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x04:
                    // instrument name
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new InstrumentName
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x05:
                    // lyrics
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new Lyrics
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x06:
                    // marker
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new Marker
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x07:
                    // cue point
                    text = reader.ReadText(length);
                    track.MetaEvents.Add(new CuePoint
                    {
                        AbsoluteTime = absoluteTime,
                        Text = text
                    });
                    break;

                case 0x20:
                    // midi channel prefix
                    if (length != 1) throw new FormatException("midi channel prefix event should have length == 1");
                    track.MetaEvents.Add(new MidiChannelPrefix
                    {
                        AbsoluteTime = absoluteTime,
                        Channel = reader.ReadByte()
                    });
                    break;

                case 0x2F:
                    // end of track
                    if (length != 0) throw new FormatException("end of track event should have length == 0");
                    track.MetaEvents.Add(new EndOfTrack { AbsoluteTime = absoluteTime });
                    break;

                case 0x51:
                    // set tempo
                    if (length != 3) throw new FormatException("set tempo event should have length == 3");
                    track.MetaEvents.Add(new SetTempo
                    {
                        AbsoluteTime = absoluteTime,
                        MicrosecondsPerQuarterNote = reader.ReadUInt24()
                    });
                    break;

                case 0x54:
                    // SMPTE offset
                    if (length != 5) throw new FormatException("SMPTE offset event should have length == 5");
                    track.MetaEvents.Add(new SmpteOffset
                    {
                        AbsoluteTime = absoluteTime,
                        Hour = reader.ReadByte(),
                        Minute = reader.ReadByte(),
                        Second = reader.ReadByte(),
                        Frame = reader.ReadByte(),
                        SubFrame = reader.ReadByte()
                    });
                    break;

                case 0x58:
                    // time signature
                    if (length != 4) throw new FormatException("time signature event should have length == 4");
                    track.MetaEvents.Add(new TimeSignature
                    {
                        AbsoluteTime = absoluteTime,
                        Numerator = reader.ReadByte(),
                        Denominator = (byte) (1 << reader.ReadByte()),
                        MetronomePulse = reader.ReadByte(),
                        NumberOf32ndNotesPerMidiQuarterNote = reader.ReadByte()
                    });
                    break;

                case 0x59:
                    // key signature
                    if (length != 2) throw new FormatException("key signature event should have length == 2");

                    sbyte key = reader.ReadSByte();
                    if (key < -7 || key > 7) throw new FormatException("Key should be from -7 to 7");

                    Scale scale = reader.ReadByte() == 0 ? Scale.Major : Scale.Minor;

                    track.MetaEvents.Add(new KeySignature
                    {
                        AbsoluteTime = absoluteTime,
                        Key = new Key(scale, key)
                    });
                    break;

                case 0x7F:
                    // sequencer specific
                    data = reader.ReadBytes((int) length); // skips sequencer specific data
                    track.MetaEvents.Add(new SequencerSpecific
                    {
                        AbsoluteTime = absoluteTime,
                        Data = data
                    });
                    break;

                default:
                    // unknown meta event
                    data = reader.ReadBytes((int) length); // skips unknown data
                    track.MetaEvents.Add(new UnknownMetaEvent
                    {
                        AbsoluteTime = absoluteTime,
                        Data = data
                    });
                    break;
            }
        }

        private static AbstractTimeDivision ParseTimeDivision(ushort input)
        {
            if ((input & 0x8000) == 0)
            {
                // ticks per beat
                return new TicksPerBeatTimeDivision { NumberOfClockTicks = (ushort) (input & 0x7FFF) };
            }
            else
            {
                byte frames = (byte) ((input & 0x7F00) >> 8);
                if (frames != 24 && frames != 25 && frames != 29 && frames != 30)
                    throw new FormatException("Invalid value of SMPTE frames, should be 24, 25, 29 or 30");
                return new FramesPerSecondTimeDivision
                {
                    NumberOfFrames = frames,
                    TicksPerFrame = (byte) (input & 0x00FF)
                };
            }
        }
    }
}