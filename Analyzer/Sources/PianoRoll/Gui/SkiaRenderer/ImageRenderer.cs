using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Midi;
using MidiModel;
using PianoRoll.Extensions;
using PianoRoll.MidiInterface;
using SkiaSharp;


namespace PianoRoll.SkiaRenderer
{
    /// <summary>
    /// Class that is responsible for the rendering of the whole skia bitmap.
    /// It renders the moving notes, semitone names and elapsed time
    /// </summary>
    class ImageRenderer : INoteRenderer
    {
        public MidiPlayer Player
        {
            set
            {
                _player = value;
                InitializePropertiesFromPlayer(_player);
            }
        }

        private MidiPlayer _player;

        private readonly BackgroundRenderer _background;

        private int _minNotePitch;
        private int _maxNotePitch;
        private int _notePixelsHeight;

        private string[] _noteNamesInRange;
        private SKPaint _notePaint;
        private SKPaint _textPaint;
        private static readonly SKColor[] BaseChannelColors;
        private static readonly SKColor[] DesaturatedChannelColors;

#if DEBUG
        private IRenderer fpsRenderer = new FpsRenderer();
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lineOffsetMillis">Gap between notes that are just being played and the left side fo the screen; in milliseconds</param>
        /// <param name="pixelsPerSecond"></param>
        public ImageRenderer(double lineOffsetMillis = 900)
        {
            _notePixelsHeight = 20;
            _background = new BackgroundRenderer(lineOffsetMillis, 200);
            _notePaint = new SKPaint { IsAntialias = true };
            _textPaint = new SKPaint { Color = SKColors.WhiteSmoke, IsAntialias = true, TextSize = 30 };

            IsBackgrounedLined = Properties.Settings.Default.ShowLinesOnBackground;
            AreNoteNamesShown = Properties.Settings.Default.ShowNoteNames;
            PixelsPerSecond = Properties.Settings.Default.PixelsPerSecond;
        }

        static ImageRenderer()
        {
            BaseChannelColors = new SKColor[Model.NumberOfChannels];
            DesaturatedChannelColors = new SKColor[Model.NumberOfChannels];

            for (var i = 0; i < BaseChannelColors.Length; i++)
            {
                var resource = Application.Current.FindResource("ChannelColor" + i);
                if (resource == null) throw new FileNotFoundException($"ChannelColor{i} cannot be found");

                var color = (Color) resource;

                BaseChannelColors[i] = color.ToSkColor();
                DesaturatedChannelColors[i] = color.Fade(0.6f).Lighten(0.3f).ToSkColor();
            }
        }

        public void Render(SKCanvas canvas)
        {
            _background.Clear(canvas, _notePixelsHeight);

            if (_player != null)
            {
                var time = _player.CurrentTime - _background.StartOffset; // time at the left side of the screen
                var length = TimeSpan.FromSeconds((double) canvas.DeviceClipBounds.Width / _background.PixelsPerSecond);
                var notes = _player.UpdateState(_background.StartOffset, length);

                _notePixelsHeight = canvas.DeviceClipBounds.Height / (_maxNotePitch - _minNotePitch + 1);

                if(_player.ImprovisationStart != TimeSpan.Zero) DrawImprovisationStart(canvas, time, _player.ImprovisationStart);

                _background.DrawNoteNames(canvas, _notePixelsHeight, _noteNamesInRange);

                foreach (var note in notes)
                {
                    if (_player.IsChannelMuted(note.ChannelNumber)) continue;

                    DrawNote(canvas, time, note);
                }

                if(_player.ShowKey && _player.Key.HasValue)
                {
                    canvas.DrawText($"Key: {_player.Key}", 40, 50, _textPaint);
                }

                if (_player.ShowChords)
                {
                    var beatPair = _player.GetCurrentChord(time + _background.StartOffset);

                    if(beatPair.HasValue) canvas.DrawText($"{beatPair.Value.key}  {beatPair.Value.beat}", 40, 100, _textPaint);
                }
            }

            _background.DrawRedLines(canvas);
            _background.DrawElapsedTime(canvas, _player?.GetElapsedTimePercantage() ?? 0);

#if DEBUG
            fpsRenderer.Render(canvas);
#endif
        }

