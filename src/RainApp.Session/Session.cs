namespace RainApp;

/// <summary>
/// The single test seam (ADR-0003): plain C#, no MAUI types. Driven by intents,
/// observed via StateChanged. All time flows through the injected clock.
/// Sleep Timer + fades (#3), Scene-ring navigation (#4) and master volume (#5)
/// are in. Later tickets attach here: Night accrual (#6), Interruption
/// policy (#7).
/// </summary>
public sealed class Session
{
    /// <summary>Fade curves owned by Session (spec: gentle 2s in, ~10s out).</summary>
    private static readonly TimeSpan FadeIn = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FadeOut = TimeSpan.FromSeconds(10);

    private readonly SceneCatalog _catalog;
    private readonly IClock _clock;
    private readonly IAudioOutput _audio;
    private readonly IPersistence _persistence;

    // Nights ledger (glossary: Night): playing time accrues to today through the
    // same clock the fades run on; the lifetime total and today's minutes persist.
    private int _nightsTotal;
    private DateOnly _listeningDay;
    private TimeSpan _listeningToday = TimeSpan.Zero;
    private DateTimeOffset? _listeningSince;

    // Elapsed-listening stopwatch: time accrues only while playing.
    private TimeSpan _elapsedAccrued;
    private DateTimeOffset? _segmentStartedAt;

    // Sleep Timer budget (glossary: Sleep Timer): remaining time drains only
    // while playing. Null remaining means Off — the state at every launch.
    private TimeSpan? _timerRemaining;
    private DateTimeOffset? _timerRunningSince;

    // Output-level envelope: fades are ramped tick-by-tick through the clock.
    // The port never sees the envelope alone — every call is master volume ×
    // envelope, so fades ramp around the user's volume and never reset it.
    private double _masterVolume = 1.0;
    private double _envelope = 1.0;
    private double _sentLevel = 1.0;
    private double _fadeFrom;
    private double _fadeTo;
    private TimeSpan _fadeDuration;
    private DateTimeOffset? _fadeStartedAt;

    public Session(SceneCatalog catalog, IClock clock, IAudioOutput audio, IPersistence persistence)
    {
        _catalog = catalog;
        _clock = clock;
        _audio = audio;
        _persistence = persistence;
        _listeningDay = Today;
    }

    public SessionState State { get; private set; } = new();

    public event Action<SessionState>? StateChanged;

    /// <summary>Intent: set the master volume (0 = silence, 1 = full scale). Values
    /// outside the range are clamped. It survives Scene switches, pauses and fades;
    /// every level Session sends to the output is master volume × fade envelope.</summary>
    public void SetVolume(double volume)
    {
        var level = Math.Clamp(volume, 0.0, 1.0);
        if (level == _masterVolume) return;

        _masterVolume = level;
        Update(State with { Volume = level });
        ApplyOutputLevel();
    }

    /// <summary>Intent: the user tapped a Scene — select it and start it playing.</summary>
    public void Select(string sceneId)
    {
        var scene = _catalog.Find(sceneId)
            ?? throw new ArgumentException($"Unknown Scene '{sceneId}'.", nameof(sceneId));

        _elapsedAccrued = TimeSpan.Zero;
        _segmentStartedAt = _clock.UtcNow;
        _listeningSince ??= _clock.UtcNow; // switching Scenes is continuous listening

        Update(State with
        {
            CurrentSceneId = scene.Id,
            Status = PlaybackStatus.Playing,
            ElapsedListening = TimeSpan.Zero,
        });
        StartFade(0, 1, FadeIn); // manual start never snaps on
        _audio.Play(scene.AudioAsset, scene.Title);
    }

    /// <summary>Intent: walk the Scene list as a ring — select the next Scene
    /// (wrapping past the end).</summary>
    public void Next() => StepRing(+1);

    /// <summary>Intent: walk the Scene list as a ring — select the previous Scene
    /// (wrapping past the start).</summary>
    public void Previous() => StepRing(-1);

