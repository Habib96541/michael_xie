# BRICK RUSH

A 90-second neon brick breaker built in **Unity 6000.3.10f1**, Universal Render Pipeline 17.3.0. The complete Unity project is this folder. No account, paid assets, or network service is used during play.

## Play locally

From this folder, run:

```powershell
python Tools/serve.py --port 8187
```

Open **http://localhost:8187/**. The WebGL entry point is **`Builds/BrickRush_WebGL/index.html`**. Use HTTP; opening the HTML with `file://` is unsupported.

## Controls and rules

- Mouse, touch drag, A/D, or arrow keys move the paddle. Space/click/tap launches a docked ball. Escape or P pauses.
- A 3-second countdown precedes the run. The active session timer is 90 seconds; pause and respawn countdowns freeze it.
- Start with 3 lives. During multiball, a life is lost only when the final ball falls. Any paddle return resets the shared combo.
- Standard / armored / explosive / bonus bricks award 10 / 25 / 20 / 50 base points. Armor needs two hits; explosions chain to neighboring bricks; striped steel is indestructible.
- Consecutive destructions award x2 at 3, x3 at 6, x4 at 10, x5 at 15. The destruction reaching a threshold receives the new multiplier. Score boost multiplies the result by 2.
- Final score = gameplay points + **100 per remaining life**. Best score and preferences persist locally in PlayerPrefs.
- Clear the field to continue into the next of eight layouts. Speed and brick types progress at 20, 45, and 70 seconds.
- Catch power drops: multiball (+2, max 5), wide paddle, piercing fireball, automatic limited lasers, slow ball, score x2, and one-use shield. Timed effects last 10 seconds; refreshing replaces the remaining duration. Drops have a 4-second cooldown (2.5 seconds in the finale).
- Settings provides independent music and SFX volume, screen-shake and reduced-effects controls. Audio begins after user interaction.

## Open and build

1. Open this folder in Unity **6000.3.10f1** with **Web Build Support** installed.
2. Open `Assets/BrickRush/Scenes/BrickRush.unity`. Press Play to run the same game as the browser build.
3. Select **Brick Rush > 3. Build WebGL** to rebuild `Builds/BrickRush_WebGL/`.

For a closed Editor, an equivalent batch build is:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\30 games\Brick Buster' -buildTarget WebGL -executeMethod BrickRush.Editor.BuildGame.BuildWebGL -logFile 'Logs\WebGLBatch.log'
```

The prepared scene, configuration, materials, fonts, post-processing profile and metadata are included. **Prepare project** regenerates the startup scene/settings through Unity APIs; it is only needed after recreating those assets. **Import typography resources** imports TMP essentials without an interactive dialog.

## Source map

- `Assets/BrickRush/Scripts/`: explicit composition, session flow, swept circle-cast balls, paddle/input, pooled brick waves and powers, scoring/persistence, VFX, audio, responsive uGUI.
- `Assets/BrickRush/Resources/GameConfig.asset`: duration, lives, speeds, power duration, ball cap and life bonus.
- `Assets/BrickRush/Shaders/`: lightweight animated grid/gradient and additive trail shaders.
- `Assets/WebGLTemplates/BrickRush/`: responsive loader, error page, fullscreen and touch handling.
- `Tools/generate_assets.py`: reproducible original raster shapes and synthesized WAV music/SFX. Requires Pillow and NumPy; retrieves OFL font sources from Google Fonts.
- `Assets/BrickRush/Fonts/`: Rajdhani Bold and Barlow Medium with their SIL Open Font License files. All other authored art/audio/code is original to this project.
- `Assets/BrickRush/Editor/RuleValidation.cs`: deterministic rules, layouts, swept-cast and brick-lifecycle checks. Run `BrickRush.Editor.RuleValidation.Run()` through the Editor.
- `Tools/browser-qa.cjs`: reproducible browser checks using installed Chrome/Edge. Run `npm ci --prefix Tools`, then `node Tools/browser-qa.cjs chrome` or `edge` with the local server running.

See **Docs/DEPLOYMENT.md** and **Docs/QA.md** for hosting and exact verification evidence. Generated local logs and test artifacts are in `Logs/` and `TestResults/`.

The release build applies Disk Size WebAssembly optimization, high managed stripping, smaller IL2CPP code generation, a 64 MB initial / 512 MB maximum heap, and gzip with decompression fallback. The loader handles compressed assets on ordinary static hosts. These settings are explicitly applied by the build method, including the optimization setting that Unity normally stores per machine.
