using System.Diagnostics;

namespace RainApp.Controls;

/// <summary>
/// The one clock and redraw pulse behind every Scene icon and equalizer view —
/// the equivalent of the mockup's `animation-play-state`: it runs while
/// Session is animating and simply stops on the frame it was showing when
/// nothing plays (paused or stopped), so the screen freezes like the mockup
/// paused, never hidden. Any later redraw paints that same frozen frame,
/// because drawing reads its state from <see cref="Seconds"/>.
/// </summary>
internal static class IconAnimation
{
    // Start past every animation-delay in SceneKeyframes, so even the
    // never-played launch screen shows the mockup's alive mid-animation look
    // rather than a 0% frame.
    private const double FrozenAtLaunchSeconds = 1.9;

    /// <summary>Redraw pulse (~30fps). CSS animates at display rate; icons
    /// this small look identical at 30 and the main thread stays free for
    /// audio.</summary>
    private const int FrameIntervalMs = 33;

    private static readonly object Gate = new();
    private static double _frozenSeconds = FrozenAtLaunchSeconds;
    private static Stopwatch? _running;
    private static IDispatcherTimer? _ticker;

    /// <summary>Fires on the UI thread once per frame while animating — views
    /// subscribe with their invalidate method.</summary>
    public static event Action? FrameInvalidated;

    /// <summary>Seconds on the shared icon timeline; advances only while
    /// animating, down to the millisecond when it stopped.</summary>
    public static double Seconds
    {
        get
        {
            lock (Gate)
            {
                return _running is { } started
                    ? _frozenSeconds + started.Elapsed.TotalSeconds
                    : _frozenSeconds;
            }
        }
    }

    /// <summary>Run the clock and redraw loop, or freeze both — called by the
    /// view-model whenever Session says whether audio is live.</summary>
    public static void SetAnimating(bool animating)
    {
        lock (Gate)
        {
            if (animating && _running is null)
            {
                _running = Stopwatch.StartNew();
            }
            else if (!animating && _running is { } started)
            {
                _frozenSeconds += started.Elapsed.TotalSeconds;
                started.Stop();
                _running = null;
            }
        }

        _ticker ??= CreateTicker();
        if (animating && !_ticker.IsRunning) _ticker.Start();
        else if (!animating && _ticker.IsRunning) _ticker.Stop();
    }

    private static IDispatcherTimer CreateTicker()
    {
        var timer = Dispatcher.GetForCurrentThread()!.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(FrameIntervalMs);
        timer.Tick += (_, _) => FrameInvalidated?.Invoke();
        return timer;
    }
}
