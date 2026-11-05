using CommunityToolkit.Maui.Views;
#if ANDROID
using CommunityToolkit.Maui.Core;
#endif

namespace RainApp.Services;

/// <summary>
/// Thin adapter from the audio-output port to one long-lived MediaElement
/// (ADR-0001): native looping on, no logic here. The page attaches its
/// MediaElement once; platform event forwarding (Interruptions, disconnects —
/// ticket #7) hangs off this same seam: platform glue reports events through the
/// Notify methods, and the matching events carry them to Session intents (wired
/// by PlatformEventBridge). Android additionally reports the native player's own
/// state changes, since the media notification and audio focus drive it directly.
/// </summary>
public sealed class MediaElementAudioOutput : IAudioOutput
{
    private MediaElement? _player;
    private string? _loadedAsset;
    private (string Asset, string Title)? _pendingLoad;

    /// <summary>The system took audio away (temporary Interruption).</summary>
    public event Action? InterruptionBegan;

    /// <summary>The system handed audio back (temporary Interruption over).</summary>
    public event Action? InterruptionEnded;

    /// <summary>The output device (headphones/Bluetooth) went away.</summary>
    public event Action? DeviceDisconnected;

    /// <summary>The native player started on its own — Android only: the media
    /// notification or an audio-focus regain drove it, not Session.</summary>
#if ANDROID
    public event Action? NativePlaybackResumed;
#endif

    public void Attach(MediaElement player)
    {
        player.ShouldLoopPlayback = true;
        player.MediaEnded += OnEnded;
#if ANDROID
        player.StateChanged += OnNativePlaybackStateChanged;
#endif
        _player = player;
        if (_pendingLoad is { } pending)
        {
            // A Scene restored at launch loads with the player: still not playing.
            _pendingLoad = null;
            LoadAsset(player, pending.Asset, pending.Title);
            player.Pause();
        }
    }

    /// <summary>Platform glue reports an Interruption beginning (glossary:
    /// Interruption). Events before the page hosts a player are ignored: with no
    /// MediaElement hosted yet there is no audio for the system to have taken.</summary>
    public void NotifyInterruptionBegan()
    {
        if (_player is null) return;
        InterruptionBegan?.Invoke();
    }

    /// <summary>Platform glue reports the Interruption ending and audio being handed
    /// back; whether Session auto-resumes is Session's policy, not this adapter's.</summary>
    public void NotifyInterruptionEnded()
    {
        if (_player is null) return;
        InterruptionEnded?.Invoke();
    }

    /// <summary>Platform glue reports the output device going away (glossary:
    /// Interruption — never auto-resumed).</summary>
    public void NotifyDeviceDisconnected()
    {
        if (_player is null) return;
        DeviceDisconnected?.Invoke();
    }

    public void Play(string audioAsset, string title)
    {
        var player = RequirePlayer();
#if ANDROID
        // The media notification needs notification permission on Android 13+: ask on
        // first play, without blocking it (spec story 17 — never a wall of prompts).
        PlatformPlaybackControls.RequestNotificationPermissionIfNeeded();
#endif
        LoadAsset(player, audioAsset, title);
        player.Play();
    }

    /// <summary>Setting a new Source can start playing on some platforms, so the
    /// pause is explicit: Load must never start audio. Before the page hosts a
    /// player (launch restore), Load is remembered and applied on Attach.</summary>
    public void Load(string audioAsset, string title)
    {
        if (_player is null)
        {
            _pendingLoad = (audioAsset, title);
            return;
        }
        var player = _player;
        if (_loadedAsset == audioAsset) return;
        LoadAsset(player, audioAsset, title);
        player.Pause();
    }

    private void LoadAsset(MediaElement player, string audioAsset, string title)
    {
        if (_loadedAsset == audioAsset) return;
        player.Source = MediaSource.FromResource(audioAsset);
        player.MetadataTitle = title;
        player.MetadataArtist = "Rain App";
        _loadedAsset = audioAsset;
    }

    public void Pause() => RequirePlayer().Pause();

    public void Resume() => RequirePlayer().Play();

    /// <summary>Session ramps this from its ticks; the volume property is the ramp target.</summary>
    public void SetOutputLevel(double level) => RequirePlayer().Volume = Math.Clamp(level, 0, 1);

    public void Stop() => RequirePlayer().Stop();

    // ShouldLoopPlayback should never let MediaEnded fire; if it ever does,
    // restart immediately (only safe fallback — never the normal path).
    private void OnEnded(object? sender, EventArgs e) => _player?.Play();

#if ANDROID
    /// <summary>The native player's own starts/pauses — driven by the media notification
    /// or by the system's audio-focus handling — arrive as platform events here: a pause
    /// Session did not ask for reads as the system taking audio away, a start as a
    /// resume. Session's own commands echo back and its state machine ignores those
    /// (its transitions are guarded on the state the echo already reflects).</summary>
    private void OnNativePlaybackStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case MediaElementState.Paused:
                InterruptionBegan?.Invoke();
                break;
            case MediaElementState.Playing:
                NativePlaybackResumed?.Invoke();
                break;
        }
    }
#endif

    private MediaElement RequirePlayer() =>
        _player ?? throw new InvalidOperationException("No MediaElement attached yet.");
}
