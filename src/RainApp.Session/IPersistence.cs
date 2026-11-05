namespace RainApp;

/// <summary>
/// Persistence port (ADR-0003): the on-device record of Nights, today's listening
/// minutes, master volume and last-used Scene. Nothing leaves the device — no
/// account, no sync; the MAUI Preferences adapter is deliberately thin and untested.
/// </summary>
public interface IPersistence
{
    /// <summary>The stored record. Defaults (no Nights, full volume, no Scene)
    /// mean nothing is stored yet — a first launch.</summary>
    PersistedState Load();

    /// <summary>Replace the stored record with Session's current one.</summary>
    void Save(PersistedState state);
}

/// <summary>
/// Session's persisted record: the Nights ledger — lifetime total plus today's
/// minutes tagged with the calendar day they belong to — and the settings that
/// survive a launch (master volume, last-used Scene).
/// </summary>
public sealed record PersistedState
{
    public int NightsTotal { get; init; }

    /// <summary>
    /// The calendar day (UTC) <see cref="MinutesToday"/> belongs to. A stale day is
    /// retired on load: minutes never carry across a calendar day (glossary: Night).
    /// </summary>
    public DateOnly? ListeningDay { get; init; }

    /// <summary>Playing minutes accumulated on that day, including any already-earned Night.</summary>
    public TimeSpan MinutesToday { get; init; }

    /// <summary>Master volume (0 = silence, 1 = full scale); full when nothing is stored.</summary>
    public double Volume { get; init; } = 1.0;

    public string? LastSceneId { get; init; }
}
