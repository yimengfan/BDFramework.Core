"""Talos PC 启动脚本测试。
Talos PC launcher script tests.

覆盖范围：
1. Windows/macOS 共用桌面脚本会为 player 注入 Talos E2E 参数，并在非 Windows Git Bash 桌面路径上附带窗口模式参数。
2. Windows Git Bash 分支会改用 batchmode + 更保守的启动参数，避免远端无头 agent 在图形初始化阶段卡死。
3. 启动脚本会复用解析后的 Node CLI 执行 Playwright，而不是依赖外部 npx。
4. 桌面脚本允许通过 TALOS_UNITY_TCP_TIMEOUT 覆盖 TCP 就绪等待上限。

Coverage:
1. The shared desktop launcher script injects Talos E2E arguments and keeps window-mode arguments on non-Windows-Git-Bash desktop launches.
2. The Windows Git Bash branch switches to batchmode plus a more conservative launch contract so remote headless agents do not stall during graphics initialization.
3. The launcher script reuses the resolved Node CLI to run Playwright instead of relying on external npx.
4. The desktop launcher allows overriding the TCP readiness timeout through TALOS_UNITY_TCP_TIMEOUT.
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
    """写入可执行测试桩，模拟 launcher、node、npm 与 nc 的最小行为。
    Write an executable test stub that simulates the minimal launcher, node, npm, and nc behavior.
    """
    path.write_text(content, encoding="utf-8")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def copy_tool_script(source_path: Path, target_path: Path) -> None:
    """复制待测脚本到临时 Playwright 根目录，并保留可执行权限。
    Copy the script under test into the temporary Playwright root while preserving executable permissions.
    """
    shutil.copy2(source_path, target_path)
    target_path.chmod(target_path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def test_test_pc_launches_player_with_force_e2e_args(tmp_path: Path) -> None:
    """验证桌面包启动时会强制附带 Talos E2E 与窗口模式参数，并走解析后的 Node CLI。
    Verify that desktop package startup forces Talos E2E and window-mode arguments and uses the resolved Node CLI.
    """
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
    node_env_path = tmp_path / "node-env.txt"
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
                "if [[ \"${1:-}\" == \"-e\" ]]; then",
                "  exit 0",
                "fi",
                f"printf '%s\\n' \"$@\" > {node_args_path}",
                f"printf '%s\\n%s\\n%s\\n%s\\n%s\\n' \"${{PLAYWRIGHT_HTML_OUTPUT_DIR:-}}\" \"${{PLAYWRIGHT_HTML_REPORT:-}}\" \"${{PLAYWRIGHT_HTML_OPEN:-}}\" \"${{PW_TEST_HTML_REPORT_OPEN:-}}\" \"${{PLAYWRIGHT_JUNIT_OUTPUT_FILE:-}}\" > {node_env_path}",
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
            "tests/testBaseFlow-e2e.spec.ts",
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
        "-screen-fullscreen",
        "0",
        "-screen-width",
        "1280",
        "-screen-height",
        "720",
        "-popupwindow",
        "-logFile",
        "-",
    ]
    node_args = node_args_path.read_text(encoding="utf-8").splitlines()
    assert str(playwright_cli_path) in node_args
    assert "tests/testBaseFlow-e2e.spec.ts" in node_args
    assert "--reporter=list,html,junit" in node_args
    assert node_env_path.read_text(encoding="utf-8").splitlines() == [
        str(playwright_root / "test-results" / "html"),
        str(playwright_root / "test-results" / "html"),
        "never",
        "never",
        str(playwright_root / "test-results" / "junit.xml"),
    ]
    assert npm_args_path.read_text(encoding="utf-8").strip() == "install"


def test_test_pc_honours_unity_tcp_timeout_override(tmp_path: Path) -> None:
    """验证桌面脚本支持通过 TALOS_UNITY_TCP_TIMEOUT 覆盖 TCP 就绪等待上限。
    Verify that the desktop script supports overriding the TCP readiness wait limit through TALOS_UNITY_TCP_TIMEOUT.
    """
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_pc = tools_dir / "test-pc.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copy_tool_script(SOURCE_TEST_PC, copied_test_pc)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)

    fake_launcher = tmp_path / "Launcher.exe"
    write_executable(
        fake_launcher,
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
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
                "exit 1",
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
                "if [[ \"${1:-}\" == \"-e\" ]]; then",
                "  if [[ $# -gt 2 ]]; then",
                "    exit 1",
                "  fi",
                "  exit 0",
                "fi",
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
        ],
        capture_output=True,
        text=True,
        cwd=str(playwright_root),
        env={
            **os.environ,
            "PATH": f"{fake_bin_dir}:/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
            "TALOS_UNITY_TCP_TIMEOUT": "2",
        },
        timeout=15,
    )

    assert result.returncode != 0
    assert "等待 TCP 服务超时 (2s)" in result.stdout


def test_test_pc_source_uses_batchmode_on_windows_git_bash() -> None:
    """验证 Windows Git Bash 分支会开启 batchmode，并且不再强制固定分辨率与 popupwindow。
    Verify that the Windows Git Bash branch enables batchmode and no longer forces fixed resolution or popupwindow.
    """

    content = SOURCE_TEST_PC.read_text(encoding="utf-8")

    assert 'PLAYER_LAUNCH_ARGS=("-batchmode" "${PLAYER_LAUNCH_ARGS[@]}")' in content
    assert "elif ! ${IS_WINDOWS_GIT_BASH}; then" in content
    assert 'Start-Process -FilePath' in content
    assert "@('-batchmode','-talosPort','${UNITY_PORT}','-talosForceE2E','-screen-fullscreen','0','-logFile','${PLAYER_LOG_FILE_WIN}')" in content
    assert "@('-talosPort','${UNITY_PORT}','-talosForceE2E','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-popupwindow','-logFile','${PLAYER_LOG_FILE_WIN}')" not in content
