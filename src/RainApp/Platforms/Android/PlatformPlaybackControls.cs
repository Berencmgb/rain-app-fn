using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Microsoft.Extensions.DependencyInjection;
using RainApp.Services;

namespace RainApp;

/// <summary>
/// Android platform glue (ticket #7, ADR-0001). The toolkit's Media3 player and
/// its foreground-service media notification handle audio focus internally;
/// what they do on their own reaches Session as native player-state echoes at
/// the adapter seam. What they cannot express as state — the output device
/// going away — is reported here: the system's becoming-noisy broadcast is
/// Android's headphones/Bluetooth disconnect signal (audio would start playing
/// through the phone's own speaker), and it feeds Session's never-auto-resume
/// path. Also hosts the non-blocking POST_NOTIFICATIONS request on first play
/// (spec story 17), which the media notification needs on Android 13+.
/// </summary>
internal static class PlatformPlaybackControls
{
    private const int NotificationPermissionRequest = 1; // not awaited; no response handled

    private static MediaElementAudioOutput? _audio;
    private static Activity? _main;
    private static NoisyReceiver? _receiver;
    private static bool _askedForNotificationPermission;

    public static void Register(Activity main, IServiceProvider services)
    {
        _main = main;
        _audio ??= services.GetRequiredService<MediaElementAudioOutput>();
        if (_receiver is not null) return; // process-singleton registration

        _receiver = new NoisyReceiver(() => _audio?.NotifyDeviceDisconnected());

        // Application context, not the Activity: the receiver must outlive any
        // re-creation; becoming-noisy is a protected system broadcast, so no
        // export flag is required (Android 14 rules).
        Android.App.Application.Context.RegisterReceiver(
            _receiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
    }

    /// <summary>Asks POST_NOTIFICATIONS once, on first play, and never blocks on the
    /// answer: the play starts either way, the notification appears if granted. Only
    /// Android 13+ treats it as a runtime permission; below that nothing prompts.</summary>
    public static void RequestNotificationPermissionIfNeeded()
    {
        if (_askedForNotificationPermission) return;
        var activity = _main;
        if (activity is null) return; // no host to ask from; try again on the next play
        _askedForNotificationPermission = true;

        if (OperatingSystem.IsAndroidVersionAtLeast(33)
            && activity.CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            activity.RequestPermissions(
                [Manifest.Permission.PostNotifications], NotificationPermissionRequest);
        }
    }

    private sealed class NoisyReceiver(Action onBecomingNoisy) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent) => onBecomingNoisy();
    }
}
