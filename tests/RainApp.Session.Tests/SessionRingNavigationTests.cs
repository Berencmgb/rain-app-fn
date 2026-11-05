using RainApp.Tests.Support;

namespace RainApp.Tests;

/// <summary>
/// Scene-ring navigation (ticket #4): Next/Previous walk the catalog as a ring,
/// wrapping past both ends. While playing they switch Scenes; while paused they
/// change the selection without starting audio. Switches keep the armed Sleep
/// Timer and reset the elapsed clock, exactly like tapping a tile.
/// </summary>
public class SessionRingNavigationTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly Session _session;

    public SessionRingNavigationTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio);
    }

    [Fact]
    public void NextFromTheLastSceneWrapsToTheFirst()
    {
        _session.Select(TestScenes.NightForest);

        _session.Next();

        Assert.Equal(TestScenes.RainOnWindow, _session.State.CurrentSceneId);
    }

    [Fact]
    public void NextWalksTheCatalogInOrderAndComesBackAround()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.Next();
        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);

        _session.Next();
        Assert.Equal(TestScenes.NightForest, _session.State.CurrentSceneId);

        _session.Next(); // wrapped: a full turn returns to the start
        Assert.Equal(TestScenes.RainOnWindow, _session.State.CurrentSceneId);
    }

    [Fact]
    public void NextWhilePlayingStartsTheFollowingScene()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.Next();

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Contains($"play {TestScenes.OceanWaves}.m4a", _audio.Log[^1]);
    }

    [Fact]
    public void PreviousWhilePlayingStartsThePrecedingScene()
    {
        _session.Select(TestScenes.OceanWaves);

        _session.Previous();

        Assert.Equal(TestScenes.RainOnWindow, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
    }

    [Fact]
    public void PreviousFromTheFirstSceneWrapsToTheLast()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.Previous();

        Assert.Equal(TestScenes.NightForest, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
    }

    [Fact]
    public void NextWhilePausedChangesTheSelectionWithoutStartingAudio()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.Pause();
        _audio.Log.Clear();

        _session.Next();

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.Contains($"load {TestScenes.OceanWaves}.m4a", _audio.Log[^1]); // the new Scene is ready
        Assert.DoesNotContain(_audio.Log, entry => entry.StartsWith("play") || entry.StartsWith("resume"));
    }

    [Fact]
    public void PreviousWhilePausedChangesTheSelectionWithoutStartingAudio()
    {
        _session.Select(TestScenes.OceanWaves);
        _session.Pause();

        _session.Previous();

        Assert.Equal(TestScenes.RainOnWindow, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
    }

    [Fact]
    public void ResumingAfterAPausedSwitchPlaysTheNewlySelectedScene()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.Pause();
        _session.Next(); // paused switch → ocean is now the selection

        _session.Play();

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        // The resumed output is the Scene the paused switch loaded, then resumed.
        Assert.Contains($"load {TestScenes.OceanWaves}.m4a", _audio.Log[^2]);
        Assert.Equal("resume", _audio.Log[^1]);
    }

    [Fact]
    public void SwitchingScenesWithNextKeepsTheArmedSleepTimer()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(45);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();

        _session.Next();
        _clock.Advance(TimeSpan.FromMinutes(1));
        _session.Tick();

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(TimeSpan.FromMinutes(39), _session.State.SleepTimerRemaining);
    }

    [Fact]
    public void SwitchingScenesWhilePausedKeepsTheSleepTimerWaiting()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(45);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();
        _session.Pause();

        _session.Previous(); // wraps to Night Forest, still paused

        Assert.Equal(TestScenes.NightForest, _session.State.CurrentSceneId);
        Assert.Equal(TimeSpan.FromMinutes(40), _session.State.SleepTimerRemaining);

        _session.Play();
        _clock.Advance(TimeSpan.FromMinutes(3));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(37), _session.State.SleepTimerRemaining);
    }

    [Fact]
    public void NextResetsTheElapsedClockForTheNewScene()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(2));
        _session.Tick();

        _session.Next();

        Assert.Equal(TimeSpan.Zero, _session.State.ElapsedListening);
    }

    [Fact]
    public void NextAndPreviousWithoutASelectionDoNothing()
    {
        _session.Next();
        _session.Previous();

        Assert.Null(_session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Idle, _session.State.Status);
        Assert.Empty(_audio.Log);
    }
}
