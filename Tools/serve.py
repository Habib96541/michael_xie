"""Local HTTP host with explicit WebAssembly MIME type; no compressed-file requirements."""
import argparse
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from functools import partial
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('--port',type=int,default=8187)
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]/'Builds/BrickRush_WebGL'
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.js':'application/javascript','.data':'application/octet-stream'}
    def end_headers(self):
        self.send_header('Cache-Control','no-cache')
        super().end_headers()
    def log_message(self,fmt,*values):
        if str(values[1] if len(values)>1 else '') not in ('200','304'):super().log_message(fmt,*values)
print(f'BRICK RUSH: http://localhost:{args.port}/  (Ctrl+C to stop)',flush=True)
ThreadingHTTPServer(('127.0.0.1',args.port),partial(Handler,directory=str(root))).serve_forever()
