# Rain App (working title)

A single-screen sleep-sounds app for iOS and Android, built with .NET MAUI on .NET 10.
Pick one of seven looping **Scenes** (Rain on Window, Ocean Waves, Distant Thunderstorm,
Crackling Campfire, Night Forest, Brown & White Noise, Fan & Room Hum); audio keeps
looping with the screen off or the app backgrounded, an optional **Sleep Timer** fades
playback out on schedule, and a device-only **Nights** counter tracks days with real
listening. The full product spec lives in
[issue #1](https://github.com/Berencmgb/rain-app-fn/issues/1); `mockup.html` in this
repo is the design and motion source of truth. The app name and app id
(`com.companyname.rainapp`, in-app header "Driftly") are deliberate placeholders pending
a naming decision — see `GLOSSARY.md`.

## Repository layout

- `src/RainApp/` — the .NET MAUI app (`net10.0-ios`, `net10.0-android`): XAML shell,
  ViewModels, SkiaSharp tile icons, and the thin adapters (`Services/`,
  `Platforms/Android`, `Platforms/iOS`) that wire the app host to the domain.
- `src/RainApp.Session/` — the entire product behaviour as one pure C# module (no MAUI
  types, no platform APIs): playback, fades, Sleep Timer, Scene ring, Interruptions,
  Nights. This is the project's single test seam ([ADR-0003](docs/adr/0003-single-session-test-seam.md)).
- `src/RainApp.Animation/` — pure keyframe engine the icons share (ADR-0002).
- `tests/` — xUnit suites exercising only the Session and Animation seams with a fake
  clock and faked ports.
- `tools/audio/generate-placeholders.sh` — regenerates the synthesized placeholder loops.
- `docs/adr/` — decisions 0001 (CommunityToolkit MediaElement), 0002 (SkiaSharp icons),
  0003 (single Session test seam). Domain language: `GLOSSARY.md`.

## Prerequisites

Verified on Apple Silicon macOS with:

- **.NET SDK 10.0.402** with the MAUI workloads. `dotnet workload list` must show
  `maui` and `ios`; that configuration built both `net10.0-ios` and `net10.0-android`
  here. On a clean machine install them with `dotnet workload install maui`, and if the
  `net10.0-android` target still fails to resolve, add `sudo dotnet workload install android maui-android`.
- **Xcode** (27 here) with a booted iPhone simulator, for the iOS target.
- **Android SDK** (at `~/Library/Android/sdk` here: platforms 35/36, build-tools,
  platform-tools, emulator, an AVD — a Pixel 9 / API 35 image was used) and a **JDK**
  (Microsoft OpenJDK 25 was auto-detected). The build finds the SDK and JDK automatically
  in a standard layout; if yours are elsewhere, pass
  `-p:AndroidSdkDirectory=<sdk> -p:JavaSdkDirectory=<jdk>` to the build.
- **ffmpeg** (only to regenerate Scene placeholders; expected at
  `/opt/homebrew/bin/ffmpeg`, overridable with `FFMPEG=...`).

## Build, run, test

Everything from the repo root.

**Tests** (Session + Animation; no device needed):

```sh
dotnet test
```

**Android** — build the Release APK:

```sh
dotnet build src/RainApp -c Release -f net10.0-android
# → src/RainApp/bin/Release/net10.0-android/com.companyname.rainapp-Signed.apk
```

Install and launch on a running emulator or device (the activity's `crc64…` package
prefix is generated per build, so resolve it first):

```sh
~/Library/Android/sdk/emulator/emulator -avd <your-avd> &        # e.g. Pixel_9
~/Library/Android/sdk/platform-tools/adb wait-for-device
adb install src/RainApp/bin/Release/net10.0-android/com.companyname.rainapp-Signed.apk
adb shell cmd package resolve-activity --brief com.companyname.rainapp   # print the activity
adb shell am start -n com.companyname.rainapp/<activity>
```

Playback runs in a media foreground service; while playing you can confirm it with
`adb shell dumpsys media_session` (a `com.companyname.rainapp` session in state
PLAYING) and `adb shell dumpsys activity services com.companyname.rainapp`
(`communityToolkit.maui.media.services`, `isForeground=true`).

**iOS** — build, then install/launch on the booted simulator:

```sh
dotnet build src/RainApp -c Release -f net10.0-ios
xcrun simctl install booted src/RainApp/bin/Release/net10.0-ios/iossimulator-arm64/RainApp.app
xcrun simctl launch booted com.companyname.rainapp
xcrun simctl io booted screenshot now.png    # evidence, or just watch the simulator
```

**Both platforms, the flow to expect:** cold start reopens the last Scene selected but
never auto-playing; tap a Scene card and it fades in and loops forever; background the
app (or lock the phone) and audio continues with lock-screen/notification controls;
prev/next walk the Scene ring keeping timer and volume; the Sleep Timer (15/30/45/60)
counts down and fades out over ~10 s at expiry; on Android 13+ the first play asks for
notification permission without blocking playback.

## Scene audio files

Each Scene is exactly one bundled file under `src/RainApp/Resources/Raw/`:

```
rain-on-window.m4a      ocean-waves.m4a        distant-thunderstorm.m4a
crackling-campfire.m4a  night-forest.m4a       brown-white-noise.m4a
fan-and-room-hum.m4a
```

Swapping a placeholder for a real recording is drop-in: replace the file, same name, no
code changes. Real recordings must be gap-friendly and playable on both platforms
(AAC/MP3; not OGG) — the shipped files are seamless AAC loops made by crossfading each
file's tail into its head. To regenerate the synthesized placeholders:

```sh
bash tools/audio/generate-placeholders.sh
```

(The encoder is not byte-deterministic, so expect a diff on all seven files; the script
prints per-loop mean/max levels — keep them near −20 dB mean.)

## Architecture pointer

All product behaviour is a single pure module — `src/RainApp.Session/Session.cs` takes
intents (select, play/pause, next/prev, set-timer, set-volume, tick, interruption,
device-disconnected) and exposes observable state; everything else (the MediaElement
audio adapter, Preferences persistence, platform event bridges, the whole UI) is a thin
adapter behind the two injected ports. Rationale: `docs/adr/` (0001 playback engine,
0002 SkiaSharp icons, 0003 the test seam); vocabulary: `GLOSSARY.md`.
