Spin the Wheel - Responsive WebGL build
Unity 6000.3.27f2
Built on 2026-11-05

This build fixes browser resizing and short sound effects.

Run
Serve this folder over HTTP or HTTPS. Opening index.html as a local file
is insufficient. The current local preview is http://127.0.0.1:8776/game/
while the local preview server remains running.

Hosting
Upload the complete folder contents with index.html at the root.
This build uses Brotli compression. Serve these headers:
- Build/WebGL-Responsive.wasm.br: Content-Encoding: br; Content-Type: application/wasm
- Build/WebGL-Responsive.framework.js.br: Content-Encoding: br; Content-Type: application/javascript
- Build/WebGL-Responsive.data.br: Content-Encoding: br; Content-Type: application/octet-stream
Use HTTPS for a hosted deployment.

Screen layout
The canvas fills the available browser area and follows live resizing.
The layout stacks on narrow screens and scrolls internally when needed.
One rendering pixel per CSS pixel keeps controls consistent with tested sizes.
Display safe-area insets are reserved by the page.

Sound
Click and tick effects use padded PCM WAV source files. This preserves their
short attacks during Unity's WebGL audio conversion. Click Play, then Spin.
Ticks follow the wheel's movement and slow as it approaches the result.

Rebuild from the Unity project
1. Tools > Spin the Wheel > Prepare Web Build.
2. Tools > Spin the Wheel > Build Web Player.
The output is Builds/WebGL-Responsive.

Validation
- Ten live browser viewport sizes from 320x320 through 2560x1080 passed.
- Canvas dimensions matched the browser with no page overflow.
- Small-screen Play, Quit, Spin, and menu return were checked.
- An active spin continued through desktop-to-portrait resizing.
- Decoded browser click and tick signals recovered their peaks.
- Forty automated scene spins passed.

Licenses are included in Licenses/.
