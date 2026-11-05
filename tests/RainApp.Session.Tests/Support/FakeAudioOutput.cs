namespace RainApp.Tests.Support;

/// <summary>
/// Fake audio-output port. Playback/pause calls are only recorded for debugging
/// (tests assert on Session state), but the fade envelope has no other observable
/// home: <see cref="Levels"/> records what Session ramped the output to, so fade
/// durations are verified against the fake clock here.
/// </summary>
public sealed class FakeAudioOutput : IAudioOutput
{
    public List<string> Log { get; } = [];

    /// <summary>Every level Session ramped the output to, in order.</summary>
    public List<double> Levels { get; } = [];

    public void Play(string audioAsset, string title) => Log.Add($"play {audioAsset} ({title})");

    public void Pause() => Log.Add("pause");

    public void Resume() => Log.Add("resume");

    public void SetOutputLevel(double level)
    {
        Log.Add($"level {level:0.###}");
        Levels.Add(level);
    }

    public void Stop() => Log.Add("stop");
}
