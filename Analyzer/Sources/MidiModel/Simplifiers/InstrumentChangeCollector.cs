using System;
using System.Collections.Generic;
using System.Linq;

namespace MidiModel
{
    public class InstrumentChangeCollector
    {
        private List<InstrumentChange>[] changes;
        private Model midi;

        public InstrumentChangeCollector(Model midi)
        {
            changes = new List<InstrumentChange>[16];
            for (int i = 0; i < 16; i++) changes[i] = new List<InstrumentChange>();

            foreach (var e in midi.EventsOfType<InstrumentChange>())
            {
                changes[e.ChannelNumber].Add(e);
            }

            this.midi = midi;
        }

        public void DetermineInstruments()
        {
            foreach (var note in midi.EventsOfType<NoteOn>())
            {
                if (note.ChannelNumber == 9)
                {
                    note.Instrument = new Percussion();
                }
                else
                {
                    var instrument = changes[note.ChannelNumber]
                        .Where(ch => ch.AbsoluteRealTime <= note.AbsoluteRealTime)
                        .DefaultIfEmpty(new InstrumentChange{ AbsoluteTime = 0, AbsoluteRealTime = TimeSpan.Zero, Instrument = new MusicalInstrument((byte)0) })
                        .Aggregate((prev, act) => prev.AbsoluteRealTime < act.AbsoluteRealTime ? act : prev); // select max

                    note.Instrument = instrument.Instrument;
                }
            }
        }
    }
}
