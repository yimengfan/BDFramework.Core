from __future__ import annotations

import sys
from pathlib import Path


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))


def pytest_addoption(parser) -> None:
    parser.addoption(
        "--run-remote-artifact-tests",
        action="store_true",
        default=False,
        help="run integration tests that upload real files to the configured remote artifact file server",
    )


def pytest_configure(config) -> None:
    config.addinivalue_line(
        "markers",
        "remote_artifact: integration test that uploads to the configured remote artifact file server",
    )