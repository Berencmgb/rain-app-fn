using RainApp.Tests.Support;

namespace RainApp.Tests;

/// <summary>
/// Interruption policy (ticket #7, glossary: Interruption) at the Session seam:
/// temporary Interruptions hand audio back and Session auto-resumes with a fade-in;
/// a device disconnect stays paused until the user resumes it. Fake clock, fake ports.
/// </summary>
public class SessionInterruptionTests
{
    private readonly FakeClock _clock = new();
    private readonly FakeAudioOutput _audio = new();
    private readonly Session _session;

    public SessionInterruptionTests()
    {
        _session = new Session(TestScenes.Catalog(), _clock, _audio, new FakePersistence());
    }

    [Fact]
    public void ATemporaryInterruptionPausesThePlayingScene()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.InterruptionBegins();

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.Contains("pause", _audio.Log);
    }

    [Fact]
    public void AudioHandedBackAfterATemporaryInterruptionResumesWithAFadeIn()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3)); // clear of the fade-in
        _session.Tick();
        _session.InterruptionBegins();

        _session.InterruptionEnds();

        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Contains("resume", _audio.Log);

        // Fades in from silence over ~2s like any resume — never snaps back on.
        Assert.Equal(0.0, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(0.5, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
        Assert.Equal(1.0, _audio.Levels[^1], 3);
    }

    [Fact]
    public void AudioHandedBackAfterAUserPauseDoesNotResume()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.Pause(); // the user paused; an unrelated Interruption ending is not their cue

        _session.InterruptionEnds();

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.DoesNotContain("resume", _audio.Log);
    }

    [Fact]
    public void ADeviceDisconnectPausesThePlayingScene()
    {
        _session.Select(TestScenes.RainOnWindow);

        _session.DeviceDisconnected();

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.Contains("pause", _audio.Log);
    }

    [Fact]
    public void AfterADeviceDisconnectAudioHandedBackDoesNotResume()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.DeviceDisconnected();

        // A device disconnect never auto-resumes (glossary: Interruption) — even with
        // the system handing audio back, and even through an Interruption around it.
        _session.InterruptionEnds();
        _session.InterruptionBegins();
        _session.InterruptionEnds();

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.DoesNotContain("resume", _audio.Log);
    }

    [Fact]
    public void ADeviceDisconnectDuringATemporaryInterruptionStillNeverAutoResumes()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.InterruptionBegins(); // a pending auto-resume is armed…

        _session.DeviceDisconnected(); // …but the device going away outranks it
        _session.InterruptionEnds();

        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.DoesNotContain("resume", _audio.Log);
    }

    [Fact]
    public void AUserResumeAfterADeviceDisconnectPlaysAgainAndInterruptionsWorkAgain()
    {
        _session.Select(TestScenes.RainOnWindow);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _session.Tick();
        _session.DeviceDisconnected();

        _session.Play();

        // Back like any user resume: fading in from silence.
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
        Assert.Equal(0.0, _audio.Levels[^1], 3);
        _clock.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();
        Assert.Equal(1.0, _audio.Levels[^1], 3);

        // And the disconnect left nothing lingering: a later temporary Interruption
        // auto-resumes normally.
        _session.InterruptionBegins();
        _session.InterruptionEnds();
        Assert.Equal(PlaybackStatus.Playing, _session.State.Status);
    }

    [Fact]
    public void AnArmedAutoResumeDoesNotOutliveANewSceneSelection()
    {
        _session.Select(TestScenes.RainOnWindow);
        _session.InterruptionBegins(); // armed: this interruption-caused pause may self-resume…

        _session.Select(TestScenes.OceanWaves); // …but the user took the controls instead
        _session.Pause();                        // …and this pause is the user's own
        _session.InterruptionEnds();

        Assert.Equal(TestScenes.OceanWaves, _session.State.CurrentSceneId);
        Assert.Equal(PlaybackStatus.Paused, _session.State.Status);
        Assert.DoesNotContain("resume", _audio.Log);
    }
}
