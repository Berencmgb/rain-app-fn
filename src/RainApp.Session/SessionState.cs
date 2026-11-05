namespace RainApp;

/// <summary>Observable state of a listening session. Immutable snapshot.</summary>
public sealed record SessionState
{
    public string? CurrentSceneId { get; init; }
    public PlaybackStatus Status { get; init; } = PlaybackStatus.Idle;

    /// <summary>How long the current Scene has been listened to (excludes time paused).</summary>
    public TimeSpan ElapsedListening { get; init; }

    /// <summary>Remaining Sleep Timer time, or null while Off (never armed at launch).</summary>
    public TimeSpan? SleepTimerRemaining { get; init; }

    /// <summary>Master volume (0 = silence, 1 = full scale); full at launch.</summary>
    public double Volume { get; init; } = 1.0;

    /// <summary>Lifetime total of Nights (glossary: Night), persisted on-device only.</summary>
    public int NightsTotal { get; init; }

    /// <summary>Playing minutes accumulated toward a Night on the current calendar day;
    /// reaching 10 awards the Night. Excludes time paused; never carries to the next day.</summary>
    public TimeSpan ListeningToday { get; init; }
}

public enum PlaybackStatus
{
    Idle,
    Playing,
    Paused,

    /// <summary>Sleep Timer expired: fading to silence, then the session stops.</summary>
    FadingOut,
}
