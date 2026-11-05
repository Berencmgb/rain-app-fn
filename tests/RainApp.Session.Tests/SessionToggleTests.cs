using RainApp.Tests.Support;

namespace RainApp.Tests;

/// <summary>
/// The play/pause-vs-select decision (spec story 6) as a single Session intent:
/// every "obvious toggle" surface — Scene tile, player button, lock-screen controls —
/// is the same state machine, and Session owns the one derived answer to
/// "is audio live?" that the UI renders (ADR-0003).
/// </summary>
public class SessionToggleTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly Session _session;

    public SessionToggleTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio, new FakePersistence());
    }

    [Fact]
    public void AudioIsLiveWhilePlayingAndFadingOutAndStillPausedAndIdle()
    {
        Assert.False(_session.State.IsAudioLive); // idle

        _session.Select(TestScenes.RainOnWindow);
        Assert.True(_session.State.IsAudioLive); // playing

        _session.Pause();
        Assert.False(_session.State.IsAudioLive); // paused

        _session.SetSleepTimer(15);
        _session.Play();
        _clock.Advance(TimeSpan.FromMinutes(15));
        _session.Tick(); // expired: fading out
        Assert.Equal(PlaybackStatus.FadingOut, _session.State.Status);
        Assert.True(_session.State.IsAudioLive); // the fade is audio still

        _clock.Advance(TimeSpan.FromSeconds(10));
        _session.Tick(); // faded to the end
        Assert.False(_session.State.IsAudioLive); // at rest
    }

    [Fact]
    public void ToggleOnThePlayingScenePausesIt()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.Toggle(TestScenes.RainOnWindow);

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.Equal("pause", _audio.Log[^1]);
    }

    [Fact]
    public void ToggleAgainOnThePausedScenePlaysItWithAFadeIn()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();
        _session.Pause();
        _audio.Log.Clear();

        _session.Toggle(TestScenes.RainOnWindow);

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Equal("resume", _audio.Log[^1]);
        Assert.Equal(0.0, _audio.Levels[^1], 3); // every resume fades in
        _clock.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();
        Assert.Equal(1.0, _audio.Levels[^1], 3);
    }

    [Fact]
    public void ToggleOnTheCurrentSceneDuringTheFadeOutCancelsTheStop()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(15);
        _clock.Advance(TimeSpan.FromMinutes(15));
        _session.Tick();
        Assert.Equal(PlaybackStatus.FadingOut, _session.State.Status);

        _session.Toggle(TestScenes.RainOnWindow);

        // Re-tapping the card mid-fade is "changed my mind", not a pause.
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.DoesNotContain("stop", _audio.Log);
    }

    [Fact]
    public void ToggleOnAnotherSceneSelectsAndPlaysIt()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.Pause();

        _session.Toggle(TestScenes.OceanWaves);

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Contains($"play {TestScenes.OceanWaves}.m4a (Ocean Waves)", _audio.Log);
    }

    [Fact]
    public void ToggleWithNoTargetTogglesTheSelectedScene()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.Toggle(); // player button: pause what plays

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);

        _session.Toggle(); // player button again: play it back, fading in

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Equal(0.0, _audio.Levels[^1], 3);
    }

    [Fact]
    public void ToggleWithNoTargetPlaysASceneRestoredSelectedAtLaunch()
    {
        var persistence = new FakePersistence();
        persistence.Save(new PersistedState { LastSceneId = TestScenes.RainOnWindow });
        var restored = new Session(TestScenes.Catalog(), _clock, _audio, persistence);
        Assert.Equal(PlaybackStatus.Paused, restored.State.Status); // selected, not playing

        restored.Toggle(); // the lock-screen / player toggle without naming a Scene

        Assert.Equal(PlaybackStatus.Playing, restored.State.Status);
    }

    [Fact]
    public void ToggleWithNoTargetAndNothingSelectedDoesNothing()
    {
        _session.Toggle();

        Assert.Equal(PlaybackStatus.Idle, _session.State.Status);
        Assert.Empty(_audio.Log);
    }

    [Fact]
    public void ToggleKeepsTheArmedSleepTimerAndVolumeExactlyAsTheyWere()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(45);
        _session.SetVolume(0.4);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();

        _session.Toggle(); // pause
        _clock.Advance(TimeSpan.FromMinutes(120)); // the timer waits while paused
        _session.Toggle(); // play

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Equal(TimeSpan.FromMinutes(40), _session.State.SleepTimerRemaining);
        Assert.Equal(0.4, _session.State.Volume, 3);
    }
}
