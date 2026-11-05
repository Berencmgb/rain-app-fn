namespace RainApp.Services;

/// <summary>
/// The one place platform audio events become Session intents (ADR-0003: the
/// audio-output port forwards platform events; constructing this wires them).
/// Everything is marshalled onto the main thread — Session is a single-threaded
/// state machine and its thread is the UI thread, where its own ticks run.
/// Being constructed is the subscription; the app root resolves it at startup.
/// </summary>
public sealed class PlatformEventBridge
{
    public PlatformEventBridge(MediaElementAudioOutput audio, Session session)
    {
        audio.InterruptionBegan += () => OnMain(session.InterruptionBegins);
        audio.InterruptionEnded += () => OnMain(session.InterruptionEnds);
        audio.DeviceDisconnected += () => OnMain(session.DeviceDisconnected);

        // Android only: the media notification and audio-focus handling drive the
        // native player directly, so a native start that was not Session's is either
        // the system handing audio back or a lock-screen resume — both are Session
        // resumes, which fade in. iOS reports those through the events above instead.
#if ANDROID
        audio.NativePlaybackResumed += () => OnMain(session.Play);
#endif
    }

    private static void OnMain(Action intent)
    {
        var dispatcher = Microsoft.Maui.Controls.Application.Current?.Dispatcher;
        if (dispatcher is null) return; // no window yet — no platform events to bridge
        if (dispatcher.IsDispatchRequired) dispatcher.Dispatch(intent);
        else intent();
    }
}
