using RainApp.Tests.Support;

namespace RainApp.Tests;

/// <summary>
/// Master volume behavior (ticket #5): set / clamp at the Session seam, master ×
/// fade-envelope math observed at the fake output port, preserved across Scene
/// switches, resumes and the end of a session.
/// </summary>
public class SessionVolumeTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly Session _session;

    public SessionVolumeTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio);
    }

    [Fact]
    public void FreshSessionHasMasterVolumeAtFull()
    {
        Assert.Equal(1.0, _session.State.Volume);
    }

    [Fact]
    public void SettingTheVolumeIsVisibleInSessionState()
    {
        _session.SetVolume(0.4);

        Assert.Equal(0.4, _session.State.Volume, 3);
    }

    [Fact]
    public void VolumeIsClampedIntoTheFullScaleRange()
    {
        _session.SetVolume(1.5);
        Assert.Equal(1.0, _session.State.Volume);

        _session.SetVolume(-0.2);
        Assert.Equal(0.0, _session.State.Volume);
    }

    [Fact]
    public void SettingTheVolumeWhilePlayingSetsTheOutputLevel()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick(); // fully faded in

        _session.SetVolume(0.4);

        Assert.Equal(0.4, _audio.Levels[^1], 3);
    }

    [Fact]
    public void ChangingTheVolumeMidFadeReScalesTheRampToTheMasterLevel()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick(); // fade-in half-way

        _session.SetVolume(0.5);

        // Immediately re-scaled at the current envelope, and the fade now ramps
        // to the master level — the envelope completes, the output never does.
        Assert.Equal(0.25, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(0.5, _audio.Levels[^1], 3);
    }

    [Fact]
    public void FadesRampAroundTheMasterVolumeAndNeverResetIt()
    {
        _session.SetVolume(0.5);
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();
        Assert.Equal(0.5, _audio.Levels[^1], 3);

        // Sleep Timer expiry fades the output to silence around the master level.
        _session.SetSleepTimer(15);
        _clock.Advance(TimeSpan.FromMinutes(15));
        _session.Tick();
        _clock.Advance(TimeSpan.FromSeconds(5)); // half-way through the fade-out
        _session.Tick();
        Assert.Equal(0.25, _audio.Levels[^1], 3);

        // Silence, stop, idle — and the master volume never moved.
        _clock.Advance(TimeSpan.FromSeconds(5));
        _session.Tick();
        Assert.Equal(0.0, _audio.Levels[^1], 3);
        Assert.Equal(PlaybackStatus.Idle, _session.State.Status);
        Assert.Equal(0.5, _session.State.Volume);

        // The next session fades in from silence to the same master level.
        _session.Select(TestScenes.RainOnWindow);
        Assert.Equal(0.0, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();
        Assert.Equal(0.5, _audio.Levels[^1], 3);
    }

    [Fact]
    public void SceneSwitchKeepsTheMasterVolume()
    {
        _session.SetVolume(0.4);
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();

        _session.Next(); // switch is a fade-in on the next tile
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();

        Assert.Equal(0.4, _session.State.Volume);
        Assert.Equal(0.4, _audio.Levels[^1], 3);

        // Including switching to a Scene while paused: a later resume fades in to it.
        _session.Pause();
        _session.Previous();
        _session.Play();
        _clock.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();
        Assert.Equal(0.4, _audio.Levels[^1], 3);
    }

    [Fact]
    public void ResumeFadesInToTheMasterLevel()
    {
        _session.SetVolume(0.6);
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();
        _session.Pause();

        _session.Play();
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();

        Assert.Equal(0.3, _audio.Levels[^1], 3); // half-way: half the master level
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(0.6, _audio.Levels[^1], 3);
    }
}
