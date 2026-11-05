namespace RainApp.Tests.Support;

/// <summary>Test fixture Scenes — a three-Scene catalog in ring order, enough to
/// exercise selection and wrap-around navigation in both directions.</summary>
public static class TestScenes
{
    public const string RainOnWindow = "rain-on-window";
    public const string OceanWaves = "ocean-waves";
    public const string NightForest = "night-forest";

    public static SceneDescriptor Rain() => new(
        RainOnWindow, "Rain on Window", "Soft rain tapping against glass",
        SceneIcon.RainOnWindow, "rain-on-window.m4a");

    public static SceneDescriptor Ocean() => new(
        OceanWaves, "Ocean Waves", "Slow waves on a distant shore",
        SceneIcon.RainOnWindow, "ocean-waves.m4a");

    public static SceneDescriptor Forest() => new(
        NightForest, "Night Forest", "Crickets, owls and a gentle breeze",
        SceneIcon.RainOnWindow, "night-forest.m4a");

    public static SceneCatalog Catalog() => new([Rain(), Ocean(), Forest()]);
}
