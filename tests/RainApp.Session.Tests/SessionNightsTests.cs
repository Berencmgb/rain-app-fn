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

    [Fact]
    public void PausedTimeDoesNotAccrueAndResumingContinuesTheSameLedger()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(4));
        _session.Tick();

        _session.Pause();
        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick(); // paused: the ledger simply waits

        Assert.Equal(TimeSpan.FromMinutes(4), _session.State.ListeningToday);

        _session.Play();
        _clock.Advance(TimeSpan.FromMinutes(2));
        _session.Tick();

        Assert.Equal(TimeSpan.FromMinutes(6), _session.State.ListeningToday);
    }

    [Fact]
    public void FadingOutTimeDoesNotAccrue()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetSleepTimer(4);
        _clock.Advance(TimeSpan.FromMinutes(4));
        _session.Tick(); // Sleep Timer expired: the session ends with a fade-out

        _clock.Advance(TimeSpan.FromSeconds(10));
        _session.Tick(); // fade reaches silence, the session is over

        Assert.Equal(PlaybackStatus.Idle, _session.State.Status);
        Assert.Equal(TimeSpan.FromMinutes(4), _session.State.ListeningToday);
    }

    [Fact]
    public void CrossingTenCumulativeMinutesOfPlayingAwardsOneNight()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(9));
        _session.Tick();
        Assert.Equal(0, _session.State.NightsTotal); // not yet a Night

        _clock.Advance(TimeSpan.FromMinutes(1));
        _session.Tick();

        Assert.Equal(1, _session.State.NightsTotal);
    }

    [Fact]
    public void MorePlayingAfterTheThresholdEarlsNoExtraNightsThatDay()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick();
        Assert.Equal(1, _session.State.NightsTotal);

        _clock.Advance(TimeSpan.FromMinutes(25));
        _session.Tick();

        Assert.Equal(1, _session.State.NightsTotal);
        Assert.Equal(TimeSpan.FromMinutes(35), _session.State.ListeningToday);
    }

    [Fact]
    public void TheNextCalendarDayEarlsANewNightFromItsOwnPlaying()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick();
        _session.Pause();
        Assert.Equal(1, _session.State.NightsTotal);

        // The next night: today's ledger starts empty whatever yesterday reached.
        _clock.Now = new DateTimeOffset(2026, 11, 6, 22, 0, 0, TimeSpan.Zero);
        _session.Play();
        _clock.Advance(TimeSpan.FromMinutes(4));
        _session.Tick();
        Assert.Equal(TimeSpan.FromMinutes(4), _session.State.ListeningToday);
        Assert.Equal(1, _session.State.NightsTotal);

        _clock.Advance(TimeSpan.FromMinutes(6));
        _session.Tick();
        Assert.Equal(2, _session.State.NightsTotal);
    }

    [Fact]
    public void PlayingAcrossMidnightCountsOnlyTimeAfterMidnight()
    {
        _clock.Now = new DateTimeOffset(2026, 11, 5, 23, 56, 0, TimeSpan.Zero);
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(4)); // continuous playing crosses midnight
        _session.Tick();

        // Yesterday's minutes belong to yesterday; they never carry into a Night.
        Assert.Equal(TimeSpan.Zero, _session.State.ListeningToday);
        Assert.Equal(0, _session.State.NightsTotal);

        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick();
        Assert.Equal(1, _session.State.NightsTotal);
    }
}
