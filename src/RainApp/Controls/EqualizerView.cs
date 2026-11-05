using RainApp.Animation;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace RainApp.Controls;

/// <summary>
/// The equalizer bars on the active tile (mockup `.eq`): four 3px bars
/// bouncing between 5 and 17px on the 1s `eq` cycle, staggered by
/// <see cref="SceneKeyframes.EqualizerBarDelays"/>. They run on the same
/// clock as the Scene icons, so they dance only while audio is live and hold
/// the exact frame the pause landed on — the mockup's
/// `.phone.paused .eq span { animation-play-state: paused }`. Showing them
/// only on the current tile is the mockup's `.card.playing .eq`, and stays
/// with the page's bindings.
/// </summary>
public sealed class EqualizerView : SKCanvasView
{
    // Mockup .eq: four 3px-wide bars, 3px apart, bottom-aligned in an 18px
    // box, border-radius 2px (which renders as a 1.5px semicircle cap).
    private const float BarWidth = 3f;
    private const float BarGap = 3f;
    private const float BoxHeight = 18f;
    private const float Corner = 1.5f;
    private static readonly SKColor Accent = new(0xFF8B7CF6); // --accent

    public EqualizerView()
    {
        EnableTouchEvents = false;
        BackgroundColor = Colors.Transparent;
        WidthRequest = BarWidth * 4 + BarGap * 3;
        HeightRequest = BoxHeight;
        PaintSurface += OnPaintSurface;
        IconAnimation.FrameInvalidated += InvalidateSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var seconds = IconAnimation.Seconds;
        var canvas = e.Surface.Canvas;
        var scale = Math.Min(e.Info.Width / (BarWidth * 4 + BarGap * 3), e.Info.Height / BoxHeight);
        canvas.Translate((e.Info.Width - (BarWidth * 4 + BarGap * 3) * scale) / 2f,
            (e.Info.Height - BoxHeight * scale) / 2f);
        canvas.Scale(scale);

        var paint = new SKPaint { IsAntialias = true, Color = Accent };
        for (var bar = 0; bar < SceneKeyframes.EqualizerBarDelays.Length; bar++)
        {
            var height = (float)SceneKeyframes.EqualizerBar
                .Sample(seconds - SceneKeyframes.EqualizerBarDelays[bar]).Height;
            var left = bar * (BarWidth + BarGap);
            canvas.DrawRoundRect(new SKRect(left, BoxHeight - height, left + BarWidth, BoxHeight),
                Corner, Corner, paint);
        }
    }
}
