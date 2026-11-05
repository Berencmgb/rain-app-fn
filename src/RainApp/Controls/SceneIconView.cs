using RainApp.Animation;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace RainApp.Controls;

/// <summary>
/// A Scene's illustrated icon, drawn with SkiaSharp (ADR-0002): geometry and
/// animation are ports of mockup.html, sampled per frame from the shared
/// <see cref="IconAnimation"/> clock — so every icon breathes while audio is
/// live and freezes on its current frame while nothing plays, exactly as the
/// mockup's `animation-play-state` does.
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
        // Redraws happen only while IconAnimation's pulse runs; once frozen, no
        // further invalidations are sent and the last frame simply stays.
        IconAnimation.FrameInvalidated += InvalidateSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        SceneIconRenderer.Draw(Icon, e.Surface.Canvas, e.Info.Width, e.Info.Height, IconAnimation.Seconds);
}

/// <summary>
/// Pure Skia drawing of the Scene icons. Geometry, colors, and timelines are
/// ported from mockup.html (the design source of truth): coordinates are the
/// mockup's 48×48 viewBox scaled to the surface, and every animated element is
/// its mockup counterpiece — <see cref="SceneKeyframes"/> sampled at the given
/// point on the shared clock, with the element's `animation-delay` subtracted.
/// Everything else is the static frame.
/// </summary>
public static class SceneIconRenderer
{
    public static void Draw(SceneIcon icon, SKCanvas canvas, int width, int height, double seconds)
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
            case SceneIcon.RainOnWindow: DrawRain(paint, canvas, seconds); break;
            case SceneIcon.OceanWaves: DrawOcean(paint, canvas, seconds); break;
            case SceneIcon.DistantThunderstorm: DrawThunderstorm(paint, canvas, seconds); break;
            case SceneIcon.CracklingCampfire: DrawCampfire(paint, canvas, seconds); break;
            case SceneIcon.NightForest: DrawNightForest(paint, canvas, seconds); break;
            case SceneIcon.BrownWhiteNoise: DrawNoiseGrain(paint, canvas, seconds); break;
            case SceneIcon.FanRoomHum: DrawFan(paint, canvas, seconds); break;
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

    // Cloud + falling drops (mockup "Rain on Window"): three .drop lines on
    // drop-fall, staggered by DropDelays.
    private static void DrawRain(SKPaint paint, SKCanvas canvas, double seconds)
    {
        Fill(paint, canvas, 0xc3cdec);
        Cloud(paint, canvas, 18, 20);

        foreach (var (x, delay) in new[]
                 {
                     (17f, SceneKeyframes.DropDelays[0]),
                     (24f, SceneKeyframes.DropDelays[1]),
                     (31f, SceneKeyframes.DropDelays[2]),
                 })
        {
            var drop = SceneKeyframes.DropFall.Sample(seconds - delay);
            Stroke(paint, canvas, WithOpacity(Rgb(0x79aaff), drop.Opacity), 2.6f);
            canvas.Save();
            canvas.Translate((float)drop.TranslateX, (float)drop.TranslateY);
            canvas.DrawLine(x, 32, x - 1.2f, 35.5f, paint);
            canvas.Restore();
        }
    }

    // Moon over two scrolling wave layers (mockup "Ocean Waves"): .wave.w1 and
    // .wave.w2 scroll ±24px per cycle — exactly one bump pair, so the seam
    // never shows.
    private static void DrawOcean(SKPaint paint, SKCanvas canvas, double seconds)
    {
        Fill(paint, canvas, 0xf2e9c8);
        canvas.DrawCircle(35, 11, 4.5f, paint);

        const string Wave = "M-72 0 q6 -4.5 12 0 t12 0 t12 0 t12 0 t12 0 t12 0 "
                          + "t12 0 t12 0 t12 0 t12 0 t12 0 t12 0 V48 H-72 Z";
        var back = SceneKeyframes.WaveRight.Sample(seconds);
        Fill(paint, canvas, 0x35617f);
        canvas.Save();
        canvas.Translate((float)back.TranslateX, 26);
        DrawSvgPath(paint, canvas, Wave);
        canvas.Restore();

        var front = SceneKeyframes.WaveLeft.Sample(seconds);
        Fill(paint, canvas, 0x5793b8);
        canvas.Save();
        canvas.Translate((float)front.TranslateX, 33); // second layer sits 7 lower in the mockup
        DrawSvgPath(paint, canvas, Wave);
        canvas.Restore();
    }

    // Cloud + bolt (mockup "Distant Thunderstorm"): the bolt is base-invisible
    // and flashes on `flash` every 3.4s.
    private static void DrawThunderstorm(SKPaint paint, SKCanvas canvas, double seconds)
    {
        Fill(paint, canvas, 0xb6bedd);
        Cloud(paint, canvas, 15, 17);

        var flash = SceneKeyframes.BoltFlash.Sample(seconds);
        if (flash.Opacity > 0)
        {
            Fill(paint, canvas, WithOpacity(Rgb(0xffd45e), flash.Opacity));
            Polygon(paint, canvas,
                [26, 21, 17.5f, 33, 22.5f, 33, 19.5f, 43, 30.5f, 29, 25, 29, 29.5f, 21]);
        }
    }

