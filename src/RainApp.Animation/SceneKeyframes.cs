namespace RainApp.Animation;

/// <summary>
/// Every animated element of mockup.html, ported 1:1 (ADR-0002): the named
/// @keyframes blocks, their durations, timing keywords, iteration directions
/// and per-element `animation-delay` values. The mockup is the timeline
/// source of truth — change it here only together with mockup.html.
///
/// CSS `animation-delay` is the caller's to apply: sample with
/// `Sample(elapsed - delay)`; the base frame shows while the delay runs.
/// Elements whose implicit 0% keyframe is the element's base value carry that
/// base value as an explicit first stop (translation 0, rotation 0 — the same
/// thing the browser interpolates from).
/// </summary>
public static class SceneKeyframes
{
    /// <summary>
    /// @keyframes drop-fall { 0% {opacity 0; translateY(-5px)} 25% {opacity 1}
    /// 85% {opacity 0} 100% {opacity 0; translateY(9px)} } — linear, 1.15s.
    /// The three drops stagger by <see cref="DropDelays"/>.
    /// </summary>
    public static readonly KeyframeAnimation DropFall = new(1.15, TimingCurve.Linear,
    [
        Tracks.Opacity((0, 0), (0.25, 1), (0.85, 0), (1, 0)),
        Tracks.TranslateY((0, -5), (1, 9)),
    ]);

    public static readonly double[] DropDelays = [0, 0.38, 0.76];

    // @keyframes wave-r { to { translateX(24px) } } / wave-l { to { translateX(-24px) } }
    // — both linear and infinite; one period scrolls exactly one 24px bump pair.
    public static readonly KeyframeAnimation WaveRight = new(3.4, TimingCurve.Linear,
    [
        Tracks.TranslateX((0, 0), (1, 24)),
    ]);

    public static readonly KeyframeAnimation WaveLeft = new(2.6, TimingCurve.Linear,
    [
        Tracks.TranslateX((0, 0), (1, -24)),
    ]);

    /// <summary>@keyframes flash — the bolt is base-invisible (opacity 0) and
    /// lights up briefly per cycle; linear, 3.4s.</summary>
    public static readonly KeyframeAnimation BoltFlash = new(3.4, TimingCurve.Linear,
    [
        Tracks.Opacity((0, 0), (0.84, 0), (0.86, 1), (0.88, 0.25), (0.91, 1), (0.94, 0.9), (0.97, 0), (1, 0)),
    ]);

    /// <summary>
    /// @keyframes flicker — the flame squashes and leans about its base.
    /// Applied with a scale/skew transform (see the renderer's transform-origin
    /// handling); ease-in-out, 0.8s.
    /// </summary>
    public static readonly KeyframeAnimation FlameFlicker = Flicker(durationSeconds: 0.8, alternate: false);

    /// <summary>.flame-inner { animation-duration: 0.55s; animation-direction: alternate }.</summary>
    public static readonly KeyframeAnimation FlameFlickerInner = Flicker(durationSeconds: 0.55, alternate: true);

    private static KeyframeAnimation Flicker(double durationSeconds, bool alternate) => new(
        durationSeconds, TimingCurve.EaseInOut,
        [
            Tracks.ScaleX((0, 1), (0.25, 1.05), (0.5, 0.95), (0.75, 1.03), (1, 1)),
            Tracks.ScaleY((0, 1), (0.25, 0.94), (0.5, 1.07), (0.75, 0.97), (1, 1)),
            Tracks.SkewXDegrees((0, 0), (0.25, 2.5), (0.5, -2.5), (0.75, 1.5), (1, 0)),
        ],
        alternate: alternate);

    /// <summary>@keyframes rise { 0% {opacity 0} 20% {opacity 1} 100% {opacity 0; translateY(-13px)} }
    /// — ease-in, 2.4s; the second spark follows at <see cref="SparkDelays"/>[1].</summary>
    public static readonly KeyframeAnimation SparkRise = new(2.4, TimingCurve.EaseIn,
    [
        Tracks.Opacity((0, 0), (0.2, 1), (1, 0)),
        Tracks.TranslateY((0, 0), (1, -13)),
    ]);

    public static readonly double[] SparkDelays = [0, 1.2];

    /// <summary>@keyframes twinkle { 0%, 100% {opacity 0.15} 50% {opacity 1} } — 1.8s.</summary>
    public static readonly KeyframeAnimation StarTwinkle = new(1.8, TimingCurve.EaseInOut,
    [
        Tracks.Opacity((0, 0.15), (0.5, 1), (1, 0.15)),
    ]);

    public static readonly double[] StarDelays = [0, 0.6, 1.2];

    /// <summary>@keyframes firefly { 0%, 100% {opacity 0; translate(0,0)} 50% {opacity 1; translate(2.5px,-3px)} } — 2.6s.</summary>
    public static readonly KeyframeAnimation FireflyDrift = new(2.6, TimingCurve.EaseInOut,
    [
        Tracks.Opacity((0, 0), (0.5, 1), (1, 0)),
        Tracks.TranslateX((0, 0), (0.5, 2.5), (1, 0)),
        Tracks.TranslateY((0, 0), (0.5, -3), (1, 0)),
    ]);

    public static readonly double[] FireflyDelays = [0, 0.9, 1.7];

    /// <summary>@keyframes grain { 0%, 100% {opacity 0.1} 50% {opacity 0.85} }.
    /// The static rule says 1.2s, but the mockup's script overrides every
    /// dot's duration (0.9–1.8s) and delay from its seeded random — so the
    /// curve is built per dot, verbatim duration and all.</summary>
    public static KeyframeAnimation GrainShimmer(double durationSeconds) => new(durationSeconds, TimingCurve.EaseInOut,
    [
        Tracks.Opacity((0, 0.1), (0.5, 0.85), (1, 0.1)),
    ]);

    /// <summary>@keyframes spin { to { rotate(360deg) } } — linear, 2.8s, about
    /// the hub centre (a transform concern, not a keyframe one).</summary>
    public static readonly KeyframeAnimation FanSpin = new(2.8, TimingCurve.Linear,
    [
        Tracks.RotateDegrees((0, 0), (1, 360)),
    ]);

    /// <summary>@keyframes eq { 0%, 100% {height 5px} 50% {height 17px} } — 1s;
    /// the four bars stagger by <see cref="EqualizerBarDelays"/> in DOM order.</summary>
    public static readonly KeyframeAnimation EqualizerBar = new(1.0, TimingCurve.EaseInOut,
    [
        Tracks.Height((0, 5), (0.5, 17), (1, 5)),
    ]);

    public static readonly double[] EqualizerBarDelays = [0, 0.25, 0.5, 0.15];
}
