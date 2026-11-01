# MineGrid

Unity 6000.3.10f1, URP 2D, uGUI/TextMeshPro and the new Input System. No additional package installation is required.

## Play
1. Open `Assets/Scenes/MineGrid.unity` and press Play.
2. Choose LET'S PLAY, then PLAY MINES.
3. Select 1–24 mines and press START GAME.
4. Reveal tiles for gems. FINISH GAME banks the current reward. A mine loses the unbanked reward; revealing all safe tiles banks automatically.
5. HOME during a round asks for confirmation. KEEP PLAYING cancels; LEAVE ROUND forfeits.

Each safe reveal adds **10 × selected mine count coins** by default. Three mines and four safe reveals bank **120 coins**. There is no entry fee or hidden win bonus. Coins are virtual. The HUD previews the next gem reward. Play Again retains the mine selection, including across restarts.

## Rewards and settings
- Daily wheel: six equal-probability rewards (50, 75, 100, 150, 200, 300 coins), one free spin per 24 hours. A pending reward and UTC cooldown save before animation; claim credits it once. Restarting recovers an unclaimed reward.
- Progression: gameplay-only lifetime points, numerically equal to banked gameplay coins. Thresholds: 500, 1,500, 3,000, 5,000, 10,000. Daily and milestone rewards do not advance progression.
- Settings: mute sound and reduce effects. Reduced effects suppress shake, reveal particles and confetti, and shorten wheel animation. No music track is included.

`Assets/MineGrid/GameConfig.asset` controls base scoring, wheel rewards and milestone values. Regenerate after changing array lengths so generated labels and UI rows match. Keep milestone thresholds and rewards the same length; wheel artwork is authored for six segments.

## Save data
Version 2 JSON is stored under PlayerPrefs key `MineGrid.Save.v2`: wallet, gameplay progression, completed round count, last completion ID, mine selection, mute/reduced effects, daily cooldown/pending coins and claimed milestones. The v1 balance, lifetime points, round count, mute and cooldown migrate automatically; the new milestone ladder starts with fresh claim flags because thresholds changed. Old awarded coins remain. Active rounds are intentionally not saved. Invalid JSON falls back to defaults; supported numeric fields are sanitized. Device clock and local save editing are not prevented.

## Authoring
No manual wiring is required. Outside Play Mode, **MineGrid > Generate Complete Game** regenerates the main scene, original procedural artwork and prefabs and adds the scene to build settings. This replaces generated files; preserve custom scene edits in a separate scene. If importing TMP Essential Resources triggers a domain reload, rerun generation after import completes.

## Verification
- **MineGrid > Verify Gameplay Rules**: custom deterministic rule/save checks; writes `MineGrid-verification.txt`. Saves are backed up and restored.
- Enter Play Mode, then **MineGrid > Run Play Mode Smoke Test**: connected UI callback checks; writes `MineGrid-playmode-verification.txt`. Saves are restored.
- Editor-only QA capture tooling checks square onscreen tiles at four resolutions and exercises the normal daily wheel animation/claim. Evidence is under `SprintDoc/Phase1Sprint2-Captures` and `MineGrid-layout-verification.txt`.

Sprint details, configuration changes, checks and limitations are recorded in `SprintDoc/Phase1Sprint2.txt`. These checks are not a substitute for physical touch-device, safe-area, performance or distribution-build testing. No store signing, publishing, backend or real-money systems are included.

## Phase 1 Sprint 3 visual polish

The connected scene now uses an original illustrated MineGrid logo, gold coin/cyan gem/navy mine sprites, Lilita One display type, and Varela Round body text. Fonts and full OFL license texts are bundled under `Assets/MineGrid/Fonts`; image-generation prompts and asset attribution are documented in `Assets/MineGrid/ASSET-SOURCES.txt` and `Assets/MineGrid/Art/Brand/GenerationPrompts.txt`.

`Assets/MineGrid/UITheme.asset` shares the sprites, fonts and theme colors. The scene generator retains the branded presentation, board frame, currency capsules, wheel art and milestone bars. `CoinFeedback` adds presentation after wallet changes; awarding coins still belongs to the existing save/rule flow. Reduced effects suppress coin flight and idle motion.

Before/after screenshots are under `SprintDoc/Phase1Sprint3-Captures`. Details are in `SprintDoc/Phase1Sprint3.txt`. Additional reports: `MineGrid-sprint3-layout-verification.txt` and `MineGrid-sprint3-visual-verification.txt`.
