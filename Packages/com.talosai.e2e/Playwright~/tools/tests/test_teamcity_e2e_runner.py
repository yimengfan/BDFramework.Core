"""Talos TeamCity E2E 编排脚本测试。

覆盖范围：
1. 平台映射与默认上游母包 buildTypeId。
2. 远端排队属性构造中的 debug 开关透传。
3. 文件服务器目录列表中的包体挑选规则。
4. Windows 包体解压后的 Launcher 定位。
"""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import zipfile

import pytest


TOOLS_ROOT = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS_ROOT / "teamcity_e2e_runner.py"
MODULE_NAME = "talos_teamcity_e2e_runner"

MODULE_SPEC = importlib.util.spec_from_file_location(MODULE_NAME, MODULE_PATH)
if MODULE_SPEC is None or MODULE_SPEC.loader is None:
    raise RuntimeError(f"无法加载测试目标模块: {MODULE_PATH}")

runner = importlib.util.module_from_spec(MODULE_SPEC)
sys.modules[MODULE_NAME] = runner
MODULE_SPEC.loader.exec_module(runner)


class FakeTextStream:
    """模拟可重配编码的文本流，用于验证 Windows 控制台日志兜底策略。"""

    def __init__(self, encoding: str | None, *, fail_encodings: set[str] | None = None) -> None:
        """保存当前编码，并按需声明哪些编码在重配置时应当失败。"""
        self.encoding = encoding
        self.fail_encodings = fail_encodings or set()
        self.calls: list[dict[str, str]] = []

    def reconfigure(self, *, encoding: str, errors: str) -> None:
        """记录重配置调用，并按测试场景模拟编码不可用。"""
        self.calls.append({"encoding": encoding, "errors": errors})
        if encoding in self.fail_encodings:
            raise LookupError(f"unsupported encoding: {encoding}")


class FakeBinaryResponse:
    """模拟 urllib 返回的二进制响应，供 Node zip 下载测试复用。"""

    def __init__(self, payload: bytes) -> None:
        """保存预构造的 zip 二进制内容。"""
        self.payload = payload

    def __enter__(self) -> "FakeBinaryResponse":
        """让测试桩兼容 with urllib.request.urlopen(...) 的调用方式。"""
        return self

    def __exit__(self, exc_type, exc, exc_tb) -> None:
        """保持与真实响应对象一致的上下文管理接口。"""
        return None

    def read(self) -> bytes:
        """返回完整的 zip 载荷，模拟网络下载结果。"""
        return self.payload


def test_resolve_platform_profile_returns_windows_defaults() -> None:
    """验证 Windows 平台会映射到默认母包构建任务与 PC 工具脚本。"""
    profile = runner.resolve_platform_profile("windows")

    assert profile.package_build_type_id == "BDFrameworkCore_BuildClientPackageWindows"
    assert profile.remote_root_prefix == "ClientPackage_windows"
    assert profile.tool_script_name == "test-pc.sh"
    assert profile.package_arg_name == "--exe"


def test_build_queue_properties_includes_debug_flag_and_extra_args() -> None:
    """验证远端排队参数会稳定透传 clientVersion、debug 开关与额外参数。"""
    properties = runner.build_queue_properties("0.1", "true", "--dry-run --foo bar")

    assert properties == [
        {"name": "build.client.version", "value": "0.1"},
        {"name": "build.debugBuild", "value": "true"},
        {"name": "build.extra.args", "value": "--dry-run --foo bar"},
    ]


def test_select_remote_package_entry_prefers_main_windows_zip() -> None:
    """验证 Windows 远端目录里会优先挑选主运行包，而不是 DoNotShip 附件 zip。"""
    profile = runner.resolve_platform_profile("windows")
    selected = runner.select_remote_package_entry(
        profile,
        [
            {"type": "file", "path": "ClientPackage_windows/123/Game_BurstDebugInformation_DoNotShip.zip"},
            {"type": "file", "path": "ClientPackage_windows/123/GameRuntime.zip"},
            {"type": "file", "path": "ClientPackage_windows/123/Game_不要发布.zip"},
        ],
    )

    assert selected.remote_path == "ClientPackage_windows/123/GameRuntime.zip"
    assert selected.file_name == "GameRuntime.zip"


def test_select_remote_package_entry_requires_android_apk() -> None:
    """验证 Android 远端目录里只会接受 APK 文件作为目标包体。"""
    profile = runner.resolve_platform_profile("android")
    selected = runner.select_remote_package_entry(
        profile,
        [
            {"type": "file", "path": "ClientPackage_android/123/readme.txt"},
            {"type": "file", "path": "ClientPackage_android/123/Launcher.apk"},
        ],
    )

    assert selected.remote_path == "ClientPackage_android/123/Launcher.apk"
    assert selected.file_name == "Launcher.apk"


