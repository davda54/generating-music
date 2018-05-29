using SkiaSharp;

namespace PianoRoll.SkiaRenderer
{
    public interface IRenderer
    {
        void Render(SKCanvas canvas);
    }

    public interface INoteRenderer : IRenderer
    {
        bool IsBackgrounedLined { get; set; }
        bool AreNoteNamesShown { get; set; }
        int PixelsPerSecond { get; set; }
    }
}
