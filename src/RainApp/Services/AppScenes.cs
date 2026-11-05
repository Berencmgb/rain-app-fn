namespace RainApp.Services;

/// <summary>
/// The app's Scene catalog (mockup copy). One entry drives tile, player and
/// playback. Ticket #4 completes the seven-Scene list; ticket #2's scope is the
/// Rain on Window tracer bullet.
/// </summary>
public static class AppScenes
{
    public static SceneCatalog CreateCatalog() => new(
    [
        new SceneDescriptor(
            Id: "rain-on-window",
            Title: "Rain on Window",
            Tagline: "Soft rain tapping against glass",
            Icon: SceneIcon.RainOnWindow,
            AudioAsset: "rain-on-window.m4a"),
    ]);
}
