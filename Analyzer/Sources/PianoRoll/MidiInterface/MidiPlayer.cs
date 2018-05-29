using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Midi;
using MidiModel;
using MidiParser;
using MusParser;
using Channel = Midi.Channel;

namespace PianoRoll.MidiInterface
{
    /// <summary>
    /// Manages all events connected to the playback of midi file
    /// </summary>
    public class MidiPlayer
    {
        /// <summary>
        /// True when the file is currently being played
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Returns the minimal and the maximal pitch of all notes in the current midi file
        /// </summary>
        public (int min, int max) NoteRange { get; private set; }

        /// <summary>
        /// The length of music played up to this moment
        /// </summary>
        public TimeSpan CurrentTime => TimeSpan.FromMinutes(_clock.Time / _clock.BeatsPerMinute) - Latency;

        public float MaxVolume => _midi.MaxRealVolume;

        public bool IsTempoNormalized { get; set; } = Properties.Settings.Default.NormalizeTempo;
        public bool ShowBeats { get; set; } = Properties.Settings.Default.ShowBeats;
        public bool ProlongSustainedNotes { get; set; } = Properties.Settings.Default.ProlongSustainedNotes;
        public bool ShowKey { get; set; } = Properties.Settings.Default.ShowKey;
        public bool ShowChords { get; set; } = Properties.Settings.Default.ShowChords;
        public bool PlayChords { get; set; } = Properties.Settings.Default.PlayChords;
        public bool DiscretizeBends { get; set; } = Properties.Settings.Default.DiscretizeBends;
        public bool TransposeToC { get; set; } = Properties.Settings.Default.TransposeToC;
        public bool RandomizeMus { get; set; } = Properties.Settings.Default.Randomize;
        public int KeyDetectorTrees { get; set; } = Properties.Settings.Default.KeyDetectorTrees;


        /// <summary>
        /// Latency that is caused by under-the-hood midi device to prevent glitches and lags
        /// </summary>
        public TimeSpan Latency = TimeSpan.FromMilliseconds(Properties.Settings.Default.Latency);

        public int MusFrameLength = Properties.Settings.Default.FrameLength;
        public TimeSpan ImprovisationStart = TimeSpan.Zero;

        public Key? Key => _midi.Key;

        public Model _midi;
        private Clock _clock;
        private readonly OutputDevice _outputDevice;
        private NoteStream _noteStream;

        private readonly ChannelState[] _channelStates;

        private Queue<BeatEvent> _beatEvents;
        private int _beatCounter;

        public MidiPlayer(IEnumerable<ChannelState> channelStates)
        {
            _channelStates = channelStates.ToArray();
            foreach (var channelState in _channelStates)
            {
                channelState.Player = this;
            }

            _outputDevice = ClockScheduler.SelectOutputDevice();
        }

        public void Open(MainWindow window, string filename)
        {
            //if (IsPlaying) Stop();

            _midi = Parse(filename);

            window.Dispatcher.Invoke(new Action(() => 
            {
                CalculateNoteRange();
                SetChannelMessages();

                ImprovisationStart = _midi.EventsOfType<ImprovisationStartMetaEvent>().Select(e => e.AbsoluteRealTime).DefaultIfEmpty().First();

                _clock = ClockScheduler.Schedule(_midi, _outputDevice);

                _noteStream = new NoteStream(_midi);
                _beatEvents = new Queue<BeatEvent>(_midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime));
                _beatCounter = 0;

                foreach (var channelState in _channelStates) channelState.Reset();

                Play();
            }));
        }

        public void Save(string filename)
        {
            switch (Path.GetExtension(filename))
            {
                case ".mid":
                    ModelToMidiParser.Parse(_midi, filename);
                    break;

                case ".mus":
                    ModelToMusicEvents.Parse(_midi, filename);
                    break;

                default: throw new ApplicationException($"Cannot save extension {Path.GetExtension(filename)}");
            }
        }

        public void Play()
        {
            if (IsPlaying) return;

            if (CurrentTime > TimeSpan.Zero) UnsilenceChannels();

            IsPlaying = true;

            if (!_outputDevice.IsOpen) _outputDevice?.Open();
            _clock?.Start();
        }

        public void Pause()
        {
            if (!IsPlaying) return;

            SilenceChannels();

            IsPlaying = false;

            _clock?.Stop();
        }

        public void Stop()
        {
            if (!IsPlaying) return;

            IsPlaying = false;

            _clock?.Stop();
            _clock?.Reset();
            _clock = ClockScheduler.Schedule(_midi, _outputDevice);

            _noteStream = new NoteStream(_midi);
            _beatEvents = new Queue<BeatEvent>(_midi.EventsOfType<BeatEvent>().OrderBy(b => b.AbsoluteRealTime));
            _beatCounter = 0;

            UnsilenceChannels();
            foreach (var channel in _channelStates) channel.Reset();

            _outputDevice?.Close();
        }

