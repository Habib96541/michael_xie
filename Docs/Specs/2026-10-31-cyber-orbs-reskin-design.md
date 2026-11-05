# Cyber Orbs — Visual Reskin Design

**Date:** 2026-10-31
**Status:** Ready for review
**Project:** C:\Unity Projects\New\Bubble Pop (Unity 6000.4.9f1)
**Reference:** bubbleshooter.com screenshot + user-supplied 7-part Cyber Orbs art direction

## 1. Summary

Reskin the existing Unity bubble-shooter prototype into **Cyber Orbs**, a portrait 9:16 arcade game with a cyber-esports look. The gameplay rules stay the same: 10x10 board, 5 starting rows, match 3+ to pop, disconnected orbs fall, a row is added on the existing 10-second cycle, win by clearing the board, lose on row overflow.

The new game lives in a new scene and a new, fully organized project layout. The original sample is preserved and remains loadable. Deliverables are a verified Windows build, an Android APK, and the seven look-dev boards.

## 2. Approved decisions

- Visual reskin, not a full gameplay redesign.
- Portrait 9:16 reference (1080x1920), safe-area aware, plus an Android APK.
- Live HUD, unchanged rules. No fake 90-second countdown, opponent, tier, or rewards.
- Full standard project layout with all scenes in one folder and all sprites in one folder.
- Modern art pipeline: High-res orbs/background/VFX/trophy, rebuilt frame/HUD/turret.
- Original project content preserved; legacy scenes and assets stay usable.
- No git repository exists, so a full backup is taken before any moves.

## 3. Goals

- Make the game look and feel like a modern Cyber Orbs mobile bubble shooter.
- Keep the six-color match-3 rules, row pressure, aiming limits, score values, win and loss conditions intact.
- Give the game a fully clean, conventional Unity structure that a developer or designer can navigate.
- Keep the original sample untouched in content and still loadable after the migration.
- Verify visually and functionally before claiming completion.

## 4. Non-goals

- PvP, fake opponents, tournaments, matchmaking, or online services.
- Currency, rewards, progression, tiers, or a lobby economy.
- A 90-second round timer, time-based loss, miss-based row drops, or a combo score multiplier.
- Music, cloud saves, analytics, IAP, iOS delivery, or store publishing.
- Reusing the legacy scripts in the new runtime assembly. The rules are ported into clean, tested code.

## 5. Full project structure

```
Assets/
  Art/
    Sprites/                      ALL sprites, one folder, flat naming
      orb_cyan.png
      orb_magenta.png
      orb_lime.png
      orb_amber.png
      orb_violet.png
      orb_silver.png
      bg_arena.png
      ui_board_frame.png
      ui_panel_glass.png
      ui_timer_badge.png
      ui_combo_badge.png
      ui_meter_cell.png
      ui_button_disc.png
      ui_icon_restart.png
      ui_icon_sound_on.png
      ui_icon_sound_off.png
      ui_icon_pause.png
      ui_icon_info.png
      ui_trophy.png
      ui_app_icon.png
      fx_shockwave.png
      fx_shard.png
      fx_spark.png
      fx_glow_soft.png
      fx_aim_dot.png
      fx_hint_arrow.png
      ui_warning_strip.png
    Fonts/
      Orbitron-*.ttf
      ShareTechMono-Regular.ttf
      OFL-Orbitron.txt
      OFL-ShareTechMono.txt
    Materials/
      ui_additive.mat
    Shaders/
      UIAdditive.shader
  Audio/
    SFX/
      sfx_shoot.wav
      sfx_pop.wav
      sfx_drop.wav
      sfx_row.wav
      sfx_win.wav
      sfx_lose.wav
      sfx_ui.wav
  Prefabs/
    OrbView.prefab
    VfxBurst.prefab
  Scenes/
    CyberOrbs.unity              new build entry
    Bust a Move.unity            original, excluded from build
    UnitTests.unity              original test scene, excluded from build
  Scripts/
    CyberOrbs/
      CyberOrbs.Runtime.asmdef
      Core/
        BoardModel.cs
        BoardGeometry.cs
        OrbColor.cs
        GameRules.cs
      Gameplay/
        GameSession.cs
        ShotSimulator.cs
        AimController.cs
        ComboTracker.cs
        RowPressure.cs
        RunStats.cs
      UI/
        BoardView.cs
        HudView.cs
        ResultsModal.cs
        PauseOverlay.cs
        HelpOverlay.cs
        UiThemeApplier.cs
        SafeAreaFitter.cs
      FX/
        VfxService.cs
        SfxService.cs
        ScreenShake.cs
        Tween.cs
    Editor/
      CyberOrbs.Editor.asmdef
      CyberOrbsSceneBuilder.cs
      SpriteAssetProcessor.cs
      FontAssetBuilder.cs
      AudioClipBuilder.cs
      BuildTools.cs
    Tests/
      CyberOrbs.Tests.asmdef
      EditMode/
        BoardModelTests.cs
        ShotSimulatorTests.cs
        ComboTrackerTests.cs
        RowPressureTests.cs
        RunStatsTests.cs
  Settings/
    CyberOrbsConfig.asset
    CyberOrbsTheme.asset
  _Legacy/
    Resources/                    original prefabs and materials, load paths intact
      Prefabs/
      Materials/
    Scripts/                      original C# code
      Controller/
      Events/
      Extensions/
      Helpers/
      Model/
      Test/
      View/

Docs/
  Specs/
    2026-10-31-cyber-orbs-reskin-design.md
  LookDev/                        generated boards, not imported or shipped

Builds/
  Windows/
  Android/
  Captures/
```

