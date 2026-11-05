using CommunityToolkit.Maui.Views;

namespace RainApp.Services;

/// <summary>
/// Thin adapter from the audio-output port to one long-lived MediaElement
/// (ADR-0001): native looping on, no logic here. The page attaches its
/// MediaElement once; platform event forwarding (Interruptions, disconnects —
/// ticket #7) will hang off this same seam.
/// </summary>
public sealed class MediaElementAudioOutput : IAudioOutput
{
    private MediaElement? _player;
    private string? _loadedAsset;
    private (string Asset, string Title)? _pendingLoad;

    public void Attach(MediaElement player)
    {
        player.ShouldLoopPlayback = true;
        player.MediaEnded += OnEnded;
        _player = player;
        if (_pendingLoad is { } pending)
        {
            // A Scene restored at launch loads with the player: still not playing.
            _pendingLoad = null;
            LoadAsset(player, pending.Asset, pending.Title);
            player.Pause();
        }
    }

    public void Play(string audioAsset, string title)
    {
        var player = RequirePlayer();
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

    private MediaElement RequirePlayer() =>
        _player ?? throw new InvalidOperationException("No MediaElement attached yet.");
}