def test_find_windows_launcher_prefers_launcher_name(tmp_path: Path) -> None:
    """验证 Windows 解压目录中优先返回 Launcher.exe。"""
    root_dir = tmp_path / "runtime"
    launcher_dir = root_dir / "Game"
    launcher_dir.mkdir(parents=True)
    (launcher_dir / "Other.exe").write_text("stub", encoding="utf-8")
    expected_launcher = launcher_dir / "Launcher.exe"
    expected_launcher.write_text("stub", encoding="utf-8")

    assert runner.find_windows_launcher(root_dir) == expected_launcher


def test_prepare_local_package_extracts_windows_zip(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """验证 Windows 包体 zip 会被解压，并返回其中的 Launcher.exe 路径。"""
    profile = runner.resolve_platform_profile("windows")
    archive_path = tmp_path / "GameRuntime.zip"
    with zipfile.ZipFile(archive_path, "w") as archive:
        archive.writestr("GameRuntime/Launcher.exe", "stub")

    monkeypatch.setattr(runner, "PLAYWRIGHT_DIR", tmp_path / "PlaywrightRoot")
    launcher_path = runner.prepare_local_package(profile, archive_path)

    assert launcher_path.name == "Launcher.exe"
    assert launcher_path.is_file()


def test_normalize_bool_flag_rejects_invalid_value() -> None:
    """验证非法的 debug 开关值会被立即拒绝。"""
    with pytest.raises(runner.TalosTeamCityE2EError, match="unsupported value"):
        runner.normalize_bool_flag("maybe")


def test_configure_console_streams_enables_backslashreplace(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证标准输出流会启用 backslashreplace，避免不可编码字符中断远端日志。"""
    stdout = FakeTextStream("gbk")
    stderr = FakeTextStream("gbk")

    monkeypatch.setattr(runner.sys, "stdout", stdout)
    monkeypatch.setattr(runner.sys, "stderr", stderr)

    runner.configure_console_streams()

    assert stdout.calls == [{"encoding": "gbk", "errors": "backslashreplace"}]
    assert stderr.calls == [{"encoding": "gbk", "errors": "backslashreplace"}]


def test_configure_console_streams_falls_back_to_utf8(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证当前编码不可用时，会回退到 utf-8 并继续保留 backslashreplace。"""
    stdout = FakeTextStream("x-unknown", fail_encodings={"x-unknown"})
    stderr = FakeTextStream("x-unknown", fail_encodings={"x-unknown"})

    monkeypatch.setattr(runner.sys, "stdout", stdout)
    monkeypatch.setattr(runner.sys, "stderr", stderr)

    runner.configure_console_streams()

    assert stdout.calls == [
        {"encoding": "x-unknown", "errors": "backslashreplace"},
        {"encoding": "utf-8", "errors": "backslashreplace"},
    ]
    assert stderr.calls == [
        {"encoding": "x-unknown", "errors": "backslashreplace"},
        {"encoding": "utf-8", "errors": "backslashreplace"},
    ]


def test_resolve_existing_node_tooling_prefers_explicit_home(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """验证 runner 会优先接受显式提供的 Node 安装目录，而不是依赖当前 PATH。"""
    node_home = tmp_path / "node-home"
    node_home.mkdir()
    node_bin = node_home / "node.exe"
    npm_bin = node_home / "npm.cmd"
    node_bin.write_text("stub", encoding="utf-8")
    npm_bin.write_text("stub", encoding="utf-8")

    monkeypatch.setenv("TALOS_NODEJS_HOME", str(node_home))
    monkeypatch.delenv("NODEJS_HOME", raising=False)
    monkeypatch.delenv("NODE_BIN", raising=False)
    monkeypatch.delenv("NPM_BIN", raising=False)
    monkeypatch.setattr(runner.shutil, "which", lambda _name: None)

    tooling = runner.resolve_existing_node_tooling()

    assert tooling is not None
    assert tooling.node_home == node_home.resolve()
    assert tooling.node_bin == node_bin.resolve()
    assert tooling.npm_bin == npm_bin.resolve()


def test_ensure_windows_portable_node_tooling_extracts_downloaded_zip(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """验证 Windows agent 缺少 Node 时，runner 会下载 zip 并解析出 node/npm。"""
    archive_path = tmp_path / "node.zip"
    with zipfile.ZipFile(archive_path, "w") as archive:
        archive.writestr("node-v20.18.0-win-x64/node.exe", "stub")
        archive.writestr("node-v20.18.0-win-x64/npm.cmd", "stub")

    monkeypatch.setattr(runner, "NODE_TOOLCACHE_DIR", tmp_path / "toolcache")
    monkeypatch.setattr(runner.urllib.request, "urlopen", lambda request, timeout=0: FakeBinaryResponse(archive_path.read_bytes()))

    tooling = runner.ensure_windows_portable_node_tooling("20.18.0")

    assert tooling.node_bin.is_file()
    assert tooling.npm_bin.is_file()
    assert tooling.node_bin.name == "node.exe"
    assert tooling.npm_bin.name == "npm.cmd"


def test_run_test_tool_injects_node_environment(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    """验证平台工具脚本执行前，runner 会把 Node/npm 环境变量显式注入子进程。"""
    tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )
    captured: dict[str, object] = {}

    class FakeCompletedProcess:
        """模拟 subprocess.run 的最小返回值，只暴露 returncode。"""

        def __init__(self, returncode: int) -> None:
            """保存测试期望的退出码。"""
            self.returncode = returncode

    def fake_run(command, cwd, check, env):
        """记录 runner 传给 subprocess 的参数，供断言环境变量是否已注入。"""
        captured["command"] = command
        captured["cwd"] = cwd
        captured["check"] = check
        captured["env"] = env
        return FakeCompletedProcess(returncode=0)

    monkeypatch.setattr(runner, "ensure_node_tooling", lambda: tooling)
    monkeypatch.setattr(runner, "build_test_command", lambda profile, package_path, args: ["bash", "tool.sh"])
    monkeypatch.setattr(runner.subprocess, "run", fake_run)

    exit_code = runner.run_test_tool(runner.resolve_platform_profile("windows"), tmp_path / "Launcher.exe", object())

    assert exit_code == 0
    assert captured["cwd"] == runner.REPO_ROOT
    assert captured["check"] is False
    assert captured["env"]["TALOS_NODEJS_HOME"] == runner.normalize_shell_path(tooling.node_home)
    assert captured["env"]["NODE_BIN"] == runner.normalize_shell_path(tooling.node_bin)
    assert captured["env"]["NPM_BIN"] == runner.normalize_shell_path(tooling.npm_bin)


def test_resolve_current_teamcity_build_context_reads_properties_file(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    """验证 runner 会从 TeamCity properties 文件里解析当前构建上下文。"""
    properties_file = tmp_path / "teamcity.properties"
    properties_file.write_text(
        "teamcity.serverUrl=http://teamcity.local\n"
        "teamcity.build.id=901\n"
        "teamcity.buildType.id=BDFrameworkCore_TalosAIStep01BaseFlowTest\n",
        encoding="utf-8",
    )

    monkeypatch.setenv("TEAMCITY_BUILD_PROPERTIES_FILE", str(properties_file))
    monkeypatch.delenv("TEAMCITY_BASE_URL", raising=False)
    monkeypatch.delenv("TEAMCITY_SERVER_URL", raising=False)
    monkeypatch.delenv("TEAMCITY_BUILD_ID", raising=False)
    monkeypatch.delenv("BUILD_ID", raising=False)
    monkeypatch.delenv("TEAMCITY_BUILD_TYPE_ID", raising=False)
    monkeypatch.delenv("TEAMCITY_BUILDCONF_ID", raising=False)
    monkeypatch.delenv("BUILD_URL", raising=False)

    context = runner.resolve_current_teamcity_build_context()

    assert context.base_url == "http://teamcity.local"
    assert context.build_id == "901"
    assert context.build_type_id == "BDFrameworkCore_TalosAIStep01BaseFlowTest"
    assert context.build_url is None


def test_build_teamcity_artifact_url_returns_repository_download_link() -> None:
    """验证标准 Playwright 报告链接会被拼成 TeamCity artifact 下载地址。"""
    context = runner.TeamCityCurrentBuildContext(
        base_url="http://teamcity.local",
        build_id="901",
        build_type_id="BDFrameworkCore_TalosAIStep01BaseFlowTest",
        build_url=None,
    )

    artifact_url = runner.build_teamcity_artifact_url(context, "talos-e2e-test-results/html/index.html")

    assert artifact_url == (
        "http://teamcity.local/repository/download/"
        "BDFrameworkCore_TalosAIStep01BaseFlowTest/901:id/talos-e2e-test-results/html/index.html"
    )