### Structure rules

- Every project scene, including the original ones, lives in `Assets/Scenes/`. `Assets/_Recovery/0.unity` is a Unity-managed recovery artifact, is not a project scene, and is never used in builds.
- Every imported game sprite lives in `Assets/Art/Sprites/`; categories are encoded in file prefixes, not subfolders.
- Fonts, materials, and shaders are not sprites and keep their own subfolders.
- The new game never uses a `Resources` folder. Assets are referenced directly by the scene, prefabs, or settings.
- The legacy sample keeps `_Legacy/Resources/Prefabs` and `_Legacy/Resources/Materials` so its existing `Resources.Load` calls still resolve. No empty archive folders are created.
- New scripts are split into runtime, editor, and test assemblies. Legacy scripts continue to compile into `Assembly-CSharp`.
- Build artifacts and generated references stay outside `Assets/`. Look-dev boards are not imported or shipped.

## 6. Migration and preservation

1. Zip `Assets`, `ProjectSettings`, and `Packages` to `C:\Unity Projects\New\Bubble Pop_Backup_2026-10-31.zip` before any change.
2. Switch the project to force-text serialization before creating new scenes. Existing binary assets are not converted or rewritten.
3. Move `Assets/Resources` to `Assets/_Legacy/Resources`; move the legacy script folders to `Assets/_Legacy/Scripts`.
4. Move `Assets/Scenes/Test/UnitTests.unity` to `Assets/Scenes/UnitTests.unity`; remove the empty `Test` folder. Leave `Bust a Move.unity` in place.
5. Preserve `.meta` GUIDs by using Unity `AssetDatabase.MoveAsset`, never by moving files in Explorer.
6. Leave `Assets/_Recovery/0.unity` untouched; it is Unity-managed and excluded from the build.
7. Set the build scene list to `CyberOrbs.unity` only.
8. Confirm the original scene still loads and the original assembly still compiles after the moves.

PlayerSettings changes are required for the new build and are applied after the backup: product name `Cyber Orbs`, package id `com.cyberorbs.game`, portrait-only orientation, ARM64 with IL2CPP, default app icon from `ui_app_icon.png`.

## 7. Screen and interaction design

### 7.1 Layout (portrait, safe-area aware)

- Reference resolution 1080x1920, CanvasScaler Scale With Screen Size, match 0.5.
- Top safe area: glass HUD block containing a score readout with LED digits, an orange digital elapsed clock, and a combo badge that appears at 2+ consecutive popping shots.
- Second HUD row: a five-cell drop meter and four circular glass controls (Restart, Sound, Pause, Help).
- Board frame: chamfered dark-titanium border with cyan neon strips and a faint hex texture, occupying most of the screen width. Its inner area holds a 10-column board with true hex row packing. Initial five rows sit at the top; the lower interior is the descent corridor.
- Bottom area: swivel turret with a glowing chamber orb and a next-orb queue beside it. A dotted cyan aim line extends from the turret, showing wall bounces. A hint arrow appears when idle at start.
- Overlays: pause card, help card, and a solo results card. A dark translucent scrim covers the game; the game is paused while overlays are open.
- A subtle danger strip appears near the bottom of the frame as the board approaches the lose line; the frame edge pulses red when a row drop will overflow.

