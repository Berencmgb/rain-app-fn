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
        _session = new Session(TestScenes.Catalog(), _clock, _audio);
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
}
