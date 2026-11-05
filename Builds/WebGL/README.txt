Egg Drop WebGL Release
Unity 6000.3.27f2

Upload this folder's contents to static HTTP or HTTPS hosting. Keep index.html,
Build, and TemplateData together. The player uses gzip compression with browser
decompression fallback, so special Content-Encoding headers are not required.
Do not open index.html directly from disk.

For local testing from the Unity project folder, run this in PowerShell:
& "C:/Program Files/Unity/Hub/Editor/6000.3.27f2/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/Emscripten/python/python.exe" -m http.server 8765 --bind 127.0.0.1 --directory "Builds/WebGL"

Then open http://127.0.0.1:8765/ in a browser. Click Play to start and allow audio.
Space drops the egg, C collects, R starts a fresh round after a result, and M
mutes or unmutes. Close the browser tab to exit the WebGL player.

Build verification: Unity reported 0 errors and 0 warnings. The generated player
loaded over HTTP and displayed its main menu. Fullscreen scaling, complete
browser gameplay, and audible playback have not been fully verified.
