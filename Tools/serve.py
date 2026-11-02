"""Serve the exact uncompressed Web build; no additional packages required."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class WebHandler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, ".wasm": "application/wasm", ".data": "application/octet-stream"}

    def end_headers(self):
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8080)
    parser.add_argument("--bind", default="127.0.0.1")
    args = parser.parse_args()
    build = Path(__file__).resolve().parents[1] / "Builds" / "Web"
    if not (build / "index.html").exists():
        raise SystemExit("Builds/Web/index.html is missing. Build the game first.")
    print(f"ShatterLeap: http://{args.bind}:{args.port}", flush=True)
    ThreadingHTTPServer((args.bind, args.port), partial(WebHandler, directory=str(build))).serve_forever()
