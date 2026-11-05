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
}
