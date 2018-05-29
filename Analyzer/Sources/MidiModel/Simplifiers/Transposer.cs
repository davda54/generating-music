using System.Linq;

namespace MidiModel.Simplifiers
{
    public class Transposer
    {
        public static void Transpose(Model midi, Tone tone)
        {
            if (!midi.Key.HasValue) return;

            var key = midi.Key.Value;

            var targetTone = key.Scale == Scale.Major ? tone : (Tone)(((int)tone - 3 + 12) % 12);
            var scaleDown = key.Tone > targetTone ? key.Tone - targetTone : key.Tone - targetTone + 12;
            var scaleUp = 12 - scaleDown;

            var scale = scaleDown < scaleUp ? -scaleDown : scaleUp; 

            foreach (var note in midi.EventsOfType<NoteOn>().Where(n => !n.IsPercussion))
            {
                var noteNumber = note.NoteNumber + scale;
                if (noteNumber < 0) noteNumber += 12;
                else if (noteNumber > 127) noteNumber -= 12;

                note.NoteNumber = (byte) noteNumber;
            }

            foreach (var beat in midi.EventsOfType<BeatEvent>())
            {
                beat.Chord.Tone = (Tone)(((int)beat.Chord.Tone + scale + 12) % 12);
            }

            foreach (var keySignature in midi.EventsOfType<KeySignature>())
            {
                keySignature.Key.Tone = (Tone)(((int)keySignature.Key.Tone + scale + 12) % 12);
            }

            key.Tone = tone;
        }
    }
}
