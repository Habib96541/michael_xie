"""Local WebGL host. Run: python Tools/serve.py (then http://localhost:8088)."""
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import os
import sys
import threading
import webbrowser
location = Path(__file__).resolve().parent
root = location if (location / 'index.html').is_file() else location.parent / 'Builds/WebGL'
os.chdir(root)
class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, '.wasm':'application/wasm', '.data':'application/octet-stream', '.js':'application/javascript'}
    def guess_type(self, path):
        return super().guess_type(path[:-3] if path.endswith('.gz') else path)
    def end_headers(self):
        if self.path.split('?',1)[0].endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()
print('Hydraulic Press: http://localhost:8088', flush=True)
try:
    server = ThreadingHTTPServer(('0.0.0.0',8088),Handler)
except OSError:
    if '--open' in sys.argv:
        webbrowser.open('http://localhost:8088')
        raise SystemExit('The local server is already running.')
    raise SystemExit('Port 8088 is already in use.')
if '--open' in sys.argv:
    threading.Timer(.5, lambda: webbrowser.open('http://localhost:8088')).start()
server.serve_forever()
