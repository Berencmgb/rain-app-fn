namespace RainApp;

/// <summary>
/// The single test seam (ADR-0003): plain C#, no MAUI types. Driven by intents,
/// observed via StateChanged. All time flows through the injected clock.
/// Later tickets attach here: Sleep Timer + fades (#3), ring navigation (#4),
/// volume (#5), Night accrual (#6), Interruption policy (#7).
/// </summary>
public sealed class Session
{
    private readonly SceneCatalog _catalog;
    private readonly IClock _clock;
    private readonly IAudioOutput _audio;

    // Elapsed-listening stopwatch: time accrues only while playing.
    private TimeSpan _elapsedAccrued;
    private DateTimeOffset? _segmentStartedAt;

    public Session(SceneCatalog catalog, IClock clock, IAudioOutput audio)
    {
        _catalog = catalog;
        _clock = clock;
        _audio = audio;
    }

    public SessionState State { get; private set; } = new();

    public event Action<SessionState>? StateChanged;

    /// <summary>Intent: the user tapped a Scene — select it and start it playing.</summary>
    public void Select(string sceneId)
    {
        var scene = _catalog.Find(sceneId)
            ?? throw new ArgumentException($"Unknown Scene '{sceneId}'.", nameof(sceneId));

        _elapsedAccrued = TimeSpan.Zero;
        _segmentStartedAt = _clock.UtcNow;

        Update(State with
        {
            CurrentSceneId = scene.Id,
            Status = PlaybackStatus.Playing,
            ElapsedListening = TimeSpan.Zero,
        });
        _audio.Play(scene.AudioAsset, scene.Title);
    }

    /// <summary>Intent: resume the paused Scene.</summary>
    public void Play()
    {
        if (State.Status != PlaybackStatus.Paused) return;

        _segmentStartedAt = _clock.UtcNow;
        Update(State with { Status = PlaybackStatus.Playing });
        _audio.Resume();
    }

    /// <summary>Intent: pause the playing Scene.</summary>
    public void Pause()
    {
        if (State.Status != PlaybackStatus.Playing) return;

        FreezeClock();
        Update(State with { Status = PlaybackStatus.Paused, ElapsedListening = _elapsedAccrued });
        _audio.Pause();
    }

    /// <summary>Intent: timer pulse from the UI. Publishes elapsed listening while playing.</summary>
    public void Tick()
    {
        if (State.Status != PlaybackStatus.Playing) return;

        Update(State with { ElapsedListening = ElapsedNow() });
    }

    private TimeSpan ElapsedNow() =>
        _elapsedAccrued + (_segmentStartedAt.HasValue ? _clock.UtcNow - _segmentStartedAt.Value : TimeSpan.Zero);

    private void FreezeClock()
    {
        _elapsedAccrued = ElapsedNow();
        _segmentStartedAt = null;
    }

    private void Update(SessionState next)
    {
        State = next;
        StateChanged?.Invoke(next);
    }
}
