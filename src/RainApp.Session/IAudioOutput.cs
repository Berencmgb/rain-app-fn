namespace RainApp;

/// <summary>
/// Audio-output port (ADR-0003). Implemented by the thin MediaElement adapter;
/// Play means "load this Scene's audio and play it with native looping".
/// </summary>
public interface IAudioOutput
{
    void Play(string audioAsset, string title);

    /// <summary>Make this Scene's audio the current track without starting it
    /// (Scene switched while paused); a later Resume continues from this track.</summary>
    void Load(string audioAsset, string title);

    void Pause();

    /// <summary>Continue the current track from where it was paused.</summary>
    void Resume();

    /// <summary>
    /// Set the master output level (0 = silence, 1 = full scale). Session drives the
    /// fade curves by calling this from its ticks; the adapter maps the value onto
    /// the player's volume, and master volume (ticket #5) multiplies in at the edge.
    /// </summary>
    void SetOutputLevel(double level);

    /// <summary>Stop playback entirely (session over): releases the player so the
    /// background-audio machinery can shut down.</summary>
    void Stop();
}
