using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SkiaSharp;

namespace PianoRoll.SkiaRenderer {
    class FpsRenderer : IRenderer {
        private Stopwatch stopwatch;
        private TimeSpan lastTime;

        private int N;
        private Queue<double> lastN;

        public FpsRenderer(int N = 60) {
            this.N = N;

            stopwatch = new Stopwatch();
            stopwatch.Start();

            lastTime = TimeSpan.Zero;
            lastN = new Queue<double>(N);
        }

        public void Render(SKCanvas canvas) {
            lastN.Enqueue((stopwatch.Elapsed - lastTime).TotalSeconds);

            if (lastN.Count > N) lastN.Dequeue();

            canvas.DrawText($"FPS: {N / lastN.Sum():0.00}", 10, 10 + 18, new SKPaint {
                Color = SKColors.White,
                TextSize = 18
            });
            lastTime = stopwatch.Elapsed;
        }
    }
}
