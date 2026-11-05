namespace RainApp.Tests.Support;

/// <summary>
/// Fake persistence port: holds whatever was last saved and hands it back to the
/// next session, which is the whole contract — tests observe the ledger through
/// Session state, and this fake is the round-trip stand-in for the device.
/// </summary>
public sealed class FakePersistence : IPersistence
{
    public PersistedState Saved { get; private set; } = new();

    public PersistedState Load() => Saved;

    public void Save(PersistedState state) => Saved = state;
}
