using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace RainApp.Controls;

/// <summary>
/// A Scene's illustrated icon, drawn with SkiaSharp (ADR-0002).
/// This ticket ships each Scene's static frame; the mockup's
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
/// 48×48 viewBox, scaled to the surface. Everything here is the static frame —
/// the mockup's per-element animation classes attach with ticket #8.
/// </summary>
public static class SceneIconRenderer
{
    public static void Draw(SceneIcon icon, SKCanvas canvas, int width, int height)
    {
        var paint = new SKPaint { IsAntialias = true };

        var (from, to) = Background(icon);
        DrawBackground(canvas, paint, width, height, from, to);

        var scale = Math.Min(width, height) / 48f;
        canvas.Save();
        canvas.Translate((width - 48 * scale) / 2f, (height - 48 * scale) / 2f);
        canvas.Scale(scale);

        switch (icon)
        {
            case SceneIcon.RainOnWindow: DrawRain(paint, canvas); break;
            case SceneIcon.OceanWaves: DrawOcean(paint, canvas); break;
            case SceneIcon.DistantThunderstorm: DrawThunderstorm(paint, canvas); break;
            case SceneIcon.CracklingCampfire: DrawCampfire(paint, canvas); break;
            case SceneIcon.NightForest: DrawNightForest(paint, canvas); break;
            case SceneIcon.BrownWhiteNoise: DrawNoiseGrain(paint, canvas); break;
            case SceneIcon.FanRoomHum: DrawFan(paint, canvas); break;
        }
        canvas.Restore();
    }

    // 135° gradient tile, as in the mockup's .card-icon background.
    private static (SKColor From, SKColor To) Background(SceneIcon icon) => icon switch
    {
        SceneIcon.OceanWaves => (Rgb(0x1d4553), Rgb(0x14303c)),
        SceneIcon.DistantThunderstorm => (Rgb(0x3a3160), Rgb(0x24203f)),
        SceneIcon.CracklingCampfire => (Rgb(0x5a3220), Rgb(0x3c2115)),
        SceneIcon.NightForest => (Rgb(0x1f4032), Rgb(0x14291f)),
        SceneIcon.BrownWhiteNoise => (Rgb(0x3c4256), Rgb(0x262b3a)),
        SceneIcon.FanRoomHum => (Rgb(0x274058), Rgb(0x1a2c3e)),
        _ => (Rgb(0x2c3e63), Rgb(0x1d2b4a)), // RainOnWindow
    };

