namespace RainApp.Session.Tests.Support;

/// <summary>Test fixture Scenes — enough of the catalog to exercise selection.</summary>
public static class TestScenes
{
    public const string RainOnWindow = "rain-on-window";
    public const string OceanWaves = "ocean-waves";

    public static SceneDescriptor Rain() => new(
        RainOnWindow, "Rain on Window", "Soft rain tapping against glass",
        SceneIcon.RainOnWindow, "rain-on-window.m4a");

    public static SceneDescriptor Ocean() => new(
        OceanWaves, "Ocean Waves", "Slow waves on a distant shore",
        SceneIcon.RainOnWindow, "ocean-waves.m4a");

    public static SceneCatalog Catalog() => new([Rain(), Ocean()]);
}
