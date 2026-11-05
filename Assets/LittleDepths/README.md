# Little Depths

Open `Assets/Scenes/LittleDepths.unity` and press **Play** in Unity 6000.3.10f1.
The scene is ready to play; no player build is needed or was generated.

## Controls

- Click **PLAY**, then **CAST!**. Stop the marker in the gold section to reach the full equipped depth.
- Hold and drag the mouse or a finger horizontally to steer. **A/D** and **Left/Right** work too.
- Touch swimming fish with the hook to collect them on the way down or up.
- A full hook or the target depth starts the return automatically. **REEL IN** or **Space** returns early.
- **Space/Enter** also starts and releases a cast. **Escape** opens settings or closes a regular menu.
- Each session has five casts. Catch results sell automatically; **FISH AGAIN** begins another session.

## Included gameplay

Ten species from common to legendary, swimming schools, depth-based populations, golden fish,
catch feedback, bubbles, synthesized sound effects, capacity and depth upgrades, passive income,
collection milestones, a field guide/aquarium, six collectible hook colors, a free prize every four
hours, persistent progression, best-haul tracking, and eight-hour-capped offline rewards.
Occasional discarded boots occupy hook capacity and clean the lake without paying coins.

The five-cast session rule comes from the supplied breakdown. Catching uses the steering-based
arcade loop shown in the screenshots, with collection on descent and ascent.

## Editing

- `Data/FishingBalance.asset`: species, rewards, rarity weights, depth ranges, speeds, capacity,
  upgrade limits, offline cap, and hook colors. Balance is exposed in the Inspector.
- `Scripts/FishingGame.cs`: casting, contact collection, reeling, sale, upgrades, and sessions.
- `Scripts/FishingWorld.cs` and `FishActor.cs`: populations, movement, water effects.
- `Scripts/FishingHUD.cs` and `FishingHUDV2.cs`: glossy reference-based UI, menus, and customization.
- `Scripts/GameProgress.cs`: JSON persistence in PlayerPrefs, key `LittleDepths.Progress.v1`.
- `Editor/FishingSceneSetup.cs`: rebuilds the designed scene via **Little Depths > Set up fishing scene**.
  This replaces the generated scene; save any custom scene edits elsewhere first.
- `Tools/generate_art.py` (at the project root): editable original artwork and audio generation.
  Requires Python, Pillow, aggdraw, and fonttools. Existing art is already included.

The scene hierarchy contains editable scenery, camera, hook, fishing line, Canvas, HUD controls,
EventSystem, and game components. Fish, catch particles, and modal menu contents spawn in Play mode.
The original template `SampleScene` is preserved. `LittleDepths` is the enabled startup scene.
The layout is designed for landscape; Unity's Canvas Scaler handles different landscape sizes.

## Validation without a player build

Run Unity with `-batchmode -projectPath "<project>" -executeMethod
LittleDepths.Editor.FishingSmokeTests.Run -littleDepthsSmoke -logFile "<log>"`.
Do not add `-quit`: the runner exits after the Play mode checks finish.
It verifies persistence, offline limits, the live catch/sell loop, upgrades, capacity, cosmetics,
prize cooldown, and session restart. Test progression uses a separate disposable save slot.
Screenshots go to the ignored `Logs/Previews` folder.

All fish and scenery are original vector-style artwork generated for this project. Nunito is
licensed under the SIL Open Font License; the license is included in `Fonts/OFL.txt`.


## Blue UI and sprite animation update

The two supplied UI concept sheets are implemented as interactive Unity UI: blue framed panels,
wooden counters, a circular orange Play control, green upgrade buttons, rarity-framed animated
fish gallery, a nine-card prize board with a flip reveal, and a catch summary. Prices and counters
come from the existing game economy.

**CREATE ANGLER** opens a live preview with three outfits, three hairstyles, three optional hats,
and three boats in their original artwork colors. **SAVE** persists the choices and updates the fishing scene;
**CANCEL** leaves progression and appearance unchanged. Existing saves remain compatible.

`Art/SpriteAtlases/FishSwimAtlas.png` contains forty transparent sprite frames. The ten species
have separate looping `AnimationClip` and `AnimatorController` assets in `Animations`. The
angler rig has Idle, Cast, Reel, and Celebrate clips; its rod tip is the fishing line anchor.
Boat bobbing, catch wiggles, water bubbles, button hover/press, popup entrance, animated gallery
previews, and prize-card flips complete the animation layer.

`Editor/ReferenceSpriteImporter.cs` slices sprites by connected alpha regions, with consistent
frame sizes per species. `Tools/generate_ui_v2.py` draws the nine-sliced UI elements. The
existing world art remains editable. The two transparent atlases were generated with the built-in
imagegen tool using the first supplied sheet as a style and character reference; see
`Art/SpriteAtlases/GenerationPrompts.txt` for the prompt specifications.

