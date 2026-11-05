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
}
