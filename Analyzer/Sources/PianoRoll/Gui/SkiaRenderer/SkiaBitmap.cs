using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace PianoRoll.SkiaRenderer
{
    public interface IUpdatableBitmap
    {
        WriteableBitmap Bitmap { get; }

        /// <summary>
        /// Draws a new frame by calling the renderer
        /// </summary>
        /// <param name="renderer">Function that takes a SKCanvas and updates it</param>
        void UpdateBitmap(Action<SKCanvas> renderer);

        void PauseRendering();
        void ContinueRendering();

        /// <summary>
        /// Change the size of the bitmap
        /// </summary>
        void Resize(int width, int height);
    }

    class SkiaBitmap : IUpdatableBitmap
    {
        public WriteableBitmap Bitmap { get; private set; }

        private readonly int _dpiX;
        private readonly int _dpiY;
        private readonly double _dpiXResize;
        private readonly double _dpiYResize;

        private bool _is_active;

        public SkiaBitmap(int width, int height)
        {
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

            _dpiX = (int) dpiXProperty.GetValue(null, null);
            _dpiY = (int) dpiYProperty.GetValue(null, null);

            _dpiXResize = _dpiX / 96.0;
            _dpiYResize = _dpiY / 96.0;

            _is_active = true;

            Bitmap = new WriteableBitmap((int) (width * _dpiXResize), (int) (height * _dpiYResize), _dpiX, _dpiY,
                PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
        }
        
        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            Bitmap = new WriteableBitmap((int) (width * _dpiXResize), (int) (height * _dpiYResize), _dpiX, _dpiY,
                PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
        }

        public void PauseRendering()
        {
            _is_active = false;
        }

        public void ContinueRendering()
        {
            _is_active = true;
        }

        public void UpdateBitmap(Action<SKCanvas> renderer)
        {
            if (!_is_active) return;

            var width = (int) (Bitmap.Width * _dpiXResize);
            var height = (int) (Bitmap.Height * _dpiYResize);

            Bitmap.Lock();

            using (var surface = SKSurface.Create(width, height, SKColorType.Bgra8888, SKAlphaType.Premul, Bitmap.BackBuffer, width * 4))
            {
                var canvas = surface.Canvas;
                renderer(canvas);
            }

            Bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            Bitmap.Unlock();
        }
    }
}