When the project is already open, `Editor/FishingEditorBridge.cs` can read a one-shot command
from the ignored `Logs/LittleDepths.command`: `setup`, `test`, or `setup-and-test`. The setup
backs up a dirty active scene before replacing the generated fishing scene. Validation stays
inside the open editor and uses its isolated test save slot; it never creates a player build.

## Complete character, hat and boat sprites

`Art/IntegratedAnglers` contains nine transparent source atlases and the saved imagegen prompt
set. Every atlas belongs to one outfit/hairstyle and contains twelve complete illustrations:
four headwear choices across the columns and three occupied boats down the rows. The full
set covers 108 distinct combinations. Hats are drawn worn around the head, and girls are drawn
sitting on their boat benches behind the near gunwale, with both hands holding the rod/reel.

`Editor/IntegratedAnglerImporter.cs` imports each complete painted subject by its connected
alpha bounds rather than cutting on approximate grid lines. `FishingConfig.CompleteIndex`
selects the full girl/headwear/boat sprite. `AnglerRig` has one SpriteRenderer; the creator has
one `SpritePartImage`. There are no separate body, hat, hair, hand or boat overlays in either
preview or gameplay. A hat or boat choice swaps the entire illustration.

The complete sprite bobs and rocks together, with Idle, Cast, Reel and Celebrate Animator
states. The fishing line follows the painted rod tip in each selected sprite. The paint palette
remains removed; old saved outfit, hairstyle, headwear and boat selections continue to work.
The earlier `Seated_*.png` and kit atlases remain source references and UI option icons.

Play mode checks verify 108 unique whole sprites, the single gameplay renderer, all 108 creator
selections, save/cancel and persistence, the live fishing loop and all 40 fish mouth frames.
`Logs/Previews/18-CompleteBoat1.png`, `19-CompleteBoat2.png`, and `20-CompleteBoat3.png` show every
look. `16-CleanBobBucketPreview.png` shows the actual creator with the new complete artwork.

## Mouth attachment

Caught fish pivot around their mouths on the hook barb. Their four-frame swimming animations
keep the mouth anchored while their bodies swing below it. `17-SingleFishMouthAttachment.png`
shows the rendered result. No player build is generated by setup or validation.

## Remastered UI and environment

`Art/ProvidedSheets` preserves the original supplied JPGs. `Art/RemasteredSheets` contains the
untouched, reference-guided PNG remasters generated with the built-in image_gen tool, plus the
exact prompt set in `GenerationPrompts.json`. The restoration follows the original layouts,
palette, framing and icon subjects; generated remasters are not pixel-identical copies.

The active HUD, dock, character creator, upgrades, prizes, results, pause and quest panels are
separate large sprites rather than enlarged small crops. The HUD is 2186×358, character panel
1599×972, dock 1987×780 and mountains 2158×444. Import settings retain native dimensions with
a 4096 maximum, no texture compression and full rectangular sprite meshes. Large panels
use bilinear sampling without mipmaps; small navigation icons and minified foreground
cutouts use trilinear mip sampling to avoid jagged outlines. The original source files remain
available for future editing.

Run `python Tools/import_remastered_art.py` to reproduce the 51 processed sprites under
`Art/Provided`. `RemasterManifest.json` records their actual native sizes and sources. This
processing slices the generated art, removes atlas mattes, adds transparent margins, and
prepares reusable blank faces from those same images. It does not enlarge the results or draw
new designs. The old extractor is guarded against accidentally overwriting the remasters;
its explicit `--restore-original` option deliberately returns to the original JPG crops.

Panel interiors are opaque; alpha is reserved for their outside silhouettes and antialiasing.
Quest row text, completion checks and progress bars are live over a generated clean panel,
so the game does not cover printed sample text with visible rectangular patches.

Live values sit in blank fields authored into the new HUD/dock/results sprites. The character
preview is also deliberately blank for the complete seated girl/hat/boat sprite. The arrows
cycle hairstyles, Outfit cycles clothing, Hat cycles headwear, the clearly labeled BOAT button
cycles boats, SELECT saves and BACK cancels. The color palette stays removed. Hooks navigation
uses a real J-shaped hook icon instead of the previous accessory/belt illustration.

Mountain peaks, trees, shore islands, clouds, bushes and rocks use remastered environment art.
The mountain cutout has no stray canyon sliver and clean transparent sky. Bank coverage extends
beyond 16:9 and ultrawide camera edges. The line still connects the painted rod tip vertically
to the hook eye, and fish remain attached at their mouths.

