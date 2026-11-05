namespace RainApp;

/// <summary>Injected clock; all time in Session flows through here (ADR-0003).</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>The same instant as the device's wall clock reads it, in the device's
    /// local time zone. A Night is a local calendar day (glossary: Night), so day
    /// boundaries are local midnights, never UTC ones.</summary>
    DateTimeOffset LocalNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset LocalNow => UtcNow.ToLocalTime();
}
