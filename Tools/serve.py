"""Serve the compiled WebGL build locally, including its correct WASM MIME type."""
from functools import partial
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import argparse

parser=argparse.ArgumentParser()
parser.add_argument('--port',type=int,default=8783)
parser.add_argument('--bind',default='127.0.0.1')
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]/'Builds/WebGL'
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.js':'application/javascript','.unityweb':'application/octet-stream'}
    def log_message(self,*args): pass
server=ThreadingHTTPServer((args.bind,args.port),partial(Handler,directory=str(root)))
print(f'Chicken Rush: http://{args.bind}:{args.port}/',flush=True)
server.serve_forever()
