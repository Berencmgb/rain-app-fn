#!/usr/bin/env bash
# Generate the per-Scene placeholder audio loops bundled with the app.
#
# Each Scene ships as ONE swappable file under src/RainApp/Resources/Raw/:
# replacing the file (same name) swaps the sound with no code changes.
# Loops are made seamless by crossfading the tail into the head
# (acrossfade of the signal with itself), so native looping wraps inaudibly.
# Format: AAC in .m4a — gap-friendly, supported on iOS and Android (no OGG).
#
# Each Scene is synthesized from the spec's audio notes — layered/filtered
# noise (anoisesrc + lowpass/bandpass) plus shaped or gated sources where the
# Scene calls for it (thunder swells, fire crackle, cricket chirps, owl calls,
# blade-pass flutter). Keep the levels of the seven loops close: the report at
# the end prints mean/max levels; aim for mean around -20 dB.
#
#   rain-on-window       low pink rumble behind a light high patter on glass
#   ocean-waves          deep swell + surf hiss, breathing on a slow LFO so
#                        each wave rises, breaks (a few seconds later) and falls
#   distant-thunderstorm muffled rain bed; rolling thunder swells every ~10 s
#   crackling-campfire   warm low flame bed with sparse bright crackle bursts
#   night-forest         breeze under two cricket chirp-layers + rare soft owl
#   brown-white-noise    smooth brown base with a thin white top, deliberately
#                        featureless — the flat one of the set
#   fan-and-room-hum     low motor tone + harmonics with blade-pass flutter
#                        behind airy room noise
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=src/RainApp/Resources/Raw
FF=${FFMPEG:-/opt/homebrew/bin/ffmpeg}
D=24       # source duration, seconds
X=4        # crossfade length, seconds

# Common tail: limit, then crossfade the last X seconds into the head so the
# wrap is inaudible. A Scene graph ends by labelling its mix [pre].
WRAP="[pre]alimiter=limit=0.9[m];\
[m]asplit=2[head][tail];\
[tail]atrim=start=$((D-X)):duration=$X,asetpts=PTS-STARTPTS[tailcut];\
[head][tailcut]acrossfade=d=$X:c1=tri:c2=tri,atrim=duration=$((D-X))[loop]"

make_loop() { # $1 = file name, $2 = filter graph emitting [pre]
  "$FF" -hide_banner -loglevel error -y \
    -filter_complex "$2;${WRAP}" \
    -map "[loop]" -c:a aac -b:a 160k "$OUT/$1.m4a"
  echo "wrote $OUT/$1.m4a"
}

noise() { # shorthand for a repeatable pink/brown/white source at the app rate
  echo "anoisesrc=color=$1:d=$D:sample_rate=44100:amplitude=$2"
}

# Rain on Window: low pink-noise rumble behind a light high patter on glass.
make_loop rain-on-window "\
$(noise pink 0.5),lowpass=f=900[a];\
$(noise pink 0.22),highpass=f=1500,lowpass=f=7000[b];\
[a][b]amix=inputs=2:duration=longest:normalize=0[pre]"

# Ocean Waves: a brown-noise swell under pink surf hiss; both breathe on a slow
# (~11 s) LFO, the hiss cresting a beat after the swell — a wave rolling in,
# breaking, and falling back.
make_loop ocean-waves "\
$(noise brown 0.85),lowpass=f=180[bass];\
$(noise pink 0.6),highpass=f=250,lowpass=f=1000[surf];\
[bass]volume='0.55+0.45*sin(2*PI*t/11)':eval=frame[bassw];\
[surf]volume='0.10+0.55*pow(0.5+0.5*sin(2*PI*(t-3)/11),2)':eval=frame[surfw];\
[bassw][surfw]amix=inputs=2:duration=longest:normalize=0[pre]"

