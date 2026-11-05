# Spin the Wheel

A Unity game with twenty equally likely multipliers from 0.1x to 50x, a bright
main menu, a responsive wheel interface, and roulette sound effects.

## Open the project

Use Unity **6000.3.27f2** with the Web Build Support module installed. The project
uses the Universal Render Pipeline 2D renderer, uGUI, TextMesh Pro, and the New
Input System.

Install Git LFS before cloning so images, sounds, fonts, and build binaries are
downloaded as complete files:

```text
git lfs install
git clone --branch Spin-the-Wheel https://github.com/Sparktech-Ai-Agency01/michael_xie.git
```

Open the repository folder through Unity Hub, let Unity restore packages, then
open `Assets/Scenes/SpinTheWheel.unity` and enter Play mode.

## Included WebGL build

- [Complete browser build](Builds/WebGL-Responsive/)
- [Packaged browser build](Builds/SpinTheWheel-WebGL-Responsive.zip)
- [Hosting and run instructions](Builds/WebGL-Responsive/README.txt)

Serve the build folder over HTTP or HTTPS. The build uses Brotli compression;
the bundled README lists the required content headers. The canvas follows the
browser viewport, and short windows use internal UI scrolling.

## Rebuild and validate

In Unity, use these commands under `Tools > Spin the Wheel`:

1. `Prepare Web Build` selects the responsive template and assigns padded WAV
   variants that retain the short click and tick attacks in WebGL.
2. `Run Sprint Validation` verifies forty spins, all twenty outcomes twice,
   pointer alignment, repeated requests, menu navigation, and unscaled time.
3. `Run Layout Validation` checks ten Game view sizes and simulated safe areas.
4. `Build Web Player` writes a non-development build to `Builds/WebGL-Responsive`.

The ZIP is a packaged snapshot of the included build folder. Repackage it after
rebuilding if distributing an updated archive.

## Project contents

- `Assets/`: scenes, scripts, UI sprites, sounds, fonts, and the WebGL template.
- `Packages/`: package manifest and lock file.
- `ProjectSettings/`: shared Unity project configuration.
- `SprintDocs/`: implementation notes, validation reports, and previews.
- `Tools/`: local asset preparation tools.
- `Builds/`: the corrected WebGL build and its distribution ZIP.

## Validation and limits

The corrected build passed ten live browser viewport sizes from 320x320 through
2560x1080, scrolling checks, resizing during a spin, and the forty-spin scene
regression. Browser audio analysis confirmed that the click and roulette tick
signals are restored after WebGL conversion.

Physical mobile devices, Safari, and Firefox have not been tested. The build
has 345 shader warnings from the existing AI inference/Sentis package and zero
build errors. Details are in [Sprint 06](SprintDocs/Sprint_06.txt).

## Asset licenses

Kenney UI and sound effects use CC0. Liberation Sans uses the SIL Open Font
License. Original notices and source details are preserved in `Assets/Art`,
`Assets/Audio/CREDITS.md`, `Assets/TextMesh Pro/Fonts`, and the build's `Licenses`
folder. The wheel frame and pointer were generated for this project.
