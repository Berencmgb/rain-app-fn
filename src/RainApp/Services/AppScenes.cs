namespace RainApp.Services;

/// <summary>
/// The app's Scene catalog (mockup copy — titles and taglines come from
/// mockup.html). One entry drives tile, player and playback; the audio files
/// are the generated placeholders (tools/audio/generate-placeholders.sh), each
/// one swappable by replacing the file of the same name. Entry order is the
/// Scene ring that the player's Next/Previous walk.
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
        new SceneDescriptor(
            Id: "ocean-waves",
            Title: "Ocean Waves",
            Tagline: "Slow waves on a distant shore",
            Icon: SceneIcon.OceanWaves,
            AudioAsset: "ocean-waves.m4a"),
        new SceneDescriptor(
            Id: "distant-thunderstorm",
            Title: "Distant Thunderstorm",
            Tagline: "Rolling thunder, muffled and far away",
            Icon: SceneIcon.DistantThunderstorm,
            AudioAsset: "distant-thunderstorm.m4a"),
        new SceneDescriptor(
            Id: "crackling-campfire",
            Title: "Crackling Campfire",
            Tagline: "Warm fire with soft crackles and pops",
            Icon: SceneIcon.CracklingCampfire,
            AudioAsset: "crackling-campfire.m4a"),
        new SceneDescriptor(
            Id: "night-forest",
            Title: "Night Forest",
            Tagline: "Crickets, owls and a gentle breeze",
            Icon: SceneIcon.NightForest,
            AudioAsset: "night-forest.m4a"),
        new SceneDescriptor(
            Id: "brown-white-noise",
            Title: "Brown & White Noise",
            Tagline: "Smooth noise to quiet a busy mind",
            Icon: SceneIcon.BrownWhiteNoise,
            AudioAsset: "brown-white-noise.m4a"),
        new SceneDescriptor(
            Id: "fan-and-room-hum",
            Title: "Fan & Room Hum",
            Tagline: "Steady fan drone for deep sleep",
            Icon: SceneIcon.FanRoomHum,
            AudioAsset: "fan-and-room-hum.m4a"),
    ]);
}
