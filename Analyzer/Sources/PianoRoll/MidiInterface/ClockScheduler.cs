using System;
using System.Collections.Generic;
using Midi;
using MidiModel;
using Channel = Midi.Channel;

namespace PianoRoll.MidiInterface
{
    /// <summary>
    /// Schedules all the events to Midi.Clock so the file can be played
    /// </summary>
    static class ClockScheduler
    {
        /// <summary>
        /// Schedules all reproducable events from $midi to an $outputDevice
        /// </summary>
        /// <returns>Midi.Clock with scheduled events</returns>
        public static Clock Schedule(Model midi, OutputDevice outputDevice)
        {
            // needs to be 60 Beats Per Minute to have the music played in sync
            Clock clock = new Clock(60); 

            var scheduledMessages = new List<Message>();

            // collect all reproducable messages
            foreach (var e in midi.ControlEvents())
            {
                if (e is NoteOn)
                {
                    var note = e as NoteOn;
                    if(note.Volume == 0 || note.RealTimeLength <= TimeSpan.Zero) continue;

                    scheduledMessages.Add(new NoteOnOffMessage(outputDevice, (Channel) e.ChannelNumber,
                        (Pitch) note.NoteNumber, note.Volume, (float) note.AbsoluteRealTime.TotalSeconds, clock,
                        (float) note.RealTimeLength.TotalSeconds * 0.99f));
                }
                else if (e is PitchBend)
                {
                    var bend = e as PitchBend;
                    scheduledMessages.Add(new PitchBendMessage(outputDevice, (Channel) e.ChannelNumber, bend.PitchValue,
                        (float) bend.AbsoluteRealTime.TotalSeconds));
                }
                else if (e is InstrumentChange)
                {
                    var instrumentChange = e as InstrumentChange;
                    scheduledMessages.Add(new ProgramChangeMessage(outputDevice, (Channel) e.ChannelNumber,
                        instrumentChange.Instrument.Type, (float) instrumentChange.AbsoluteRealTime.TotalSeconds));
                }
                else if (e is Controller)
                {
                    var controller = e as Controller;
                    if (controller.ControlEnum != null)
                        scheduledMessages.Add(new ControlChangeMessage(outputDevice, (Channel) e.ChannelNumber,
                            controller.ControlEnum.Value, controller.ControllerValue,
                            (float) controller.AbsoluteRealTime.TotalSeconds));
                }
            }

            // sort them in orded to be scheduled more quickly
            // scheduling unsorted messages in midi-dot-net version 1.1.0 has time complexity n^2

            var comparer = Comparer<Message>.Create((a, b) => a.Time.CompareTo(b.Time));
            scheduledMessages.Sort(comparer);

            clock.Schedule(scheduledMessages, 0);

            return clock;
        }

        public static OutputDevice SelectOutputDevice()
        {
            if (OutputDevice.InstalledDevices.Count == 0)
            {
                throw new Exception("MIDI output device not found");
            }

            // TODO: It is possible that some advanced user has more than one device installed in OS
            // TODO: there should be an option in the settings to choose the right one

            return OutputDevice.InstalledDevices[0];
        }
    }
}
