using System.Collections.Generic;
using System.Linq;
using Midi;

namespace MidiModel
{
    public class VolumeChangeCollector
    {
        private List<Controller>[] volumeChanges;
        private List<Controller>[] expressionChanges;
        private Model midi;

        public VolumeChangeCollector(Model midi)
        {
            volumeChanges = new List<Controller>[16];
            expressionChanges = new List<Controller>[16];

            for (var i = 0; i < 16; i++) volumeChanges[i] = new List<Controller>();
            for (var i = 0; i < 16; i++) expressionChanges[i] = new List<Controller>();

            foreach (var e in midi.EventsOfType<Controller>().Where(c => c.ControlEnum == Control.Volume))
                volumeChanges[e.ChannelNumber].Add(e);

            foreach (var e in midi.EventsOfType<Controller>().Where(c => c.ControlEnum == Control.Expression))
                expressionChanges[e.ChannelNumber].Add(e);

            this.midi = midi;
        }

        public void DetermineVolumes()
        {
            var max = 0f;
            foreach (var note in midi.EventsOfType<NoteOn>())
            {
                var volume = ControllerValueDuringNote(volumeChanges, note, 96);
                var expression = ControllerValueDuringNote(expressionChanges, note, 96);
                

                note.RealVolume = (float) (note.Volume / 127f * volume / 127f * expression / 127f);

                if (note.RealVolume > max) max = note.RealVolume;
            }

            midi.MaxRealVolume = max;
        }

        double ControllerValueDuringNote(IEnumerable<Controller>[] changes, NoteOn note, double defaultValue = 96)
        {
            var during = changes[note.ChannelNumber]
                .Where(ch => ch.AbsoluteRealTime < note.AbsoluteRealTime + note.RealTimeLength)
                .Where(ch => ch.AbsoluteRealTime >= note.AbsoluteRealTime);

            if (during.Any())
            {
                return during.Average(d => d.ControllerValue);
            }

            var before = changes[note.ChannelNumber].Where(ch => ch.AbsoluteRealTime <= note.AbsoluteRealTime);

            if (before.Any())
            {
                // select max according to AbsoluteRealTIme
                return before.Aggregate((prev, act) => prev.AbsoluteRealTime < act.AbsoluteRealTime ? act : prev).ControllerValue; 
            }

            return defaultValue;
        }
    }
}
