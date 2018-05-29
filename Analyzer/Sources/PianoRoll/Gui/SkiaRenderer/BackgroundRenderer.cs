using System;
using SkiaSharp;

namespace PianoRoll.SkiaRenderer
{
    /// <summary>
    /// Renders background on the piano roll screen
    /// </summary>
    class BackgroundRenderer : INoteRenderer
    {

        /// <summary>
        /// Time difference between the actual time and the time in the left side of the screen
        /// </summary>
        public TimeSpan StartOffset { get; }

        public int PixelsPerSecond { get; set; }

        /// <summary>
        /// Determines whether the background has lines for each semitone
        /// </summary>
        public bool IsBackgrounedLined { get; set; } = true;

        /// <summary>
        /// Determines whether there are note names for each semitone
        /// </summary>
        public bool AreNoteNamesShown { get; set; } = true;

        /// <summary>
        /// Height of the time bar
        /// </summary>
        private const int ElapsedTimeHeight = 4;

        private static readonly SKTypeface Font = SKTypeface.FromFamilyName("Sagoe UI");

        private static readonly SKColor Background = SKColors.Black;
        private static readonly SKPaint BackgroundLinePaint = new SKPaint { Color = new SKColor(30, 30, 30) };
        private static readonly SKPaint BorderPaint = new SKPaint { Color = new SKColor(0xFF555555) };
        private static readonly SKPaint HighlightPaint = new SKPaint { Color = SKColors.Crimson };

        private static readonly SKPaint TextPaint =
            new SKPaint
            {
                Typeface = Font,
                IsAntialias = true,
                Color = new SKColor(0xFF555555),
                IsStroke = false,
                SubpixelText = true
            };

        public BackgroundRenderer(double lineOffsetMilis, int pixelsPerSecond)
        {
            StartOffset = TimeSpan.FromMilliseconds(lineOffsetMilis);
            PixelsPerSecond = pixelsPerSecond;
        }

        /// <summary>
        /// Renders the background on the canvas
        /// </summary>
        public void Render(SKCanvas canvas)
        {
            Clear(canvas);
            DrawRedLines(canvas);
            DrawElapsedTime(canvas);
        }

        /// <summary>
        /// Draws a little time bar on the bottom of the screen
        /// </summary>
        public void DrawElapsedTime(SKCanvas canvas, float elapsedTimePercentage = 0)
        {
            var rect = new SKRect(0, canvas.DeviceClipBounds.Height - ElapsedTimeHeight, 
                canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height);
            canvas.DrawRect(rect, BorderPaint);

            rect = new SKRect(0, canvas.DeviceClipBounds.Height - ElapsedTimeHeight, 
                elapsedTimePercentage * canvas.DeviceClipBounds.Width, canvas.DeviceClipBounds.Height);
            canvas.DrawRect(rect, HighlightPaint);
        }

        /// <summary>
        /// Draws red lines at the actual position of the music
        /// </summary>
        public void DrawRedLines(SKCanvas canvas)
        {
            var lineX = (float) StartOffset.TotalSeconds * PixelsPerSecond;

            canvas.DrawRect(new SKRect(lineX - 1, 0, lineX + 1, canvas.DeviceClipBounds.Height), HighlightPaint);
            canvas.DrawRect(new SKRect(lineX + 4, 0, lineX + 5, canvas.DeviceClipBounds.Height), HighlightPaint);
        }

        /// <summary>
        /// Draws grey background (and semitone lines if $IsBackgroundLines == true)
        /// </summary>
        public void Clear(SKCanvas canvas, int lineHeight = 20)
        {
            canvas.Clear(Background);

            if (IsBackgrounedLined && lineHeight > 0)
            {
                for (int y = 0; y < canvas.DeviceClipBounds.Height; y += lineHeight * 2)
                {
                    var rect = new SKRect(0, y, canvas.DeviceClipBounds.Width, y + lineHeight);
                    canvas.DrawRect(rect, BackgroundLinePaint);
                }
            }
        }

        /// <summary>
        /// Draws a note name for each semitone on the screen
        /// </summary>
        public void DrawNoteNames(SKCanvas canvas, int lineHeight, string[] noteNames)
        {
            if (AreNoteNamesShown && lineHeight > 0)
            {
                TextPaint.TextSize = lineHeight * 2f / 3f;

                for (int y = 1; y * lineHeight < canvas.DeviceClipBounds.Height; y++)
                {
                    canvas.DrawText(noteNames[y-1], 5, (y - 2f / 7f) * lineHeight, TextPaint);
                }
            }
        }
    }
}