    // Logs + flickering flame + rising sparks (mockup "Crackling Campfire").
    // The flicker scales/skews each flame about its own fill-box bottom-centre
    // (`transform-box: fill-box; transform-origin: 50% 100%`): outer flame
    // spans y 7..38, inner flame y 20..35.5.
    private static void DrawCampfire(SKPaint paint, SKCanvas canvas, double seconds)
    {
        Stroke(paint, canvas, 0x7a5236, 4);
        canvas.DrawLine(13, 41, 33, 38.5f, paint);
        canvas.DrawLine(15, 38.5f, 35, 41, paint);

        var outer = SceneKeyframes.FlameFlicker.Sample(seconds);
        // .flame-outer { filter: drop-shadow(0 0 5px rgba(245,119,47,0.65)) }
        paint.ImageFilter = SKImageFilter.CreateDropShadow(
            0, 0, 3.15f, 3.15f, Rgb(0xf5772f).WithAlpha((byte)Math.Round(0.65 * 255)));
        Fill(paint, canvas, 0xf5772f);
        canvas.Save();
        SquashAbout(canvas, outer, 24, 38);
        DrawSvgPath(paint, canvas,
            "M24 7 C20.2 13.5 13 18.5 13 27 a11 11 0 0 0 22 0 C35 18.5 27.8 13.5 24 7 Z");
        canvas.Restore();
        paint.ImageFilter = null;

        var inner = SceneKeyframes.FlameFlickerInner.Sample(seconds);
        Fill(paint, canvas, 0xfbbf4d);
        canvas.Save();
        SquashAbout(canvas, inner, 24, 35.5f);
        DrawSvgPath(paint, canvas,
            "M24 20 C21.7 23.8 18.8 26.3 18.8 30.3 a5.2 5.2 0 0 0 10.4 0 C29.2 26.3 26.3 23.8 24 20 Z");
        canvas.Restore();

        // .spark circles rise on `rise`, the second staggered by half a cycle.
        foreach (var (x, y, r, color, delay) in new[]
                 {
                     (16.5f, 15f, 1.3f, 0xffb45e, SceneKeyframes.SparkDelays[0]),
                     (31.5f, 12f, 1.1f, 0xffd08a, SceneKeyframes.SparkDelays[1]),
                 })
        {
            var spark = SceneKeyframes.SparkRise.Sample(seconds - delay);
            if (spark.Opacity <= 0) continue;
            Fill(paint, canvas, WithOpacity(Rgb((uint)color), spark.Opacity));
            canvas.DrawCircle(x + (float)spark.TranslateX, y + (float)spark.TranslateY, r, paint);
        }
    }

    // Crescent moon, twinkling stars, fir silhouettes, drifting fireflies
    // (mockup "Night Forest"): stars fade on `twinkle` and fireflies wander on
    // `firefly`, each staggered by its delay array.
    private static void DrawNightForest(SKPaint paint, SKCanvas canvas, double seconds)
    {
        Fill(paint, canvas, 0xf0ead0);
        DrawSvgPath(paint, canvas, "M35 6a6.5 6.5 0 1 0 5.5 9.8A7 7 0 0 1 35 6Z");

        Fill(paint, canvas, 0xdfe6ff);
        foreach (var (x, y, r, delay) in new[]
                 {
                     (9f, 9f, 1.1f, SceneKeyframes.StarDelays[0]),
                     (19f, 5.5f, 0.9f, SceneKeyframes.StarDelays[1]),
                     (27f, 10f, 1f, SceneKeyframes.StarDelays[2]),
                 })
        {
            var star = SceneKeyframes.StarTwinkle.Sample(seconds - delay);
            Fill(paint, canvas, WithOpacity(Rgb(0xdfe6ff), star.Opacity));
            canvas.DrawCircle(x, y, r, paint);
        }

        Fill(paint, canvas, 0x0c2316);
        DrawSvgPath(paint, canvas, "M13 17 L5.5 30 H20.5 Z M13 24 L4 38 H22 Z");
        canvas.DrawRect(12, 37, 2, 5, paint);
        DrawSvgPath(paint, canvas, "M33 14 L24.5 29 H41.5 Z M33 22 L23 38 H43 Z");
        canvas.DrawRect(32, 37, 2, 5, paint);

        Fill(paint, canvas, 0xffe97a);
        foreach (var (x, y, r, delay) in new[]
                 {
                     (23f, 22f, 1.2f, SceneKeyframes.FireflyDelays[0]),
                     (10f, 26f, 1f, SceneKeyframes.FireflyDelays[1]),
                     (41f, 24f, 1.1f, SceneKeyframes.FireflyDelays[2]),
                 })
        {
            var firefly = SceneKeyframes.FireflyDrift.Sample(seconds - delay);
            if (firefly.Opacity <= 0) continue;
            Fill(paint, canvas, WithOpacity(Rgb(0xffe97a), firefly.Opacity));
            canvas.DrawCircle(x + (float)firefly.TranslateX, y + (float)firefly.TranslateY, r, paint);
        }
    }

