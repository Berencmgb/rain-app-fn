namespace RainApp;

/// <summary>A Scene: a named, continuously looping ambient sound.</summary>
public sealed record SceneDescriptor(
    string Id,
    string Title,
    string Tagline,
    SceneIcon Icon,
    string AudioAsset);

/// <summary>Which illustrated icon renders for a Scene (icons are SkiaSharp-drawn, ADR-0002).</summary>
public enum SceneIcon
{
    RainOnWindow,
    OceanWaves,
    DistantThunderstorm,
    CracklingCampfire,
    NightForest,
    BrownWhiteNoise,
    FanRoomHum,
}

/// <summary>
/// Data-driven catalog of the available Scenes. One entry drives tile, player and
/// playback: adding a Scene is one entry here plus its audio file in Resources/Raw.
/// The entry order is the Scene ring that Next/Previous walk.
/// </summary>
public sealed class SceneCatalog
{
    private readonly IReadOnlyList<SceneDescriptor> _scenes;

    public SceneCatalog(IEnumerable<SceneDescriptor> scenes)
    {
        _scenes = scenes.ToList();
    }

    public IReadOnlyList<SceneDescriptor> Scenes => _scenes;

    public SceneDescriptor? Find(string id) =>
        _scenes.FirstOrDefault(s => s.Id == id);
}
