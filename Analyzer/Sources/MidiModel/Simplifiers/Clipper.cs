using System;
using System.Linq;

namespace MidiModel.Simplifiers
{
    public static class Clipper
    {
        public static void Clip(Model midi, TimeSpan min, TimeSpan max)
        {
            foreach (var channel in midi.Tracks.SelectMany(t => t.Channels))
            {
                for (var i = 0; i < channel.Count; i++)
                {
                    var e = channel.Events[i];
                    if (e is NoteOff)
                    {
                        channel.Events.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if(!(e is NoteOn))
                        continue;

                    var note = e as NoteOn;

                    if (note.End < min || note.AbsoluteRealTime > max)
                    {
                        channel.Events.RemoveAt(i);
                        i--;
                    }
                }

                var buffer = channel.Events.OfType<NoteOn>().ToArray();
                foreach (var note in buffer)
                {
                    if (note.AbsoluteRealTime < min) note.AbsoluteRealTime = TimeSpan.Zero;
                    else note.AbsoluteRealTime -= min;

                    if (note.End > max) note.End = TimeSpan.FromSeconds(30);
                    else note.End -= min;


                    channel.Events.Add(new NoteOff
                    {
                        ChannelNumber = note.ChannelNumber,
                        AbsoluteRealTime = note.End,
                        NoteNumber = note.NoteNumber,
                        Velocity = 64
                    });
                }
            }
        }
    }
}
