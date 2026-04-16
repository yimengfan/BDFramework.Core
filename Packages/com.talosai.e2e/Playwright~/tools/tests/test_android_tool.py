"""Talos Android 启动脚本测试。
Talos Android launcher script tests.

覆盖范围：
1. 当 PATH 缺少 adb 时，脚本会回退到 ANDROID_SDK_ROOT 下的 platform-tools/adb。
2. Android 工具脚本会使用解析后的 adb 执行安装、端口转发、启动应用与清理流程。
3. Playwright 启动仍复用解析后的 Node CLI，而不是依赖外部 npx。

Coverage:
1. When PATH does not contain adb, the script falls back to platform-tools/adb under ANDROID_SDK_ROOT.
2. The Android tool script uses the resolved adb for install, port-forward, app launch, and cleanup.
3. Playwright startup still reuses the resolved Node CLI instead of depending on external npx.
"""

from __future__ import annotations

import os
from pathlib import Path
import shutil
import stat
import subprocess


TOOLS_ROOT = Path(__file__).resolve().parents[1]
SOURCE_TEST_ANDROID = TOOLS_ROOT / "test-android.sh"
SOURCE_NODE_TOOLS = TOOLS_ROOT / "node-tools.sh"


def write_executable(path: Path, content: str) -> None:
    """写入可执行测试桩，模拟 adb、node、npm 与 nc 的最小行为。
    Write an executable test stub that simulates the minimal adb, node, npm, and nc behavior.
    """
    path.write_text(content, encoding="utf-8")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def copy_tool_script(source_path: Path, target_path: Path) -> None:
    """复制待测脚本到临时 Playwright 根目录，并保留可执行权限。
    Copy the script under test into the temporary Playwright root while preserving executable permissions.
    """
    shutil.copy2(source_path, target_path)
    target_path.chmod(target_path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


def test_test_android_resolves_adb_from_android_sdk_root_without_path(tmp_path: Path) -> None:
    """验证 Android 启动脚本会在 PATH 缺少 adb 时回退到 SDK 目录，并完成最小启动闭环。
    Verify that the Android launcher script falls back to the SDK directory when PATH lacks adb and completes the minimal startup loop.
    """
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_android = tools_dir / "test-android.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copy_tool_script(SOURCE_TEST_ANDROID, copied_test_android)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)

    fake_apk = tmp_path / "com.talos.BuildTest.debug.apk"
    fake_apk.write_text("stub apk", encoding="utf-8")

    adb_log_path = tmp_path / "adb-args.txt"
    node_args_path = tmp_path / "node-args.txt"
    node_env_path = tmp_path / "node-env.txt"
    npm_args_path = tmp_path / "npm-args.txt"

    android_sdk_root = tmp_path / "android-sdk"
    platform_tools_dir = android_sdk_root / "platform-tools"
    platform_tools_dir.mkdir(parents=True)
    write_executable(
        platform_tools_dir / "adb.exe",
        "\n".join(
            [
                "#!/bin/bash",
                "set -euo pipefail",
                f"printf '%s\\n' \"$*\" >> {adb_log_path}",
                "if [[ \"${1:-}\" == \"-s\" ]]; then",
                "  shift 2",
                "fi",
                "case \"${1:-}\" in",
                "  devices)",
                "    printf 'List of devices attached\\nemulator-5554\\tdevice\\n'",
                "    ;;",
                "  *)",
                "    exit 0",
                "    ;;",
                "esac",
            ]
        )
        + "\n",
    )

    fake_bin_dir = tmp_path / "fake-bin"
    fake_bin_dir.mkdir()
    write_executable(fake_bin_dir / "nc", "#!/bin/bash\nexit 0\n")

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
            str(copied_test_android),
            "--apk",
            str(fake_apk),
            "--port",
            "12345",
            "--serial",
            "emulator-5554",
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
            "ANDROID_SDK_ROOT": str(android_sdk_root),
        },
        timeout=30,
    )

    assert result.returncode == 0, result.stdout + result.stderr
    adb_log_lines = adb_log_path.read_text(encoding="utf-8").splitlines()
    assert any(line.startswith("-s emulator-5554 devices") for line in adb_log_lines)
    assert any(line == f"-s emulator-5554 install -r -t {fake_apk}" for line in adb_log_lines)
    assert any(line == "-s emulator-5554 forward tcp:12345 tcp:12345" for line in adb_log_lines)
    assert any(
        line == "-s emulator-5554 shell am start -n com.talos.BuildTest.debug/com.unity3d.player.UnityPlayerActivity -e unity -talosPort 12345 -talosForceE2E"
        for line in adb_log_lines
    )
    assert any(line == "-s emulator-5554 shell am force-stop com.talos.BuildTest.debug" for line in adb_log_lines)
    assert f"adb={platform_tools_dir / 'adb.exe'}" in result.stdout

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
    assert "--project=android" in node_args
    assert npm_args_path.read_text(encoding="utf-8").strip() == "install"