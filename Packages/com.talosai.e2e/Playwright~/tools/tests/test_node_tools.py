"""Talos Playwright Node 工具解析脚本测试。

覆盖范围：
1. Windows TeamCity service 缺少 PATH 时，可通过 Node 安装目录稳定解析 node/npm。
2. Playwright 依赖安装会调用解析后的 npm，并确保 CLI 文件准备完成。
"""

from __future__ import annotations

import json
import os
from pathlib import Path
import shlex
import shutil
import socket
import stat
import subprocess
import threading


TOOLS_ROOT = Path(__file__).resolve().parents[1]
NODE_TOOLS_PATH = TOOLS_ROOT / "node-tools.sh"


def write_executable(path: Path, content: str) -> None:
    """写入可执行测试桩，模拟 node 或 npm 在 CI 环境中的最小行为。"""
    path.write_text(content, encoding="utf-8")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def run_node_tools(script_body: str, env: dict[str, str]) -> subprocess.CompletedProcess[str]:
    """在独立 bash 进程里 source 公共脚本，并执行给定的断言片段。"""
    shell_script = "\n".join(
        [
            "set -euo pipefail",
            f"source {shlex.quote(str(NODE_TOOLS_PATH))}",
            script_body,
        ]
    )
    merged_env = os.environ.copy()
    merged_env.update(env)
    return subprocess.run(
        ["/bin/bash", "-c", shell_script],
        capture_output=True,
        text=True,
        env=merged_env,
        cwd=str(TOOLS_ROOT),
    )


def test_ensure_talos_node_tooling_resolves_node_home_without_path(tmp_path: Path) -> None:
    """验证当 PATH 缺少 node/npm 时，脚本会回退到显式提供的 Node 安装目录。"""
    node_home = tmp_path / "nodejs"
    node_home.mkdir()
    node_bin = node_home / "node.exe"
    npm_bin = node_home / "npm.cmd"
    write_executable(node_bin, "#!/usr/bin/env bash\nexit 0\n")
    write_executable(npm_bin, "#!/usr/bin/env bash\nexit 0\n")

    result = run_node_tools(
        'ensure_talos_node_tooling\nprintf "%s\\n%s\\n" "$TALOS_NODE_BIN" "$TALOS_NPM_BIN"',
        {
            "PATH": "/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
        },
    )

    assert result.returncode == 0, result.stderr
    output_lines = [line for line in result.stdout.splitlines() if line]
    assert output_lines[-2:] == [str(node_bin), str(npm_bin)]


def test_ensure_talos_playwright_dependencies_uses_resolved_npm_install(tmp_path: Path) -> None:
    """验证依赖安装会调用解析后的 npm，并在安装后确认 Playwright CLI 已生成。"""
    node_home = tmp_path / "nodejs"
    node_home.mkdir()
    playwright_dir = tmp_path / "PlaywrightRoot"
    playwright_dir.mkdir()
    npm_log_path = tmp_path / "npm-install.log"
    cli_path = playwright_dir / "node_modules" / "@playwright" / "test" / "cli.js"

    write_executable(node_home / "node.exe", "#!/usr/bin/env bash\nexit 0\n")
    write_executable(
        node_home / "npm.cmd",
        "\n".join(
            [
                "#!/usr/bin/env bash",
                "set -euo pipefail",
                f"mkdir -p {shlex.quote(str(cli_path.parent))}",
                f"printf '%s\\n' \"$@\" > {shlex.quote(str(npm_log_path))}",
                f"cat <<'EOF' > {shlex.quote(str(cli_path))}",
                "console.log('stub playwright cli');",
                "EOF",
            ]
        )
        + "\n",
    )

    result = run_node_tools(
        "\n".join(
            [
                f"ensure_talos_playwright_dependencies {shlex.quote(str(playwright_dir))}",
                f"test -f {shlex.quote(str(cli_path))}",
            ]
        ),
        {
            "PATH": "/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
        },
    )

    assert result.returncode == 0, result.stderr
    assert cli_path.is_file()
    assert npm_log_path.read_text(encoding="utf-8").strip() == "install"


def test_probe_talos_tcp_port_falls_back_to_node_when_nc_missing(tmp_path: Path) -> None:
    """验证缺少 nc 时，会回退到解析出的 Node 可执行文件完成 TCP 探测。"""
    node_home = tmp_path / "nodejs"
    node_home.mkdir()
    node_bin = node_home / "node.exe"
    write_executable(node_bin, "#!/bin/bash\nexit 0\n")

    result = run_node_tools(
        'probe_talos_tcp_port 127.0.0.1 10002',
        {
            "PATH": str(tmp_path / "missing-bin"),
            "NODE_BIN": str(node_bin),
            "TALOS_NODEJS_HOME": str(node_home),
        },
    )

    assert result.returncode == 0, result.stderr


def test_probe_talos_unity_ready_rejects_socket_that_closes_without_hello_ack(tmp_path: Path) -> None:
    """验证 Unity 就绪探测不会把“能连上但立即断开”的端口误判成 ready。
    Verify that the Unity readiness probe does not treat a port that accepts and closes immediately as ready.
    """
    node_bin = shutil.which("node")
    assert node_bin, "node executable is required for readiness probe tests"

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind(("127.0.0.1", 0))
    server.listen(1)
    port = server.getsockname()[1]

    def close_on_accept() -> None:
        connection, _ = server.accept()
        connection.close()
        server.close()

    thread = threading.Thread(target=close_on_accept, daemon=True)
    thread.start()

    result = run_node_tools(
        f'probe_talos_unity_ready 127.0.0.1 {port} 1000',
        {
            "PATH": str(tmp_path / "missing-bin"),
            "NODE_BIN": node_bin,
        },
    )

    thread.join(timeout=5)
    assert result.returncode != 0


def test_ensure_talos_adb_tooling_derives_sdk_from_unity_path(tmp_path: Path) -> None:
    """验证当 PATH 缺少 adb 时，脚本会从 UNITY_PATH 推导出 Unity Android SDK 下的 adb。
    Verify that when PATH lacks adb, the script derives adb from the Unity Android SDK located by UNITY_PATH.
    """
    unity_editor_dir = tmp_path / "UnityEditor" / "Editor"
    unity_editor_dir.mkdir(parents=True)
    unity_executable = unity_editor_dir / "Unity.exe"
    unity_executable.write_text("stub unity", encoding="utf-8")

    adb_path = (
        unity_editor_dir
        / "Data"
        / "PlaybackEngines"
        / "AndroidPlayer"
        / "SDK"
        / "platform-tools"
        / "adb.exe"
    )
    adb_path.parent.mkdir(parents=True)
    write_executable(adb_path, "#!/usr/bin/env bash\nexit 0\n")

    result = run_node_tools(
        'ensure_talos_adb_tooling\nprintf "%s\\n" "$TALOS_ADB_BIN"',
        {
            "PATH": "/usr/bin:/bin",
            "UNITY_PATH": str(unity_executable),
        },
    )

    assert result.returncode == 0, result.stderr
    output_lines = [line for line in result.stdout.splitlines() if line]
    assert output_lines[-1] == str(adb_path)