# Watermelon

A Unity game about adding rubber bands to a watermelon. Each band increases the
multiplier and pressure around the middle until the watermelon bursts.

## Open the project

Use Unity 6000.3.27f2 with Web Build Support installed. Add this repository folder
to Unity Hub, open it, then open `Assets/Scenes/Watermelon.unity`.

The project uses Universal Render Pipeline and the new Input System. Package
dependencies are recorded in `Packages/manifest.json` and `Packages/packages-lock.json`.
Some source images and audio use Git LFS; run `git lfs pull` after cloning.

## Play

Press Play on the main menu, then Add Band to increase the multiplier. Try Again
starts a fresh round after a burst. Menu clears the current round and returns to
the main menu. Band and burst sounds are included.

## WebGL build

The complete exported player is in `Builds/WebGL`. A ready-to-upload package is
included at [Builds/Watermelon-WebGL.zip](Builds/Watermelon-WebGL.zip).

Serve the exported folder over HTTP or HTTPS and open `index.html`. Keep `Build`
and `TemplateData` beside it. The build uses a 450 by 800 portrait canvas and
uncompressed files. Serve `.wasm` files as `application/wasm`.

In Unity, choose **Watermelon > Build WebGL** to export a new player. Recreate the
ZIP after rebuilding. Exporting does not update the ZIP automatically.

## Validation

The exported player was checked in desktop Chromium: menu, band count and
multiplier, pressure deformation, burst, restart, and menu navigation. The tested
round burst at 18 bands. Other browsers and mobile devices have not been tested.

Unity also provides **Watermelon > Check Round Flow (Play Mode)** for the existing
automated round and navigation check. Sprint notes are in `SprintDocs`.

Unity regenerates `Library`, `Temp`, `Logs`, and local editor settings, so those
folders are excluded from Git. The project source and distributable build are
included.
