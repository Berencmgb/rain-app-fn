using RainApp.Animation;

namespace RainApp.Animation.Tests;

/// <summary>
/// SceneKeyframes is mockup.html ported 1:1 (ADR-0002). Every expected value
/// below is worked from the mockup's own keyframe percentages and durations —
/// the mockup is the timeline source of truth.
/// </summary>
public class SceneKeyframesTests
{
    // ── Rain: @keyframes drop-fall, 1.15s linear; .d2 .38s, .d3 .76s ───────

    [Fact]
    public void Drops_fallOnTheMockupTimeline_withStaggeredDelays()
    {
        Assert.Equal([0, 0.38, 0.76], SceneKeyframes.DropDelays);

        // p = 25%: opacity 1; translateY = -5 + 14·0.25 = -1.5
        var frame = SceneKeyframes.DropFall.Sample(0.25 * 1.15);
        Assert.Equal(1.0, frame.Opacity, 10);
        Assert.Equal(-1.5, frame.TranslateY, 10);

        // The second drop starts 0.38s in: still base before, mid-fall after.
        Assert.Equal(1.0, SceneKeyframes.DropFall.Sample(0.30 - 0.38).Opacity, 10);
        Assert.Equal(1.0, SceneKeyframes.DropFall.Sample((0.38 + 0.25 * 1.15) - 0.38).Opacity, 10);
    }

    // ── Ocean: wave-r 3.4s → +24px, wave-l 2.6s → −24px, linear ────────────

    [Fact]
    public void Waves_scrollSeamlesslyInOppositeDirections()
    {
        // One full period walks exactly one bump pair (24 viewBox px), so the
        // layer is pixel-identical when the cycle wraps: seamless scroll.
        Assert.Equal(12.0, SceneKeyframes.WaveRight.Sample(1.7).TranslateX, 10);
        Assert.Equal(3.0, SceneKeyframes.WaveRight.Sample(6.8 + 3.4 * 0.125).TranslateX, 10); // third cycle
        Assert.Equal(0.0, SceneKeyframes.WaveRight.Sample(3.4).TranslateX, 10); // wrap

        Assert.Equal(-6.0, SceneKeyframes.WaveLeft.Sample(0.65).TranslateX, 10);
    }

    // ── Storm: @keyframes flash, 3.4s linear — the exact opacity timeline ───

    [Theory]
    [InlineData(0.00, 0.0)] // 0%
    [InlineData(0.80, 0.0)] // 80%
    [InlineData(0.84, 0.0)] // 84%
    [InlineData(0.85, 0.5)] // half-way between 84% (0) and 86% (1)
    [InlineData(0.86, 1.0)] // 86%
    [InlineData(0.87, 0.625)] // half-way between 86% (1) and 88% (0.25)
    [InlineData(0.88, 0.25)] // 88%
    [InlineData(0.91, 1.0)] // 91%
    [InlineData(0.94, 0.9)] // 94%
    [InlineData(0.955, 0.45)] // half-way between 94% (0.9) and 97% (0)
    [InlineData(0.97, 0.0)] // 97%
    [InlineData(1.00, 0.0)] // 100%
    public void Bolt_flickersOnTheExactFlashTimeline(double phase, double expected)
    {
        Assert.Equal(expected, SceneKeyframes.BoltFlash.Sample(phase * 3.4).Opacity, 10);
    }

    // ── Fire: flicker 0.8s ease-in-out; inner 0.55s alternate; rise 2.4s ───

    [Fact]
    public void Flame_outerBreathesOnTheFlickerKeyframes()
    {
        var full = SceneKeyframes.FlameFlicker;

        var quarter = full.Sample(0.2); // 25%
        Assert.Equal(1.05, quarter.ScaleX, 10);
        Assert.Equal(0.94, quarter.ScaleY, 10);
        Assert.Equal(2.5, quarter.SkewXDegrees, 10);

        var half = full.Sample(0.4); // 50%
        Assert.Equal(0.95, half.ScaleX, 10);
        Assert.Equal(1.07, half.ScaleY, 10);
        Assert.Equal(-2.5, half.SkewXDegrees, 10);

        var threeQuarter = full.Sample(0.6); // 75%
        Assert.Equal(1.03, threeQuarter.ScaleX, 10);
        Assert.Equal(0.97, threeQuarter.ScaleY, 10);
        Assert.Equal(1.5, threeQuarter.SkewXDegrees, 10);

        // Half-way between the 25% and 50% keyframes: ease-in-out at its
        // symmetric midpoint is exactly half over.
        var between = full.Sample(0.3);
        Assert.Equal(1.0, between.ScaleX, 10);
        Assert.Equal(1.005, between.ScaleY, 10);
        Assert.Equal(0.0, between.SkewXDegrees, 10);
    }

