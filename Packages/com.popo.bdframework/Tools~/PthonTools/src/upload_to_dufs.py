import argparse
import pathlib
import posixpath
import sys
import urllib.parse
from typing import Iterable, Optional, Sequence, Union

import requests


class DufsClient:
    def __init__(self, base_url: str, verify_tls: bool = True) -> None:
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()
        self.session.verify = verify_tls

    def _build_url(self, remote_path: str = "") -> str:
        cleaned = remote_path.strip("/")
        if not cleaned:
            return self.base_url
        return f"{self.base_url}/{urllib.parse.quote(cleaned, safe='/')}"

    def upload_file(self, local_file: pathlib.Path, remote_path: str) -> None:
        target = remote_path.strip("/")
        url = self._build_url(target or local_file.name)

        def _send():
            with local_file.open("rb") as fh:
                return self.session.put(url, data=fh)

        response = _send()
        if response.status_code == 403:
            self.delete_path(target or local_file.name)
            response = _send()
        response.raise_for_status()

    def delete_path(self, remote_path: str) -> None:
        target = remote_path.strip("/")
        if not target:
            return
        response = self.session.delete(self._build_url(target))
        if response.status_code not in (200, 204, 404):
            response.raise_for_status()

    def download_file(self, remote_path: str, local_path: pathlib.Path) -> None:
        resp = self.session.get(self._build_url(remote_path), stream=True)
        resp.raise_for_status()
        local_path.parent.mkdir(parents=True, exist_ok=True)
        with local_path.open("wb") as f:
            for chunk in resp.iter_content(chunk_size=8192):
                if chunk:
                    f.write(chunk)


def iter_files(source: pathlib.Path) -> Iterable[pathlib.Path]:
    if source.is_file():
        yield source
        return
    for path in sorted(source.rglob("*")):
        if path.is_file():
            yield path




def run_upload(
    source: Union[pathlib.Path, str],
    remote_dir: str,
    *,
    endpoint: str = "http://127.0.0.1:8080",
    insecure: bool = False,
) -> int:
    source_path = pathlib.Path(source).resolve()
    if not source_path.exists():
        print(f"Source path not found: {source_path}", file=sys.stderr)
        return 1

    print(f"Dufs endpoint: {endpoint}")
    print(f"Uploading '{source_path}' into remote directory '{remote_dir}'")

    client = DufsClient(
        base_url=endpoint,
        verify_tls=not insecure,
    )
    remote_root = remote_dir.strip("/")

    for local_path in iter_files(source_path):
        relative = local_path.name if source_path.is_file() else local_path.relative_to(source_path).as_posix()
        remote_path = posixpath.join(remote_root, relative) if remote_root else relative
        print(f"Uploading file: {local_path} -> /{remote_path}")
        client.upload_file(local_path, remote_path)

    return 0


def run_download(
    remote_path: str,
    local_path: Union[pathlib.Path, str],
    *,
    endpoint: str = "http://127.0.0.1:8080",
    insecure: bool = False,
) -> int:
    local = pathlib.Path(local_path).resolve()
    print(f"Dufs endpoint: {endpoint}")
    print(f"Downloading '{remote_path}' -> '{local}'")

    client = DufsClient(
        base_url=endpoint,
        verify_tls=not insecure,
    )
    client.download_file(remote_path, local)
    print("Download completed.")
    return 0


def main(argv: Optional[Sequence[str]] = None) -> int:
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
