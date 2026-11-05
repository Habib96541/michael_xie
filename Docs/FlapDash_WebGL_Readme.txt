Flap Dash - WebGL
Unity version: 6000.3.6f1

Play
1. Extract the ZIP completely.
2. Serve the folder containing index.html through a local HTTP server or upload its contents to a static web host.
3. Open the server address in a browser. Opening index.html directly as a local file is not supported.
4. Choose START ROUND. Click, tap, or press Space to flap. Pass pipes for points. A collision or 60 seconds ends the round.
5. Choose PLAY AGAIN to restart. Adjust VISUAL ENERGY on the Ready or Results screen to reduce or disable decorative effects.

Local server on this development PC
Open PowerShell in the extracted folder and run:
& 'C:/Program Files/Unity/Hub/Editor/6000.3.6f1/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/Emscripten/python/python.exe' -m http.server 8765 --bind 127.0.0.1
Then open http://127.0.0.1:8765/ in Chrome or Edge. Keep the server running while playing; Ctrl+C stops it.

Share or host
Keep index.html, Build, TemplateData, and all other generated files together with their original names. Upload the contents of this folder, not just index.html. No backend is required for this local prototype.
The build uses gzip with JavaScript decompression fallback for basic static hosting. Public deployment and phone-browser testing have not been performed.

Rebuild from Unity
Open Games/26_FlapDash in Unity 6000.3.6f1. Open the WebGL asset under Assets/Settings/Build Profiles, switch to that profile if necessary, and choose Build. Use Builds/WebGL as the output folder. Web Build Support must be installed for this Editor version.

Scope
This package contains game 26, Flap Dash, only. Games 27-30 remain separate planned Unity projects.
There is no multiplayer service, score verifier, or input replay in this prototype.

License record
See AssetLicenses.txt and Tuffy_PublicDomain.txt beside this file. Project-created visuals and imported public-domain font content are distinct from Unity and package software, which retain their licenses.

Validation
The pre-build Unity behavior suite passed 17 checks. See the delivery record for the build result and HTTP file checks. Browser gameplay remains a manual check because browser automation was stopped when its URL could not be verified reliably.
