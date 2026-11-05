# Hosting Brick Rush

Upload the **contents** of `Builds/BrickRush_WebGL/` together. Keep `index.html`, `Build/`, and any additional files at the same relative paths. Subdirectory hosting is supported because asset URLs are relative.

The supplied build uses **gzip with Unity's decompression fallback** for portable static hosting. The loader decompresses `.unityweb` assets itself; this works on a simple HTTP server without special compression headers. Do not label those files with a `Content-Encoding` unless your host deliberately serves them through native browser decompression with the corresponding original MIME type. Transparent HTTP compression must always have matching headers.

Required MIME types:

| Extension | Content-Type |
| --- | --- |
| `.html` | `text/html; charset=utf-8` |
| `.js` | `application/javascript` |
| `.wasm` | `application/wasm` |
| `.data` | `application/octet-stream` |
| `.unityweb` | `application/octet-stream` |

Use HTTPS in production. No cross-origin credentials, accounts, API keys, server logic, or SharedArrayBuffer/cross-origin isolation headers are required. The build runs single-threaded. Avoid embedding hosts that intercept drag gestures or force scrolling. Allow fullscreen in an iframe when desired.

**itch.io:** zip the distribution contents with `index.html` at the archive root. Select HTML game, enable mobile support, and allow fullscreen. Use a resizable viewport.

**Cloudflare Pages / other static host:** deploy the distribution directory as the publish directory; no build command is needed. Confirm `.wasm` MIME and load the page on a real mobile device before public release.

To remove the fallback and use native streaming decompression, rebuild with fallback disabled, then serve gzip/Brotli artifacts with the corresponding `Content-Encoding` plus their original MIME types. Never rename compressed artifacts to make headers appear correct. Keep the fallback configuration for hosts where response headers cannot be configured.

The supplied local server sets the WebAssembly MIME explicitly. It is a development server, not a production service.

PlayerPrefs uses browser-local storage. A new origin, private browsing session, cleared site data, or blocked persistence may start with a fresh personal best. Assets require a network connection to load initially; gameplay itself makes no service calls. This distribution does not install an offline service worker.

## Cloudflare deployment

Live game: https://brick-rush-brick-buster.am-ome.workers.dev

Deploy the committed WebGL distribution with: npx wrangler deploy. Configuration is in wrangler.jsonc. Cloudflare authentication is required; credentials are not stored in the repository.

