#!/usr/bin/env bash
# Generate the per-Scene placeholder audio loops bundled with the app.
#
# Each Scene ships as ONE swappable file under src/RainApp/Resources/Raw/:
# replacing the file (same name) swaps the sound with no code changes.
# Loops are made seamless by crossfading the tail into the head
# (acrossfade of the signal with itself), so native looping wraps inaudibly.
# Format: AAC in .m4a — gap-friendly, supported on iOS and Android (no OGG).
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=src/RainApp/Resources/Raw
FF=${FFMPEG:-/opt/homebrew/bin/ffmpeg}
D=24       # source duration, seconds
X=4        # crossfade length, seconds

# Rain on Window: low pink-noise rumble behind a light high patter on glass.
"$FF" -y \
  -f lavfi -i "anoisesrc=color=pink:d=$D:sample_rate=44100:amplitude=0.5,lowpass=f=900" \
  -f lavfi -i "anoisesrc=color=pink:d=$D:sample_rate=44100:amplitude=0.22,highpass=f=1500,lowpass=f=7000" \
  -filter_complex "\
amix=inputs=2:duration=longest:normalize=0[mix];\
[mix]alimiter=limit=0.9[m];\
[m]asplit=2[head][tail];\
[tail]atrim=start=$((D-X)):duration=$X,asetpts=PTS-STARTPTS[tailcut];\
[head][tailcut]acrossfade=d=$X:c1=tri:c2=tri,atrim=duration=$((D-X))[loop]" \
  -map "[loop]" -c:a aac -b:a 160k "$OUT/rain-on-window.m4a"

echo "wrote $OUT/rain-on-window.m4a"
