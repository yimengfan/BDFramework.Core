import argparse
import pathlib
import posixpath
import sys
import urllib.parse
from typing import Iterable, Optional, Sequence, Union

import requests


class FileBrowserClient:
    def __init__(self, base_url: str, token: Optional[str] = None,
                 username: Optional[str] = None, password: Optional[str] = None,
                 verify_tls: bool = True) -> None:
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()
        self.session.verify = verify_tls
        if token:
            self.session.headers["Authorization"] = f"Bearer {token}"
        elif username and password:
            self.session.auth = (username, password)

    def ensure_directory(self, remote_dir: str) -> None:
        cleaned = remote_dir.strip("/")
        if not cleaned:
            return
        parts = cleaned.split("/")
        parent = ""
        for part in parts:
            target_parent = parent
            url = f"{self.base_url}/api/resources"
            if target_parent:
                url = f"{url}/{urllib.parse.quote(target_parent, safe='')}"
            payload = {"operation": "mkdir", "items": [part]}
            response = self.session.post(url, json=payload)
            if response.status_code in (200, 201, 204, 409):
                parent = f"{parent}/{part}" if parent else part
                continue
            response.raise_for_status()
            parent = f"{parent}/{part}" if parent else part

    def upload_file(self, local_file: pathlib.Path, remote_dir: str) -> None:
        safe_dir = remote_dir.strip("/")
        endpoint = f"{self.base_url}/api/files"
        if safe_dir:
            endpoint = f"{endpoint}/{urllib.parse.quote(safe_dir, safe='')}"
        with local_file.open("rb") as fh:
            files = {"files": (local_file.name, fh, "application/octet-stream")}
            response = self.session.post(endpoint, files=files)
            response.raise_for_status()


def iter_files(source: pathlib.Path) -> Iterable[pathlib.Path]:
    if source.is_file():
        yield source
        return
    for path in sorted(source.rglob("*")):
        if path.is_file():
            yield path


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Upload a local file or directory into FileBrowser"
    )
    parser.add_argument("source", type=pathlib.Path,
                        help="Local file or directory to upload")
    parser.add_argument("remote_dir",
                        help="Remote directory inside FileBrowser where data will be stored")
    parser.add_argument("--endpoint", default="http://127.0.0.1:8080",
                        help="Base URL of the FileBrowser instance")
    parser.add_argument("--token", help="Optional API token for FileBrowser authentication")
    parser.add_argument("--username", help="Optional username for basic authentication")
    parser.add_argument("--password", help="Optional password for basic authentication")
    parser.add_argument("--insecure", action="store_true",
                        help="Skip TLS certificate verification")
    args_list = list(argv) if argv is not None else None
    return parser.parse_args(args_list)


def run_upload(
    source: Union[pathlib.Path, str],
    remote_dir: str,
    *,
    endpoint: str = "http://127.0.0.1:8080",
    token: Optional[str] = None,
    username: Optional[str] = None,
    password: Optional[str] = None,
    insecure: bool = False,
) -> int:
    source_path = pathlib.Path(source).resolve()
    if not source_path.exists():
        print(f"Source path not found: {source_path}", file=sys.stderr)
        return 1

    print(f"FileBrowser endpoint: {endpoint}")
    print(f"Uploading '{source_path}' into remote directory '{remote_dir}'")

    client = FileBrowserClient(
        base_url=endpoint,
        token=token,
        username=username,
        password=password,
        verify_tls=not insecure,
    )

    for local_path in iter_files(source_path):
        if source_path.is_file():
            relative = local_path.name
        else:
            relative = local_path.relative_to(source_path).as_posix()
        target_dir = remote_dir.strip("/")
        parent = posixpath.dirname(relative) if "/" in relative else ""
        if parent:
            combined_dir = posixpath.join(target_dir, parent) if target_dir else parent
        else:
            combined_dir = target_dir
        print(f"Ensuring remote directory: '{combined_dir or '/'}'")
        client.ensure_directory(combined_dir)
        remote_parent = combined_dir
        print(f"Uploading file: {local_path}")
        client.upload_file(local_path, remote_parent)
        print(
            f"Uploaded {local_path} -> "
            f"{posixpath.join(remote_parent, local_path.name) if remote_parent else local_path.name}"
        )

    return 0


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    return run_upload(
        source=args.source,
        remote_dir=args.remote_dir,
        endpoint=args.endpoint,
        token=args.token,
        username=args.username,
        password=args.password,
        insecure=args.insecure,
    )


if __name__ == "__main__":
    raise SystemExit(main())
