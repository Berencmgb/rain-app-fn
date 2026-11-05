using RainApp.Tests.Support;

namespace RainApp.Tests;

/// <summary>
/// Sleep Timer behavior (ticket #3): arm / re-arm / disarm at the Session seam,
/// under a fake clock, with fades observed through the fake audio-output port.
/// </summary>
public class SessionSleepTimerTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly Session _session;

    public SessionSleepTimerTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio, new FakePersistence());
    }

    [Fact]
    public void FreshSessionStartsWithTheSleepTimerOff()
    {
        Assert.Null(_session.State.SleepTimerRemaining);
    }

    [Fact]
    public void ArmingASleepTimerMakesItActiveImmediately()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.SetSleepTimer(45);

        Assert.Equal(TimeSpan.FromMinutes(45), _session.State.SleepTimerRemaining);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
    }

    [Fact]
    public void SleepTimerDrainsOnlyWhilePlaying()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(45);

        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(40), _session.State.SleepTimerRemaining);

        // Paused: the Sleep Timer waits — even past what would have been expiry.
        _session.Pause();
        _clock.Advance(TimeSpan.FromMinutes(120));
        _session.Tick();
        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.Equal(TimeSpan.FromMinutes(40), _session.State.SleepTimerRemaining);

        _session.Play();
        _clock.Advance(TimeSpan.FromMinutes(3));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(37), _session.State.SleepTimerRemaining);
    }

    [Fact]
    public void ReplacingAnArmedSleepTimerStartsTheNewDurationImmediately()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(45);
        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick();

        _session.SetSleepTimer(15);

        Assert.Equal(TimeSpan.FromMinutes(15), _session.State.SleepTimerRemaining);
        _clock.Advance(TimeSpan.FromMinutes(1));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(14), _session.State.SleepTimerRemaining);
    }

    [Fact]
    public void SettingTheSleepTimerOffDisarmsWithoutStoppingPlayback()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(30);
        _clock.Advance(TimeSpan.FromMinutes(1));
        _session.Tick();

        _session.SetSleepTimer(null);

        Assert.Null(_session.State.SleepTimerRemaining);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);

        // Nothing pending: far past the old duration, still playing, nothing stopped.
        _clock.Advance(TimeSpan.FromMinutes(120));
        _session.Tick();
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Null(_session.State.SleepTimerRemaining);
        Assert.DoesNotContain("stop", _audio.Log);
    }

    [Fact]
    public void SwitchingScenesKeepsTheArmedSleepTimer()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(45);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();

        _session.Select(TestScenes.OceanWaves);
        _clock.Advance(TimeSpan.FromMinutes(1));
        _session.Tick();

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(TimeSpan.FromMinutes(39), _session.State.SleepTimerRemaining);
    }

    [Fact]
    public void ExpiryFadesToSilenceThenStopsAndReturnsToIdle()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(15);
        _clock.Advance(TimeSpan.FromSeconds(3)); // clear of the fade-in
        _session.Tick();

        // Expiry: the Sleep Timer disarms and the output begins fading out.
        _clock.Advance(TimeSpan.FromMinutes(15));
        _session.Tick();
        Assert.Equal(PlaybackStatus.FadingOut, _session.State.Status);
        Assert.Null(_session.State.SleepTimerRemaining);
        Assert.DoesNotContain("stop", _audio.Log);

        // Half-way through the ~10s fade: output sits at half level.
        _clock.Advance(TimeSpan.FromSeconds(5));
        _session.Tick();
        Assert.Equal(PlaybackStatus.FadingOut, _session.State.Status);
        Assert.Equal(0.5, _audio.Levels[^1], 3);

        // End of the fade: silence, then stop; the UI returns to idle.
        _clock.Advance(TimeSpan.FromSeconds(5));
        _session.Tick();
        Assert.Equal(0.0, _audio.Levels[^1], 3);
        Assert.Contains("stop", _audio.Log);
        Assert.Equal(PlaybackStatus.Idle, _session.State.Status);
        Assert.Null(_session.State.CurrentSceneId);
    }

    [Fact]
    public void StartingASceneFadesInOverTwoSeconds()
    {
        _session.Select(TestScenes.RainOnWindow);

        // Starts silent, then ramps to full over ~2s — never snaps on.
        Assert.Equal([0.0], _audio.Levels);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(0.5, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(1.0, _audio.Levels[^1], 3);

        // Full from here on: no further level changes.
        _clock.Advance(TimeSpan.FromSeconds(10));
        _session.Tick();
        Assert.Equal(3, _audio.Levels.Count);
    }

    [Fact]
    public void ResumingASceneFadesInOverTwoSeconds()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();
        _session.Pause();

        _session.Play();

        Assert.Equal([0.0, 1.0, 0.0], _audio.Levels); // restarts from silence
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(0.5, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(1.0, _audio.Levels[^1], 3);
    }

    [Fact]
    public void ResumingDuringTheFadeOutCancelsTheStop()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(15);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();
        _clock.Advance(TimeSpan.FromMinutes(15));
        _session.Tick();
        Assert.Equal(PlaybackStatus.FadingOut, _session.State.Status);

        _session.Play();

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.DoesNotContain("stop", _audio.Log);
        Assert.DoesNotContain("pause", _audio.Log); // audio never actually stopped mid-fade

        // And it fades back up to full like any resume.
        _clock.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();
        Assert.Equal(1.0, _audio.Levels[^1], 3);
    }

    [Fact]
    public void ArmingANewSleepTimerDuringTheFadeOutCancelsTheStop()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(15);
        _clock.Advance(TimeSpan.FromMinutes(15));
        _session.Tick();
        Assert.Equal(PlaybackStatus.FadingOut, _session.State.Status);

        _session.SetSleepTimer(30);

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Equal(TimeSpan.FromMinutes(30), _session.State.SleepTimerRemaining);
        Assert.DoesNotContain("stop", _audio.Log);
    }
}
