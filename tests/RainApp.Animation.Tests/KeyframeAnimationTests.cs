using RainApp.Animation;

namespace RainApp.Animation.Tests;

/// <summary>
/// The animation-state seam (ticket #8): given elapsed time + a keyframe
/// definition, the helper answers with the property values CSS would show.
/// Expected values are worked from mockup.html's keyframe percentages by hand —
/// the mockup is the timeline source of truth (ADR-0002).
/// </summary>
public class KeyframeAnimationTests
{
    // @keyframes drop-fall { 0% {opacity 0; translateY(-5px)} 25% {opacity 1}
    //                       85% {opacity 0} 100% {opacity 0; translateY(9px)} }
    // .drop { animation: drop-fall 1.15s linear infinite }
    private static readonly KeyframeAnimation DropFall = new(
        durationSeconds: 1.15,
        timing: TimingCurve.Linear,
        tracks:
        [
            Tracks.Opacity((0, 0), (0.25, 1), (0.85, 0), (1, 0)),
            Tracks.TranslateY((0, -5), (1, 9)),
        ]);

    [Fact]
    public void Sample_atKeyframeStop_returnsThatFramesValues()
    {
        // 25% of 1.15s: drop fully visible, transform interpolates only between
        // the two keyframes that declare it (0% → -5px, 100% → +9px):
        // -5 + (9 - -5) * 0.25 = -1.5
        var atStart = DropFall.Sample(0.0); // 0%
        Assert.Equal(0.0, atStart.Opacity, 10);
        Assert.Equal(-5.0, atStart.TranslateY, 10);

        var atQuarter = DropFall.Sample(0.25 * 1.15);
        Assert.Equal(1.0, atQuarter.Opacity, 10);
        Assert.Equal(-1.5, atQuarter.TranslateY, 10);
    }

    [Fact]
    public void Sample_midwayBetweenStops_interpolatesLinearly()
    {
        // half-way between 0% and 25% (p = 0.125): opacity 0.5,
        // translateY = -5 + 14 * 0.125 = -3.25
        var state = DropFall.Sample(0.125 * 1.15);
        Assert.Equal(0.5, state.Opacity, 10);
        Assert.Equal(-3.25, state.TranslateY, 10);
    }

    [Fact]
    public void Sample_afterLastOpacityStop_holdsOpacityButKeepsTransformTrack()
    {
        // p = 0.9: opacity track ends its fade-out at 85% → 0; the transform
        // track declared only at 0% and 100% keeps running: -5 + 14*0.9 = 7.6
        var state = DropFall.Sample(0.9 * 1.15);
        Assert.Equal(0.0, state.Opacity, 10);
        Assert.Equal(7.6, state.TranslateY, 10);
    }

    [Fact]
    public void Sample_repeatsForever_fromAnyCycle()
    {
        // `infinite`: the timeline repeats with no special first/last cycle —
        // 1.4 cycles in shows the same frame as 0.4 cycles in.
        var firstCycle = DropFall.Sample(0.4 * 1.15);
        Assert.Equal(firstCycle.Opacity, DropFall.Sample(1.4 * 1.15).Opacity, 10);
        Assert.Equal(firstCycle.TranslateY, DropFall.Sample(1.4 * 1.15).TranslateY, 10);
        Assert.Equal(firstCycle.TranslateY, DropFall.Sample(57.4 * 1.15).TranslateY, 10);
    }

    [Fact]
    public void Sample_insideAnAnimationDelay_showsTheBaseFrame()
    {
        // `.drop.d2 { animation-delay: 0.38s }`: until then CSS shows the
        // element's base style — visible (opacity 1) and untranslated.
        var delayed = DropFall.Sample(0.2 - 0.38);
        Assert.Equal(1.0, delayed.Opacity, 10);
        Assert.Equal(0.0, delayed.TranslateY, 10);

        // and the moment the delay is over, the animation's 0% frame takes over
        Assert.Equal(0.0, DropFall.Sample(0.0).Opacity, 10);
    }

    [Fact]
    public void Sample_leavesPropertiesNoTrackAnimatesAtTheirBaseValue()
    {
        var state = DropFall.Sample(0.5 * 1.15);
        Assert.Equal(1.0, state.ScaleX, 10);
        Assert.Equal(1.0, state.ScaleY, 10);
        Assert.Equal(0.0, state.TranslateX, 10);
        Assert.Equal(0.0, state.RotateDegrees, 10);
    }
}
