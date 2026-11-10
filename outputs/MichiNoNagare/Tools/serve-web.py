#!/usr/bin/env python3
"""Serve a Unity Gzip release on localhost, including required MIME/encoding headers."""
import argparse
import base64
import hashlib
import json
import os
import threading
from datetime import datetime, timezone
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

class CityBackupStore:
    def __init__(self, root):
        self.root = Path(root)
        self.lock = threading.Lock()

    @staticmethod
    def validate(pair):
        if not isinstance(pair, dict) or not isinstance(pair.get('Primary'), str):
            raise ValueError('Invalid save pair')
        for key in ('Primary', 'Backup'):
            raw = pair.get(key, '')
            if key == 'Backup' and not raw:
                continue
            if not isinstance(raw, str) or len(raw.encode('utf-8')) > 900000:
                raise ValueError('Invalid save size')
            try:
                envelope = json.loads(raw)
                data = envelope['Data']
                checksum = base64.b64encode(hashlib.sha256(data.encode('utf-8')).digest()).decode()
                city = json.loads(data)
                if envelope['Version'] != 1 or envelope['Checksum'] != checksum or city['Version'] != 1 or not isinstance(city['Network'], dict):
                    raise ValueError('Invalid save checksum or version')
            except (KeyError, TypeError, json.JSONDecodeError) as exc:
                raise ValueError('Invalid save envelope') from exc
        return {'Primary': pair['Primary'], 'Backup': pair.get('Backup', '')}

    def load(self):
        with self.lock:
            for path in sorted(self.root.glob('*.json'), reverse=True):
                try:
                    return self.validate(json.loads(path.read_text()))
                except (ValueError, OSError):
                    continue
        return None

    def save(self, pair):
        pair = self.validate(pair)
        with self.lock:
            self.root.mkdir(parents=True, exist_ok=True)
            stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
            final = self.root / (stamp + '.json')
            temporary = final.with_suffix('.tmp')
            with temporary.open('w') as stream:
                json.dump(pair, stream, ensure_ascii=False)
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(temporary, final)
        return pair

class UnityHandler(SimpleHTTPRequestHandler):
    def same_origin(self):
        origin = self.headers.get('Origin')
        port = self.server.server_port
        return not origin or origin in (f'http://127.0.0.1:{port}', f'http://localhost:{port}')

    def send_json(self, code, value):
        data = json.dumps(value, ensure_ascii=False).encode('utf-8')
        self.send_response(code)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path.split('?', 1)[0] != '/api/city-save':
            return super().do_GET()
        if not self.same_origin():
            return self.send_json(403, {'error': 'Origin rejected'})
        pair = self.server.city_backups.load()
        self.send_json(200, pair or {})

    def do_POST(self):
        if self.path != '/api/city-save' or not self.same_origin():
            return self.send_json(403, {'error': 'Request rejected'})
        try:
            size = int(self.headers.get('Content-Length', '0'))
            if size <= 0 or size > 2000000:
                raise ValueError('Invalid request size')
            pair = json.loads(self.rfile.read(size))
            self.server.city_backups.save(pair)
            self.send_json(200, {'saved': True})
        except (ValueError, OSError) as exc:
            self.send_json(400, {'error': str(exc)})

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
    parser.add_argument('--save-directory', type=Path, default=None)
    args = parser.parse_args()
    root = args.directory.resolve()
    if not (root / 'index.html').is_file(): parser.error(f'No WebGL build at {root}. Run Tools/build-web.sh first.')
    server = ThreadingHTTPServer(('127.0.0.1', args.port), partial(UnityHandler, directory=str(root)))
    server.city_backups = CityBackupStore(args.save_directory or Path(__file__).resolve().parents[3] / 'work' / 'city-saves' / str(args.port))
    print(f'LUMEN / GRID: http://127.0.0.1:{args.port}/\nServing {root}. Ctrl+C to stop.', flush=True)
    try: server.serve_forever()
    except KeyboardInterrupt: pass
    finally: server.server_close()
