using System.ComponentModel;
using System.Windows.Input;
using RainApp.Controls;

namespace RainApp;

/// <summary>
/// Thin MVVM view-model (ADR-0003): translates taps into Session intents and
/// renders Session state. No logic here — if this class accumulates `if`s,
/// they belong in Session. Also drives Session's tick intent while audio is
/// live (playing or fading out); the tick interval drops to 100ms so fades
/// and the Sleep Timer countdown render smoothly.
/// </summary>
public sealed class RainViewModel : INotifyPropertyChanged
{
    private static readonly (string Label, int? Minutes)[] SleepTimerPresets =
    [
        ("15 minutes", 15), ("30 minutes", 30), ("45 minutes", 45), ("60 minutes", 60),
        ("Off", null),
    ];

    private readonly Session _session;
    private readonly IDispatcherTimer? _ticker;
    private bool _isVolumeOpen;

    public RainViewModel(Session session, SceneCatalog catalog)
    {
        _session = session;
        Scenes = [.. catalog.Scenes.Select(s => new SceneTileViewModel(s, this))];

        TogglePlayCommand = new Command(TogglePlay);
        OpenSleepTimerCommand = new Command(async () => await OpenSleepTimerAsync());
        NextSceneCommand = new Command(() => { DismissVolume(); _session.Next(); });
        PreviousSceneCommand = new Command(() => { DismissVolume(); _session.Previous(); });
        ToggleVolumeCommand = new Command(() => IsVolumeOpen = !IsVolumeOpen);
        CloseVolumeCommand = new Command(DismissVolume);

        _ticker = Dispatcher.GetForCurrentThread().CreateTimer();
        _ticker.Interval = TimeSpan.FromMilliseconds(100);
        _ticker.Tick += (_, _) => _session.Tick();

        _session.StateChanged += OnStateChanged;
    }

    public IReadOnlyList<SceneTileViewModel> Scenes { get; }

    public ICommand TogglePlayCommand { get; }
    public ICommand OpenSleepTimerCommand { get; }
    public ICommand NextSceneCommand { get; }
    public ICommand PreviousSceneCommand { get; }
    public ICommand ToggleVolumeCommand { get; }
    public ICommand CloseVolumeCommand { get; }

    public bool HasSelection => _session.State.CurrentSceneId is not null;
    public string? CurrentSceneId => _session.State.CurrentSceneId;
    public bool IsPlaying => _session.State.Status == PlaybackStatus.Playing;
    public string StatusText => _session.State.Status switch
    {
        PlaybackStatus.Playing => "Playing now",
        PlaybackStatus.FadingOut => "Fading out…",
        _ => "Paused",
    };
    public string PlayerTitle => Tile(CurrentSceneId)?.Title ?? string.Empty;
    public SceneIcon PlayerIcon => Tile(CurrentSceneId)?.Icon ?? SceneIcon.RainOnWindow;
    public string ElapsedText => FormatElapsed(_session.State.ElapsedListening);

    /// <summary>The Nights badge (glossary: Night): lifetime total, kept on-device only.</summary>
    public string NightsText => _session.State.NightsTotal.ToString();

    /// <summary>The Sleep Timer countdown: shown only while one is armed.</summary>
    public bool IsSleepTimerArmed => _session.State.SleepTimerRemaining is not null;
    public string SleepTimerText => _session.State.SleepTimerRemaining is { } remaining
        ? FormatRemaining(remaining) : string.Empty;

