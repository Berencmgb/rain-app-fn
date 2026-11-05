using RainApp.Tests.Support;

namespace RainApp.Tests;

/// <summary>
/// Night accrual and launch state (ticket #6): only playing time counts toward a
/// Night; crossing 10 cumulative minutes in a calendar day awards one Night once;
/// rollover moves to the next day; the ledger, volume and last Scene round-trip
/// through the persistence port and seed the next session's state.
/// </summary>
public class SessionNightsTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly FakePersistence _persistence = new();
    private readonly Session _session;

    public SessionNightsTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio, _persistence);
    }

    [Fact]
    public void ListeningTimeAccruesOnlyWhilePlaying()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(4));
        _session.Tick();

        Assert.Equal(TimeSpan.FromMinutes(4), _session.State.ListeningToday);
    }
}
