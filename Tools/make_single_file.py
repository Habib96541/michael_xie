#!/usr/bin/env python3
"""
Packs the Unity WebGL build into ONE self-contained .html file that runs straight
from disk (file://) with no web server.

Unity's stock loader needs HTTP: it fetches the .data file, injects the framework as
a <script src>, and stream-streams the .wasm. A file:// page cannot fetch local files,
so this packer rewires the pipeline:

  * .wasm      -> gzipped + base64 inside the HTML, decompressed in JS and handed to
                  Unity through Module.instantiateWasm (no fetch at all)
  * .data      -> same, decompressed and exposed as a Blob object URL (fetch(blob:)
                  is allowed from file://)
  * framework  -> base64, rebuilt as a Blob URL <script src> (also allowed)
  * loader     -> inlined verbatim

Usage: python Tools/make_single_file.py
"""

import base64
import gzip
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUILD = os.path.join(ROOT, "Build", "WebGL", "Build")
OUT = os.path.join(ROOT, "Deliverables", "index.html")

PRODUCT = "Geometry Dash"
COMPANY = "Neon Dash"
VERSION = "1.0"


def read(name: str) -> bytes:
    path = os.path.join(BUILD, name)
    if not os.path.exists(path):
        sys.exit("missing build file: " + path)
    with open(path, "rb") as handle:
        return handle.read()


def gz64(data: bytes) -> str:
    buffer = io.BytesIO()
    # mtime=0 keeps the output byte-stable between runs (a rebuild that changes
    # nothing produces an identical file, so hashes stay comparable).
    with gzip.GzipFile(fileobj=buffer, mode="wb", compresslevel=9, mtime=0) as gz:
        gz.write(data)
    return base64.b64encode(buffer.getvalue()).decode("ascii")


def b64(data: bytes) -> str:
    return base64.b64encode(data).decode("ascii")


