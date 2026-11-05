#!/usr/bin/env python3
"""
Packs the Unity WebGL build into ONE self-contained .html file that runs straight
from disk (file://) with no web server.

  * .wasm      -> gzipped + base64, decompressed in JS, Module.instantiateWasm
  * .data      -> gzipped + base64 -> Blob object URL
  * framework  -> base64 -> Blob URL <script>
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
OUT = os.path.join(ROOT, "Deliverables", "BullRide.html")

PRODUCT = "Bull Ride"
COMPANY = "Bull Ride Studio"
VERSION = "1.0"


def read(name: str) -> bytes:
    path = os.path.join(BUILD, name)
    if not os.path.exists(path):
        sys.exit("missing build file: " + path)
    with open(path, "rb") as handle:
        return handle.read()


def gz64(data: bytes) -> str:
    buffer = io.BytesIO()
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

    raw = os.path.getsize(OUT)
    print("wasm  %8.1f MB -> %6.1f MB gzip" % (len(wasm) / 1e6, len(gz64(wasm)) * 0.75 / 1e6))
    print("data  %8.1f MB -> %6.1f MB gzip" % (len(data) / 1e6, len(gz64(data)) * 0.75 / 1e6))
    print("wrote %s (%.1f MB)" % (OUT, raw / 1e6))


PAGE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<link rel="icon" href="data:,">
<title>__PRODUCT__</title>
<style>
  html, body {
    margin: 0; padding: 0; height: 100%; width: 100%;
    background: #0d121f; overflow: hidden;
    overscroll-behavior: none;
    font-family: "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    -webkit-tap-highlight-color: transparent;
  }
  #unity-container {
    position: fixed;
    inset: env(safe-area-inset-top, 0px) env(safe-area-inset-right, 0px)
           env(safe-area-inset-bottom, 0px) env(safe-area-inset-left, 0px);
    /* Keep intrinsic canvas dimensions from expanding the mobile layout. */
    overflow: hidden; contain: strict;
    height: calc(100dvh - env(safe-area-inset-top, 0px) - env(safe-area-inset-bottom, 0px));
  }
  #unity-canvas {
    position: absolute; inset: 0;
    width: 100%; height: 100%; display: block; outline: none; touch-action: none;
    background: #0d121f;
  }

  #boot {
    position: fixed; inset: 0; z-index: 10;
    /* The 620px glow must not widen a phone page, even after boot is hidden. */
    overflow: hidden;
    display: flex; flex-direction: column; align-items: center; justify-content: center;
    gap: 26px; background: #0d121f;
    transition: opacity .55s ease, visibility .55s ease;
  }
  #boot.done { opacity: 0; visibility: hidden; }

  #boot .glow {
    position: absolute; width: 620px; height: 620px; border-radius: 50%;
    background: radial-gradient(circle, rgba(255,184,48,.16), rgba(190,50,40,.1) 45%, transparent 70%);
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
    color: #f8f1e3; text-shadow: 0 0 18px rgba(255,184,48,.7), 0 0 42px rgba(190,50,40,.35);
    animation: pulse 2.2s ease-in-out infinite;
  }
  @keyframes pulse {
    0%, 100% { opacity: .92; transform: translateY(0); }
    50%      { opacity: 1;   transform: translateY(-2px); }
  }

  #boot .sub {
    margin: -10px 0 0; font-size: 12px; letter-spacing: .34em;
    text-transform: uppercase; color: rgba(210,190,150,.55);
  }

  #track {
    position: relative; width: min(340px, 72vw); height: 6px; border-radius: 99px;
    background: rgba(180,150,100,.16); overflow: hidden;
  }
  #bar {
    position: absolute; left: 0; top: 0; bottom: 0; width: 0%; border-radius: 99px;
    background: linear-gradient(90deg, #ffb830, #e04a2f 55%, #8b2a1a);
    box-shadow: 0 0 12px rgba(255,184,48,.75), 0 0 26px rgba(224,74,47,.4);
    transition: width .22s ease-out;
  }
  #boot .status {
    font-size: 11px; letter-spacing: .22em; text-transform: uppercase;
    color: rgba(210,190,150,.62); min-height: 14px;
  }
  #boot .hint { font-size: 11px; color: rgba(180,160,120,.34); letter-spacing: .04em; }

  #boot .error {
    max-width: min(560px, 84vw); text-align: center; line-height: 1.55;
    font-size: 13px; color: #ffc4b0; border: 1px solid rgba(224,74,47,.45);
    background: rgba(224,74,47,.07); border-radius: 12px; padding: 14px 18px;
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
    <p class="sub">Hold On</p>
    <div id="track"><div id="bar"></div></div>
    <div class="status" id="status">Unpacking build&hellip;</div>
    <div class="hint" id="hint">Runs offline &mdash; no server needed</div>
    <div class="error" id="error"></div>
  </div>
</div>

<script type="application/octet-stream" id="p-framework">__FRAMEWORK_B64__</script>
<script type="application/octet-stream" id="p-wasm">__WASM_GZ_B64__</script>
<script type="application/octet-stream" id="p-data">__DATA_GZ_B64__</script>

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
    bar.style.background = "#e04a2f";
    console.error("[boot] " + message, detail || "");
  }

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
    var stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"));
    return new Response(stream).arrayBuffer().then(function (buffer) {
      return new Uint8Array(buffer);
    });
  }

  function nextFrame() {
    return new Promise(function (resolve) { requestAnimationFrame(function () { resolve(); }); });
  }

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
      showBanner: function (message, type) {
        if (type === "error") setStatus(String(message).split("\n")[0]);
      },
      instantiateWasm: function (imports, onSuccess) {
        WebAssembly.instantiate(wasmBytes, imports).then(
          function (result) { onSuccess(result.instance, result.module); },
          function (error) { fail("The game engine failed to load.", error); }
        );
        return {};
      }
    };
    if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
        (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1)) {
      config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);
    }

    createUnityInstance(canvas, config, function (progress) {
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