Play-mode validation checks native sprite resolution/import settings, transparent padding,
runtime Play/menu/boat controls, all 108 whole character combinations, all 40 mouth-aligned
fish frames, 16:9/21:9 edges, five casts, upgrades, economy and persistence. It never builds a
player. `Logs/Previews/09-CharacterCreator.png`, `16-CleanBobBucketPreview.png`, and
`21-ProvidedHarbor16x9.png` through `25-ProvidedHarbor21x9.png` are rendered previews.

The existing speed-upgrade card buys +15% cast/reel speed per level. The video reward artwork
is locked because the project has no advertising integration.

## Window close, daily quests and event cards

Every popup uses the same blue cross sprite in its top-right corner, including character,
prize, results, pause, upgrades, missions, collection, hooks, welcome and session windows.
Closing the results returns to the harbor; closing customization discards the unsaved draft.
The results' labeled HOME button remains a separate navigation action. New art was created
with built-in image_gen; the exact prompts are in `Art/RemasteredSheets/WindowAndEventPrompts.json`.

Daily quests have their own saved counters, separate from lifetime totals. Land 10 fish for
$100, reach 15m for $150, land 5 different species for $125, and make 5 casts for $100. A species
already known in the gallery still counts toward today's variety. Trash never counts as fish.
The mission panel displays progress, a claim button per reward, the claimed state, and the
time until midnight UTC. Each reward pays once per day; progress and claims reset on the next
UTC day, including while the panel is open. A clock rollback cannot reopen old claims.

The event background is intentionally empty where the nine separate card images are drawn.
The flip changes one image's sprite at zero width, and uses a clean white reward face with live
text inside its border. No old yellow tile or printed sample value remains underneath. The
selected card/reward and four-hour cooldown are saved, so reopening or reloading restores the
same revealed card without granting a second prize. Closing during the flip is safe.

`WindowAndDailyChecks` covers migration from old saves, actual daily counters, reward thresholds,
unique species, duplicate claims, save/reload, midnight reset, rollback protection, close button
placement/action and event card layering. The Play checks also click a daily reward after five
real casts. `07b-PrizeRevealed.png` and `26-DailyQuestClaimed.png` show the final runtime states.

## Standalone foliage and crafted fishing controls

The pine cluster, flowering oak, fruit tree and two grassy shores now use their own large
generated illustrations instead of crops from the small environment atlas. The source PNGs
end in `_hd.png` under `Art/RemasteredSheets`; `SceneryAndDivePrompts.json` records the exact
built-in image_gen prompts. Import processing keeps full silhouettes and transparent margins,
updates the existing stable sprite paths, and retains native resolution without compression.

The cast-timing control has a generated wooden/brass/navy frame, an animated red/teal bobber,
a live percentage badge and a separate gold target. Its target begins at the same 92% threshold
used by the actual perfect-cast feedback. The cursor follows real cast power; no sample bar,
plain colored rectangle or white line substitutes for the generated controls.

The dive UI has a matching depth instrument, a framed haul-value panel with live catch count,
and a separately generated amber reel control. The reel button works while descending and
remains visibly disabled during retrieval. Its text changes to REELING. `FishingHUDActions.cs`
owns this layout and updates the serialized references used by the saved Unity scene.

`FishingActionChecks` verifies sprite resolution/padding, actual bobber movement, gold-zone
agreement with gameplay, phase visibility, live values and the reel interaction. The existing
smoke checks retain daily quests, event cards, all 108 complete anglers and fish-mouth behavior.
`28-CraftedCastMeter.png`, `29-CraftedDiveHUD.png` and `30-CraftedReelingHUD.png` are Play-mode
previews. Validation does not build a player.

## Welcome panel, pause controls and visible retrieval

The welcome-back popup shares the Play/Equipment dock's complete cyan aquatic frame, bubbles and seaweed, with a white equipment-style earnings card. The pause artwork no longer contains the painted settings gear; its two live audio controls are centered, with resume, restart and quit retained. `Tools/WelcomeReelUpdate.json` records the built-in imagegen edit and asset path.

REEL IN now switches to continuous ascent instead of teleporting. Retrieval moves at exactly 1.4 times the configured descent speed, including speed upgrades and a full haul. The hook and its attached fish visibly return to the rod tip, the line retracts, and payment/results occur only on arrival. Repeated reel presses do not restart or accelerate retrieval. The retrieved hook remains at the player during the celebration.

Run `Tools/Deploy/update-welcome-reel.ps1 -Action Verify` in PowerShell 7 for isolated Unity Play-mode checks and rendered previews in `Logs/Previews/WelcomeReel-*`. Use `-Action Build` for the isolated WebGL export, followed by `node Tools/Deploy/prepare_cloudflare.mjs`. The saved source scene is loaded directly and is never regenerated by this workflow.
