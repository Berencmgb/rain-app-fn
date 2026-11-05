namespace RainApp;

/// <summary>Observable state of a listening session. Immutable snapshot.</summary>
public sealed record SessionState
{
    public string? CurrentSceneId { get; init; }
    public PlaybackStatus Status { get; init; } = PlaybackStatus.Idle;

    /// <summary>How long the current Scene has been listened to (excludes time paused).</summary>
    public TimeSpan ElapsedListening { get; init; }
}

public enum PlaybackStatus
{
    Idle,
    Playing,
    Paused,
}
