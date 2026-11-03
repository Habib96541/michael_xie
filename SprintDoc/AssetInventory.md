# Bullseye Blitz — reusable asset inventory

Phase 1, Sprint 1. Inspected 2026-11-03. Paths below are relative to the Unity project root. Read this inventory before repeating asset discovery. Preserve imported originals, GUIDs, documentation, and licenses; create game-specific prefabs separately.

## Selected visual direction

Use Violet Theme UI's smooth cartoon artwork, purple panels, gold coins, and warm accents. Reuse imported graphics before generating replacements. Selection below means a candidate for implementation, not an engine-tested asset.

## Primary reuse map

| Purpose | Existing asset | Implementation note |
| --- | --- | --- |
| Coin target and wallet icon | `Assets/Violet Theme Ui/Colored Icons/Coin.png` | Visually inspected. Add separate 10/20/50/100 labels; use a tight gameplay collider, independent of transparent image bounds. |
| Bomb target | `Assets/Violet Theme Ui/Colored Icons/Bomb.png` | Visually inspected: dark round body, fuse, spark. Add a clear danger symbol. |
| Main buttons | `Assets/Violet Theme Ui/Buttons/Button Purple.png`, `Button Yellow.png` | Purple secondary actions, gold primary actions. Purple importer has a sliced sub-sprite with borders 25/30/25/25; select the actual sub-sprite and verify slicing. |
| Menu backgrounds | `Assets/Violet Theme Ui/Panels/Dark Panel Purple.png`, `Dark Panel Violet.png` | Inspect slice borders before resizing; do not stretch entire artwork. |
| Settings layout reference | `Assets/Violet Theme Ui/Prefab/Settings.prefab` | Adapt layout into game-owned prefab; wire our own persistent controls. |
| Music/SFX controls | `Assets/Violet Theme Ui/Prefab/Slider/Slider Violet.prefab` | Reuse visuals, wire music and sound volume separately. |
| Reduced effects/vibration control | `Assets/Violet Theme Ui/Prefab/Toggles/Toggle Violet.prefab` | Vibration shown only when supported. |
| Results layout reference | `Assets/Violet Theme Ui/Prefab/Win.prefab`, `Lose.prefab` | Adapt visuals and replace wording with game results. |
| Confirmation dialog reference | `Assets/Violet Theme Ui/Prefab/Message.prefab` | Restart/Home confirmation must explain loss of pending coins. |
| HUD reference | `Assets/Violet Theme Ui/Prefab/Top Menu.prefab`, `Status.prefab` | Borrow graphic elements; build responsive gameplay layout. |
| Pause/Home/help icons | `Assets/Violet Theme Ui/White Icons/White Pause.png`, `White Home.png`, `White Information.png` | Decorative images must not add extra raycast blockers. |
| Audio icons | `Assets/Violet Theme Ui/White Icons/White Music.png`, `White Music Off.png`, `White Sound On.png`, `White Sound Off.png` | Settings visuals. |
| Timer/settings/back/close | `Assets/Violet Theme Ui/Colored Icons/Timer.png`, `Settings.png`, `Return.png`, `Close.png` | Existing consistent icon family. |
| Shot flash/spark | `Assets/Toon Muzzleflash Pack/Components/Spritesheets/Front/toon_muzzleflash_front_spritesheet_1.png` | Visually inspected: unequal frame sizes/placement. Never assume a regular grid. Use actual sprite rectangles. |
| Existing flash prefabs | `Assets/Toon Muzzleflash Pack/Example Prefabs/Toon Muzzleflash 1.prefab` through `Toon Muzzleflash 25.prefab` | Cosmetic only; inspect sorting, duration, material compatibility, and pooling reset. |

## Audio candidates — audition still required

