"""Static WebGL server. python Tools/serve.py [port]. Ctrl+C to stop."""
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1] / 'WebGL_Build'
class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, '.wasm': 'application/wasm', '.unityweb': 'application/octet-stream'}
    def __init__(self, *args, **kwargs): super().__init__(*args, directory=str(ROOT), **kwargs)
    def end_headers(self):
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

port = int(sys.argv[1]) if len(sys.argv) > 1 else 8765
print(f'Hi / Lo: http://localhost:{port} — serving {ROOT}', flush=True)
ThreadingHTTPServer(('0.0.0.0', port), Handler).serve_forever()
