namespace RainApp.Session;

/// <summary>Injected clock; all time in Session flows through here (ADR-0003).</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