    // TV-static grain (mockup "Brown & White Noise"): 44 dots from the mockup's
    // seeded generator — same seed, same LCG, same toFixed rounding — each
    // twinkling on `grain` with the per-dot delay and duration the mockup's
    // script gives it.
    private static void DrawNoiseGrain(SKPaint paint, SKCanvas canvas, double seconds)
    {
        foreach (var dot in GrainDots)
        {
            var shimmer = dot.Shimmer.Sample(seconds - dot.Delay);
            Fill(paint, canvas, WithOpacity(Rgb(0xdfe4f5), shimmer.Opacity));
            canvas.DrawCircle(dot.X, dot.Y, dot.R, paint);
        }
    }

    private static readonly (float X, float Y, float R, double Delay, KeyframeAnimation Shimmer)[] GrainDots =
        BuildGrainDots();

    private static (float X, float Y, float R, double Delay, KeyframeAnimation Shimmer)[] BuildGrainDots()
    {
        var seed = 7UL; // mockup: seed = (seed * 16807) % 2147483647
        var next = () => (seed = seed * 16807UL % 2147483647UL) / 2147483647.0;
        var dots = new (float, float, float, double, KeyframeAnimation)[44];
        for (var i = 0; i < dots.Length; i++)
        {
            // Same draw order as the mockup's script, same rounding: cx/cy are
            // written to one decimal, r and delay and duration to two.
            var x = (float)Math.Round(4 + next() * 40, 1);
            var y = (float)Math.Round(5 + next() * 38, 1);
            var r = (float)Math.Round(0.7 + next() * 0.8, 2);
            var delay = Math.Round(next() * 1.4, 2);
            var duration = Math.Round(0.9 + next() * 0.9, 2);
            dots[i] = (x, y, r, delay, SceneKeyframes.GrainShimmer(duration));
        }
        return dots;
    }

    // Cage ring, spinning blades, hub, stand (mockup "Fan & Room Hum"): the
    // .fan group rotates about its hub (`transform-box: view-box;
    // transform-origin: 24px 21px`), everything else is static.
    private static void DrawFan(SKPaint paint, SKCanvas canvas, double seconds)
    {
        var ring = Rgb(0x9fb6d8).WithAlpha(0x8c); // mockup opacity .55
        Stroke(paint, canvas, ring, 2);
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawCircle(24, 21, 15.5f, paint);

        var spin = SceneKeyframes.FanSpin.Sample(seconds);
        var blade = SKPath.ParseSvgPathData(
            "M24 21 C20.6 14.8 20.2 8 24 5.6 C27.8 8 27.4 14.8 24 21 Z");
        Fill(paint, canvas, 0xbcd3ee);
        canvas.Save();
        canvas.RotateDegrees((float)spin.RotateDegrees, 24, 21);
        foreach (var rotation in new[] { 0f, 120f, 240f })
        {
            canvas.Save();
            canvas.RotateDegrees(rotation, 24, 21);
            canvas.DrawPath(blade, paint);
            canvas.Restore();
        }
        canvas.Restore();

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

    /// <summary>Applies an animated element's CSS transform list (translate,
    /// rotate, scale, skew — in the order CSS composes them) about a
    /// transform-origin point, as `transform-box`-resolved origins work.</summary>
    private static void SquashAbout(SKCanvas canvas, AnimState state, float originX, float originY)
    {
        if (state is { TranslateX: 0, TranslateY: 0, RotateDegrees: 0, SkewXDegrees: 0, ScaleX: 1, ScaleY: 1 })
        {
            return;
        }

        canvas.Translate(originX, originY);
        canvas.Translate((float)state.TranslateX, (float)state.TranslateY);
        if (state.RotateDegrees != 0) canvas.RotateDegrees((float)state.RotateDegrees);
        if (state.ScaleX != 1 || state.ScaleY != 1) canvas.Scale((float)state.ScaleX, (float)state.ScaleY);
        if (state.SkewXDegrees != 0)
        {
            // CSS skewX(a) is the shear x' = x + tan(a)·y.
            canvas.Skew((float)Math.Tan(state.SkewXDegrees * Math.PI / 180), 0);
        }
        canvas.Translate(-originX, -originY);
    }

    private static SKColor WithOpacity(SKColor color, double opacity) =>
        color.WithAlpha((byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255));

    private static void Fill(SKPaint paint, SKCanvas canvas, uint rgb) =>
        Prepare(paint, Rgb(rgb), SKPaintStyle.Fill, 0);

    private static void Fill(SKPaint paint, SKCanvas canvas, SKColor color) =>
        Prepare(paint, color, SKPaintStyle.Fill, 0);

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