# Distant Thunderstorm: a light muffled-rain bed; two distant thunder rolls —
# brown noise under a steep lowpass, swelled by slow raised-sine envelopes at
# offset periods and slowly modulated so each roll wanders and rumbles.
make_loop distant-thunderstorm "\
$(noise pink 0.3),highpass=f=250,lowpass=f=750[rain];\
$(noise brown 0.95),lowpass=f=110,\
volume='pow(sin(PI*mod((t+1.5)/10.5,1)),3)*1.2':eval=frame,tremolo=f=0.7:d=0.7[t1];\
$(noise brown 0.95),lowpass=f=70,\
volume='pow(sin(PI*mod(t/16.3,1)),5)':eval=frame,tremolo=f=0.5:d=0.8[t2];\
[rain][t1][t2]amix=inputs=3:duration=longest:normalize=0[pre]"

# Crackling Campfire: a warm low flame bed breathing on two slow LFOs; crackles
# are bandpassed white noise opened by sparse per-frame random gates, so the
# pops land at irregular moments with no two loops alike.
make_loop crackling-campfire "\
$(noise pink 0.7),lowpass=f=420,\
volume='0.45+0.16*sin(2*PI*t/3.1)+0.11*sin(2*PI*t/1.7+1.3)':eval=frame[flames];\
$(noise white 0.9),highpass=f=1400,lowpass=f=6500,\
volume='if(lt(random(1),0.04),0.85,0)':eval=frame[cracks];\
[flames][cracks]amix=inputs=2:duration=longest:normalize=0[pre]"

# Night Forest: a gentle filtered-breeze bed; two cricket layers — near-pure
# tones amplitude-modulated into chirps (~22-26 Hz) and gated on and off at
# different cycles; a soft owl call (low vibrato tone, lowpassed) twice in the
# loop. Crickets and owl stay quiet enough to sit under the breeze, not over it.
make_loop night-forest "\
$(noise pink 0.25),bandpass=f=450:width_type=h:w=500,\
volume='2.8+1.2*sin(2*PI*t/9.5)':eval=frame[breezes];\
sine=frequency=4700:sample_rate=44100:d=$D,tremolo=f=26:d=1,\
volume='between(mod(t,2.8),0.15,1.05)*0.5':eval=frame[crickets_a];\
sine=frequency=5250:sample_rate=44100:d=$D,tremolo=f=22:d=1,\
volume='between(mod(t+1.1,3.6),0.2,1.35)*0.32':eval=frame[crickets_b];\
sine=frequency=440:sample_rate=44100:d=$D,vibrato=f=5.5:d=0.35,lowpass=f=900,\
volume='(between(t,5.2,5.75)+between(t,6.0,6.4)+between(t,17.3,17.9)+between(t,18.2,18.55))*0.4':eval=frame[owl];\
[breezes][crickets_a][crickets_b][owl]amix=inputs=4:duration=longest:normalize=0[pre]"

# Brown & White Noise: a deep brown base kept smooth (gentle band limits, no
# LFOs — the point is that nothing moves) with a thin veil of white top.
make_loop brown-white-noise "\
$(noise brown 0.5),highpass=f=30,lowpass=f=280[brn];\
$(noise white 0.05),highpass=f=900[wht];\
[brn][wht]amix=inputs=2:duration=longest:normalize=0[pre]"

# Fan & Room Hum: a low motor tone plus two harmonics (an electric-fan timbre)
# fluttered at blade-pass rate (~31 Hz), behind airy filtered noise breathing a
# little slower — the air moving as the blades cut it.
make_loop fan-and-room-hum "\
sine=frequency=96:sample_rate=44100:d=$D,volume=0.5[h1];\
sine=frequency=192:sample_rate=44100:d=$D,volume=0.16[h2];\
sine=frequency=288:sample_rate=44100:d=$D,volume=0.06[h3];\
[h1][h2][h3]amix=inputs=3:duration=longest:normalize=0,\
tremolo=f=31:d=0.22[blades];\
$(noise brown 0.5),lowpass=f=350,volume=0.6,tremolo=f=27:d=0.3[air];\
[blades][air]amix=inputs=2:duration=longest:normalize=0[pre]"

# Rough loudness report — the seven loops should sit near the same mean level.
echo
for f in rain-on-window ocean-waves distant-thunderstorm crackling-campfire \
         night-forest brown-white-noise fan-and-room-hum; do
  echo "$f"
  "$FF" -hide_banner -i "$OUT/$f.m4a" -af volumedetect -f null - 2>&1 |
    grep -E "mean_volume|max_volume" | sed 's/.*\] //'
done