    /// <summary>The master-volume slider (volume control toggles it; a tap away
    /// dismisses). Dragging it is the set-volume intent straight through.</summary>
    public bool IsVolumeOpen
    {
        get => _isVolumeOpen;
        private set
        {
            if (_isVolumeOpen == value) return;
            _isVolumeOpen = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVolumeOpen)));
        }
    }

    public double MasterVolume
    {
        get => _session.State.Volume;
        set => _session.SetVolume(value);
    }

    public string VolumePercentText => $"{(int)Math.Round(_session.State.Volume * 100)}%";

    private SceneTileViewModel? Tile(string? id) => Scenes.FirstOrDefault(t => t.Id == id);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Tap on a Scene card: the obvious toggle (story: tap plays, re-tap pauses).</summary>
    internal void ToggleTile(SceneTileViewModel tile)
    {
        DismissVolume();
        if (tile.Id == _session.State.CurrentSceneId)
        {
            if (IsPlaying) _session.Pause(); else _session.Play();
        }
        else
        {
            _session.Select(tile.Id);
        }
    }

    private void TogglePlay()
    {
        DismissVolume();
        if (!HasSelection) return;
        if (IsPlaying) _session.Pause(); else _session.Play();
    }

    /// <summary>Any other interaction is a "tap elsewhere" for the volume slider.</summary>
    private void DismissVolume() => IsVolumeOpen = false;

    /// <summary>Timer chip / control: offers the fixed Sleep Timer durations (spec:
    /// no custom durations in v1). The chosen value becomes the Sleep Timer.</summary>
    private async Task OpenSleepTimerAsync()
    {
        DismissVolume();
        if (Application.Current?.Windows.FirstOrDefault()?.Page is not Page page) return;

        var choice = await page.DisplayActionSheetAsync(
            "Sleep Timer", "Cancel", null,
            [.. SleepTimerPresets.Select(p => p.Label)]);

        var index = Array.FindIndex(SleepTimerPresets, p => p.Label == choice);
        if (index >= 0) _session.SetSleepTimer(SleepTimerPresets[index].Minutes);
    }

    private void OnStateChanged(SessionState state)
    {
        // The mockup's animation-play-state: icon and equalizer clocks run
        // while audio is live (through the fade-out too) and freeze otherwise.
        IconAnimation.SetAnimating(state.Status is PlaybackStatus.Playing or PlaybackStatus.FadingOut);

        if (_ticker is not null)
        {
            // Ticks drive fades and the countdown: run while audio is live,
            // including through the fade-out that ends a session.
            if (state.Status is PlaybackStatus.Playing or PlaybackStatus.FadingOut && !_ticker.IsRunning)
            {
                _ticker.Start();
            }
            else if (state.Status is not (PlaybackStatus.Playing or PlaybackStatus.FadingOut) && _ticker.IsRunning)
            {
                _ticker.Stop();
            }
        }

        foreach (var tile in Scenes) tile.Refresh();
        foreach (var name in new[]
                 {
                     nameof(HasSelection), nameof(IsPlaying), nameof(StatusText),
                     nameof(PlayerTitle), nameof(PlayerIcon), nameof(ElapsedText),
                     nameof(IsSleepTimerArmed), nameof(SleepTimerText),
                     nameof(MasterVolume), nameof(VolumePercentText),
                     nameof(NightsText),
                 })
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    internal static string FormatElapsed(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes}:{t.Seconds:D2}";

    /// <summary>Sleep Timer countdown, rounded up so it only shows 0:00 at the very end.</summary>
    internal static string FormatRemaining(TimeSpan t)
    {
        var seconds = (int)Math.Ceiling(t.TotalSeconds);
        return $"{seconds / 60}:{seconds % 60:D2}";
    }
}

/// <summary>
/// One row of the Scene list: catalog data plus the highlight state pushed
/// by the parent (highlighted when active; pause icon shown when playing).
/// </summary>
public sealed class SceneTileViewModel : INotifyPropertyChanged
{
    private readonly SceneDescriptor _scene;
    private readonly RainViewModel _owner;

    internal SceneTileViewModel(SceneDescriptor scene, RainViewModel owner)
    {
        _scene = scene;
        _owner = owner;
        TapCommand = new Command(() => owner.ToggleTile(this));
    }

    public string Id => _scene.Id;
    public string Title => _scene.Title;
    public string Tagline => _scene.Tagline;
    public SceneIcon Icon => _scene.Icon;
    public ICommand TapCommand { get; }

    public bool IsCurrent => _owner.CurrentSceneId == Id;
    public bool ShowsPauseIcon => IsCurrent && _owner.IsPlaying;
    public bool ShowsPlayIcon => !ShowsPauseIcon;

    internal void Refresh()
    {
        foreach (var name in new[] { nameof(IsCurrent), nameof(ShowsPauseIcon), nameof(ShowsPlayIcon) })
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
