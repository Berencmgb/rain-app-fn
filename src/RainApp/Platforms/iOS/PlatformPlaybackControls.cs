using AVFoundation;
using MediaPlayer;
using Microsoft.Extensions.DependencyInjection;
using RainApp.Services;

namespace RainApp;

/// <summary>
/// iOS platform glue (ticket #7, ADR-0001): forwards the audio session's
/// Interruption and route-change notifications to the audio-output adapter's
/// event seam, and claims the lock-screen / Control-Center remote controls so
/// they drive the Session rather than poking the player directly. Registering is
/// idempotent per process; the toolkit owns the audio session itself
/// (playback category, now-playing metadata from MetadataTitle/Artist).
/// </summary>
internal static class PlatformPlaybackControls
{
    private static bool _registered;

    public static void Register(IServiceProvider services)
    {
        if (_registered) return;
        _registered = true;

        var audio = services.GetRequiredService<MediaElementAudioOutput>();
        var session = services.GetRequiredService<Session>();

        // Interruption (glossary: Interruption): a call or Siri takes audio away —
        // began pauses; ended is the system handing audio back, and Session's policy
        // decides whether that auto-resumes.
        AVAudioSession.Notifications.ObserveInterruption((_, e) =>
        {
            switch (e.InterruptionType)
            {
                case AVAudioSessionInterruptionType.Began:
                    audio.NotifyInterruptionBegan();
                    break;
                case AVAudioSessionInterruptionType.Ended:
                    audio.NotifyInterruptionEnded();
                    break;
            }
        });

        // Device disconnect: the audio route lost a device (headphones out, Bluetooth
        // gone) — Session's never-auto-resume path. Other route reasons (a device
        // arriving, a route *switching* to one) must not pause playback.
        AVAudioSession.Notifications.ObserveRouteChange((_, e) =>
        {
            if (e.Reason == AVAudioSessionRouteChangeReason.OldDeviceUnavailable)
            {
                audio.NotifyDeviceDisconnected();
            }
        });

        // Lock-screen / Control-Center controls drive the Session, not the player
        // (AC: remote play/pause → Session intents; the state change then pauses or
        // resumes the player through the port, with the fades Session owns).
        var commands = MPRemoteCommandCenter.Shared;
        commands.PlayCommand.AddTarget(_ => { session.Play(); return MPRemoteCommandHandlerStatus.Success; });
        commands.PauseCommand.AddTarget(_ => { session.Pause(); return MPRemoteCommandHandlerStatus.Success; });
        commands.TogglePlayPauseCommand.AddTarget(_ =>
        {
            if (session.State.Status == PlaybackStatus.Playing) session.Pause();
            else session.Play();
            return MPRemoteCommandHandlerStatus.Success;
        });

        // Scenes loop forever: positions, rates and skips have no meaning here, so
        // the Control Center shows play/pause and the Scene name, nothing else.
        commands.ChangePlaybackPositionCommand.Enabled = false;
        commands.ChangePlaybackRateCommand.Enabled = false;
        commands.SkipForwardCommand.Enabled = false;
        commands.SkipBackwardCommand.Enabled = false;
    }
}
