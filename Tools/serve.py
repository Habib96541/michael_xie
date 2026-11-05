"""Serve the real Unity build over HTTP, including the WebAssembly MIME type."""
import argparse
import functools
import http.server
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("--port", type=int, default=8767)
parser.add_argument("--bind", default="127.0.0.1")
parser.add_argument("--directory", default=str(Path(__file__).resolve().parents[1] / "Builds" / "WebGL"))
args = parser.parse_args()
class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {**http.server.SimpleHTTPRequestHandler.extensions_map, ".wasm": "application/wasm", ".data": "application/octet-stream", ".unityweb": "application/octet-stream"}
    def end_headers(self):
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()
server = http.server.ThreadingHTTPServer((args.bind, args.port), functools.partial(Handler, directory=args.directory))
print(f"Pocket Borough: http://{args.bind}:{args.port}", flush=True)
server.serve_forever()