    private void StepRing(int delta)
    {
        if (State.CurrentSceneId is null) return;

        var scenes = _catalog.Scenes;
        var index = -1;
        for (var i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].Id == State.CurrentSceneId) { index = i; break; }
        }
        if (index < 0) return;

        var next = scenes[(index + delta + scenes.Count) % scenes.Count];

        if (State.Status == PlaybackStatus.Paused)
        {
            // Paused: navigation changes the selection only — no audio starts, the
            // Sleep Timer keeps waiting, and the new Scene is loaded so a later
            // resume picks it up. Elapsed belongs to the new Scene, like any switch.
            _elapsedAccrued = TimeSpan.Zero;
            _segmentStartedAt = null;
            Update(State with
            {
                CurrentSceneId = next.Id,
                ElapsedListening = TimeSpan.Zero,
            });
            _audio.Load(next.AudioAsset, next.Title);
            return;
        }

        // Playing or fading out: switching is exactly a tap on the next tile —
        // the armed Sleep Timer carries over untouched.
        Select(next.Id);
    }

    /// <summary>Intent: resume the paused Scene.</summary>
    public void Play()
    {
        if (State.Status == PlaybackStatus.FadingOut)
        {
            // Changed my mind: the stop is cancelled and playback simply continues.
            CancelFadeOut();
            return;
        }
        if (State.Status != PlaybackStatus.Paused) return;

        _segmentStartedAt = _clock.UtcNow;
        if (_timerRemaining is not null) _timerRunningSince = _clock.UtcNow;
        _listeningSince ??= _clock.UtcNow;
        Update(State with { Status = PlaybackStatus.Playing });
        StartFade(0, 1, FadeIn); // every resume fades in
        _audio.Resume();
    }

    /// <summary>Intent: pause the playing Scene.</summary>
    public void Pause()
    {
        if (State.Status != PlaybackStatus.Playing) return;

        FreezeClock();
        AdvanceSleepTimerToNow();
        AccumulateListeningToNow();
        _listeningSince = null; // paused time is not listening time
        _fadeStartedAt = null; // the level freezes with the paused audio
        Update(State with
        {
            Status = PlaybackStatus.Paused,
            ElapsedListening = _elapsedAccrued,
            SleepTimerRemaining = _timerRemaining,
        });
        _audio.Pause();
    }

    /// <summary>Intent: the user arms or replaces the Sleep Timer with a duration in
    /// minutes, or sets it Off (null). An Off Sleep Timer never stops playback.</summary>
    public void SetSleepTimer(int? minutes)
    {
        if (minutes is not null) CancelFadeOut(); // arming again means "don't stop"

        if (minutes is null)
        {
            _timerRemaining = null;
            _timerRunningSince = null;
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minutes.Value, 1);
            _timerRemaining = TimeSpan.FromMinutes(minutes.Value);
            _timerRunningSince = State.Status == PlaybackStatus.Playing ? _clock.UtcNow : null;
        }

        Update(State with { SleepTimerRemaining = _timerRemaining });
    }

    /// <summary>Intent: timer pulse from the UI. Drives elapsed listening, the Sleep Timer
    /// countdown and the fade envelope while playing; drives the fade-out through to a
    /// stop. A paused countdown simply waits.</summary>
    public void Tick()
    {
        switch (State.Status)
        {
            case PlaybackStatus.Playing:
                AdvanceFade();
                AdvanceSleepTimerToNow();
                if (_timerRemaining is { } remaining && remaining <= TimeSpan.Zero)
                {
                    BeginFadeOut();
                    return;
                }
                AccumulateListeningToNow();
                Update(State with { ElapsedListening = ElapsedNow(), SleepTimerRemaining = _timerRemaining });
                break;

            case PlaybackStatus.FadingOut:
                AdvanceFade();
                if (_fadeStartedAt is null) FinishFadeOut();
                break;
        }
    }

    private TimeSpan ElapsedNow() =>
        _elapsedAccrued + (_segmentStartedAt.HasValue ? _clock.UtcNow - _segmentStartedAt.Value : TimeSpan.Zero);

    /// <summary>Drains the Sleep Timer budget up to now; only ever runs while playing.</summary>
    private void AdvanceSleepTimerToNow()
    {
        if (_timerRemaining is null || _timerRunningSince is null) return;

        var remaining = _timerRemaining.Value - (_clock.UtcNow - _timerRunningSince.Value);
        _timerRemaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        _timerRunningSince = _clock.UtcNow;
    }

    /// <summary>Sleep Timer expired: the countdown goes away and the output starts its
    /// gentle fade to silence; the elapsed clock stops here.</summary>
    private void BeginFadeOut()
    {
        FreezeClock();
        AccumulateListeningToNow();
        _listeningSince = null; // the fade to silence is not listening time either
        _timerRemaining = null;
        _timerRunningSince = null;
        StartFade(_envelope, 0, FadeOut);
        Update(State with
        {
            Status = PlaybackStatus.FadingOut,
            ElapsedListening = _elapsedAccrued,
            SleepTimerRemaining = null,
        });
    }

    /// <summary>Fade reached silence: stop the audio and return the UI to idle.
    /// The master volume outlives the session; only the envelope resets.</summary>
    private void FinishFadeOut()
    {
        _envelope = 1.0; // the next start fades in from silence again
        _sentLevel = 1.0; // …and its first level is sent, as at any fresh start
        _audio.Stop();
        Update(new SessionState { Volume = _masterVolume });
    }

    /// <summary>The user changed their mind before the fade reached silence: the audio
    /// never stopped, so cancelling is just ramping the envelope back up (faded in,
    /// like any resume) and continuing to run.</summary>
    private void CancelFadeOut()
    {
        if (State.Status != PlaybackStatus.FadingOut) return;

        _segmentStartedAt = _clock.UtcNow;
        _listeningSince ??= _clock.UtcNow;
        Update(State with { Status = PlaybackStatus.Playing });
        StartFade(0, 1, FadeIn);
    }

    /// <summary>Starts ramping the fade envelope; the ramp advances with each tick.</summary>
    private void StartFade(double from, double to, TimeSpan duration)
    {
        _fadeFrom = from;
        _fadeTo = to;
        _fadeDuration = duration;
        _fadeStartedAt = _clock.UtcNow;
        SetEnvelope(from);
    }

    private void AdvanceFade()
    {
        if (_fadeStartedAt is null) return;

        var progress = (_clock.UtcNow - _fadeStartedAt.Value) / _fadeDuration;
        if (progress >= 1)
        {
            _fadeStartedAt = null;
            SetEnvelope(_fadeTo);
        }
        else if (progress > 0)
        {
            SetEnvelope(_fadeFrom + (_fadeTo - _fadeFrom) * progress);
        }
    }

    private void SetEnvelope(double envelope)
    {
        _envelope = envelope;
        ApplyOutputLevel();
    }

    /// <summary>Whatever the user set is the level the audio plays at: the port only
    /// ever sees master volume × fade envelope.</summary>
    private void ApplyOutputLevel()
    {
        var level = _masterVolume * _envelope;
        if (level == _sentLevel) return;

        _sentLevel = level;
        _audio.SetOutputLevel(level);
    }

    private void FreezeClock()
    {
        _elapsedAccrued = ElapsedNow();
        _segmentStartedAt = null;
    }

    /// <summary>A Night is a calendar day reaching this much cumulative playing time
    /// (glossary: Night). The day's first crossing awards it — once.</summary>
    private static readonly TimeSpan NightThreshold = TimeSpan.FromMinutes(10);

    /// <summary>The calendar day (UTC) the clock currently reads.</summary>
    private DateOnly Today => DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

    /// <summary>Counts elapsed playing time into today's ledger; only ever called while
    /// Playing. The watermark advances with each call so every second counts exactly once.
    /// Crossing the Night threshold awards exactly one Night — later minutes that day
    /// only add to today's total, since a crossing needs a below-threshold before.
    /// A calendar-day crossing retires yesterday's minutes: only time after the day
    /// boundary counts toward the new day's Night.</summary>
    private void AccumulateListeningToNow()
    {
        if (_listeningSince is null) return;

        var now = _clock.UtcNow;
        if (Today != _listeningDay)
        {
            _listeningDay = Today;
            _listeningToday = TimeSpan.Zero;
            var midnight = new DateTimeOffset(_listeningDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            if (_listeningSince.Value < midnight) _listeningSince = midnight;
        }

        var before = _listeningToday;
        if (now > _listeningSince) _listeningToday += now - _listeningSince.Value;
        _listeningSince = now;

        if (before < NightThreshold && _listeningToday >= NightThreshold) _nightsTotal++;
    }

    private void Update(SessionState next)
    {
        // The Nights ledger fields flow from Session-owned state into every update,
        // so no call site can forget them.
        State = next with { NightsTotal = _nightsTotal, ListeningToday = _listeningToday };
        StateChanged?.Invoke(State);
    }
}
