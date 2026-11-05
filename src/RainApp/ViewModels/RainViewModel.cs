using System.ComponentModel;
using System.Windows.Input;

namespace RainApp;

/// <summary>
/// Thin MVVM view-model (ADR-0003): translates taps into Session intents and
/// renders Session state. No logic here — if this class accumulates `if`s,
/// they belong in Session. Also drives Session's tick intent while playing,
/// the hook the Sleep Timer countdown (ticket #3) will reuse.
/// </summary>
public sealed class RainViewModel : INotifyPropertyChanged
{
    private readonly Session _session;
    private readonly IDispatcherTimer? _ticker;

    public RainViewModel(Session session, SceneCatalog catalog)
    {
        _session = session;
        Scenes = [.. catalog.Scenes.Select(s => new SceneTileViewModel(s, this))];

        TogglePlayCommand = new Command(TogglePlay);

        _ticker = Dispatcher.GetForCurrentThread().CreateTimer();
        _ticker.Interval = TimeSpan.FromMilliseconds(500);
        _ticker.Tick += (_, _) => _session.Tick();

        _session.StateChanged += OnStateChanged;
    }

    public IReadOnlyList<SceneTileViewModel> Scenes { get; }

    public ICommand TogglePlayCommand { get; }

    public bool HasSelection => _session.State.CurrentSceneId is not null;
    public string? CurrentSceneId => _session.State.CurrentSceneId;
    public bool IsPlaying => _session.State.Status == PlaybackStatus.Playing;
    public string StatusText => IsPlaying ? "Playing now" : "Paused";
    public string PlayerTitle => Tile(CurrentSceneId)?.Title ?? string.Empty;
    public SceneIcon PlayerIcon => Tile(CurrentSceneId)?.Icon ?? SceneIcon.RainOnWindow;
    public string ElapsedText => FormatElapsed(_session.State.ElapsedListening);

    private SceneTileViewModel? Tile(string? id) => Scenes.FirstOrDefault(t => t.Id == id);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Tap on a Scene card: the obvious toggle (story: tap plays, re-tap pauses).</summary>
    internal void ToggleTile(SceneTileViewModel tile)
    {
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
        if (!HasSelection) return;
        if (IsPlaying) _session.Pause(); else _session.Play();
    }

    private void OnStateChanged(SessionState state)
    {
        if (_ticker is not null)
        {
            if (state.Status == PlaybackStatus.Playing && !_ticker.IsRunning)
            {
                _ticker.Start();
            }
            else if (state.Status != PlaybackStatus.Playing && _ticker.IsRunning)
            {
                _ticker.Stop();
            }
        }

        foreach (var tile in Scenes) tile.Refresh();
        foreach (var name in new[]
                 {
                     nameof(HasSelection), nameof(IsPlaying), nameof(StatusText),
                     nameof(PlayerTitle), nameof(PlayerIcon), nameof(ElapsedText),
                 })
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    internal static string FormatElapsed(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes}:{t.Seconds:D2}";
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