- `Assets/Casual Game Sounds U6/CasualGameSounds/DM-CGS-01.wav` through `DM-CGS-50.wav`: 50 clips; numeric names do not establish sound roles. Audition and map UI, shot, hit, miss, bomb, countdown, and results explicitly.
- License retained at `Assets/Casual Game Sounds U6/CasualGameSounds/license.pdf`; contents not yet reviewed.
- `Assets/FREE CASUAL MUSIC PACK/CASUAL THEME 2/CASUAL THEME 2 - LOOP 1.wav`: initial music candidate, not yet auditioned.
- Other music families: CASUAL THEME 3, CASUAL THEME 4, RUNNING WILD, OTHERS, and TROPICAL GAME.
- `Assets/FREE CASUAL MUSIC PACK/BONUS 2 - SFX/POP 1.wav`, `SPECIAL COLECT 1.wav`, `FIREWORKS 3.wav`, `POWERUP 1.wav`: possible feedback clips; verify by listening.
- Publisher notes: `Assets/FREE CASUAL MUSIC PACK/WELCOME_CGMUSIC.txt` (Matheus Klein).
- Start playback after a user gesture; implement persistent volume controls in game code.

## Optional imports

- `Assets/Eric VFX Studio/Free Game VFX/Prefab/FX_Green_Hit.prefab`, `FX_Purple_Hit_02.prefab`: optional small hit feedback. Pack notes specify URP and Unity 2022.3.45f1. Current project is Unity 6000.3.10f1/URP; compatibility remains untested. Other effects include meshes and elaborate particles. Adapt only lightweight effects that fit 2D rendering; no 3D target simulation.
- `Assets/2DWeaponPack/Sprites/Glock_18/glock-18.png`: inspected; dark military style. Omit by default; visible weapons are not required.
- `Assets/RPG Consumables & Potions Icons Pack/03_Art/12_Treasure & Loot/`: optional chest/gift decoration. Potions, equipment, shops, and power-ups are outside this sprint.
- RPG pack contains eight demo scripts, including global `MusicManager`. Do not use its automatic-play music controller for this game's browser audio. Avoid class-name collisions with imported scripts.
- RPG music provenance: `Assets/RPG Consumables & Potions Icons Pack/Third-Party_Notices.txt`; names EnergySound/Pixabay for `Music.mp3`. Prefer the dedicated casual music pack for stylistic consistency.
- Preserve imported Store/Login/Bottom Menu prefabs, but do not expose nonfunctional shop/login controls.

## Create only these missing pieces

1. Cohesive carnival backdrop, curtains, booth frame, and three shelf fronts, with layering that hides rising/retreating targets correctly.
2. Original Bullseye Blitz title treatment.
3. Small crosshair and touch/miss feedback if existing icons are unsuitable at gameplay scale.
4. Game-specific reward labels, danger marking, countdown, and pooled feedback prefabs assembled from existing artwork.
5. Readable font selection using available Unity resources or a properly licensed font; no standalone TTF/OTF was found in imported assets.
6. Any audio role still missing after auditioning; do not infer audio suitability from filenames.

## Verification still needed

Inspect transparency, padding, pivots, frame rectangles, Web compression, sprite sorting, UI slices, and mobile readability in Unity. Audition audio and review supplied license documentation. Test selected VFX in the current URP renderer. Inventory inspection alone does not establish compilation, browser compatibility, or performance.

## Source baseline (before implementation)

Main scene `Assets/Scenes/SampleScene.unity` still contains only Main Camera and Global Light 2D. It is the only enabled build scene. No game-specific code has been implemented; current C# scripts belong to the RPG demo import. No AGENTS.md or root Git metadata was found during this inventory pass.

## Implementation selection

The game now uses Violet UI's coin, bomb, and sliced purple button sprites, plus FREE CASUAL MUSIC PACK's CASUAL THEME 2 - LOOP 1, POP 1, and SPECIAL COLECT 1. Imported demo scripts are not attached to the game. Procedural booth art, crosshair, and pooled feedback keep the first sprint lightweight; the other VFX/weapon/RPG packs remain preserved optional resources. The current playable scene is `Assets/Bullseye/Scenes/BullseyeBlitz.unity`. See `SprintDoc/Phase1Sprint1.txt` for current validation and delivery status.
