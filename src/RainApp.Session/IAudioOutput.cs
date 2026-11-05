namespace RainApp.Session;

/// <summary>
/// Audio-output port (ADR-0003). Implemented by the thin MediaElement adapter;
/// Play means "load this Scene's audio and play it with native looping".
/// </summary>
public interface IAudioOutput
{
    void Play(string audioAsset, string title);

    void Pause();

    /// <summary>Continue the current track from where it was paused.</summary>
    void Resume();
}