        private void DrawImprovisationStart(SKCanvas canvas, TimeSpan time, TimeSpan improvisationStart)
        {
            var left = 0;
            var right = (float)(improvisationStart - time).TotalSeconds * _background.PixelsPerSecond;
            var top = 0;
            var bottom = canvas.DeviceClipBounds.Height;

            if (right > -10)
            {
                var rect = new SKRect(left, top, right, bottom);
                _notePaint.Color = BaseChannelColors[3].WithAlpha((byte)10);
                canvas.DrawRect(rect, _notePaint);

                rect = new SKRect(right - 2, top, right + 2, bottom);
                _notePaint.Color = BaseChannelColors[3].WithAlpha((byte)255);
                canvas.DrawRect(rect, _notePaint);
            }
        }

        private void DrawNote(SKCanvas canvas, TimeSpan offset, NoteOn note)
        {
            _notePaint.Color = note.AbsoluteRealTime - offset <= _background.StartOffset
                ? BaseChannelColors[note.ChannelNumber].WithAlpha((byte)((note.RealVolume / _player.MaxVolume)*255.999))
                : DesaturatedChannelColors[note.ChannelNumber].WithAlpha((byte)((note.RealVolume / _player.MaxVolume) * 255.999));

            if (note.Bends.Count > 0)
            {
                DrawPitchBend(canvas, offset, _notePaint, note);
                return;
            }

            var start = note.AbsoluteRealTime - offset;
            var end = start + note.RealTimeLength;

            DrawNoteRect(canvas, start, end, note.NoteNumber, _notePaint);
        }

        private void DrawPitchBend(SKCanvas canvas, TimeSpan offset, SKPaint paint, NoteOn note)
        {

            // draw the beginning of the note without bend
            var start = note.AbsoluteRealTime - offset;
            var end = note.Bends[0].AbsoluteRealTime - offset;
            DrawNoteRect(canvas, start, end, note.NoteNumber, paint);

            // draw bends
            for (var i = 0; i < note.Bends.Count - 1; i++)
            {
                start = note.Bends[i].AbsoluteRealTime - offset;
                end = note.Bends[i + 1].AbsoluteRealTime - offset;
                DrawNoteRect(canvas, start, end, Pitch(note, note.Bends[i]), paint);
            }

            // draw last bend
            start = note.Bends.Last().AbsoluteRealTime - offset;
            end = note.AbsoluteRealTime + note.RealTimeLength - offset;
            DrawNoteRect(canvas, start, end, Pitch(note, note.Bends.Last()), paint);
        }

        private void DrawNoteRect(SKCanvas canvas, TimeSpan start, TimeSpan stop, float pitch, SKPaint paint)
        {
            var left = (float) start.TotalSeconds * _background.PixelsPerSecond;
            var right = (float) stop.TotalSeconds * _background.PixelsPerSecond;
            var top = PitchToPixels(pitch);
            var bottom = PitchToPixels(pitch) - _notePixelsHeight;

            var rect = new SKRect(left, top, right, bottom);

            canvas.DrawRect(rect, paint);
        }

        private float Pitch(NoteOn note, PitchBend bend)
        {
            return note.NoteNumber + bend.RealPitchChange;
        }

        private float PitchToPixels(float pitch)
        {
            return (_maxNotePitch - pitch) * _notePixelsHeight;
        }

        private void InitializePropertiesFromPlayer(MidiPlayer player)
        {
            _minNotePitch = player.NoteRange.min - 1;
            _maxNotePitch = player.NoteRange.max + 3;

            _noteNamesInRange = new string[(_maxNotePitch - _minNotePitch + 1)*2]; // larger to fit extreme cases
            for (var y = 0; y < _noteNamesInRange.Length; y++)
            {
                var pitch = (Pitch) (_noteNamesInRange.Length/2 - y - 2 + _minNotePitch);
                _noteNamesInRange[y] = pitch.NotePreferringSharps().ToString() + pitch.Octave();
            }
        }

        public bool IsBackgrounedLined
        {
            get => _background.IsBackgrounedLined;
            set => _background.IsBackgrounedLined = value;
        }

        public bool AreNoteNamesShown
        {
            get => _background.AreNoteNamesShown;
            set => _background.AreNoteNamesShown = value;
        }

        public int PixelsPerSecond { get => _background.PixelsPerSecond; set => _background.PixelsPerSecond = value; }

    }
}
