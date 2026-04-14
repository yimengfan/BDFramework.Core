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