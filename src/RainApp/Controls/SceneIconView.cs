using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace RainApp.Controls;

/// <summary>
/// A Scene's illustrated icon, drawn with SkiaSharp (ADR-0002).
/// Ticket #2 ships the static frame of each Scene's look; the mockup's
/// animations (falling drops, flicker, …) and the freeze-when-paused rule are
/// ticket #8 and attach here without changing call sites.
/// </summary>
public sealed class SceneIconView : SKCanvasView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(SceneIcon), typeof(SceneIconView),
            defaultValue: SceneIcon.RainOnWindow,
            propertyChanged: (bindable, _, _) => ((SceneIconView)bindable).InvalidateSurface());

    public SceneIcon Icon
    {
        get => (SceneIcon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public SceneIconView()
    {
        EnableTouchEvents = false;
        BackgroundColor = Colors.Transparent;
        PaintSurface += OnPaintSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        SceneIconRenderer.Draw(Icon, e.Surface.Canvas, e.Info.Width, e.Info.Height);
}

/// <summary>
/// Pure Skia drawing of the Scene icons. Geometry and colors are ported from
/// mockup.html (the design source of truth); coordinates are the mockup's
/// 48×48 viewBox, scaled to the surface.
/// </summary>
public static class SceneIconRenderer
{
    public static void Draw(SceneIcon icon, SKCanvas canvas, int width, int height)
    {
        var paint = new SKPaint { IsAntialias = true };

        var background = icon switch
        {
            SceneIcon.RainOnWindow => (new SKColor(0x2C, 0x3E, 0x63), new SKColor(0x1D, 0x2B, 0x4A)),
            _ => (new SKColor(0x2C, 0x3E, 0x63), new SKColor(0x1D, 0x2B, 0x4A)),
        };
        DrawBackground(canvas, paint, width, height, background.Item1, background.Item2);

        var scale = Math.Min(width, height) / 48f;
        canvas.Save();
        canvas.Translate((width - 48 * scale) / 2f, (height - 48 * scale) / 2f);
        canvas.Scale(scale);

        switch (icon)
        {
            case SceneIcon.RainOnWindow:
                DrawRain(paint, canvas);
                break;
        }
        canvas.Restore();
    }

    // Cloud + falling drops (mockup "Rain on Window" SVG, static frame).
    private static void DrawRain(SKPaint paint, SKCanvas canvas)
    {
        paint.Color = new SKColor(0xC3, 0xCD, 0xEC);
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawCircle(21, 18, 8, paint);
        canvas.DrawCircle(30, 20, 6.5f, paint);
        canvas.DrawCircle(13.5f, 22.5f, 5.5f, paint);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(12, 20, 36, 28), 4, 4), paint);

        paint.Color = new SKColor(0x79, 0xAA, 0xFF);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2.6f;
        paint.StrokeCap = SKStrokeCap.Round;
        foreach (var x in new[] { 17f, 24f, 31f })
        {
            canvas.DrawLine(x, 32, x - 1.2f, 35.5f, paint);
        }
    }

    // 135° gradient tile, as in the mockup's .card-icon background.
    private static void DrawBackground(SKCanvas canvas, SKPaint paint, int width, int height, SKColor from, SKColor to)
    {
        paint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(width, height),
            [from, to], null, SKShaderTileMode.Clamp);
        canvas.DrawRect(0, 0, width, height, paint);
        paint.Shader = null;
    }
}
