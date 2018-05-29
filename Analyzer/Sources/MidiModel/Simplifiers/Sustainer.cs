using System;
using System.Collections.Generic;
using System.Linq;
using Midi;

namespace MidiModel.Simplifiers
{
    public class Sustainer
    {
        private List<SustainEvent> _sustainEvents;
        private (NoteOn note, bool isReleased)?[][] _sustainedNotes;
        private bool[] _isChannelSustained;
        private Model _midi;

        class SustainEvent
        {
            public TimeSpan Time;
        }

        class OnEvent : SustainEvent
        {
            public NoteOn Note;
        }

        class OffEvent : SustainEvent
        {
            public NoteOn Note;
        }

        class ChangeEvent : SustainEvent
        {
            public Controller Controller;
        }

        public Sustainer(Model midi)
        {
            _isChannelSustained = new bool[16];
            _sustainEvents = new List<SustainEvent>();

            _sustainedNotes = new(NoteOn note, bool isReleased)?[16][];
            for(var i = 0; i < _sustainedNotes.Length; i++) _sustainedNotes[i] = new (NoteOn note, bool isReleased)?[128];


            foreach (var e in midi.EventsOfType<Controller>().Where(c => c.ControlEnum == Control.SustainPedal))
            {
                _sustainEvents.Add(new ChangeEvent { Controller = e, Time = e.AbsoluteRealTime });
            }

            foreach (var note in midi.EventsOfType<NoteOn>().Where(n => n.Volume > 0))
            {
                _sustainEvents.Add(new OnEvent {Note = note, Time = note.AbsoluteRealTime});
                _sustainEvents.Add(new OffEvent {Note = note, Time = note.End});
            }

            _sustainEvents = _sustainEvents.OrderBy(e => e.Time).ToList();

            _midi = midi;
        }

        public void ProlongSustainedNotes()
        {
            foreach (var e in _sustainEvents)
            {
                //TODO: too lazy to think about polymorhism :(

                if(e is OnEvent) ProcessEvent((OnEvent)e);
                else if(e is OffEvent) ProcessEvent((OffEvent)e);
                else ProcessEvent((ChangeEvent)e);
            }
        }

        private void ProcessEvent(OnEvent e)
        {
            if (_sustainedNotes[e.Note.ChannelNumber][e.Note.NoteNumber] != null)
            {
                var previousNote = _sustainedNotes[e.Note.ChannelNumber][e.Note.NoteNumber].Value.note;

                previousNote.End = e.Time;
                previousNote.RealTimeLength = previousNote.End - previousNote.AbsoluteRealTime;
            }

            _sustainedNotes[e.Note.ChannelNumber][e.Note.NoteNumber] = (e.Note, false);
        }

        private void ProcessEvent(OffEvent e)
        {
            if (_isChannelSustained[e.Note.ChannelNumber])
            {
                _sustainedNotes[e.Note.ChannelNumber][e.Note.NoteNumber] = (e.Note, true);
            }
            else
            {
                _sustainedNotes[e.Note.ChannelNumber][e.Note.NoteNumber] = null;
            }
        }

        private void ProcessEvent(ChangeEvent e)
        {
            bool isOn = e.Controller.ControllerValue >= 64;

            if (!isOn)
            {
                foreach (var noteEvent in _sustainedNotes[e.Controller.ChannelNumber].Where(n => n != null && n.Value.isReleased))
                {
                    var note = noteEvent.Value.note;
                    note.End = e.Time;
                    note.RealTimeLength = note.End - note.AbsoluteRealTime;

                    _sustainedNotes[e.Controller.ChannelNumber][note.NoteNumber] = null;
                }
            }

            _isChannelSustained[e.Controller.ChannelNumber] = isOn;
        }
    }
}
