using System;
using System.Collections.Generic;
using System.Linq;
using Midi;
using MidiModel;
using PianoRoll.Gui;

namespace PianoRoll.MidiInterface
{
    /// <summary>
    /// Represents current state of a midi channel
    /// This means: current instrument, volume and whether the channel is just playing or is being muted
    /// </summary>
    public class ChannelState
    {
        public int ChannelNumber { get; }

        private MidiPlayer _player;
        public MidiPlayer Player
        {
            set
            {
                _player = value;
                Reset();
            }
        }

        private readonly ChannelButton _button;

        private MidiModel.Instrument _instrument;
        private bool _isPlaying;
        private bool _isMuted;

        private Controller[] _volumeMessages;
        private InstrumentChange[] _instrumentMessages;
        private TimeSpan _lastTimeUpdated;

        public ChannelState(ChannelButton button, int channelNumber)
        {
            _button = button;
            ChannelNumber = channelNumber;
        }

        public MidiModel.Instrument Instrument
        {
            get => _instrument;
            set
            {
                if (value == _instrument) return;

                _instrument = value;
                _button.SetInstrument(_instrument);
            }
        }

        public byte Volume { get; set; }

        /// <summary>
        /// True when a note from the channel is being played
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (value == _isPlaying) return;

                _isPlaying = value;
                if (!_isMuted || !_isPlaying) _button.IsPlaying = _isPlaying;
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (value == _isMuted) return;

                _isMuted = value;
                if (_isMuted) IsPlaying = false;

                _button.IsMuted = _isMuted;
            }
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void Reset()
        {
            Volume = (byte) sbyte.MaxValue;
            Instrument = null;
            IsPlaying = false;
            IsMuted = false;

            _lastTimeUpdated = TimeSpan.MinValue;
        }

        /// <summary>
        /// This needs to be called when a new midi is opened
        /// Saves important control change messages into the channel cache
        /// </summary>
        public void SetChannelMessages(Model midi)
        {
            _volumeMessages = midi.EventsOfType<Controller>()
                .Where(c => c.ControlEnum == Control.Volume && c.ChannelNumber == ChannelNumber).ToArray();

            _instrumentMessages = midi.EventsOfType<InstrumentChange>().Where(c => c.ChannelNumber == ChannelNumber).ToArray();
        }

        /// <summary>
        /// Updates all states of the channel according to the current time
        /// </summary>
        /// <param name="midi"></param>
        /// <param name="currentTime"></param>
        /// <param name="notes">notes in the current frame</param>
        public void UpdateChannelStates(Model midi, TimeSpan currentTime, IEnumerable<NoteOn> notes)
        {
            // update playable channels
            var playingNotes = from note in notes
                where note.ChannelNumber == ChannelNumber
                where note.AbsoluteRealTime <= currentTime &&
                      note.AbsoluteRealTime + note.RealTimeLength > _lastTimeUpdated
                select note;

            IsPlaying = playingNotes.Any();

            // update instruments
            // ordering overhead doesn't matter, because there are 0-1 items most of the time
            var instruments = from m in _instrumentMessages
                where m.AbsoluteRealTime >= _lastTimeUpdated && 
                      m.AbsoluteRealTime <= currentTime
                orderby m.AbsoluteTime descending
                select m;
            if (instruments.Any()) Instrument = instruments.First().Instrument;

            // update volumes
            // ordering overhead doesn't matter, because there are 0-1 items most of the time
            var volumes = from m in _volumeMessages
                where m.AbsoluteRealTime >= _lastTimeUpdated && 
                      m.AbsoluteRealTime <= currentTime
                orderby m.AbsoluteTime descending
                select m;
            if (volumes.Any()) Volume = volumes.First().ControllerValue;


            _lastTimeUpdated = currentTime;
        }

        public void Mute()
        {
            if (_player == null) return;

            if (IsMuted)
            {
                IsMuted = false;
                _player.UnmuteChannel(ChannelNumber);
            }
            else
            {
                IsMuted = true;
                _player.MuteChannel(ChannelNumber);
            }
        }
    }
}
