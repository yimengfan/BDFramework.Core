"""Talos Android 启动脚本测试。
Talos Android launcher script tests.

覆盖范围：
1. 当 PATH 缺少 adb 时，脚本会回退到 ANDROID_SDK_ROOT 下的 platform-tools/adb。
2. Android 工具脚本会使用解析后的 adb 执行安装、端口转发、启动应用与清理流程。
3. Playwright 启动仍复用解析后的 Node CLI，而不是依赖外部 npx。
4. ADB daemon 重启后，脚本会优先保留配置里的 TCP 序列号，而不是退回到不稳定的 emulator-* 别名。

Coverage:
1. When PATH does not contain adb, the script falls back to platform-tools/adb under ANDROID_SDK_ROOT.
2. The Android tool script uses the resolved adb for install, port-forward, app launch, and cleanup.
3. Playwright startup still reuses the resolved Node CLI instead of depending on external npx.
4. After an ADB daemon restart, the script keeps the configured TCP serial instead of falling back to unstable emulator-* aliases.
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
SOURCE_CONNECT_ANDROID = TOOLS_ROOT / "connect_androidVirtualDevice.sh"


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
    copied_connect_android = tools_dir / "connect_androidVirtualDevice.sh"
    copy_tool_script(SOURCE_TEST_ANDROID, copied_test_android)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)
    copy_tool_script(SOURCE_CONNECT_ANDROID, copied_connect_android)

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
    assert any(line == "-s emulator-5554 logcat -c" for line in adb_log_lines)
    assert any(
        line == "-s emulator-5554 shell am start -n com.talos.BuildTest.debug/com.unity3d.player.UnityPlayerActivity -e unity '-talosPort 12345 -talosForceE2E'"
        for line in adb_log_lines
    )
    assert any(line == "-s emulator-5554 logcat -d -v threadtime" for line in adb_log_lines)
    assert any(line == "-s emulator-5554 shell am force-stop com.talos.BuildTest.debug" for line in adb_log_lines)
    assert f"adb={platform_tools_dir / 'adb.exe'}" in result.stdout
    assert (playwright_root / "test-results" / "android-logcat.txt").exists()

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


def test_test_android_connect_targets_calls_adb_connect_before_devices(tmp_path: Path) -> None:
    """验证传入 --connect-targets 或设置 TALOS_ADB_CONNECT_TARGETS 时，脚本会在 adb devices 探测前调用 adb connect。
    Verify that when --connect-targets is given (or TALOS_ADB_CONNECT_TARGETS is set),
    the script calls adb connect for each target before running the device detection check.
    这用于 MuMu 等宿主机模拟器的修复模式：设备在 connect 之前对 adb devices 不可见。
    This covers the MuMu-emulator fix mode where the device is invisible to adb devices until connected.
    """
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_android = tools_dir / "test-android.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copied_connect_android = tools_dir / "connect_androidVirtualDevice.sh"

    copy_tool_script(SOURCE_TEST_ANDROID, copied_test_android)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)
    copy_tool_script(SOURCE_CONNECT_ANDROID, copied_connect_android)

    fake_apk = tmp_path / "com.talos.BuildTest.debug.apk"
    fake_apk.write_text("stub apk", encoding="utf-8")

    adb_log_path = tmp_path / "adb-args.txt"
    node_args_path = tmp_path / "node-args.txt"
    node_env_path = tmp_path / "node-env.txt"
    npm_args_path = tmp_path / "npm-args.txt"

    fake_bin_dir = tmp_path / "fake-bin"
    fake_bin_dir.mkdir()
    write_executable(fake_bin_dir / "nc", "#!/bin/bash\nexit 0\n")

    # ADB 桩：第一次 connect 时返回 "connected to ..." 模拟 MuMu 接受连接；
    # devices 命令在 connect 之后返回已连接设备，模拟修复成功。
    # ADB stub: return "connected to ..." for 'connect', return a device on 'devices' afterwards.
    write_executable(
        fake_bin_dir / "adb",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"printf '%s\\n' \"$*\" >> {adb_log_path}",
            "case \"${1:-}\" in",
            "  connect)",
            "    printf 'connected to %s\\n' \"${2:-}\"",
            "    ;;",
            "  devices)",
            "    printf 'List of devices attached\\n127.0.0.1:16384\\tdevice\\n'",
            "    ;;",
            "  *)",
            "    exit 0",
            "    ;;",
            "esac",
        ]) + "\n",
    )

    node_home = tmp_path / "node-home"
    node_home.mkdir()
    playwright_cli_path = playwright_root / "node_modules" / "@playwright" / "test" / "cli.js"
    write_executable(
        node_home / "node.exe",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            "if [[ \"${1:-}\" == \"-e\" ]]; then exit 0; fi",
            f"printf '%s\\n' \"$@\" > {node_args_path}",
            f"printf '%s\\n%s\\n%s\\n%s\\n%s\\n' \"${{PLAYWRIGHT_HTML_OUTPUT_DIR:-}}\" \"${{PLAYWRIGHT_HTML_REPORT:-}}\" \"${{PLAYWRIGHT_HTML_OPEN:-}}\" \"${{PW_TEST_HTML_REPORT_OPEN:-}}\" \"${{PLAYWRIGHT_JUNIT_OUTPUT_FILE:-}}\" > {node_env_path}",
            "exit 0",
        ]) + "\n",
    )
    write_executable(
        node_home / "npm.cmd",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"printf '%s\\n' \"$@\" > {npm_args_path}",
            f"mkdir -p {playwright_cli_path.parent}",
            f"cat <<'EOF' > {playwright_cli_path}",
            "console.log('stub playwright cli');",
            "EOF",
        ]) + "\n",
    )

    result = subprocess.run(
        [
            "/bin/bash",
            str(copied_test_android),
            "--apk", str(fake_apk),
            "--connect-targets", "127.0.0.1:16384,127.0.0.1:7555",
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
    adb_log_lines = adb_log_path.read_text(encoding="utf-8").splitlines()

    # connect は devices より先に呼ばれているはず。
    # 'connect' entries must appear before any 'devices' entry in the ADB call log.
    connect_indices = [i for i, line in enumerate(adb_log_lines) if line.startswith("connect ")]
    devices_indices = [i for i, line in enumerate(adb_log_lines) if "devices" in line]
    assert connect_indices, "adb connect should have been called for the emulator targets"
    assert devices_indices, "adb devices should have been called after connect"
    assert connect_indices[0] < devices_indices[0], (
        "adb connect must be called before adb devices in emulator fix mode"
    )
    assert any("connect 127.0.0.1:16384" in line for line in adb_log_lines)
    assert any("connect 127.0.0.1:7555" in line for line in adb_log_lines)


def test_test_android_start_mumu_flag_searches_process_list_and_logs(tmp_path: Path) -> None:
    """验证 --start-mumu true 时脚本会进入 MuMu 检测分支，即使在非 Windows 环境下也不报错。
    Verify that --start-mumu true causes the script to enter MuMu detection logic without
    crashing, even in a non-Windows environment where MuMu cannot actually be started.
    """
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_android = tools_dir / "test-android.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copied_connect_android = tools_dir / "connect_androidVirtualDevice.sh"
    copy_tool_script(SOURCE_TEST_ANDROID, copied_test_android)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)
    copy_tool_script(SOURCE_CONNECT_ANDROID, copied_connect_android)

    fake_apk = tmp_path / "com.talos.BuildTest.debug.apk"
    fake_apk.write_text("stub apk", encoding="utf-8")

    adb_log_path = tmp_path / "adb-args.txt"
    android_sdk_root = tmp_path / "android-sdk"
    platform_tools_dir = android_sdk_root / "platform-tools"
    platform_tools_dir.mkdir(parents=True)
    write_executable(
        platform_tools_dir / "adb.exe",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"printf '%s\\n' \"$*\" >> {adb_log_path}",
            "if [[ \"${1:-}\" == \"-s\" ]]; then shift 2; fi",
            "case \"${1:-}\" in",
            "  devices) printf 'List of devices attached\\nemulator-5554\\tdevice\\n' ;;",
            "  *) exit 0 ;;",
            "esac",
        ]) + "\n",
    )

    fake_bin_dir = tmp_path / "fake-bin"
    fake_bin_dir.mkdir()
    write_executable(fake_bin_dir / "nc", "#!/bin/bash\nexit 0\n")

    node_home = tmp_path / "node-home"
    node_home.mkdir()
    playwright_cli_path = playwright_root / "node_modules" / "@playwright" / "test" / "cli.js"
    playwright_cli_path.parent.mkdir(parents=True, exist_ok=True)
    playwright_cli_path.write_text("console.log('stub playwright cli');", encoding="utf-8")

    write_executable(
        node_home / "node",
        "\n".join([
            "#!/bin/bash",
            "exit 0",
        ]) + "\n",
    )
    write_executable(
        node_home / "npm",
        "#!/bin/bash\nexit 0\n",
    )

    result = subprocess.run(
        [
            "/bin/bash",
            str(copied_test_android),
            "--apk", str(fake_apk),
            "--start-mumu", "true",
            "--connect-targets", "127.0.0.1:16384",
        ],
        capture_output=True,
        text=True,
        cwd=str(playwright_root),
        env={
            **os.environ,
            "PATH": f"{fake_bin_dir}:/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
            "ANDROID_SDK_ROOT": str(android_sdk_root),
            # 覆盖等待时间为 0 避免测试变慢。
            # Override wait to 0 so the test does not sleep 20s.
            "TALOS_MUMU_WAIT_SECONDS": "0",
        },
        timeout=30,
    )

    # 脚本应运行成功（MuMu 检测分支不会导致退出）。
    # Script should complete successfully (MuMu detection branch must not cause exit).
    assert result.returncode == 0, result.stdout + result.stderr
    # 日志应包含 MuMu 检测入口日志。
    # Log should contain the MuMu detection entry log line.
    combined = result.stdout + result.stderr
    assert "检查 MuMu 模拟器是否正在运行" in combined


def test_test_android_reconnect_prefers_configured_tcp_serial_after_offline_retry(tmp_path: Path) -> None:
    """验证 daemon 重启后的重试会优先继续使用配置里的 TCP 序列号，而不是退回到 emulator-* 别名。
    Verify that the daemon-restart retry flow keeps using the configured TCP serial instead of falling back to an emulator-* alias.
    """
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_android = tools_dir / "test-android.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copied_connect_android = tools_dir / "connect_androidVirtualDevice.sh"
    copy_tool_script(SOURCE_TEST_ANDROID, copied_test_android)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)
    copy_tool_script(SOURCE_CONNECT_ANDROID, copied_connect_android)

    fake_apk = tmp_path / "com.talos.BuildTest.debug.apk"
    fake_apk.write_text("stub apk", encoding="utf-8")

    adb_log_path = tmp_path / "adb-args.txt"
    adb_state_dir = tmp_path / "adb-state"
    adb_state_dir.mkdir(parents=True)
    node_args_path = tmp_path / "node-args.txt"
    node_env_path = tmp_path / "node-env.txt"
    npm_args_path = tmp_path / "npm-args.txt"

    fake_bin_dir = tmp_path / "fake-bin"
    fake_bin_dir.mkdir()
    write_executable(fake_bin_dir / "nc", "#!/bin/bash\nexit 0\n")

    write_executable(
        fake_bin_dir / "adb",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"state_dir={adb_state_dir}",
            f"printf '%s\\n' \"$*\" >> {adb_log_path}",
            "serial=''",
            "if [[ \"${1:-}\" == \"-s\" ]]; then",
            "  serial=\"${2:-}\"",
            "  shift 2",
            "fi",
            "case \"${1:-}\" in",
            "  connect)",
            "    printf 'connected to %s\\n' \"${2:-}\"",
            "    ;;",
            "  disconnect)",
            "    exit 0",
            "    ;;",
            "  devices)",
            "    if [[ -f \"${state_dir}/offline-retry\" ]]; then",
            "      printf 'List of devices attached\\nemulator-5554\\tdevice\\n127.0.0.1:62001\\tdevice\\n'",
            "    else",
            "      printf 'List of devices attached\\n127.0.0.1:62001\\tdevice\\n'",
            "    fi",
            "    ;;",
            "  install)",
            "    exit 0",
            "    ;;",
            "  forward)",
            "    if [[ \"${2:-}\" == \"--remove\" ]]; then",
            "      if [[ ! -f \"${state_dir}/daemon-restarted\" ]]; then",
            "        touch \"${state_dir}/daemon-restarted\"",
            "        printf 'adb server version (36) does not match this client (41); killing...\\n* daemon started successfully *\\n'",
            "        exit 1",
            "      fi",
            "      if [[ ! -f \"${state_dir}/offline-retry\" ]]; then",
            "        touch \"${state_dir}/offline-retry\"",
            "        printf 'adb.exe: error: device offline\\n'",
            "        exit 1",
            "      fi",
            "    fi",
            "    exit 0",
            "    ;;",
            "  logcat)",
            "    exit 0",
            "    ;;",
            "  shell)",
            "    exit 0",
            "    ;;",
            "  *)",
            "    exit 0",
            "    ;;",
            "esac",
        ]) + "\n",
    )

    node_home = tmp_path / "node-home"
    node_home.mkdir()
    playwright_cli_path = playwright_root / "node_modules" / "@playwright" / "test" / "cli.js"
    write_executable(
        node_home / "node.exe",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            "if [[ \"${1:-}\" == \"-e\" ]]; then exit 0; fi",
            f"printf '%s\\n' \"$@\" > {node_args_path}",
            f"printf '%s\\n%s\\n%s\\n%s\\n%s\\n' \"${{PLAYWRIGHT_HTML_OUTPUT_DIR:-}}\" \"${{PLAYWRIGHT_HTML_REPORT:-}}\" \"${{PLAYWRIGHT_HTML_OPEN:-}}\" \"${{PW_TEST_HTML_REPORT_OPEN:-}}\" \"${{PLAYWRIGHT_JUNIT_OUTPUT_FILE:-}}\" > {node_env_path}",
            "exit 0",
        ]) + "\n",
    )
    write_executable(
        node_home / "npm.cmd",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"printf '%s\\n' \"$@\" > {npm_args_path}",
            f"mkdir -p {playwright_cli_path.parent}",
            f"cat <<'EOF' > {playwright_cli_path}",
            "console.log('stub playwright cli');",
            "EOF",
        ]) + "\n",
    )

    result = subprocess.run(
        [
            "/bin/bash",
            str(copied_test_android),
            "--apk", str(fake_apk),
            "--connect-targets", "127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555",
            "--port", "12345",
        ],
        capture_output=True,
        text=True,
        cwd=str(playwright_root),
        env={
            **os.environ,
            "PATH": f"{fake_bin_dir}:/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
            "TALOS_ADB_RECONNECT_BASELINE_SLEEP_SECONDS": "0",
            "TALOS_ADB_RECONNECT_RETRY_SLEEP_SECONDS": "0",
        },
        timeout=30,
    )

    assert result.returncode == 0, result.stdout + result.stderr
    adb_log_lines = adb_log_path.read_text(encoding="utf-8").splitlines()
    forward_remove_lines = [line for line in adb_log_lines if "forward --remove tcp:12345" in line]
    assert forward_remove_lines, "forward --remove should have been retried after daemon restart"
    assert all("-s 127.0.0.1:62001" in line for line in forward_remove_lines), (
        "reconnect retries must keep the configured TCP serial instead of switching to emulator-* aliases"
    )
    assert not any("-s emulator-5554 forward --remove tcp:12345" in line for line in adb_log_lines)
    combined = result.stdout + result.stderr
    assert "设备不可用，重连中" in combined
    assert "重连后序列号: 127.0.0.1:62001" in combined


def test_test_android_tcp_timeout_failure_path_bounds_logcat_and_cleanup(tmp_path: Path) -> None:
    """验证 Unity TCP 超时后的失败路径不会被 logcat / cleanup 卡死。
    Verify that the Unity TCP-timeout failure path does not hang forever on logcat / cleanup commands.
    """
    playwright_root = tmp_path / "PlaywrightRoot"
    tools_dir = playwright_root / "tools"
    tools_dir.mkdir(parents=True)
    (playwright_root / "test-results").mkdir(parents=True)

    copied_test_android = tools_dir / "test-android.sh"
    copied_node_tools = tools_dir / "node-tools.sh"
    copied_connect_android = tools_dir / "connect_androidVirtualDevice.sh"
    copy_tool_script(SOURCE_TEST_ANDROID, copied_test_android)
    copy_tool_script(SOURCE_NODE_TOOLS, copied_node_tools)
    copy_tool_script(SOURCE_CONNECT_ANDROID, copied_connect_android)

    fake_apk = tmp_path / "com.talos.BuildTest.debug.apk"
    fake_apk.write_text("stub apk", encoding="utf-8")

    adb_log_path = tmp_path / "adb-timeout-args.txt"
    node_args_path = tmp_path / "node-args.txt"
    node_env_path = tmp_path / "node-env.txt"
    npm_args_path = tmp_path / "npm-args.txt"
    node_home = tmp_path / "node-home"
    node_home.mkdir()
    fake_bin_dir = tmp_path / "fake-bin"
    fake_bin_dir.mkdir()

    write_executable(fake_bin_dir / "nc", "#!/bin/bash\nexit 1\n")
    write_executable(
        fake_bin_dir / "adb",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"printf '%s\\n' \"$*\" >> {adb_log_path}",
            "if [[ \"${1:-}\" == \"-s\" ]]; then",
            "  shift 2",
            "fi",
            "case \"${1:-}\" in",
            "  devices)",
            "    printf 'List of devices attached\\n127.0.0.1:62001\\tdevice\\n'",
            "    ;;",
            "  connect)",
            "    printf 'connected to %s\\n' \"${2:-}\"",
            "    ;;",
            "  install|forward)",
            "    exit 0",
            "    ;;",
            "  logcat)",
            "    sleep 2",
            "    printf 'slow logcat output\\n'",
            "    ;;",
            "  shell)",
            "    sleep 2",
            "    exit 0",
            "    ;;",
            "  *)",
            "    exit 0",
            "    ;;",
            "esac",
        ]) + "\n",
    )
    playwright_cli_path = playwright_root / "node_modules" / "@playwright" / "test" / "cli.js"
    write_executable(
        node_home / "node.exe",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            "if [[ \"${1:-}\" == \"-e\" ]]; then exit 0; fi",
            f"printf '%s\\n' \"$@\" > {node_args_path}",
            f"printf '%s\\n%s\\n%s\\n%s\\n%s\\n' \"${{PLAYWRIGHT_HTML_OUTPUT_DIR:-}}\" \"${{PLAYWRIGHT_HTML_REPORT:-}}\" \"${{PLAYWRIGHT_HTML_OPEN:-}}\" \"${{PW_TEST_HTML_REPORT_OPEN:-}}\" \"${{PLAYWRIGHT_JUNIT_OUTPUT_FILE:-}}\" > {node_env_path}",
            "exit 0",
        ]) + "\n",
    )
    write_executable(
        node_home / "npm.cmd",
        "\n".join([
            "#!/bin/bash",
            "set -euo pipefail",
            f"printf '%s\\n' \"$@\" > {npm_args_path}",
            f"mkdir -p {playwright_cli_path.parent}",
            f"cat <<'EOF' > {playwright_cli_path}",
            "console.log('stub playwright cli');",
            "EOF",
        ]) + "\n",
    )

    result = subprocess.run(
        [
            "/bin/bash",
            str(copied_test_android),
            "--apk", str(fake_apk),
            "--connect-targets", "127.0.0.1:62001",
            "--port", "12345",
        ],
        capture_output=True,
        text=True,
        cwd=str(playwright_root),
        env={
            **os.environ,
            "PATH": f"{fake_bin_dir}:/usr/bin:/bin",
            "TALOS_NODEJS_HOME": str(node_home),
            "TALOS_UNITY_TCP_TIMEOUT": "0",
            "TALOS_ADB_LOGCAT_TIMEOUT_SECONDS": "1",
            "TALOS_ADB_CLEANUP_TIMEOUT_SECONDS": "1",
        },
        timeout=10,
    )

    assert result.returncode == 1, result.stdout + result.stderr
    combined = result.stdout + result.stderr
    assert "Android logcat 导出超时" in combined
    assert "force-stop package 超时" in combined
    adb_log_lines = adb_log_path.read_text(encoding="utf-8").splitlines()
    assert any("logcat -d -v threadtime" in line for line in adb_log_lines)
    assert any("shell am force-stop com.talos.BuildTest.debug" in line for line in adb_log_lines)