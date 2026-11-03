"""Local Unity WebGL preview with the same Brotli headers as Cloudflare Pages."""
import argparse
import functools
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit


class WebGameHandler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map,
                      '.wasm': 'application/wasm', '.js': 'application/javascript',
                      '.data': 'application/octet-stream'}

    def guess_type(self, path):
        return super().guess_type(path[:-3] if path.endswith('.br') else path)

    def end_headers(self):
        if urlsplit(self.path).path.endswith('.br'):
            self.send_header('Content-Encoding', 'br')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=4173)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1] / 'Builds' / 'WebGL'
    handler = functools.partial(WebGameHandler, directory=str(root))
    server = ThreadingHTTPServer(('127.0.0.1', args.port), handler)
    print(f'Local: http://127.0.0.1:{args.port}', flush=True)
    server.serve_forever()
