"""Talos Batchmode 启动脚本测试。
Talos batchmode launcher script tests.

覆盖范围：
1. 本地 batchmode TCP 入口必须允许显式锁定 Playwright spec，避免本地 gate 与远端真机跑到不同文件。
2. TCP 模式下仍需复用解析后的 Node CLI，而不是退回到外部 npx。

Coverage:
1. The local batchmode TCP entry must allow an explicit Playwright spec selection so the local gate cannot drift away from the remote device file.
2. TCP mode must still reuse the resolved Node CLI instead of falling back to external npx.
"""

from __future__ import annotations

import os
from pathlib import Path
import shutil
import stat
import subprocess


TOOLS_ROOT = Path(__file__).resolve().parents[1]
SOURCE_TEST_BATCHMODE = TOOLS_ROOT / "test-batchmode.sh"
SOURCE_NODE_TOOLS = TOOLS_ROOT / "node-tools.sh"


def write_executable(path: Path, content: str) -> None:
    """写入最小可执行测试桩。
    Write a minimal executable test stub.
    """

    path.write_text(content, encoding="utf-8")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def copy_tool_script(source_path: Path, target_path: Path) -> None:
    """复制待测脚本到临时 Playwright 根目录并保留执行权限。
    Copy the script under test into the temporary Playwright root and preserve the executable bit.
    """

    shutil.copy2(source_path, target_path)
    target_path.chmod(target_path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def test_test_batchmode_tcp_honours_explicit_playwright_test_file(tmp_path: Path) -> None:
    """验证 batchmode TCP 入口会把显式 test-file 透传给 Playwright CLI。
    Verify that the batchmode TCP entry forwards an explicit test-file to the Playwright CLI.
    """

    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_batchmode = tools_dir / "test-batchmode.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copy_tool_script(SOURCE_TEST_BATCHMODE, copied_test_batchmode)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)

    project_root = tmp_path / "UnityProject"
    (project_root / "Assets").mkdir(parents=True)
    (project_root / "Logs").mkdir(parents=True)

    unity_args_path = tmp_path / "unity-args.txt"
    node_args_path = tmp_path / "node-args.txt"
    ready_marker_path = tmp_path / "unity-ready.marker"

    fake_unity = tmp_path / "Unity"
    write_executable(
        fake_unity,
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                f"printf '%s\\n' \"$@\" > {unity_args_path}",
                f"touch {ready_marker_path}",
                "trap 'exit 0' TERM INT",
                "while true; do",
                "  sleep 1",
                "done",
            ]
        )
        + "\n",
    )

    fake_bin_dir = tmp_path / "fake-bin"
    fake_bin_dir.mkdir()
    write_executable(
        fake_bin_dir / "nc",
        "\n".join(
            [
                "#!/bin/bash",
                f"[[ -f {ready_marker_path} ]]",
            ]
        )
        + "\n",
    )

    node_home = tmp_path / "node-home"
    node_home.mkdir()
    playwright_cli_path = playwright_root / "node_modules" / "@playwright" / "test" / "cli.js"
    write_executable(
        node_home / "node",
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                "if [[ \"${1:-}\" == \"-e\" ]]; then",
                "  exit 0",
                "fi",
                f"printf '%s\\n' \"$@\" > {node_args_path}",
                "exit 0",
            ]
        )
        + "\n",
    )
    write_executable(
        node_home / "npm",
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                f"mkdir -p {playwright_cli_path.parent}",
                f"cat <<'EOF' > {playwright_cli_path}",
                "console.log('stub playwright cli');",
                "EOF",
            ]
        )
        + "\n",
    )

    result = subprocess.run(
        [
            "/bin/bash",
            str(copied_test_batchmode),
            "--test-file",
            "tests/testBaseFlow-e2e.spec.ts",
        ],
        capture_output=True,
        text=True,
        cwd=str(playwright_root),
        env={
            **os.environ,
            "PATH": f"{fake_bin_dir}:/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
            "UNITY_PATH": str(fake_unity),
            "PROJECT_PATH": str(project_root),
        },
        timeout=30,
    )

    assert result.returncode == 0, result.stdout + result.stderr
    assert "LaunchTalosE2EEditorOnly" in unity_args_path.read_text(encoding="utf-8")
    node_args = node_args_path.read_text(encoding="utf-8").splitlines()
    assert str(playwright_cli_path) in node_args
    assert "--project=batchmode" in node_args
    assert "tests/testBaseFlow-e2e.spec.ts" in node_args