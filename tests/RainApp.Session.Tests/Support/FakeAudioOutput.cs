using RainApp.Session;

namespace RainApp.Session.Tests.Support;

/// <summary>
/// Fake audio-output port. Tests assert on Session state, not on these calls
/// (ADR-0003); the recording is only here for debugging.
/// </summary>
public sealed class FakeAudioOutput : IAudioOutput
{
    public List<string> Log { get; } = [];

    public void Play(string audioAsset, string title) => Log.Add($"play {audioAsset} ({title})");

    public void Pause() => Log.Add("pause");

    public void Resume() => Log.Add("resume");
}
