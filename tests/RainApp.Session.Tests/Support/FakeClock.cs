using RainApp.Session;

namespace RainApp.Session.Tests.Support;

/// <summary>Hand-written fake clock: tests move time by setting <see cref="Now"/>.</summary>
public sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = new DateTimeOffset(2026, 11, 5, 22, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow => Now;

    public void Advance(TimeSpan by) => Now += by;
}