### 7.2 Controls

- Touch and mouse: press and drag to aim, release or tap to fire. Aim is limited to 85 degrees either side of straight up, matching the original.
- Buttons are above the input catcher and never fire the turret.
- Desktop: the same logic works with the left mouse button.
- Restart resets all run state immediately. Sound toggles SFX and updates its icon. Pause freezes gameplay, timers, and tweens behind a resume card. Help explains drag-to-aim, match-3, row pressure, and scoring.

### 7.3 Feedbacks

- Orb spawn scales in with an ease-out pop.
- Aim line dots fade with distance; bounces show a chevron at each wall.
- Popped clusters produce a shockwave ring, colored shards, a central flash, and a floating score callout.
- Two or more consecutive popping shots show `x2 COMBO`, `x3 COMBO`, and so on, with a scale punch.
- Row drops shake the board lightly, flash the frame, and play a low warning tone.
- A cluster of five or more adds a brief screen shake and a stronger flash.
- Score digits count up quickly rather than snapping.

### 7.4 HUD polish system

- **Grid:** 8px base unit, 24px outer safe padding, 16px component gaps, 12px button gaps, 32px panel radius, 1px cyan rim, 2px outer glow.
- **Type scale:** 22px uppercase labels, 56px score, 40px clock, 44px combo, 28px stat labels, 36px stat values, 84px modal headers, 44px button labels.
- **Touch targets:** control discs are 96px at the 1080px reference; no interactive target is smaller than 88px.
- **Score module:** Share Tech Mono tabular digits with thousands separators, cyan LED glow, 180ms count-up, 0.25-second flash on increase, and no leading zeros.
- **Clock module:** orange digital `mm:ss` elapsed time inside a circular badge; a thin ring fills once per minute. It never presents a fake countdown.
- **Combo module:** appears at `x2`, scale-punches in, uses magenta glow, remains visible while the chain is active, and fades in 240ms after a reset. It never overlaps the board.
- **Drop meter:** five cells fill over the 10-second cycle, shift to amber and pulse when armed, then reset when the row drops. The label reads `DROP`.
- **Buttons:** Restart, Sound, Pause, and Help use the same glass-disc state system: normal, pressed, disabled, and state-changed. Press scale is 0.92 for 80ms. Sound shows the correct on/off icon; Pause is disabled while results are open.
- **Panels:** dark glass with subtle noise and a cyan rim. No runtime blur is used; the frosted look comes from baked texture, opacity, and rim glow.
- **Modals:** 70% scrim, 220ms card entrance, aligned label/value stat rows, a highlighted best-score row, primary Play Again and secondary Quit buttons.
- **Turret polish:** the chamber glow matches the loaded orb color, the next orb slides in over 160ms, the turret recoils 6% on fire, and aim dots fade with distance.
- **Motion language:** micro feedback is 80-120ms, standard transitions are 180-260ms, entrances are 300ms. Use ease-out cubic by default; elastic easing is reserved for combo and trophy moments.
- **Visual QA:** no clipping or overlap at 16:9, 19.5:9, and 20:9; text contrast remains readable over the background; all button and modal states are captured during verification.

## 8. Visual direction

- Background: deep obsidian carbon with dim cyan circuit traces and a soft vignette. Readability first; no bright lines behind the board.
- Frame: dark brushed titanium, 45-degree chamfered corners, recessed cyan edge lighting, subtle inner hex grid.
- Orbs: six high-contrast colors with distinct internal cores: cyan diamond, magenta double ring, lime circuit veins, amber starburst, violet orbital swirl, silver triangular rune. Each has a consistent top-left highlight and outer rim aura. Shapes are also distinguishable without color.
- HUD: glassmorphism panels with thin cyan rims, sharp monospace numerals, orange digital clock, magenta combo accent, cyan drop cells.
- Turret: dark alloy base, glowing heat vents, loaded orb, next-orb queue, dotted laser sight.
- Results: frosted obsidian card, cyan border trim, trophy emblem, large VICTORY or MISSION FAILED header, stat rows, primary Play Again button, secondary Quit button. No opponent column.
- Palette: background `#070B12`, panel `#101823` at 90% opacity, cyan `#22E7FF`, magenta `#FF2BD6`, lime `#A6FF2E`, amber `#FFC23D`, violet `#9A6BFF`, silver `#D7E3F2`, text `#EAF6FF`, muted `#7F93A8`.
- Type: Orbitron for headings and labels; Share Tech Mono for score and clock digits. Both OFL-licensed and shipped with license files.

