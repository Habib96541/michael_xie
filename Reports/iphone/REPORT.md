# iPhone responsiveness check — November 5, 2026

Status: Passed available browser emulation checks; physical iPhone/Safari performance unverified.

Unity 6000.3.10f1 has its built-in Device Simulator module and iOS build support installed. The host is Windows. Apple Simulator/Xcode cannot run here. Unity Device Simulator checks appearance/input and does not emulate an iPhone GPU or Safari browser. The actual WebGL release was therefore tested in installed Chrome using iPhone viewport, touch and DPR 3 emulation. This is not an Apple Simulator run.

Four landscape layouts passed: SE (667×375), mini (812×375), 14-class (844×390), Pro Max (932×430). Each check exercised main menu → countdown → gameplay → touch pause → frozen timer → resume → portrait rotation → automatic pause. Artificial 44px left/right and 21px bottom insets verified the canvas stays within the usable area. Screenshots were inspected for menu clipping. Browser console/page errors: 0.

Sampled frame rates: 60.0–60.0 FPS, median 60.0, on the Windows desktop GPU. These short samples measure the available host, not real iPhone smoothness or sustained thermal performance.

Confirmed bug: rotating a live match to portrait hid gameplay behind the rotation notice while time continued decreasing. Before the fix, the timer fell from 119.00 to 117.48 seconds while portrait was displayed. The browser wrapper now pauses the match on portrait rotation and leaves resume under the player's control. CSS safe-area insets now reserve space for browser-reported cutouts/home-indicator areas. The same changes were applied to the Unity WebGL template and existing build HTML; WebAssembly gameplay was unchanged. The ZIP and SHA-256 manifest were regenerated.

Main menu: available, with Play, Settings, How to Play, skins and best score. Pause menu: available, with Keep Eating, Settings and Main Menu.

Evidence: results.json and the menu/pause/portrait/safe-area PNGs in this folder. Repeat with node Tools/browser-tests/iphone.cjs while the local preview server runs.

References: [Unity Device Simulator limitations](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/cross-platform-features/device-simulator/introduction), [Apple Simulator requirements](https://developer.apple.com/documentation/xcode/running-your-app-on-simulated-or-physical-devices).
