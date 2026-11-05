# Bloom & Doom — Unity game

This is the native Unity project for the complete garden-defense game. The scene played in Unity and the Cloudflare WebGL game use the same C# code and assets.

## Open and play

Open this folder with Unity **6000.3.10f1**, open `Assets/Scenes/SampleScene.unity`, and press **Play**. The scene contains **Bloom & Doom - Native Game**. No separate browser app or launcher is involved.

The game includes six campaign days, endless survival, eight plants, six enemy types, animated characters, sunshine, shields, explosives, mowers, music and sound effects, settings, and saved progress.

A custom plants-and-zombies title logo appears during loading. The browser shows actual download progress and waits for Unity's first ready frame before revealing the menu. Unity Play mode shows the same logo as a short startup intro. Reduced-motion preferences are respected.

## Project layout

- `Assets/BloomDoom/Runtime`: the game, deterministic simulation, native UI, audio, artwork loading, and pointer input.
- `Assets/BloomDoom/Editor`: scene configuration, builds, and native gameplay checks.
- `Assets/Resources/BloomDoom`: the artwork atlases, garden backgrounds, icons, and bundled fonts actually used by the game.
- `Assets/Scenes/SampleScene.unity`: the game scene.
- `Assets/WebGLTemplates/BloomDoom`: the Unity WebGL loading page.
- `Assets/Plugins/WebGL/GardenLoading.jslib`: the first-frame readiness signal used by the loading page.
- `Tools/Art`: the source illustration generator. Run `npm install`, then `node generate.mjs` there only when regenerating artwork.
- `Builds/WebGL`: the generated Unity browser build. Only this folder is deployed to Cloudflare.

Git includes the playable build in `Builds/WebGL`. Unity regenerates `Library`, `Temp`, and other local build outputs.

## Validate and build

Use **Bloom & Doom → Run Gameplay Checks** for the native simulation checks, including a strategy that clears all six campaign days.

Use **Bloom & Doom → Build WebGL** with Unity 6000.3.10f1 and WebGL Build Support installed, or run `Tools/Build-Web.ps1`. Publish the contents of `Builds/WebGL` to your own static web host.

## Controls

Choose a seed, then click or tap a lawn patch. Collect sunshine by clicking it. Keys **1–8** select seeds; **S** selects the shovel; **C** collects all sunshine; **Space** sends the next wave; **Escape** pauses. Arrow keys select a patch and **Enter** plants. Right-click deselects.

The soundtrack and effects are original synthesized audio. Lilita One and Nunito font licenses are included beside the fonts.

## Client delivery — original assets, version 2.1.1

This independent copy preserves the original cartoon plant atlases and garden-card main menu from before the papercraft asset update. It includes the logo loading screen. Open this folder in Unity 6000.3.10f1, open Assets/Scenes/SampleScene.unity, and press Play.

The client WebGL player with the original artwork and the 2.1.1 landscape layout is included in Builds/WebGL. Serve this folder over HTTP with node Tools/serve.mjs, or upload its contents to your own static web host. Browser builds need a web server.

The live-site deployment script and account metadata are intentionally omitted from this client copy. The newer working project and its live deployment are separate. Unity regenerates Library, Temp and solution files when you open this project; those caches are not included in the handoff.

The client WebGL edition uses a **2.17:1** game and loading frame on iPhones. Version **2.1.1** also reflows the native Unity HUD and widens the lawn to fill landscape screens, retaining proportional characters, readable menus and accurate patch input. Native iPhone Device Simulator safe areas and the browser notch/home-indicator margins are respected. The Unity WebGL template retains the browser setting for future builds. Run `node Tools/verify-iphone-layout.mjs` to check the phone frame, or `node Tools/preview-iphone.mjs` for a local browser simulation at `http://127.0.0.1:8082/?iphone`. A development Windows build launched with `--garden-layout-smoke` checks the actual native layout, rotation, all 45 patch targets and character proportions.
