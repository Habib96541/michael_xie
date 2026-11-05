#!/usr/bin/env python3
"""Pack a Unity WebGL build into one self-contained .html file.

The build files are embedded as base64 inside the HTML, so the page needs no
web server: double-clicking the .html (file://) works, and so does emailing or
dropping it into any static host.

Gzip'd .data/.wasm payloads are decoded in the browser with DecompressionStream
and handed to Unity as blob: URLs, which fetch() can read from a file:// page.
"""

import base64
import glob
import gzip
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BUILD_DIR = os.path.join(ROOT, "BottleFlipUnity", "Builds", "WebGL")
OUT_FILE = os.path.join(ROOT, "BottleFlip.html")

BOOTSTRAP = """\
    // ---------------------------------------------------------------------
    // Single-file bootstrap: every asset below is embedded in this document.
    // Nothing is fetched from disk or the network, so the file runs from
    // file://, a USB stick, an email attachment or any static host.
    // ---------------------------------------------------------------------
    (function () {
      var node = document.getElementById("bf-assets");

      function fail(message) {
        hint.textContent = "failed to load";
        unityShowBanner("Could not load the game: " + message, "error");
      }

      function decode(base64Text) {
        var binary = atob(base64Text);
        var length = binary.length;
        var bytes = new Uint8Array(length);
        for (var i = 0; i < length; i++) bytes[i] = binary.charCodeAt(i);
        return bytes;
      }

      function objectUrl(bytes, type) {
        return URL.createObjectURL(new Blob([bytes], { type: type }));
      }

      async function gunzip(bytes) {
        if (typeof DecompressionStream === "undefined") {
          throw new Error("this browser cannot decompress the build (try Chrome, Edge, Firefox or Safari 16.4+)");
        }
        var stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"));
        return new Uint8Array(await new Response(stream).arrayBuffer());
      }

      (async function boot() {
        try {
          var packed = JSON.parse(node.textContent || node.innerHTML);
          node.textContent = "";
          node.parentNode.removeChild(node);

          hint.textContent = "unpacking";

          var loaderBytes = decode(packed.loader);
          var frameworkBytes = decode(packed.framework);
          var dataBytes = decode(packed.data);
          var codeBytes = decode(packed.code);
          packed = null;

          if (!dataBytes.length || !codeBytes.length) throw new Error("embedded build is incomplete");
          if (dataGzip) dataBytes = await gunzip(dataBytes);
          if (codeGzip) codeBytes = await gunzip(codeBytes);

          var config = {
            arguments: [],
            dataUrl: objectUrl(dataBytes, "application/octet-stream"),
            frameworkUrl: objectUrl(frameworkBytes, "text/javascript"),
            codeUrl: objectUrl(codeBytes, "application/wasm"),
            streamingAssetsUrl: "StreamingAssets",
            companyName: COMPANY_NAME,
            productName: PRODUCT_NAME,
            productVersion: PRODUCT_VERSION,
            showBanner: unityShowBanner
          };

          // Keep the render resolution sane on high-DPI phones.
          config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 2);

          window.addEventListener("contextmenu", function (e) { e.preventDefault(); });
          document.addEventListener("gesturestart", function (e) { e.preventDefault(); });

          document.querySelector("#loader").style.display = "flex";

          var script = document.createElement("script");
          script.src = objectUrl(loaderBytes, "text/javascript");
          script.onload = function () {
            createUnityInstance(canvas, config, function (progress) {
              barFill.style.width = Math.round(progress * 100) + "%";
              if (progress > 0.45) hint.textContent = "warming up";
              if (progress > 0.9) hint.textContent = "almost there";
            }).then(function (unityInstance) {
              window.unityInstance = unityInstance;
              loader.classList.add("done");
              setTimeout(function () { loader.style.display = "none"; }, 500);
              canvas.focus();
            }).catch(fail);
          };
          script.onerror = function () { fail("the embedded loader did not start"); };
          document.body.appendChild(script);
        } catch (error) {
          fail(error && error.message ? error.message : String(error));
        }
      })();
    })();
"""


def find_one(pattern):
    matches = sorted(glob.glob(os.path.join(BUILD_DIR, "Build", pattern)))
    if not matches:
        matches = sorted(glob.glob(os.path.join(BUILD_DIR, pattern)))
    return matches


def read(path):
    with open(path, "rb") as handle:
        return handle.read()


def embed(data, path):
    print("  %-24s %7.1f KB" % (os.path.basename(path), len(data) / 1024.0))
    return base64.b64encode(data).decode("ascii")


def main():
    index_path = os.path.join(BUILD_DIR, "index.html")
    if not os.path.exists(index_path):
        sys.exit("missing web build: " + index_path)

    html = read(index_path).decode("utf-8")
    if "@@BOOTSTRAP_START@@" not in html or "@@PACKED_ASSETS@@" not in html:
        sys.exit("index.html has no packer markers - rebuild with the current template")

    loader = find_one("*.loader.js")[0]
    framework = (find_one("*.framework.js") + find_one("*.framework.js.gz"))[0]
    data_files = find_one("*.data.gz") + find_one("*.data")
    code_files = find_one("*.wasm.gz") + find_one("*.wasm")
    if not data_files or not code_files:
        sys.exit("missing .data/.wasm in " + BUILD_DIR)

    data_path, code_path = data_files[0], code_files[0]
    data_gzip = data_path.endswith(".gz")
    code_gzip = code_path.endswith(".gz")

    print("packing single-file build")
    framework_bytes = read(framework)
    if framework.endswith(".gz"):
        framework_bytes = gzip.decompress(framework_bytes)

    assets = {
        "loader": embed(read(loader), loader),
        "framework": embed(framework_bytes, framework),
        "data": embed(read(data_path), data_path),
        "code": embed(read(code_path), code_path),
    }

    # Pull the real product metadata out of the generated loader config so the
    # packed file stays in sync with PlayerSettings.
    def field(name, fallback):
        marker = name + ': "'
        start = html.find(marker)
        if start < 0:
            return json.dumps(fallback)
        start += len(marker)
        return json.dumps(html[start:html.find('"', start)])

    bootstrap = BOOTSTRAP
    bootstrap = bootstrap.replace("COMPANY_NAME", field("companyName", "Freebuff"))
    bootstrap = bootstrap.replace("PRODUCT_NAME", field("productName", "Bottle Flip"))
    bootstrap = bootstrap.replace("PRODUCT_VERSION", field("productVersion", "1.0.0"))
    bootstrap = bootstrap.replace(
        "if (dataGzip)", "if (%s)" % ("true" if data_gzip else "false")
    )
    bootstrap = bootstrap.replace(
        "if (codeGzip)", "if (%s)" % ("true" if code_gzip else "false")
    )

    start = html.index("// @@BOOTSTRAP_START@@")
    end = html.index("// @@BOOTSTRAP_END@@") + len("// @@BOOTSTRAP_END@@")
    html = html[:start] + bootstrap.strip("\n") + html[end:]
    html = html.replace(
        "<!-- @@PACKED_ASSETS@@ -->",
        '<script id="bf-assets" type="application/json">' + json.dumps(assets) + "</script>",
    )

    with open(OUT_FILE, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(html)

    size = os.path.getsize(OUT_FILE) / (1024.0 * 1024.0)
    print("wrote %s (%.1f MB, single file, runs offline)" % (os.path.relpath(OUT_FILE, ROOT), size))


if __name__ == "__main__":
    main()
