using System;
using System.IO;
using System.Linq;
using MathExtension;
using MidiModel;
using MidiParser.ChordDetector;
using MidiParser.MetreNormalizer;

namespace MusParser
{
    public class MusicEventsToModel
    {
        public static Model Parse(string filename, int frameRateMillis = 50, bool showBeats = false, bool showChords = false, bool randomize = true)
        {
            var track = new Track();
            var midi = new Model { Tracks = new[] { track } };
            
            byte[] fileBytes = File.ReadAllBytes(filename);

            var random = new Random(123456);

            var timeMillis = 0;
            var beatLength = 12 * frameRateMillis;
            var actualNotes = new NoteOn[16,128];
            var actualPercussion = new NoteOn[128];

            var instrumentsOnChannels = new MusicalInstrument[16];

            for (byte i = 0; i < Model.NumberOfChannels; i++)
            {
                track.Channels[i].Events.Add(new Controller { AbsoluteRealTime = TimeSpan.Zero, AbsoluteTime = 0, ChannelNumber = i, ControllerNumber = 7, ControllerValue = 64 });
            }

            for (int i = 0; i < fileBytes.Length; i += 4)
            {
                var eventType = (EventType) (fileBytes[i] & 0x0F);

                if (i == 400 * 4)
                {
                    track.MetaEvents.Add(new ImprovisationStartMetaEvent { AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis) });
                }

                switch (eventType)
                {
                    case EventType.NoteOnEvent:
                    {
                        var channel = (byte) (fileBytes[i] >> 4);
                        var pitch = (byte)(fileBytes[i + 1] + ClusterRanges.Min((InstrumentCluster)channel));
                        var instrument = (MusicalInstrument)Instrument.TypicalInstrument((InstrumentCluster)channel);

                        var volume = fileBytes[i + 2] / 128.0 - 1;
                        volume = Math.Pow(volume, 2) * Math.Sign(volume);
                        if (randomize) volume = (byte)(volume*(0.9 + random.NextDouble() * 0.1));
                        volume = 64.0 + 64.0 * volume;

                        if (!instrument.Equals(instrumentsOnChannels[channel]))
                        {
                            track.Channels[channel].Events.Add(new InstrumentChange
                            {
                                AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis - 1),
                                AbsoluteTime = (uint) (timeMillis / frameRateMillis + 0.5),
                                ChannelNumber = channel,
                                Instrument = instrument
                            });
                            instrumentsOnChannels[channel] = instrument;
                        }

                        var prevNote = actualNotes[channel, pitch];
                        if (prevNote != null)
                        {
                            TimeSpan end;
                            if (timeMillis < prevNote.AbsoluteRealTime.TotalMilliseconds + 5000)
                                end = TimeSpan.FromMilliseconds(timeMillis);
                            else
                                end = prevNote.AbsoluteRealTime + TimeSpan.FromMilliseconds(5000);

                            prevNote.End = end;
                            prevNote.RealTimeLength = end - prevNote.AbsoluteRealTime;
                            prevNote.Length = (uint)(prevNote.RealTimeLength.TotalMilliseconds / frameRateMillis);

                            track.Channels[prevNote.ChannelNumber].Events.Add(new NoteOff
                            {
                                ChannelNumber = prevNote.ChannelNumber,
                                AbsoluteRealTime = prevNote.End,
                                AbsoluteTime = (uint) (timeMillis / frameRateMillis + 0.5),
                                NoteNumber = prevNote.NoteNumber,
                                Velocity = 64
                            });
                        }

                        var note = new NoteOn
                        {
                            ChannelNumber = channel,
                            NoteNumber = pitch,
                            Volume = (byte)Calc.Clamp((int)(volume + 0.5), 0, 127),
                            AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5),
                            Length = (uint)(4800/frameRateMillis),
                            AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis),
                            End = TimeSpan.FromMilliseconds(timeMillis + 4800),
                            RealTimeLength = TimeSpan.FromMilliseconds(4800)
                        };

                        track.Channels[channel].Events.Add(note);
                        actualNotes[channel,pitch] = note;
                        break;
                    }

                    case EventType.NoteOffEvent:
                    {
                        var channel = (byte)fileBytes[i + 2];
                        var pitch = (byte)(fileBytes[i + 1] + ClusterRanges.Min((InstrumentCluster)channel));

                        var note = actualNotes[channel,pitch];
                        if (note == null) //throw new Exception("neni na co navazovat");
                            continue;

                        TimeSpan end;
                        if (timeMillis < note.AbsoluteRealTime.TotalMilliseconds + 5000)
                            end = TimeSpan.FromMilliseconds(timeMillis);
                        else
                            end = note.AbsoluteRealTime + TimeSpan.FromMilliseconds(5000);

                        note.End = end;
                        note.RealTimeLength = end - note.AbsoluteRealTime;
                        note.Length = (uint)(note.RealTimeLength.TotalMilliseconds / frameRateMillis);
                            
                        track.Channels[note.ChannelNumber].Events.Add(new NoteOff
                        {
                            ChannelNumber = note.ChannelNumber,
                            AbsoluteRealTime = note.End,
                            AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5),
                            NoteNumber = note.NoteNumber,
                            Velocity = 64
                        });

