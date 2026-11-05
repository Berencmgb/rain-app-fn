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
}

/// <summary>
/// Data-driven catalog of the available Scenes. One entry drives tile, player and playback.
/// Ticket #4 extends this to the full seven-Scene list and ring navigation.
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