## 9. Art pipeline

### 9.1 Look-dev boards

Save the seven reference boards under `Docs/LookDev/`:

1. `01_full_screen_mockup.png`
2. `02_orb_sheet.png`
3. `03_board_frame.png`
4. `04_turret_kit.png`
5. `05_hud_kit.png`
6. `06_pop_vfx_sheet.png`
7. `07_results_modal.png`

These are references only. They are not imported into Unity or included in builds.

### 9.2 Game-ready art

- Custom created and processed for the game: six orb sprites, arena background, shockwave/shard/glow textures, trophy emblem, app icon.
- Generated orbs are keyed from black to alpha with edge feathering, trimmed, resized to 512x512, and checked on a contact sheet. If sheet cells are misaligned, the six orbs are regenerated individually.
- Rebuilt as scalable in-project assets: board frame, glass panels, badges, meter cells, buttons, icons, turret, aim dots, warning strip. They are generated by the editor sprite processor into `Art/Sprites/`, so every game sprite still lives in the single sprites folder.
- Fonts are fetched from the official Google Fonts repository with OFL license files.
- SFX are generated as short WAV assets and stored in `Audio/SFX/`. No copyrighted audio is used.

## 10. Rules and live systems

### 10.1 Preserved rules

- 10 rows x 10 columns, 5 initial rows, one orb is shot at a time.
- Six colors. Row parity alternates; each new row shifts the board down and flips the baseline side.
- A settled shot joins the matrix. A same-color cluster of 3 or more pops.
- Orbs disconnected from the top row fall.
- Score: +20 per popped orb, +25 per fallen orb.
- A row is queued after 10 seconds; it drops after the next shot settles, then the cycle restarts.
- Lose when the board overflows or a shot settles below the last row. Win when no orbs remain.
- Shot speed, wall bounces, ceiling stop, and the 85-degree aim limits match the original feel.

### 10.2 New live UI data

- Elapsed clock counts up; it is styled like the mockup's digital clock but is not a round timer.
- Drop meter: five cells fill over the 10-second cycle, then pulse as ARMED until the row drops.
- Combo: consecutive shots that pop; displayed at 2+; it resets on a shot that does not pop; it does not alter score; max combo is recorded.
- Next-orb preview shows the actual upcoming color; it is generated ahead and used for the next shot.
- Results stats: final score, elapsed time, orbs cleared, max combo, pop rate (popping shots / shots fired), and best score.

## 11. Technical architecture

- **UI-rendered board.** The playfield, orbs, turret, aim line, and VFX live in uGUI canvases. No 3D physics. This gives exact frame alignment, simple safe-area handling, and clean 9:16 scaling.
- **Deterministic shot simulation.** Each frame steps the shot orb, reflects it off walls, stops it on ceiling or orb contact, and snaps it to the nearest free legal cell. The preview line uses the same simulation, so it never lies.
- **Clean ported core.** `BoardModel`, `BoardGeometry`, and `GameRules` implement the original topology and rules in a new namespace. They have no dependency on legacy code.
- **Session flow.** `GameSession` owns run state and delegates view updates to `BoardView` and `HudView`. It starts immediately, handles shots, row cycles, win/lose, restart, pause, and results.
- **Config and theme.** `CyberOrbsConfig` holds board geometry, timings, speed, and aim limits. `CyberOrbsTheme` holds colors, sprites, fonts, sizes, and VFX references. Runtime code reads them; no magic values.
- **Scene builder.** `CyberOrbsSceneBuilder` creates the full UI hierarchy once, assigns serialized references, creates `OrbView.prefab` and `VfxBurst.prefab`, sets the build scene, and saves `CyberOrbs.unity`. It never runs automatically and never overwrites an edited scene without an explicit menu action.
- **Canvases.** Separate canvases for background, board/VFX, HUD, and overlays keep rebuild costs localized. Raycasts are disabled on non-interactive graphics.
- **Additive neon.** `UIAdditive.shader` and its material give glow sprites a true additive look without breaking UI batching.
- **Pooling.** Orbs, shards, rings, sparks, and score callouts are pooled; no per-shot allocations beyond small model objects.
- **Responsive.** CanvasScaler plus `SafeAreaFitter` and a max-width board support 16:9 through 20:9 screens.
- **Assemblies.** `CyberOrbs.Runtime` references uGUI and TextMeshPro. `CyberOrbs.Editor` references the runtime assembly and editor APIs. `CyberOrbs.Tests` references runtime and NUnit. Legacy scripts stay in `Assembly-CSharp`.
- **Packages.** Add built-in `com.unity.ugui` 2.0.0 and `com.unity.test-framework` 1.6.0 to the manifest. No other package changes.