                        actualNotes[channel,pitch] = null;
                        break;
                    }

                    case EventType.PercussionOnEvent:
                    {
                        var percussionType = (byte)(fileBytes[i + 1] + Percussion.MinNoteNumber);
                            //var volume = (byte)(clusterVolumes[InstrumentCluster.Percussion] * 128.0);
                        var volume = fileBytes[i + 2] / 128.0 - 1;
                        volume = Math.Pow(volume, 2) * Math.Sign(volume);
                        if (randomize) volume = (byte)(volume * (0.9 + random.NextDouble() * 0.1));
                        volume = 64.0 + 64.0 * volume;
                            
                        var prevNote = actualPercussion[percussionType];
                        if (prevNote != null)
                        {
                            TimeSpan end;
                            if (timeMillis < prevNote.AbsoluteRealTime.TotalMilliseconds + 4800)
                                end = TimeSpan.FromMilliseconds(timeMillis);
                            else
                                end = prevNote.AbsoluteRealTime + TimeSpan.FromMilliseconds(4800);

                            prevNote.End = end;
                            prevNote.RealTimeLength = end - prevNote.AbsoluteRealTime;
                            prevNote.Length = (uint)(prevNote.RealTimeLength.TotalMilliseconds / frameRateMillis);

                            track.Channels[prevNote.ChannelNumber].Events.Add(new NoteOff
                            {
                                ChannelNumber = prevNote.ChannelNumber,
                                AbsoluteRealTime = prevNote.End,
                                AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5),
                                NoteNumber = prevNote.NoteNumber,
                                Velocity = 64
                            });
                        }

                        var note = new NoteOn
                        {
                            ChannelNumber = Channel.PercussionChannelNumber,
                            NoteNumber = percussionType,
                            Volume = (byte)Calc.Clamp((int)(volume + 0.5), 0, 127),
                            AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5),
                            Length = (uint)(4800 / frameRateMillis),
                            AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis),
                            End = TimeSpan.FromMilliseconds(timeMillis + 4800),
                            RealTimeLength = TimeSpan.FromMilliseconds(4800)
                        };

                        track.Channels[Channel.PercussionChannelNumber].Events.Add(note);
                        actualPercussion[percussionType] = note;
                        break;
                    }

                    case EventType.PercussionOffEvent:
                    {
                        var percussionType = (byte)(fileBytes[i + 1] + Percussion.MinNoteNumber);

                        var percussion = actualPercussion[percussionType];
                        if (percussion == null) //throw new Exception("neni na co navazovat");
                            continue;

                        TimeSpan end;
                        if (timeMillis < percussion.AbsoluteRealTime.TotalMilliseconds + 4800)
                            end = TimeSpan.FromMilliseconds(timeMillis);
                        else
                            end = percussion.AbsoluteRealTime + TimeSpan.FromMilliseconds(4800);

                        percussion.End = end;
                        percussion.RealTimeLength = end - percussion.AbsoluteRealTime;
                        percussion.Length = (uint)(percussion.RealTimeLength.TotalMilliseconds / frameRateMillis);

                            track.Channels[percussion.ChannelNumber].Events.Add(new NoteOff
                        {
                            ChannelNumber = percussion.ChannelNumber,
                            AbsoluteRealTime = percussion.End,
                            AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5),
                            NoteNumber = percussion.NoteNumber,
                            Velocity = 64
                        });

                        actualPercussion[percussionType] = null;
                        break;
                    }

                    case EventType.SmallSpaceEvent:
                    {
                        var tempo = fileBytes[i + 1] | fileBytes[i + 2] << 8;
                        var chord = fileBytes[i + 3];

                        if (timeMillis % beatLength == 0 && chord != 24)
                        {
                            var beat = new BeatEvent {
                                AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5 + beatLength),
                                AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis + beatLength),
                                Length = TimeSpan.FromMilliseconds(beatLength),
                                Chord = new Key(chord),
                                Level = 2
                            };
                            track.MetaEvents.Add(beat);
                        }

                        timeMillis += frameRateMillis /** tempo / 600.0*/;
                        break;
                    }

                    case EventType.BigSpaceEvent:
                    {
                        var tempo = fileBytes[i + 1] | fileBytes[i + 2] << 8;
                        var chord = fileBytes[i + 3];

                        if (timeMillis % beatLength == 0 && chord != 24)
                        {
                            var beat = new BeatEvent
                            {
                                AbsoluteTime = (uint)(timeMillis / frameRateMillis + 0.5 + beatLength),
                                AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis + beatLength),
                                Length = TimeSpan.FromMilliseconds(beatLength),
                                Chord = new Key(chord),
                                Level = 2
                            };
                            track.MetaEvents.Add(beat);
                        }

                        timeMillis += frameRateMillis * 6 /** tempo / 600.0*/;
                        break;
                    }

                    case EventType.EndEvent:
                    {
                        timeMillis += frameRateMillis*200;
                        break;
                    }
                }
            }
            
            track.MetaEvents.Add(new EndOfTrack { AbsoluteRealTime = TimeSpan.FromMilliseconds(timeMillis + 1200) });

            {
                var collector = new VolumeChangeCollector(midi);
                collector.DetermineVolumes();
            }

            {
                var collector = new InstrumentChangeCollector(midi);
                collector.DetermineInstruments();
            }

            if (showBeats)
            {
                Normalizer.AddBeats(midi, false, 32);
            }
            if (showChords)
            {
                var analyzer = new ChordAnalyzer(midi);
                analyzer.AddChordNotesToModel(48);
            }

            if (randomize)
            {
                foreach (var note in midi.EventsOfType<NoteOn>())
                {
                    var startOffset = random.Next(10);
                    var lengthChange = 0.1*random.NextDouble() * note.RealTimeLength.TotalMilliseconds;

                    note.AbsoluteRealTime += TimeSpan.FromMilliseconds(startOffset);
                    note.RealTimeLength -= TimeSpan.FromMilliseconds(lengthChange);
                    note.End -= TimeSpan.FromMilliseconds(startOffset + lengthChange);
                }
            }

            ChannelPlayabilityChecker.Check(midi);
            midi.Length = midi.EventsOfType<EndOfTrack>().Max(e => e.AbsoluteRealTime);



            return midi;
        }
    }
}
