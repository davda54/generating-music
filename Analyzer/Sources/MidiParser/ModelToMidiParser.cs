using System.IO;
using System.Linq;
using MidiModel;
using MidiParser.BigEndian;

namespace MidiParser
{
    public static class ModelToMidiParser
    {
        public static void Parse(Model midi, string filename, int frameRateMillis = 50)
        {
            using (MidiBigEndianWriter writer = new MidiBigEndianWriter(File.Open(filename, FileMode.Create)))
            {
                ParseHeaderChunk(writer, frameRateMillis);
                ParseTrackChunk(midi, writer, frameRateMillis);
            }
        }

        private static void ParseHeaderChunk(BigEndianWriter writer, int frameRateMillis)
        {
            // MIDI should start with "MThd"
            writer.Write((uint) 0x4D546864);

            // the length of this header
            writer.Write((uint) 6);

            // midi format
            writer.Write((ushort) 1);

            // number of tracks
            writer.Write((ushort) 1);

            // time division
            writer.Write((ushort) (1000 / 2));
        }

        private static void ParseTrackChunk(Model midi, MidiBigEndianWriter writer, int frameRateMillis)
        {
            // track chunk signature
            writer.Write((uint)0x4D54726B);

            var notes = midi.Where((MidiControlEvent e) => e is NoteOn || e is NoteOff).OrderBy(e => e.AbsoluteRealTime).ToArray();

            // notes + instrument + end of track
            var length = notes.Length * 7 + 6*(Instrument.NumberOfClusters-1) + 4;

            // write the length of the track
            writer.Write((uint)length);

            // write instrument changes
            for (var i = 0; i < Instrument.NumberOfClusters; i++)
            {
                if(i == Channel.PercussionChannelNumber) continue;

                writer.WriteSemiVariableLengthValue(0);
                writer.Write((byte) (0xC0 | i));
                writer.Write((byte) Instrument.TypicalInstrument((InstrumentCluster)i).Id());
            }


            var lastTime = 0.0;
            foreach (var note in notes)
            {
                // delta time
                writer.WriteSemiVariableLengthValue((uint)((note.AbsoluteRealTime.TotalMilliseconds - lastTime)));

                if (note is NoteOn)
                {
                    var noteOn = (NoteOn) note;

                    // event type
                    writer.Write((byte)(0x90 | note.ChannelNumber));

                    writer.Write((byte)noteOn.NoteNumber);
                    writer.Write((byte)noteOn.Volume);
                }
                else if (note is NoteOff)
                {
                    var noteOff = (NoteOff) note;

                    // event type
                    writer.Write((byte)(0x80 | note.ChannelNumber));

                    writer.Write((byte)noteOff.NoteNumber);
                    writer.Write((byte)noteOff.Velocity);
                }

                lastTime = note.AbsoluteRealTime.TotalMilliseconds;
            }


            // end of track

            // delta time
            writer.WriteVariableLengthValue(0);

            // meta event
            writer.Write((byte) 0xFF);

            // end of track event
            writer.Write((byte) 0x2F);

            // length
            writer.WriteVariableLengthValue(0);
        }
    }
}