    [Fact]
    public void Flame_innerRunsFasterAndAlternates()
    {
        // .flame-inner { animation-duration: 0.55s; animation-direction: alternate }
        var inner = SceneKeyframes.FlameFlickerInner;

        var halfCycle = inner.Sample(0.275); // 50% forward
        Assert.Equal(0.95, halfCycle.ScaleX, 10);
        Assert.Equal(1.07, halfCycle.ScaleY, 10);

        // Second cycle at 75% backwards = what the forward pass showed at 25%.
        var reversed = inner.Sample(0.55 + 0.75 * 0.55);
        Assert.Equal(1.05, reversed.ScaleX, 10);
        Assert.Equal(0.94, reversed.ScaleY, 10);
        Assert.Equal(2.5, reversed.SkewXDegrees, 10);
    }

    [Fact]
    public void Sparks_riseAndFadeOnTheMockupTimeline()
    {
        Assert.Equal([0, 1.2], SceneKeyframes.SparkDelays);

        // 0% invisible at origin, fully visible at 20%, gone by 100%.
        Assert.Equal(0.0, SceneKeyframes.SparkRise.Sample(0).Opacity, 10);
        Assert.Equal(0.0, SceneKeyframes.SparkRise.Sample(0).TranslateY, 10);
        Assert.Equal(1.0, SceneKeyframes.SparkRise.Sample(0.2 * 2.4).Opacity, 10);
        Assert.Equal(0.0, SceneKeyframes.SparkRise.Sample(2.4).Opacity, 10);
    }

    // ── Night forest: twinkle 1.8s, firefly 2.6s, ease-in-out ──────────────

    [Fact]
    public void Stars_twinkleBetweenFifteenPercentAndFull()
    {
        Assert.Equal([0, 0.6, 1.2], SceneKeyframes.StarDelays);

        var twinkle = SceneKeyframes.StarTwinkle;
        Assert.Equal(0.15, twinkle.Sample(0).Opacity, 10);
        Assert.Equal(1.0, twinkle.Sample(0.9).Opacity, 10); // peak at 50%
        Assert.Equal(0.575, twinkle.Sample(1.35).Opacity, 10); // half-way down 50→100%
    }

    [Fact]
    public void Fireflies_driftUpAndRightThenBack()
    {
        Assert.Equal([0, 0.9, 1.7], SceneKeyframes.FireflyDelays);

        var drift = SceneKeyframes.FireflyDrift;
        Assert.Equal(0.0, drift.Sample(0).Opacity, 10); // hidden at origin

        // 50%: fully visible, displaced (2.5, -3) — exactly the keyframe.
        var peak = drift.Sample(1.3);
        Assert.Equal(1.0, peak.Opacity, 10);
        Assert.Equal(2.5, peak.TranslateX, 10);
        Assert.Equal(-3.0, peak.TranslateY, 10);
    }

    // ── Noise: grain 0.1 ↔ 0.85; per-dot duration 0.9–1.8s (JS override) ───

    [Fact]
    public void Grain_shimmersBetweenTenAndEightyFivePercent_AtThePerDotDuration()
    {
        // The mockup's .ngrain curve is fixed; each dot's duration comes from
        // its seeded random — the curve must take it verbatim.
        var dot = SceneKeyframes.GrainShimmer(durationSeconds: 1.35);
        Assert.Equal(0.1, dot.Sample(0).Opacity, 10);
        Assert.Equal(0.85, dot.Sample(0.675).Opacity, 10); // peak at 50%
        Assert.Equal(0.1, dot.Sample(1.35).Opacity, 10);
    }

    // ── Fan: spin 360° in 2.8s linear about the hub ────────────────────────

    [Fact]
    public void Fan_spinsContinuousClockwise()
    {
        var spin = SceneKeyframes.FanSpin;
        Assert.Equal(0.0, spin.Sample(0).RotateDegrees, 10);
        Assert.Equal(90.0, spin.Sample(0.7).RotateDegrees, 10); // quarter turn in a quarter period
        Assert.Equal(270.0, spin.Sample(2.1).RotateDegrees, 10);
        Assert.Equal(0.0, spin.Sample(2.8).RotateDegrees, 10); // full turn wraps
        Assert.Equal(90.0, spin.Sample(3.5).RotateDegrees, 10); // straight into the next cycle
    }

    // ── Equalizer: eq 1s ease-in-out, height 5 ↔ 17; delays 0/.25/.5/.15 ───

    [Fact]
    public void EqualizerBars_bounceBetweenFiveAndSeventeenPx()
    {
        Assert.Equal([0, 0.25, 0.5, 0.15], SceneKeyframes.EqualizerBarDelays);

        var bar = SceneKeyframes.EqualizerBar;
        Assert.Equal(5.0, bar.Sample(0).Height, 10);
        Assert.Equal(11.0, bar.Sample(0.25).Height, 10); // half-way up = 5 + 12·0.5
        Assert.Equal(17.0, bar.Sample(0.5).Height, 10);
        Assert.Equal(11.0, bar.Sample(0.75).Height, 10); // half-way back down

        // The second bar is armed at 0.25s — at t = 0.5 it stands mid-rise.
        var second = SceneKeyframes.EqualizerBar.Sample(0.5 - 0.25);
        Assert.Equal(11.0, second.Height, 10);
    }
}
