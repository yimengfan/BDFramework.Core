"""Talos PC 启动脚本测试。

覆盖范围：
1. Windows/macOS 共用桌面脚本会为 player 注入 -talosForceE2E 与 -talosPort。
2. 启动脚本会复用解析后的 Node CLI 执行 Playwright，而不是依赖外部 npx。
"""

from __future__ import annotations

import os
from pathlib import Path
import shutil
import stat
import subprocess


TOOLS_ROOT = Path(__file__).resolve().parents[1]
SOURCE_TEST_PC = TOOLS_ROOT / "test-pc.sh"
SOURCE_NODE_TOOLS = TOOLS_ROOT / "node-tools.sh"


def write_executable(path: Path, content: str) -> None:
    """写入可执行测试桩，模拟 launcher、node、npm 与 nc 的最小行为。"""
    path.write_text(content, encoding="utf-8")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def copy_tool_script(source_path: Path, target_path: Path) -> None:
    """复制待测脚本到临时 Playwright 根目录，并保留可执行权限。"""
    shutil.copy2(source_path, target_path)
    target_path.chmod(target_path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def test_test_pc_launches_player_with_force_e2e_args(tmp_path: Path) -> None:
    """验证桌面包启动时会强制附带 Talos E2E 启动参数，并走解析后的 Node CLI。"""
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_pc = tools_dir / "test-pc.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copy_tool_script(SOURCE_TEST_PC, copied_test_pc)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)

    launcher_args_path = tmp_path / "launcher-args.txt"
    node_args_path = tmp_path / "node-args.txt"
    npm_args_path = tmp_path / "npm-args.txt"
    ready_marker_path = tmp_path / "unity-ready.marker"

    fake_launcher = tmp_path / "Launcher.exe"
    write_executable(
        fake_launcher,
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                f"printf '%s\\n' \"$@\" > {launcher_args_path}",
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
        node_home / "node.exe",
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                f"printf '%s\\n' \"$@\" > {node_args_path}",
                "exit 0",
            ]
        )
        + "\n",
    )
    write_executable(
        node_home / "npm.cmd",
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                f"printf '%s\\n' \"$@\" > {npm_args_path}",
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
            str(copied_test_pc),
            "--exe",
            str(fake_launcher),
            "--port",
            "12345",
            "--host",
            "127.0.0.1",
            "--test-file",
            "tests/基础启动流程-e2e.spec.ts",
        ],
        capture_output=True,
        text=True,
        cwd=str(playwright_root),
        env={
            **os.environ,
            "PATH": f"{fake_bin_dir}:/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
        },
        timeout=30,
    )

    assert result.returncode == 0, result.stdout + result.stderr
    assert launcher_args_path.read_text(encoding="utf-8").splitlines() == [
        "-talosPort",
        "12345",
        "-talosForceE2E",
    ]
    node_args = node_args_path.read_text(encoding="utf-8").splitlines()
    assert str(playwright_cli_path) in node_args
    assert "tests/基础启动流程-e2e.spec.ts" in node_args
    assert npm_args_path.read_text(encoding="utf-8").strip() == "install"