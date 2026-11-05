# Bull Ride

An eight-second rodeo game with a two-axis balance stick. The multiplier starts at 0.1x and grows to 2.5x by the whistle from time alone. Holding the balance dot near the center raises it faster, up to 5x. A qualifying ride can get a high buck in slow motion with a visible green target for a bigger bonus. The existing bull and rider rigs appear in a floodlit indoor arena inspired by the [8 to Glory mobile gameplay footage](https://www.youtube.com/watch?v=6QBnKdhmFHs).

## Play in Unity

1. Open this project in Unity **6000.6.7f1**.
2. Open `Assets/Scenes/BullRide.unity` or choose **Bull Ride → Open Main Scene**.
3. Enter Play mode, choose a stake, and press **PLAY** inside the game.

Use **WASD**, the **arrow keys**, or the bottom-center **joystick** to push against the rider’s lean. **R** during the fall or result returns to the menu. The balance starts at $100 and is saved between sessions.

The Unity scene uses skinned meshes for the bull, fixed-length leg joints for the buck poses, and a rider rig with spring-driven upper-body motion in the saddle. After a throw, a separate set of jointed physics bodies drives the same intact rider rig. The shared art is bundled in `Assets/Resources/BullRideArt.bytes`. Run `node Tools/ExportArenaArt.cjs` after editing the arena, rider, or bull art in the standalone page to update Unity’s copy.

## Play in a browser

Double-click `Deliverables/BullRide.html` to run the packaged Unity player, or serve `Deliverables/BullRide-WebGL` with a local static server. Rebuild it with **Bull Ride → Build WebGL**, then run `python Tools/make_single_file.py` to regenerate the single-file Unity player. The project also includes a separate self-contained WebGL1 version at `index.html`.

## Check the project

Choose **Bull Ride → Run Logic Checks** in Unity to verify the gate timing, multiplier, unattended fall, eight-second ride, and imported hoof rig.

## iPhone and iPad

The game adapts to portrait and landscape. The Unity player keeps the controls inside the device safe area. The browser build uses Safari's viewport insets and supports touch steering.

Open this folder as a Unity project in Unity 6000.6.7f1. The scene is `Assets/Scenes/BullRide.unity`. To preview device sizes in the Editor, select **Simulator** in the Game view and choose an iPhone or iPad preset.

For a native build, add **iOS Build Support** to this Unity version. On a Mac with Xcode, select iOS in Unity's Build Profiles, then use **Bull Ride → Export iOS for Simulator** or **Bull Ride → Export iOS for Device**. Set Simulator Architecture in the iOS Player Settings to match your Mac before exporting for Simulator. The export commands create separate Xcode projects under `Deliverables`. Open the generated `Unity-iPhone.xcodeproj` in Xcode and choose a simulator or an attached device. For device signing, use your Apple development team and a bundle identifier registered to it. [Unity iOS build instructions](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/iphone/ios-building-and-delivering/build-process)

The included `Deliverables/BullRide-WebGL` and `Deliverables/BullRide.html` are browser builds. A native iOS app was not exported or tested on this Windows machine.