        public bool IsChannelPlayable(int channelNumber)
        {
            return _midi.IsChannelPlayable[channelNumber];
        }

        /// <summary>
        /// Returns notes that are in the time interval [currentTime - startOffset, currentTime - startOffset + frameLength]
        /// </summary>
        public NoteOn[] UpdateState(TimeSpan startOffset, TimeSpan frameLength)
        {
            var frame = _noteStream == null
                ? new NoteOn[0]
                : _noteStream.GetFrame(CurrentTime - startOffset, frameLength).ToArray();

            UpdateChannelStates(frame);

            return frame;
        }

        /// <summary>
        /// Returns the fraction of the length of music played up to this moment and the total length of the file
        /// </summary>
        public float GetElapsedTimePercantage()
        {
            return (float) (CurrentTime.TotalSeconds / _midi.Length.TotalSeconds);
        }

        public bool IsChannelMuted(int channelNumber)
        {
            return _channelStates[channelNumber].IsMuted;
        }

        /// <summary>
        /// Sets the volume of the channel to the one the channel was playing before $MuteChannel had been called
        /// </summary>
        public void MuteChannel(int channelNumber)
        {
            if (IsPlaying)
                _outputDevice.SendControlChange((Channel)channelNumber, Control.Volume, 0);
        }

        public (Key key, int beat)? GetCurrentChord(TimeSpan time)
        {
            if(_beatEvents.Count == 0) return null;

            var actualBeat = _beatEvents.Peek();
            while (_beatEvents.Count > 1 && actualBeat.AbsoluteRealTime + actualBeat.Length <= time)
            {
                _beatEvents.Dequeue();
                _beatCounter++;
                actualBeat = _beatEvents.Peek();
            }

            return (actualBeat.Chord, _beatCounter);
        }

        /// <summary>
        /// Sets the volume of the channel to 0
        /// </summary>
        public void UnmuteChannel(int channelNumber)
        {
            if (IsPlaying)
                _outputDevice.SendControlChange((Channel)channelNumber, Control.Volume, _channelStates[channelNumber].Volume);
        }

        private void CalculateNoteRange()
        {
            if (!_midi.EventsOfType<NoteOn>().Any())
            {
                NoteRange = (64, 64);
                return;
            }

            NoteRange = _midi.EventsOfType<NoteOn>().Aggregate((128, 0), AggregateFunction);

            // selects the minimal and the maximal note heights
            (int min, int max) AggregateFunction((int min, int max) input, NoteOn note)
            {
                (int min, int max) output;

                output.min = note.NoteNumber < input.min ? note.NoteNumber : input.min;
                output.max = note.NoteNumber > input.max ? note.NoteNumber : input.max;

                return output;
            }
        }

        private void SetChannelMessages()
        {
            for (int i = 0; i < _channelStates.Length; i++)
            {
                if (IsChannelPlayable(i)) _channelStates[i].SetChannelMessages(_midi);
            }
        }

        private void UpdateChannelStates(NoteOn[] notes)
        {
            for (int i = 0; i < _channelStates.Length; i++)
            {
                if (IsChannelPlayable(i)) _channelStates[i].UpdateChannelStates(_midi, CurrentTime, notes);
            }
        }
        
        private void SilenceChannels()
        {
            for (int i = 0; i < _channelStates.Length; i++)
            {
                if (!IsChannelPlayable(i) || IsChannelMuted(i)) continue;

                _outputDevice.SendControlChange((Channel) i, Control.Volume, 0);
            }
        }

        private void UnsilenceChannels()
        {
            for (int i = 0; i < Model.NumberOfChannels; i++)
            {
                if (!IsChannelPlayable(i) || IsChannelMuted(i)) continue;

                _outputDevice.SendControlChange((Channel) i, Control.Volume, _channelStates[i].Volume);
            }
        }

        public Model Parse(string filename)
        {
            var extension = Path.GetExtension(filename)?.ToLower();

            switch (extension)
            {
                case ".mid": return MidiToModelParser.Parse(filename, true, true, true, true, IsTempoNormalized, ProlongSustainedNotes, ShowBeats, ShowKey, ShowChords, PlayChords, DiscretizeBends, TransposeToC ? (Tone?)Tone.C : null);
                case ".mus": return MusicEventsToModel.Parse(filename, MusFrameLength, ShowBeats, PlayChords, RandomizeMus);

                default:
                    throw new ArgumentException("Unsupported file format");
            }
        }
    }
}
