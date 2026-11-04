# Rain App (working title)

Sleep-sounds app: pick a sound scene, drift off. Single context. Shipped name TBD; currently Rain App / `com.companyname.rainapp` as placeholder.

## Language

**Scene**:
A named, continuously looping ambient sound (e.g. Rain on Window). Every Scene loops forever; nothing about a Scene has a fixed length.
_Avoid_: track, sound card, preset

**Sleep Timer**:
An optional user-chosen duration after which playback ends. When no Sleep Timer is active, playback continues until the user stops it. The only element of the UI that expresses time is the active Sleep Timer countdown.
_Avoid_: countdown, alarm

**Night**:
A calendar day on which the user played sound for a cumulative 10 minutes. The Nights count is a lifetime total of Nights, kept on-device only.
_Avoid_: streak, session

**Mixing**:
Playing multiple Scenes at once with per-Scene volume. Deliberately not part of v1: only one Scene plays at a time, with a single master volume.

**Interruption**:
An outside event taking audio away from the app. Temporary interruptions (calls, Siri) hand audio back, and playback resumes with a gentle fade-in; a device disconnect (headphones/Bluetooth) does not auto-resume — playback stays paused until the user resumes it. Any resume, manual or automatic, fades in rather than starting abruptly.
_Avoid_: pause event
