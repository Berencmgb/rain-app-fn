namespace RainApp.Animation;

/// <summary>
/// The animation's timing function: how keyframe-interval progress maps to
/// interpolation weight. Applied per interval, exactly as `animation-timing-function` is.
/// </summary>
public static class TimingCurveEasing
{
    public static double Ease(this TimingCurve curve, double progress) => curve switch
    {
        TimingCurve.Linear => progress,
        TimingCurve.EaseIn => CubicBezier(0.42, 0, 1, 1, progress),
        TimingCurve.EaseInOut => CubicBezier(0.42, 0, 0.58, 1, progress),
        _ => throw new ArgumentOutOfRangeException(nameof(curve), curve, "Unknown timing curve."),
    };

    /// <summary>
    /// The CSS cubic-bezier(x1, y1, x2, y2) timing function, solved with
    /// bisection: the Bézier x-component is monotonic for timing-function
    /// control points, so half-the-interval 60 times is exact to double.
    /// </summary>
    private static double CubicBezier(double x1, double y1, double x2, double y2, double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;

        double low = 0, high = 1, t = x;
        for (var i = 0; i < 60; i++)
        {
            t = (low + high) / 2;
            if (Bezier(x1, x2, t) < x) low = t;
            else high = t;
        }
        return Bezier(y1, y2, t);
    }

    // One component of a cubic Bézier through (0,0)–(1,1) with control offsets
    // c1, c2 — the CSS cubic-bezier form, evaluated at parameter t.
    private static double Bezier(double c1, double c2, double t) =>
        3 * (1 - t) * (1 - t) * t * c1 + 3 * (1 - t) * t * t * c2 + t * t * t;
}
