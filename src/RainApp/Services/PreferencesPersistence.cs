using System.Globalization;

namespace RainApp.Services;

/// <summary>
/// Persistence-port adapter on MAUI Preferences (ADR-0003): deliberately thin
/// and untested. Everything stays on the device — no account, no sync, no
/// analytics (spec: Nights never leave the device). Missing keys mean the
/// record's defaults: a first launch with nothing stored.
/// </summary>
public sealed class PreferencesPersistence : IPersistence
{
    private const string NightsKey = "nights.total";
    private const string ListeningDayKey = "nights.day";
    private const string MinutesTodayKey = "nights.minutes-today";
    private const string VolumeKey = "settings.master-volume";
    private const string LastSceneKey = "scene.last";

    public PersistedState Load() => new()
    {
        NightsTotal = Preferences.Default.Get(NightsKey, 0),
        ListeningDay = ParseDay(Preferences.Default.Get<string?>(ListeningDayKey, null)),
        MinutesToday = TimeSpan.FromMinutes(Preferences.Default.Get(MinutesTodayKey, 0.0)),
        Volume = Preferences.Default.Get(VolumeKey, 1.0),
        LastSceneId = EmptyToNull(Preferences.Default.Get<string?>(LastSceneKey, null)),
    };

    public void Save(PersistedState state)
    {
        Preferences.Default.Set(NightsKey, state.NightsTotal);
        Preferences.Default.Set(ListeningDayKey, state.ListeningDay?.ToString("O", CultureInfo.InvariantCulture) ?? "");
        Preferences.Default.Set(MinutesTodayKey, state.MinutesToday.TotalMinutes);
        Preferences.Default.Set(VolumeKey, state.Volume);
        Preferences.Default.Set(LastSceneKey, state.LastSceneId ?? "");
    }

    private static DateOnly? ParseDay(string? stored) =>
        DateOnly.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : null;

    private static string? EmptyToNull(string? stored) =>
        string.IsNullOrEmpty(stored) ? null : stored;
}
