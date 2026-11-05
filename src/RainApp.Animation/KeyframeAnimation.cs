namespace RainApp.Animation;

/// <summary>
/// The subset of the CSS Easing and Easing Functions spec that the mockup's
/// keyframes use. <see cref="Linear"/> is `linear`, <see cref="EaseIn"/> is
/// the CSS keyword `ease-in` (cubic-bezier(0.42, 0, 1, 1)), <see cref="EaseInOut"/>
/// is `ease-in-out` (cubic-bezier(0.42, 0, 0.58, 1)).
/// </summary>
public enum TimingCurve
{
    Linear,
    EaseIn,
    EaseInOut,
}

/// <summary>
/// One CSS animation restricted to what mockup.html declares: a fixed duration
/// (always `infinite` iteration), optionally `alternate` (every other cycle
/// reversed), per-property keyframe tracks, and one timing function shared by
/// every interval — exactly what `animation: <name> <d> <timing> infinite`
/// resolves to. Sample a wall-clock elapsed time to get the values CSS would
/// be showing at that instant.
///
/// Like the browser, a paused animation freezes: pass a frozen time in and the
/// same frame comes back out. Delays belong to the caller (pass
/// `elapsed - elementDelay`), matching how each mockup element carries its own
/// `animation-delay`.
/// </summary>
public sealed class KeyframeAnimation
{
    private readonly AnimTrack[] _tracks;

    public KeyframeAnimation(
        double durationSeconds, TimingCurve timing, IEnumerable<AnimTrack> tracks, bool alternate = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(durationSeconds, 0.0);
        DurationSeconds = durationSeconds;
        Timing = timing;
        Alternate = alternate;
        _tracks = tracks.ToArray();
        foreach (var track in _tracks)
        {
            if (track.Stops.Count < 2 || track.Stops[0].Offset != 0 || track.Stops[^1].Offset != 1)
            {
                throw new ArgumentException(
                    $"Keyframe track {track.Property} needs explicit 0% and 100% stops.", nameof(tracks));
            }
            if (track.Stops.Select(s => s.Offset). SequenceEqual(track.Stops.Select(s => s.Offset).Order()))
            {
                continue;
            }
            throw new ArgumentException(
                $"Keyframe track {track.Property} stops must be in ascending order.", nameof(tracks));
        }
    }

    public double DurationSeconds { get; }
    public TimingCurve Timing { get; }
    public bool Alternate { get; }

    /// <summary>
    /// The values at `elapsedSeconds` into this animation's infinite repeat —
    /// the values the element's base style shows for properties no track animates.
    /// A negative elapsed time (still inside an `animation-delay`) shows the base
    /// frame, as a delayed CSS animation does.
    /// </summary>
    public AnimState Sample(double elapsedSeconds)
    {
        if (elapsedSeconds < 0) return AnimState.Base;

        // `infinite`: any cycle count maps back onto the [0,1) keyframe range.
        var cycles = elapsedSeconds / DurationSeconds;
        var phase = cycles - Math.Floor(cycles);

        // `alternate`: every other cycle plays the timeline backwards; the
        // forward curve is sampled mirrored, which also mirrors its easing.
        if (Alternate && (long)Math.Floor(cycles) % 2 != 0) phase = 1 - phase;

        var state = AnimState.Base;
        foreach (var track in _tracks)
        {
            state = track.Property switch
            {
                AnimProperty.Opacity => state with { Opacity = Evaluate(track, phase) },
                AnimProperty.TranslateX => state with { TranslateX = Evaluate(track, phase) },
                AnimProperty.TranslateY => state with { TranslateY = Evaluate(track, phase) },
                AnimProperty.ScaleX => state with { ScaleX = Evaluate(track, phase) },
                AnimProperty.ScaleY => state with { ScaleY = Evaluate(track, phase) },
                AnimProperty.SkewXDegrees => state with { SkewXDegrees = Evaluate(track, phase) },
                AnimProperty.RotateDegrees => state with { RotateDegrees = Evaluate(track, phase) },
                AnimProperty.Height => state with { Height = Evaluate(track, phase) },
                _ => throw new ArgumentOutOfRangeException(nameof(track), track.Property, "Unknown animated property."),
            };
        }
        return state;
    }

    private double Evaluate(AnimTrack track, double phase)
    {
        var stops = track.Stops;
        var interval = 0;
        while (interval < stops.Count - 2 && phase > stops[interval + 1].Offset) interval++;

        var (from, to) = (stops[interval], stops[interval + 1]);
        var progress = (phase - from.Offset) / (to.Offset - from.Offset);

        // A CSS animation's timing function applies per keyframe interval.
        var eased = Timing.Ease(progress);
        return from.Value + (to.Value - from.Value) * eased;
    }
}

/// <summary>The animated property of a keyframe track.</summary>
public enum AnimProperty
{
    Opacity,
    TranslateX,
    TranslateY,
    ScaleX,
    ScaleY,
    SkewXDegrees,
    RotateDegrees,
    Height,
}

/// <summary>One property's keyframes: (0%-offset, value) stops, ascending.</summary>
public sealed record AnimTrack(AnimProperty Property, IReadOnlyList<AnimStop> Stops);

public sealed record AnimStop(double Offset, double Value);

/// <summary>Factories for keyframe tracks, e.g. `Tracks.Opacity((0, 0), (1, 1))`.</summary>
public static class Tracks
{
    public static AnimTrack Opacity(params (double Offset, double Value)[] stops) => Make(AnimProperty.Opacity, stops);
    public static AnimTrack TranslateX(params (double Offset, double Value)[] stops) => Make(AnimProperty.TranslateX, stops);
    public static AnimTrack TranslateY(params (double Offset, double Value)[] stops) => Make(AnimProperty.TranslateY, stops);
    public static AnimTrack ScaleX(params (double Offset, double Value)[] stops) => Make(AnimProperty.ScaleX, stops);
    public static AnimTrack ScaleY(params (double Offset, double Value)[] stops) => Make(AnimProperty.ScaleY, stops);
    public static AnimTrack SkewXDegrees(params (double Offset, double Value)[] stops) => Make(AnimProperty.SkewXDegrees, stops);
    public static AnimTrack RotateDegrees(params (double Offset, double Value)[] stops) => Make(AnimProperty.RotateDegrees, stops);
    public static AnimTrack Height(params (double Offset, double Value)[] stops) => Make(AnimProperty.Height, stops);

    private static AnimTrack Make(AnimProperty property, (double Offset, double Value)[] stops) =>
        new(property, [.. stops.Select(s => new AnimStop(s.Offset, s.Value))]);
}

/// <summary>
/// What an animated element would show right now. Property names are the CSS
/// ones; values not covered by a track of the sampled animation stay at the
/// element's base value (visible, untranslated, untransformed, full height).
/// </summary>
public readonly record struct AnimState
{
    /// <summary>The base frame: everything as if the animation had never run.</summary>
    public static AnimState Base => new() { Opacity = 1, ScaleX = 1, ScaleY = 1 };

    public double Opacity { get; init; }
    public double TranslateX { get; init; }
    public double TranslateY { get; init; }
    public double ScaleX { get; init; }
    public double ScaleY { get; init; }
    public double SkewXDegrees { get; init; }
    public double RotateDegrees { get; init; }
    public double Height { get; init; }
}