## 12. Audio and persistence

- Synthesized WAV assets: shoot zap, crystal pop, drop whoosh, row warning, win sting, lose sting, UI click.
- SFX start enabled. The Sound button toggles and persists.
- Best score and mute state use PlayerPrefs keys under `CyberOrbs.`. Nothing resets existing user data.
- Pause sets `Time.timeScale = 0`; UI tweens and overlay animation use unscaled time; resuming restores the previous scale.

## 13. Testing and verification

### 13.1 EditMode tests

- Board creation dimensions, parity, and baseline side.
- Insert, row shift, parity flip, and overflow.
- Neighbor topology and cluster detection for 3+ matches.
- Loose-orb detection after popping.
- Shot wall bounce, ceiling stop, collision snap, and aim limits.
- Combo counting and reset behavior.
- Drop meter fill, armed state, and reset.
- Score, pop rate, max combo, and best-score handling.
- Aim preview matching the simulator path.

### 13.2 Visual and functional verification

- Build the Windows player and run the capture sequence for four states: fresh board, aiming with bounce preview, pop/combo VFX, and results modal.
- Inspect every capture against the look-dev boards; fix until readable, aligned, and free of clipping.
- Exercise restart, mute, pause, help, next-orb preview, win, lose, and safe areas in the player.
- Run EditMode tests in batch mode and require zero failures.
- Build the Android APK, verify package id, version, and portrait manifest with build tooling, and install it if a device becomes available.

## 14. Build and delivery

- Windows: `Builds/Windows/CyberOrbs.exe`, development build with capture mode.
- Android: `Builds/Android/CyberOrbs.apk`, ARM64 IL2CPP, debug signing, portrait.
- Captures: `Builds/Captures/`.
- App name: `Cyber Orbs`; package id: `com.cyberorbs.game`.
- A short `README.md` at the project root documents the structure, scene to open, build commands, and legacy archive.

## 15. Risks and mitigations

- **Sprite sheet misalignment:** test the orb sheet immediately; fall back to individual orb generation.
- **Legacy reference breakage:** backup, GUID-preserving moves, verify the original scene and assembly after migration.
- **TMP/font setup:** package is available in the installed Unity; create font assets in the editor builder and verify text renders in the first capture.
- **Batch-mode licensing/build issues:** prove a trivial Windows build early, before deep implementation.
- **UI performance on Android:** separate canvases, pooling, no masks, no raycasts on decorative graphics; target 60 FPS.
- **Layout on tall screens:** SafeAreaFitter plus max-width board; verify two aspect ratios.
- **Overwriting hand-authored work:** scene builder is manual-only; the original scene and the Unity recovery scene are never rewritten.

## 16. Acceptance criteria

- `CyberOrbs.unity` runs in portrait with the Cyber Orbs look: obsidian arena, neon frame, six distinct orbs, turret and dotted aim, glass HUD, pop VFX, and solo results card.
- All HUD values are live; no fake opponent, timer, tier, or reward.
- The HUD meets the polish system: consistent 8px grid, defined type scale, 96px controls, animated score/clock/combo/drop meter, complete button states, and no clipping on 16:9 through 20:9 screens.
- Gameplay rules match the original as proven by EditMode tests.
- Original scene content is preserved and still loadable; it is excluded from builds.
- All project scenes are in `Assets/Scenes/`; all imported game sprites are in `Assets/Art/Sprites/`.
- New code is split into runtime, editor, and test assemblies; settings live in ScriptableObjects.
- Windows captures verified; EditMode tests pass; Android APK produced.
- No legacy content is lost, overwritten, or reset.