def main() -> None:
    wasm = read("WebGL.wasm")
    data = read("WebGL.data")
    framework = read("WebGL.framework.js")
    loader = read("WebGL.loader.js").decode("utf-8")

    if "</script" in loader:
        sys.exit("loader.js contains a </script sequence; needs escaping before inlining")

    os.makedirs(os.path.dirname(OUT), exist_ok=True)

    html = PAGE.replace("__LOADER__", loader)
    html = html.replace("__FRAMEWORK_B64__", b64(framework))
    html = html.replace("__WASM_GZ_B64__", gz64(wasm))
    html = html.replace("__DATA_GZ_B64__", gz64(data))
    html = html.replace("__PRODUCT__", PRODUCT)
    html = html.replace("__COMPANY__", COMPANY)
    html = html.replace("__VERSION__", VERSION)

    with open(OUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(html)

    # Crude but useful sanity numbers.
    raw = os.path.getsize(OUT)
    print("wasm  %8.1f MB -> %6.1f MB gzip" % (len(wasm) / 1e6, len(gz64(wasm)) * 0.75 / 1e6))
    print("data  %8.1f MB -> %6.1f MB gzip" % (len(data) / 1e6, len(gz64(data)) * 0.75 / 1e6))
    print("wrote %s (%.1f MB)" % (OUT, raw / 1e6))


PAGE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>__PRODUCT__</title>
<style>
  html, body {
    margin: 0; padding: 0; height: 100%; width: 100%;
    background: #06030e; overflow: hidden;
    font-family: "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    -webkit-tap-highlight-color: transparent;
  }
  #unity-container {
    position: fixed; inset: 0;
    inset: env(safe-area-inset-top) env(safe-area-inset-right)
           env(safe-area-inset-bottom) env(safe-area-inset-left);
  }
  #unity-canvas {
    width: 100%; height: 100%; display: block; outline: none;
    background: #06030e; touch-action: none;
    -webkit-user-select: none; user-select: none;
  }

  /* Boot screen: neon, so the wait matches the game's look. */
  #boot {
    position: absolute; inset: 0; z-index: 10;
    display: flex; flex-direction: column; align-items: center; justify-content: center;
    gap: 26px; background: #06030e;
    transition: opacity .55s ease, visibility .55s ease;
  }
  #boot.done { opacity: 0; visibility: hidden; }

  #boot .glow {
    position: absolute; width: 620px; height: 620px; border-radius: 50%;
    background: radial-gradient(circle, rgba(34,225,255,.14), rgba(255,45,149,.08) 45%, transparent 70%);
    filter: blur(6px); animation: breathe 3.4s ease-in-out infinite;
  }
  @keyframes breathe {
    0%, 100% { transform: scale(1); opacity: .75; }
    50%      { transform: scale(1.14); opacity: 1; }
  }

  #boot .stack { position: relative; display: flex; flex-direction: column; align-items: center; gap: 22px; }

  #boot h1 {
    margin: 0; font-size: clamp(28px, 6vw, 54px); font-weight: 800;
    letter-spacing: .16em; text-transform: uppercase;
    color: #f2fbff; text-shadow: 0 0 18px rgba(34,225,255,.75), 0 0 42px rgba(255,45,149,.35);
    animation: pulse 2.2s ease-in-out infinite;
  }
  @keyframes pulse {
    0%, 100% { opacity: .92; transform: translateY(0); }
    50%      { opacity: 1;   transform: translateY(-2px); }
  }

  #boot .sub {
    margin: -10px 0 0; font-size: 12px; letter-spacing: .34em;
    text-transform: uppercase; color: rgba(170,196,220,.55);
  }

  #track {
    position: relative; width: min(340px, 72vw); height: 6px; border-radius: 99px;
    background: rgba(120,150,190,.16); overflow: hidden;
  }
  #bar {
    position: absolute; left: 0; top: 0; bottom: 0; width: 0%; border-radius: 99px;
    background: linear-gradient(90deg, #22e1ff, #8b5cf6 55%, #ff2d95);
    box-shadow: 0 0 12px rgba(34,225,255,.8), 0 0 26px rgba(255,45,149,.45);
    transition: width .22s ease-out;
  }
  #boot .status {
    font-size: 11px; letter-spacing: .22em; text-transform: uppercase;
    color: rgba(170,196,220,.62); min-height: 14px;
  }
  #boot .hint { font-size: 11px; color: rgba(150,175,200,.34); letter-spacing: .04em; }

  #boot .error {
    max-width: min(560px, 84vw); text-align: center; line-height: 1.55;
    font-size: 13px; color: #ffb4c8; border: 1px solid rgba(255,45,149,.45);
    background: rgba(255,45,149,.07); border-radius: 12px; padding: 14px 18px;
    display: none;
  }
</style>
</head>
<body>
<div id="unity-container">
  <canvas id="unity-canvas" width="960" height="600" tabindex="-1"></canvas>
</div>

<div id="boot">
  <div class="glow"></div>
  <div class="stack">
    <h1>__PRODUCT__</h1>
    <p class="sub">Neon Runner</p>
    <div id="track"><div id="bar"></div></div>
    <div class="status" id="status">Unpacking build&hellip;</div>
    <div class="hint" id="hint">Runs offline &mdash; no server needed</div>
    <div class="error" id="error"></div>
  </div>
</div>

<!-- ===== payloads: base64 only, so nothing can terminate the tag early ===== -->
<script type="application/octet-stream" id="p-framework">__FRAMEWORK_B64__</script>
<script type="application/octet-stream" id="p-wasm">__WASM_GZ_B64__</script>
<script type="application/octet-stream" id="p-data">__DATA_GZ_B64__</script>

<!-- ===== Unity loader, inlined verbatim ===== -->
<script>
__LOADER__
</script>

