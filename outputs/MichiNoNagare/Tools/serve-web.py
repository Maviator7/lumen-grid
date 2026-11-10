#!/usr/bin/env python3
"""Serve a Unity Gzip release on localhost, including required MIME/encoding headers."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

class UnityHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        bare = path[:-3] if path.endswith('.gz') else path
        if bare.endswith('.wasm'): return 'application/wasm'
        if bare.endswith('.js'): return 'application/javascript'
        if bare.endswith('.data'): return 'application/octet-stream'
        return super().guess_type(bare)

    def end_headers(self):
        if self.path.split('?', 1)[0].endswith('.gz') and Path(self.translate_path(self.path)).is_file():
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-cache')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path(__file__).resolve().parents[2] / 'LumenGrid-Web')
    parser.add_argument('--port', type=int, default=8766)
    args = parser.parse_args()
    root = args.directory.resolve()
    if not (root / 'index.html').is_file(): parser.error(f'No WebGL build at {root}. Run Tools/build-web.sh first.')
    server = ThreadingHTTPServer(('127.0.0.1', args.port), partial(UnityHandler, directory=str(root)))
    print(f'LUMEN / GRID: http://127.0.0.1:{args.port}/\nServing {root}. Ctrl+C to stop.', flush=True)
    try: server.serve_forever()
    except KeyboardInterrupt: pass
    finally: server.server_close()
