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

    [Fact]
    public void ACompletedNightSurvivesARestartAndIsNotAwardedAgain()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(10));
        _session.Tick();
        _session.Pause();

        var reopened = Reopen(out _);
        Assert.Equal(1, reopened.State.NightsTotal);
        Assert.Equal(TimeSpan.FromMinutes(10), reopened.State.ListeningToday);

        // The day that already earned its Night is not counted a second time.
        reopened.Play();
        _clock.Advance(TimeSpan.FromMinutes(5));
        reopened.Tick();
        Assert.Equal(1, reopened.State.NightsTotal);
    }

    [Fact]
    public void PartialListeningTodaySurvivesARestartAndCompletesTonight()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(4));
        _session.Tick();
        _session.Pause();

        var reopened = Reopen(out _);
        Assert.Equal(0, reopened.State.NightsTotal);
        Assert.Equal(TimeSpan.FromMinutes(4), reopened.State.ListeningToday);

        reopened.Play();
        _clock.Advance(TimeSpan.FromMinutes(6));
        reopened.Tick();

        Assert.Equal(1, reopened.State.NightsTotal);
    }

    [Fact]
    public void AStoredLedgerFromADifferentDayStartsFreshToday()
    {
        // Yesterday reached 7 minutes — not a Night. Lifetime total was 3.
        _persistence.Save(new PersistedState
        {
            NightsTotal = 3,
            ListeningDay = new DateOnly(2026, 11, 4),
            MinutesToday = TimeSpan.FromMinutes(7),
        });

        var today = new Session(TestScenes.Catalog(), _clock, _audio, _persistence);
        Assert.Equal(3, today.State.NightsTotal);
        Assert.Equal(TimeSpan.Zero, today.State.ListeningToday);

        today.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromMinutes(10));
        today.Tick();
        Assert.Equal(4, today.State.NightsTotal);
    }

    [Fact]
    public void FirstLaunchStoresNothingAndChangesNothing()
    {
        var fresh = Reopen(out var audio);

        Assert.Null(fresh.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Idle, fresh.State.Status);
        Assert.Equal(1.0, fresh.State.Volume);
        Assert.Null(fresh.State.SleepTimerRemaining);
        Assert.Equal(0, fresh.State.NightsTotal);
        Assert.Empty(audio.Log); // a quiet launch: no audio, no levels
    }

    [Fact]
    public void TheLastSceneComesBackSelectedButNeverPlaying()
    {
        _session.Select(TestScenes.RainOnWindow); // tapped, listened, killed the app

        var reopened = Reopen(out var audio);
        Assert.Equal(TestScenes.RainOnWindow, reopened.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Paused, reopened.State.Status); // selected, not playing
        Assert.Null(reopened.State.SleepTimerRemaining);
        Assert.Equal(["load rain-on-window.m4a (Rain on Window)"], audio.Log); // loaded, never played
    }

    [Fact]
    public void TheSleepTimerIsAlwaysOffAtLaunchAndVolumeIsRestoredThroughTheOutput()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.SetVolume(0.4);
        _session.SetSleepTimer(30);

        var reopened = Reopen(out var audio);
        Assert.Equal(0.4, reopened.State.Volume, 3); // the setting is remembered…
        Assert.Null(reopened.State.SleepTimerRemaining); // …the mood is not

        reopened.Play(); // resuming reaches the restored level like any fade-in
        _clock.Advance(TimeSpan.FromSeconds(2));
        reopened.Tick();
        Assert.Equal(0.4, audio.Levels[^1], 3);
    }

    [Fact]
    public void APausedSceneSwitchBecomesTheRestoredSceneToo()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.Pause();
        _session.Next(); // paused walk: Ocean Waves is now the selected Scene

        var reopened = Reopen(out var audio);
        Assert.Equal(TestScenes.OceanWaves, reopened.State.CurrentSceneId);
        Assert.Contains("load ocean-waves.m4a (Ocean Waves)", audio.Log);
    }

    [Fact]
    public void AnUnknownStoredSceneLaunchesQuietly()
    {
        _persistence.Save(new PersistedState { LastSceneId = "a-retired-scene" });

        var today = new Session(TestScenes.Catalog(), _clock, _audio, _persistence);
        Assert.Null(today.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Idle, today.State.Status);
        Assert.Empty(_audio.Log);
    }

    private Session Reopen(out FakeAudioOutput audio)
    {
        audio = new FakeAudioOutput();
        return new Session(TestScenes.Catalog(), _clock, audio, _persistence);
    }
}