<script>
(function () {
  "use strict";

  var boot = document.getElementById("boot");
  var bar = document.getElementById("bar");
  var statusEl = document.getElementById("status");
  var hintEl = document.getElementById("hint");
  var errorEl = document.getElementById("error");
  var canvas = document.getElementById("unity-canvas");

  var finished = false;

  function setProgress(value) {
    if (finished) return;
    bar.style.width = Math.max(0, Math.min(1, value)) * 100 + "%";
  }

  function setStatus(text) {
    if (finished) return;
    statusEl.textContent = text;
  }

  function fail(message, detail) {
    finished = true;
    errorEl.style.display = "block";
    errorEl.textContent = message;
    statusEl.textContent = "Could not start";
    hintEl.style.display = "none";
    bar.style.width = "100%";
    bar.style.background = "#ff2d95";
    console.error("[boot] " + message, detail || "");
  }

  // ---------------------------------------------------------------- decoding
  function base64Bytes(id) {
    var el = document.getElementById(id);
    if (!el) throw new Error("payload " + id + " is missing");
    var text = el.textContent.replace(/\s+/g, "");
    var binary = atob(text);
    var out = new Uint8Array(binary.length);
    for (var i = 0; i < binary.length; i++) out[i] = binary.charCodeAt(i);
    return out;
  }

  function gunzip(bytes) {
    // DecompressionStream is the platform gunzip; every current browser has it.
    var stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"));
    return new Response(stream).arrayBuffer().then(function (buffer) {
      return new Uint8Array(buffer);
    });
  }

  function nextFrame() {
    return new Promise(function (resolve) { requestAnimationFrame(function () { resolve(); }); });
  }

  // ------------------------------------------------------------------- boot
  function start() {
    setProgress(0.04);
    setStatus("Unpacking build\u2026");
    return nextFrame()
      .then(function () {
        var frameworkB64 = base64Bytes("p-framework");
        var frameworkText = new TextDecoder("utf-8").decode(frameworkB64);
        setProgress(0.12);
        setStatus("Unpacking engine\u2026");
        return nextFrame().then(function () {
          return gunzip(base64Bytes("p-wasm")).then(function (wasmBytes) {
            setProgress(0.42);
            setStatus("Unpacking assets\u2026");
            return nextFrame().then(function () {
              return gunzip(base64Bytes("p-data")).then(function (dataBytes) {
                setProgress(0.58);
                setStatus("Starting engine\u2026");
                return nextFrame().then(function () {
                  return launch(frameworkText, wasmBytes, dataBytes);
                });
              });
            });
          });
        });
      })
      .catch(function (error) {
        fail(
          "This browser could not unpack the game. Try a current version of Chrome, Edge, Firefox or Safari.",
          error
        );
      });
  }

  function launch(frameworkText, wasmBytes, dataBytes) {
    var frameworkUrl = URL.createObjectURL(new Blob([frameworkText], { type: "text/javascript" }));
    var dataUrl = URL.createObjectURL(new Blob([dataBytes], { type: "application/octet-stream" }));

    var config = {
      dataUrl: dataUrl,
      frameworkUrl: frameworkUrl,
      codeUrl: "WebGL.wasm",
      streamingAssetsUrl: "StreamingAssets",
      companyName: "__COMPANY__",
      productName: "__PRODUCT__",
      productVersion: "__VERSION__",
      devicePixelRatio: Math.min(window.devicePixelRatio || 1, 1.5),
      showBanner: function (message, type) {
        if (type === "error") setStatus(String(message).split("\n")[0]);
      },
      // Hand the module over directly: no fetch, no streaming-compile, no MIME issues.
      instantiateWasm: function (imports, onSuccess) {
        WebAssembly.instantiate(wasmBytes, imports).then(
          function (result) { onSuccess(result.instance, result.module); },
          function (error) { fail("The game engine failed to load.", error); }
        );
        return {};
      }
    };

    createUnityInstance(canvas, config, function (progress) {
      // resize/first-frame phases run after the download; keep the bar honest.
      setProgress(0.6 + progress * 0.4);
      if (progress >= 0.99) setStatus("Almost there\u2026");
    }).then(function () {
      finished = false;
      setProgress(1);
      statusEl.textContent = "Ready";
      setTimeout(function () {
        boot.classList.add("done");
      }, 260);
    }).catch(function (error) {
      fail("The game failed to start.", error);
    });
  }

  if (typeof DecompressionStream !== "function") {
    fail("This browser is missing DecompressionStream (needed to unpack the game). Please update your browser.");
    return;
  }

  start();
})();
</script>
</body>
</html>
"""


if __name__ == "__main__":
    main()
