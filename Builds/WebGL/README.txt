Watermelon - WebGL build
Unity 6000.3.27f2

Play
Serve this folder over HTTP or HTTPS, then open index.html in a browser.
Keep index.html, Build, and TemplateData together.
Opening index.html directly from disk is not supported by the Unity web player.

Upload
The ZIP has index.html at its root for static web hosting.
The build uses uncompressed files and needs no Content-Encoding configuration.
Serve .wasm files as application/wasm.

Controls
Play starts a fresh round. Add Band raises the multiplier and pressure.
Try Again starts another round after a burst. Menu returns to the main menu.

Rebuild
In Unity, choose Watermelon > Build WebGL.
The command exports Assets/Scenes/Watermelon.unity to Builds/WebGL.
The default browser canvas is 450 by 800 pixels.

Validation
Loaded and played the exported build in the desktop Chromium browser.
Verified menu, band count, multiplier, central pressure, burst at 18 bands,
restart to zero bands and 1.00x, return to menu, and page reload.
Other browsers and mobile devices have not been tested.
