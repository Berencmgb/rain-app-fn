# SkiaSharp renders the animated Scene icons

The mockup's Scene icons are animated SVGs (falling drops, flickering flame, spinning fan, etc.) and the user chose full animation parity on both platforms. MAUI XAML cannot render SVG or its CSS keyframes, so we decided to port the scenes to SkiaSharp — SVG path data parses directly into Skia paths and the mockup's keyframe timings port 1:1 onto a rendering loop — rather than authoring a Lottie/SKottie asset pipeline (seven hand-authored Lottie JSON files, plus a conversion toolchain) or approximating with MAUI-native animations (option (b), rejected by the user in favor of full parity).

## Consequences

- Icon animation logic lives in a shared, platform-agnostic drawing layer; the mockup's HTML remains the animation source of truth for timing values.
- Animated tiles are opaque Skia views, so MAUI styling can't restyle them — acceptable, since each Scene owns its illustrated look.
