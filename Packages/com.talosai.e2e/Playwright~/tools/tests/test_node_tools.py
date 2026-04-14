"""Talos Playwright Node 工具解析脚本测试。

覆盖范围：
1. Windows TeamCity service 缺少 PATH 时，可通过 Node 安装目录稳定解析 node/npm。
2. Playwright 依赖安装会调用解析后的 npm，并确保 CLI 文件准备完成。
"""

from __future__ import annotations

import os
from pathlib import Path
import shlex
import stat
import subprocess


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
        ["bash", "-lc", shell_script],
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