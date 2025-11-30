import http.server
import json
import socketserver
import sys
import threading
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT / "src") not in sys.path:
    sys.path.insert(0, str(ROOT / "src"))

import upload_to_filebrowser


def test_main_uploads_single_file(tmp_path):
    requests_log = []

    class RequestHandler(http.server.BaseHTTPRequestHandler):
        def do_POST(self):  # type: ignore[override]
            length = int(self.headers.get("Content-Length", "0"))
            body = self.rfile.read(length)
            requests_log.append({
                "path": self.path,
                "headers": dict(self.headers),
                "body": body,
            })
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"{}")

        def log_message(self, format, *args):  # noqa: D401 - silence default logging
            return

    class ThreadedServer(socketserver.ThreadingMixIn, http.server.HTTPServer):
        daemon_threads = True

    server = ThreadedServer(("127.0.0.1", 0), RequestHandler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        endpoint = f"http://192.168.0.240:20000"
        print(f"[test] Mock FileBrowser endpoint: {endpoint}")

        local_file = tmp_path / "demo.txt"
        local_file.write_text("hello", encoding="utf-8")
        print(f"[test] Created temp file: {local_file}")

        exit_code = upload_to_filebrowser.run_upload(
            source=local_file,
            remote_dir="/uploads",
            endpoint=endpoint,
        )

        print(f"[test] run_upload exited with code: {exit_code}")

        assert exit_code == 0

        # Give the server a moment to record both POST calls
        for _ in range(50):
            if len(requests_log) >= 2:
                break
            time.sleep(0.01)

        print(f"[test] Captured {len(requests_log)} HTTP POST calls")
        for idx, call in enumerate(requests_log, start=1):
            print(f"[test] Call {idx}: path={call['path']}, body-length={len(call['body'])}")

        assert len(requests_log) >= 2

        mkdir_call = requests_log[0]
        assert mkdir_call["path"].startswith("/api/resources")
        payload = json.loads(mkdir_call["body"].decode("utf-8"))
        assert payload == {"operation": "mkdir", "items": ["uploads"]}

        upload_call = next(call for call in requests_log if call["path"].startswith("/api/files"))
        assert upload_call["path"].endswith("/api/files/uploads")
        assert b"hello" in upload_call["body"], "Uploaded payload should contain file bytes"
    finally:
        server.shutdown()
        thread.join(timeout=1)
