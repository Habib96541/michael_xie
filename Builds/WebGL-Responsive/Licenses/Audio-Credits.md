# Sound effects

All clips in this folder come from **Kenney's Sound Pack** by Kenney (www.kenney.nl),
released under **Creative Commons Zero (CC0 1.0)** - https://creativecommons.org/publicdomain/zero/1.0/

CC0 places the works in the public domain: no attribution is required, and commercial use is allowed.
Credit is included here as a courtesy.

| File in project | Original file in the pack | Used for |
| --- | --- | --- |
| `ui_click.ogg` | `Interface Sounds/click_002.ogg` | SPIN button press |
| `wheel_spin.ogg` | `Foley Sounds/Woosh/woosh3.ogg` | spin start whoosh |
| `wheel_tick.ogg` | `Interface Sounds/tick_001.ogg` | ratchet tick as segments pass the pointer |
| `wheel_win.ogg` | `Interface Sounds/confirmation_002.ogg` | result chime when the wheel stops |

The pack's own readme states: "License: (Creative Commons Zero, CC0) ... This content is free to
use in personal, educational and commercial projects."

All four clips are wired through `WheelController` (`sfxSource`, `clickClip`, `spinClip`, `tickClip`,
`winClip`, `sfxVolume`, `minTickInterval`). Adjust or replace them there without touching code.

## Web audio variants

ui_click_web.wav and wheel_tick_web.wav are PCM WAV copies of the original
Kenney effects with 50 milliseconds of leading silence and 75 milliseconds
of trailing silence. The padding preserves the short attack when Unity
converts audio for WebGL. They use the same CC0 license as the source clips.
WheelWebBuildSetup creates the variants and assigns them to the scene.
