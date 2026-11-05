# WebGL deployment

The checked-in phone-ready WebGL player is in `Builds/WebGL`. Do not open
`index.html` through `file://`; serve it over HTTP so WebAssembly, compressed
assets and persistent storage work as intended.

For Cloudflare Pages Direct Upload, build the Unity player and run
`python Tools/package_build.py`, then deploy the `Builds/WebGL` folder with
Wrangler:

```powershell
npx wrangler pages project create michael-xie-hydraulic-press --production-branch Hydraulic-Press
npx wrangler pages deploy Builds/WebGL --project-name=michael-xie-hydraulic-press --branch=Hydraulic-Press
```

The Pages `_headers` file sets the Unity-generated `.gz` files' original MIME
types and `Content-Encoding: gzip`. Keep it beside `index.html` when deploying.
The mobile viewport uses the player's 2x device-pixel-ratio cap, safe-area CSS
and responsive Unity canvas.

The Unity build is reproducible through `Hydraulic Press > Build WebGL`. The
local HTTP host is `python Tools/serve.py` at `http://localhost:8088`.
