Flap Dash - WebGL
Build date: 2026-11-05
Unity version: 6000.3.27f2

Play
1. Extract the ZIP completely.
2. Serve the folder containing index.html over HTTP or HTTPS. Keep Build, TemplateData and other generated files beside index.html.
3. Open the server address in a browser. Do not open index.html directly through file://.
4. Choose START ROUND. Press Space, click, or tap to flap. Pass pipes to score. Collision or the 60-second limit ends the round.
5. Choose PLAY AGAIN to restart. Adjust VISUAL ENERGY on the menu to reduce decorative motion.

Local server example, if Python is installed
Open a terminal in this extracted folder and run:
python -m http.server 8765 --bind 127.0.0.1
Then open http://127.0.0.1:8765/ in a browser. Keep the terminal running and use Ctrl+C to stop it.
If the port is already in use, choose another port and open that matching address.

Included saved features
- Bright animated bird, pipes and layered scenery.
- Flap sound from the last preview selected by the user, named frog.wav in the creator archive.
- Centered score replaces the title during play; the gameplay countdown appears below the header.
- Larger alternating vertical pipe heights.
- Target pipe speed increases by 0.20 every ten points, from 2.65 up to 3.65, with smooth acceleration and per-round reset.

Scope
This package contains game 26, Flap Dash, only. The other four games are independent planned projects.
There is no backend, multiplayer service, score verifier or input replay.

Hosting
Upload the entire extracted folder to your static HTTP/HTTPS host, preserving paths and filenames.
The build profile uses gzip with JavaScript decompression fallback for simple static hosting.
Browser autoplay policies may require the player to press START ROUND before hearing sound.

License record
See AssetLicenses.txt and the Licenses folder for the current creator/source/CC0 records.
Unity and software dependencies retain their original software licenses; the engine is not public domain.

Validation
See Build_Validation.txt for the checks performed on this exact package.
Browser startup, audio, input and gameplay require a manual review; no desktop/browser automation was used.