    private static void DrawBackground(SKCanvas canvas, SKPaint paint, int width, int height, SKColor from, SKColor to)
    {
        paint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(width, height),
            [from, to], null, SKShaderTileMode.Clamp);
        canvas.DrawRect(0, 0, width, height, paint);
        paint.Shader = null;
    }

    // Cloud + falling drops (mockup "Rain on Window" SVG, static frame).
    private static void DrawRain(SKPaint paint, SKCanvas canvas)
    {
        Fill(paint, canvas, 0xc3cdec);
        Cloud(paint, canvas, 18, 20);

        Stroke(paint, canvas, 0x79aaff, 2.6f);
        foreach (var x in new[] { 17f, 24f, 31f })
        {
            canvas.DrawLine(x, 32, x - 1.2f, 35.5f, paint);
        }
    }

    // Moon over two scrolling wave layers (mockup "Ocean Waves" SVG, static
    // frame at its unscrolled position).
    private static void DrawOcean(SKPaint paint, SKCanvas canvas)
    {
        Fill(paint, canvas, 0xf2e9c8);
        canvas.DrawCircle(35, 11, 4.5f, paint);

        const string Wave = "M-72 0 q6 -4.5 12 0 t12 0 t12 0 t12 0 t12 0 t12 0 "
                          + "t12 0 t12 0 t12 0 t12 0 t12 0 t12 0 V48 H-72 Z";
        Fill(paint, canvas, 0x35617f);
        canvas.Translate(0, 26);
        DrawSvgPath(paint, canvas, Wave);
        canvas.Translate(0, 7); // second layer sits 7 lower in the mockup
        Fill(paint, canvas, 0x5793b8);
        DrawSvgPath(paint, canvas, Wave);
    }

    // Cloud + bolt (mockup "Distant Thunderstorm" SVG, static frame).
    private static void DrawThunderstorm(SKPaint paint, SKCanvas canvas)
    {
        Fill(paint, canvas, 0xb6bedd);
        Cloud(paint, canvas, 15, 17);

        Fill(paint, canvas, 0xffd45e);
        Polygon(paint, canvas,
            [26, 21, 17.5f, 33, 22.5f, 33, 19.5f, 43, 30.5f, 29, 25, 29, 29.5f, 21]);
    }

    // Logs + outer/inner flame + sparks (mockup "Crackling Campfire" SVG,
    // static frame).
    private static void DrawCampfire(SKPaint paint, SKCanvas canvas)
    {
        Stroke(paint, canvas, 0x7a5236, 4);
        canvas.DrawLine(13, 41, 33, 38.5f, paint);
        canvas.DrawLine(15, 38.5f, 35, 41, paint);

        Fill(paint, canvas, 0xf5772f);
        DrawSvgPath(paint, canvas,
            "M24 7 C20.2 13.5 13 18.5 13 27 a11 11 0 0 0 22 0 C35 18.5 27.8 13.5 24 7 Z");
        Fill(paint, canvas, 0xfbbf4d);
        DrawSvgPath(paint, canvas,
            "M24 20 C21.7 23.8 18.8 26.3 18.8 30.3 a5.2 5.2 0 0 0 10.4 0 C29.2 26.3 26.3 23.8 24 20 Z");

        Fill(paint, canvas, 0xffb45e);
        canvas.DrawCircle(16.5f, 15, 1.3f, paint);
        Fill(paint, canvas, 0xffd08a);
        canvas.DrawCircle(31.5f, 12, 1.1f, paint);
    }

    // Crescent moon, stars, fir silhouettes, fireflies (mockup "Night Forest"
    // SVG, static frame).
    private static void DrawNightForest(SKPaint paint, SKCanvas canvas)
    {
        Fill(paint, canvas, 0xf0ead0);
        DrawSvgPath(paint, canvas, "M35 6a6.5 6.5 0 1 0 5.5 9.8A7 7 0 0 1 35 6Z");

        Fill(paint, canvas, 0xdfe6ff);
        canvas.DrawCircle(9, 9, 1.1f, paint);
        canvas.DrawCircle(19, 5.5f, 0.9f, paint);
        canvas.DrawCircle(27, 10, 1, paint);

        Fill(paint, canvas, 0x0c2316);
        DrawSvgPath(paint, canvas, "M13 17 L5.5 30 H20.5 Z M13 24 L4 38 H22 Z");
        canvas.DrawRect(12, 37, 2, 5, paint);
        DrawSvgPath(paint, canvas, "M33 14 L24.5 29 H41.5 Z M33 22 L23 38 H43 Z");
        canvas.DrawRect(32, 37, 2, 5, paint);

        Fill(paint, canvas, 0xffe97a);
        canvas.DrawCircle(23, 22, 1.2f, paint);
        canvas.DrawCircle(10, 26, 1, paint);
        canvas.DrawCircle(41, 24, 1.1f, paint);
    }

    // TV-static grain (mockup "Brown & White Noise"): 44 dots from the mockup's
    // seeded generator — same seed, same LCG — so the art matches it exactly.
    // The per-dot twinkle is ticket #8.
    private static void DrawNoiseGrain(SKPaint paint, SKCanvas canvas)
    {
        Fill(paint, canvas, 0xdfe4f5);
        var seed = 7UL; // mockup: seed = (seed * 16807) % 2147483647
        var next = () => (seed = seed * 16807UL % 2147483647UL) / 2147483647f;
        for (var i = 0; i < 44; i++)
        {
            var x = 4 + next() * 40;
            var y = 5 + next() * 38;
            var r = 0.7f + next() * 0.8f;
            canvas.DrawCircle(x, y, r, paint);
        }
    }

    // Cage ring, three blades, hub, stand (mockup "Fan & Room Hum" SVG, static
    // frame; the spin is ticket #8).
    private static void DrawFan(SKPaint paint, SKCanvas canvas)
    {
        var ring = Rgb(0x9fb6d8).WithAlpha(0x8c); // mockup opacity .55
        Stroke(paint, canvas, ring, 2);
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawCircle(24, 21, 15.5f, paint);

        var blade = SKPath.ParseSvgPathData(
            "M24 21 C20.6 14.8 20.2 8 24 5.6 C27.8 8 27.4 14.8 24 21 Z");
        Fill(paint, canvas, 0xbcd3ee);
        foreach (var rotation in new[] { 0f, 120f, 240f })
        {
            canvas.Save();
            canvas.RotateDegrees(rotation, 24, 21);
            canvas.DrawPath(blade, paint);
            canvas.Restore();
        }

        Fill(paint, canvas, 0xe7f0fb);
        canvas.DrawCircle(24, 21, 2.7f, paint);

        Stroke(paint, canvas, 0x9fb6d8, 2.4f);
        DrawSvgPath(paint, canvas, "M24 37 v4 M18.5 42.5 h11");
    }

    // Shared cloud shape (three circles + pill), at the mockup's two heights.
    private static void Cloud(SKPaint paint, SKCanvas canvas, float top, float rectY)
    {
        canvas.DrawCircle(21, top, 8, paint);
        canvas.DrawCircle(30, top + 2, 6.5f, paint);
        canvas.DrawCircle(13.5f, top + 4.5f, 5.5f, paint);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(12, rectY, 36, rectY + 8), 4, 4), paint);
    }

    private static void Fill(SKPaint paint, SKCanvas canvas, uint rgb) =>
        Prepare(paint, Rgb(rgb), SKPaintStyle.Fill, 0);

    private static void Stroke(SKPaint paint, SKCanvas canvas, uint rgb, float width) =>
        Prepare(paint, Rgb(rgb), SKPaintStyle.Stroke, width);

    private static void Stroke(SKPaint paint, SKCanvas canvas, SKColor color, float width) =>
        Prepare(paint, color, SKPaintStyle.Stroke, width);

    private static void Prepare(SKPaint paint, SKColor color, SKPaintStyle style, float width)
    {
        paint.Color = color;
        paint.Style = style;
        paint.StrokeWidth = width;
        paint.StrokeCap = SKStrokeCap.Round;
    }

    private static void DrawSvgPath(SKPaint paint, SKCanvas canvas, string svgPath) =>
        canvas.DrawPath(SKPath.ParseSvgPathData(svgPath), paint);

    private static void Polygon(SKPaint paint, SKCanvas canvas, float[] points)
    {
        var path = new SKPath();
        path.MoveTo(points[0], points[1]);
        for (var i = 2; i < points.Length; i += 2)
        {
            path.LineTo(points[i], points[i + 1]);
        }
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private static SKColor Rgb(uint rgb) => new(0xFF00_0000 | rgb);
}
