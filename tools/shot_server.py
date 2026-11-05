#!/usr/bin/env python
"""Tiny screenshot receiver used while playtesting the WebGL build.

The game posts PNG bytes to http://localhost:8124/shot?name=<name> and this server
writes them into ../screenshots so they can be inspected as files.
"""
import http.server
import os
import urllib.parse

ROOT = os.path.dirname(os.path.abspath(__file__))
SHOT_DIR = os.path.normpath(os.path.join(ROOT, "..", "screenshots"))
os.makedirs(SHOT_DIR, exist_ok=True)


class Handler(http.server.SimpleHTTPRequestHandler):
    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "POST, GET, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")

    def do_OPTIONS(self):
        self.send_response(204)
        self._cors()
        self.end_headers()

    def do_GET(self):
        if self.path.startswith("/health"):
            body = b"ok"
            self.send_response(200)
            self._cors()
            self.send_header("Content-Type", "text/plain")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        self.send_response(404)
        self._cors()
        self.end_headers()

    def do_POST(self):
        parsed = urllib.parse.urlparse(self.path)
        if parsed.path != "/shot":
            self.send_response(404)
            self._cors()
            self.end_headers()
            return

        name = urllib.parse.parse_qs(parsed.query).get("name", ["shot"])[0]
        name = "".join(c for c in name if c.isalnum() or c in "-_.")
        if not name.endswith(".png"):
            name += ".png"

        length = int(self.headers.get("Content-Length", 0))
        data = self.rfile.read(length)
        path = os.path.join(SHOT_DIR, name)
        with open(path, "wb") as fh:
            fh.write(data)

        print("saved %s (%d bytes)" % (name, len(data)), flush=True)
        body = b"ok"
        self.send_response(200)
        self._cors()
        self.send_header("Content-Type", "text/plain")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, fmt, *args):
        pass


if __name__ == "__main__":
    port = 8124
    http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
