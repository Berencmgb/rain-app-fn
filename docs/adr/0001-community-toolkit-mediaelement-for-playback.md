# CommunityToolkit MediaElement as the playback engine

.NET MAUI ships no built-in audio playback API (the framework proposal dotnet/maui#7152 was closed `not_planned` in Oct 2026), and this app needs looping background audio, lock-screen controls, and volume control on iOS and Android. We decided to build playback on `CommunityToolkit.Maui.MediaElement`, which wraps AVPlayer (iOS) and Media3/ExoPlayer (Android) and provides native looping, background playback (with `enableForegroundService: true`, which also manifest-merges the Android 14 media-playback foreground-service permissions), and now-playing metadata — instead of hand-maintaining per-platform AVAudioPlayer + MediaSessionService implementations. iOS still requires declaring the `audio` background mode in Info.plist and handling audio-session interruptions in platform glue.

## Consequences

- Looping must use the player's native loop (never re-`Play()` on MediaEnded — that produces an audible gap); Scene audio must ship as gap-friendly formats (AAC/MP3, not OGG).
- A single long-lived MediaElement instance is required; per-page players would deactivate the iOS audio session mid-playback.
- Platform-specific behaviors (Android FGS cannot start from background; OEM battery killers; iOS interruption and route-change events) are handled in a thin platform adapter, never in the domain layer.
