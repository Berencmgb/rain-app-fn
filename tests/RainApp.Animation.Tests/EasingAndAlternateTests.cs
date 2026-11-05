using RainApp.Animation;

namespace RainApp.Animation.Tests;

public class EasingAndAlternateTests
{
    /// <summary>A deliberately asymmetric triangle ramp: rising for the first
    /// quarter, falling for the rest — so "reversed" is never ambiguous.</summary>
    private static KeyframeAnimation Ramp(TimingCurve timing = TimingCurve.Linear, bool alternate = false) =>
        new(
            durationSeconds: 1,
            timing: timing,
            tracks: [Tracks.Opacity((0, 0), (0.25, 1), (1, 0))],
            alternate: alternate);

    [Fact]
    public void Alternate_reversesEveryOtherCycle()
    {
        var forward = Ramp();
        Assert.Equal(0.4, forward.Sample(0.1).Opacity, 10); // rising edge: 0.1/0.25

        // Second cycle runs backwards: CSS shows the value the forward pass had
        // at 1 - 0.1 = 0.9, i.e. on the falling edge: 1 - (0.9 - 0.25)/0.75.
        var reversed = Ramp(alternate: true);
        Assert.Equal(0.1333333333, reversed.Sample(1.1).Opacity, 9);

        // Third cycle is forward again.
        Assert.Equal(0.4, reversed.Sample(2.1).Opacity, 10);
    }

    [Fact]
    public void Alternate_transitionsSmoothlyAtCycleBoundaries()
    {
        // Reversed iterations meet the same value at both ends — that is what
        // makes `alternate` not snap. (1 - 1 = 0 ≡ 0, and 1 - 0 = 1 ≡ 1.)
        var reversed = Ramp(alternate: true);
        Assert.Equal(reversed.Sample(0.999).Opacity, reversed.Sample(1.001).Opacity, 3);
    }

    [Theory]
    [InlineData(TimingCurve.EaseInOut)]
    [InlineData(TimingCurve.EaseIn)]
    public void TimingCurves_matchTheCssKeywordShape(TimingCurve curve)
    {
        var ease = (Func<double, double>)(x => curve.Ease(x));
        Assert.Equal(0.0, ease(0.0), 10);
        Assert.Equal(1.0, ease(1.0), 10);
        Assert.True(ease(0.25) < 0.25, "both curves start slower than linear");
        Assert.True(ease(0.4) < ease(0.6), "the curve is monotonic");
    }

    [Fact]
    public void EaseIn_lagsLinearAcrossTheWholeCurve()
    {
        // cubic-bezier(0.42, 0, 1, 1) stays below the diagonal — `ease-in`
        // ends by accelerating, not by overshooting.
        foreach (var x in new[] { 0.25, 0.5, 0.75, 0.95 })
        {
            Assert.True(TimingCurve.EaseIn.Ease(x) < x, $"ease-in at {x} must be below linear");
        }
    }

    [Fact]
    public void EaseInOut_isTheCssSymmetricCurve()
    {
        // cubic-bezier(0.42, 0, 0.58, 1): symmetric — half-way is exactly half,
        // and quarter mirrors three-quarter.
        Assert.Equal(0.5, TimingCurve.EaseInOut.Ease(0.5), 10);
        Assert.Equal(1 - TimingCurve.EaseInOut.Ease(0.75), TimingCurve.EaseInOut.Ease(0.25), 10);
    }

    [Fact]
    public void TimingFunction_appliesPerKeyframeInterval_notAcrossTheWholeAnimation()
    {
        // CSS applies the timing function to each interval between keyframes.
        // Half-way into either interval of a 0→10→0 triangle must be 5
        // (ease(0.5) = 0.5) — not what one global curve across the whole
        // animation would show (~0.5 into the first, ~2.6 into the second).
        var triangle = new KeyframeAnimation(
            durationSeconds: 1,
            timing: TimingCurve.EaseInOut,
            tracks: [Tracks.Opacity((0, 0), (0.5, 1), (1, 0))]);
        Assert.Equal(5.0, triangle.Sample(0.25).Opacity * 10, 6);
        Assert.Equal(5.0, triangle.Sample(0.75).Opacity * 10, 6);
    }

    [Fact]
    public void Alternate_reversesTheInterpolatedCurveNotJustTheStops()
    {
        // Inside a reversed interval the eased values mirror the forward ones:
        // 0.3 and 0.7 into cycle 1 are 1 - 0.3 and 1 - 0.7 into cycle 0.
        var reversed = Ramp(TimingCurve.EaseInOut, alternate: true);
        Assert.Equal(
            Ramp(TimingCurve.EaseInOut).Sample(1 - 0.3).Opacity,
            reversed.Sample(1 + 0.3).Opacity,
            10);
    }
}
