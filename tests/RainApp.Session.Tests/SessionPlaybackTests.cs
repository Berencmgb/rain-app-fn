using RainApp.Tests.Support;

namespace RainApp.Tests;

public class SessionPlaybackTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly Session _session;

    public SessionPlaybackTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio);
    }

    [Fact]
    public void SelectingASceneStartsItPlaying()
    {
        _session.Select(TestScenes.RainOnWindow);

        Assert.Equal(TestScenes.RainOnWindow, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
    }

    [Fact]
    public void PausingThePlayingSceneStopsIt()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.Pause();

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.Equal(TestScenes.RainOnWindow, _session.State.CurrentSceneId);
    }

    [Fact]
    public void ResumingThePausedScenePlaysItAgain()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.Pause();

        _session.Play();

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
    }

    [Fact]
    public void ElapsedListeningGrowsWhilePlaying()
    {
        _session.Select(TestScenes.RainOnWindow);

        _clock.Advance(TimeSpan.FromMinutes(17));
        _session.Tick();

        Assert.Equal(TimeSpan.FromMinutes(17), _session.State.ElapsedListening);
    }

    [Fact]
    public void PausedTimeDoesNotCountAsListening()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(2));
        _session.Tick();

        _session.Pause();
        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(2), _session.State.ElapsedListening);

        _session.Play();
        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(7), _session.State.ElapsedListening);
    }

    [Fact]
    public void SwitchingScenesPlaysTheNewSceneWithFreshListeningTime()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(5));
        _session.Tick();

        _session.Select(TestScenes.OceanWaves);

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Equal(TimeSpan.Zero, _session.State.ElapsedListening);
    }

    [Fact]
    public void ChangingStatePublishesTheNewStateToObservers()
    {
        var published = new List<SessionState>();
        _session.StateChanged += published.Add;

        _session.Select(TestScenes.RainOnWindow);

        var last = Assert.Single(published);
        Assert.Same(_session.State, last);
        Assert.Equal(PlaybackStatus.Playing, last.Status);
    }
}
