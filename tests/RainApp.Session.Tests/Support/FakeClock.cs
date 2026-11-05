
namespace RainApp.Tests.Support;


/// <summary>Hand-written fake clock: tests move time by setting <see cref="Now"/>,
/// and simulate living somewhere else by setting <see cref="LocalZone"/>.</summary>
public sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = new DateTimeOffset(2026, 11, 5, 22, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow => Now;

    /// <summary>The zone tests pretend is local. UTC by default, so unless a test
    /// simulates another place, its local days are its UTC days.</summary>
    public TimeZoneInfo LocalZone { get; set; } = TimeZoneInfo.Utc;

    public DateTimeOffset LocalNow => TimeZoneInfo.ConvertTime(Now, LocalZone);

    public void Advance(TimeSpan by) => Now += by;
}
