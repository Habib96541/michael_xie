#!/usr/bin/env python3
"""Drive the WebGL build in headless Chrome over the DevTools protocol.

Used to playtest the offline single-file build: real key events, real GPU-less
rendering, real screenshots, and a reload to confirm the best score persists.

    python tools/playtest.py [file-or-url]
"""

import base64
import json
import os
import subprocess
import sys
import tempfile
import time
import urllib.request

import websocket

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHOTS = os.path.join(ROOT, "screenshots")
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
PORT = 9223
DEFAULT_URL = "file:///" + os.path.join(ROOT, "BottleFlip.html").replace("\\", "/")
COUNTER = [0]


class Cdp:
    def __init__(self, ws_url):
        self.ws = websocket.create_connection(ws_url, timeout=60)

    def send(self, method, **params):
        COUNTER[0] += 1
        mid = COUNTER[0]
        self.ws.send(json.dumps({"id": mid, "method": method, "params": params}))
        while True:
            message = json.loads(self.ws.recv())
            if message.get("id") == mid:
                if "error" in message:
                    raise RuntimeError("%s -> %s" % (method, message["error"]))
                return message.get("result", {})

    def eval(self, expression):
        result = self.send(
            "Runtime.evaluate",
            expression=expression,
            returnByValue=True,
            awaitPromise=True,
        )
        return result.get("result", {}).get("value")

    def key(self, key, code, vk):
        base = {"key": key, "code": code, "windowsVirtualKeyCode": vk, "nativeVirtualKeyCode": vk}
        self.send("Input.dispatchKeyEvent", type="rawKeyDown", **base)
        time.sleep(0.05)
        self.send("Input.dispatchKeyEvent", type="keyUp", **base)

    def shot(self, name):
        data = self.send("Page.captureScreenshot", format="png")["data"]
        path = os.path.join(SHOTS, name)
        os.makedirs(SHOTS, exist_ok=True)
        with open(path, "wb") as handle:
            handle.write(base64.b64decode(data))
        print("  shot -> %s" % os.path.relpath(path, ROOT))


def state(cdp):
    raw = cdp.eval("JSON.stringify(window.__bottleFlipState || null)")
    return json.loads(raw) if raw else None


def main():
    url = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_URL
    profile = tempfile.mkdtemp(prefix="bf-chrome-")
    chrome = subprocess.Popen(
        [
            CHROME,
            "--headless=new",
            "--no-sandbox",
            "--enable-unsafe-swiftshader",
            "--use-angle=swiftshader",
            "--disable-background-timer-throttling",
            "--disable-renderer-backgrounding",
            "--window-size=900,1600",
            "--user-data-dir=" + profile,
            "--remote-debugging-port=%d" % PORT,
            url,
        ],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )

    ws_url = None
    for _ in range(60):
        try:
            targets = json.load(urllib.request.urlopen("http://127.0.0.1:%d/json/list" % PORT))
            pages = [t for t in targets if t.get("type") == "page"]
            if pages:
                ws_url = pages[0]["webSocketDebuggerUrl"]
                break
        except Exception:
            pass
        time.sleep(0.5)

    if not ws_url:
        chrome.kill()
        sys.exit("could not attach to chrome")

    cdp = Cdp(ws_url)
    cdp.send("Page.enable")
    cdp.send("Runtime.enable")

    try:
        print("loading %s" % url)
        ready = False
        for _ in range(240):
            if cdp.eval("!!document.querySelector('#loader.done')"):
                ready = True
                break
            time.sleep(0.5)
        if not ready:
            sys.exit("the build never finished loading")

        # Let Unity's built-in splash finish before capturing the title screen.
        time.sleep(4.0)
        print("  boot:", state(cdp))
        cdp.shot("play-01-title.png")

        cdp.key(" ", "Space", 32)
        time.sleep(0.9)
        print("  start:", state(cdp))

        # Flip until the 10% topple ends the run (cap it so a bad streak can't hang).
        flips = 0
        for _ in range(40):
            current = state(cdp) or {}
            if current.get("phase") == "over":
                break
            if current.get("last") == "start" or current.get("phase") == "playing":
                cdp.key(" ", "Space", 32)
                flips += 1
                time.sleep(1.05)
                if flips == 4 and (state(cdp) or {}).get("phase") == "playing":
                    cdp.shot("play-01-hud.png")
        time.sleep(3.6)

        final = state(cdp)
        print("  after %d flips:" % flips, final)
        print("  localStorage:", cdp.eval("JSON.stringify(Object.assign({}, localStorage))"))
        cdp.shot("play-02-over.png")

        cdp.send("Page.reload")
        for _ in range(240):
            if cdp.eval("!!document.querySelector('#loader.done')"):
                break
            time.sleep(0.5)
        time.sleep(1.5)
        print("  after reload:", state(cdp))
        cdp.shot("play-03-reload.png")

        errors = cdp.eval(
            "JSON.stringify((window.__bfErrors || []).slice(0, 10))"
        )
        print("  page errors:", errors)
    finally:
        try:
            cdp.ws.close()
        except Exception:
            pass
        chrome.kill()


if __name__ == "__main__":
    main()
