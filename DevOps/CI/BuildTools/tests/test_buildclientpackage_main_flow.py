"""BuildClientPackage 各平台入口流程测试。

通过参数化覆盖 Android、iOS、Windows 三个平台，验证：
1. dry-run 执行跳过破坏性步骤但保留主流程顺序。
2. 非 dry-run 执行清理输出目录并上传构建产物。
3. batchmode 失败时输出日志尾部并抛出执行错误。
"""

from __future__ import annotations

import importlib
from pathlib import Path
import sys
from types import SimpleNamespace

import pytest


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
BUILD_CLIENT_PACKAGE_ROOT = BUILD_TOOLS_ROOT / "BuildClientPackage"
if str(BUILD_CLIENT_PACKAGE_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_CLIENT_PACKAGE_ROOT))


BUILD_SCRIPT_CASES = (
    pytest.param(
        ("build_android", "android", "[BuildClientPackage][Android]"),
        id="android",
    ),
    pytest.param(
        ("build_ios", "ios", "[BuildClientPackage][iOS]"),
        id="ios",
    ),
    pytest.param(
        ("build_windows", "windows", "[BuildClientPackage][Windows]"),
        id="windows",
    ),
)
"""参数化测试用例：(模块名, 平台标识, 日志前缀)。"""


@pytest.fixture(params=BUILD_SCRIPT_CASES)
def build_script_module(request):
    """加载每个平台的构建脚本模块，用于共享主流程测试。"""
    module_name, platform_key, log_prefix = request.param
    module = importlib.import_module(module_name)
    return module, platform_key, log_prefix


def install_flow_fakes(
    monkeypatch: pytest.MonkeyPatch,
    module,
    *,
    dry_run: bool,
    debug_build: str = "false",
    return_code: int = 0,
):
    """安装共享的 mock 协作者并返回捕获的流程上下文。

    通过 monkeypatch 替换模块内的所有外部依赖（参数解析、路径解析、Unity 执行、上传等），
    使测试可以验证主流程的调用顺序和参数传递。

    参数：
        monkeypatch: pytest monkeypatch 实例。
        module: 待测试的平台构建模块。
        dry_run: 是否启用 dry-run 模式。
        return_code: Unity batchmode 返回码（默认 0 表示成功）。

    返回：
        包含 events（事件列表）、publish_output_dir、tail_output 的字典。
    """
    resolved_project_dir = Path("/tmp/BDFramework.Core")
    unity_path = Path(
        "/Applications/Unity/Hub/Editor/2022.3.74f1/Unity.app/Contents/MacOS/Unity"
    )
    log_path = Path(f"/tmp/TCLog/Nightly_Build/238/{module.PLATFORM_KEY}_0.1.238.log")
    execute_method = f"CI.BuildClientPackage.{module.PLATFORM_KEY}"
    command = [
        str(unity_path),
        "-batchmode",
        "-projectPath",
        str(resolved_project_dir),
        "-quit",
    ]
    publish_output_dir = (
        resolved_project_dir / "DevOps" / "PublishPackages" / module.PLATFORM_KEY
    )
    tail_output = f"[UnityBatchMode] tail for {module.PLATFORM_KEY}"
    events: list[str] = []

    args = SimpleNamespace(
        client_version=" 0.1 ",
        build_name=" Nightly Build ",
        build_number=" 238 ",
        unity_version="2022.3.74f1",
        project_dir=str(resolved_project_dir),
        debug_build=debug_build,
        dry_run=dry_run,
    )

    monkeypatch.setattr(
        module,
        "configure_live_console_output",
        lambda: events.append("configure_live_console_output"),
    )
    monkeypatch.setattr(module, "parse_args", lambda: args)

    def fake_resolve_build_metadata(build_name, build_number):
        events.append("resolve_build_metadata")
        assert build_name == " Nightly Build "
        assert build_number == " 238 "
        return "Nightly Build", "238"

    monkeypatch.setattr(module, "resolve_build_metadata", fake_resolve_build_metadata)

    def fake_detect_host_os() -> str:
        events.append("detect_host_os")
        return "mac"

    monkeypatch.setattr(module, "detect_host_os", fake_detect_host_os)

    def fake_ensure_platform_allowed(platform_key: str) -> None:
        events.append("ensure_platform_allowed")
        assert platform_key == module.PLATFORM_KEY

    monkeypatch.setattr(module, "ensure_platform_allowed", fake_ensure_platform_allowed)

    def fake_resolve_unity_executable(unity_version, *, allow_missing):
        events.append("resolve_unity_executable")
        assert unity_version == "2022.3.74f1"
        assert allow_missing is dry_run
        return unity_path, "2022.3.74f1"

    monkeypatch.setattr(module, "resolve_unity_executable", fake_resolve_unity_executable)

    def fake_resolve_project_dir(project_dir_arg):
        events.append("resolve_project_dir")
        assert project_dir_arg == str(resolved_project_dir)
        return resolved_project_dir

    monkeypatch.setattr(module, "resolve_project_dir", fake_resolve_project_dir)

    def fake_get_execute_method(platform_key: str) -> str:
        events.append("get_execute_method")
        assert platform_key == module.PLATFORM_KEY
        return execute_method

    monkeypatch.setattr(module, "get_execute_method", fake_get_execute_method)

    def fake_get_log_path(
        platform_key: str,
        client_version: str,
        *,
        project_dir: Path,
        build_name: str | None,
        build_number: str | None,
    ) -> Path:
        events.append("get_log_path")
        assert platform_key == module.PLATFORM_KEY
        assert client_version == "0.1.238"
        assert project_dir == resolved_project_dir
        assert build_name == "Nightly Build"
        assert build_number == "238"
        return log_path

    monkeypatch.setattr(module, "get_log_path", fake_get_log_path)

    def fake_clear_publish_package_dir(platform_key: str, *, project_dir: Path) -> Path:
        events.append("clear_publish_package_dir")
        assert platform_key == module.PLATFORM_KEY
        assert project_dir == resolved_project_dir
        return publish_output_dir

    monkeypatch.setattr(module, "clear_publish_package_dir", fake_clear_publish_package_dir)

    def fake_build_batchmode_command(**kwargs) -> list[str]:
        events.append("build_batchmode_command")
        assert kwargs == {
            "unity_path": unity_path,
            "project_dir": resolved_project_dir,
            "execute_method": execute_method,
            "client_version": "0.1.238",
            "log_path": log_path,
        }
        return command

    monkeypatch.setattr(module, "build_batchmode_command", fake_build_batchmode_command)

    def fake_run_batchmode(command_value, *, dry_run: bool) -> int:
        events.append("run_batchmode")
        assert command_value == [
            str(unity_path),
            "-batchmode",
            "-projectPath",
            str(resolved_project_dir),
            "-buildDebug",
            debug_build,
            "-quit",
        ]
        assert dry_run is args.dry_run
        return return_code

    monkeypatch.setattr(module, "run_batchmode", fake_run_batchmode)

    def fake_read_log_tail(log_path_arg: Path) -> str:
        events.append("read_log_tail")
        assert log_path_arg == log_path
        return tail_output

    monkeypatch.setattr(module, "read_log_tail", fake_read_log_tail)

    def fake_upload_publish_package(
        platform_key: str,
        *,
        project_dir: Path,
        build_number: str | None,
        client_version: str,
        log_prefix: str,
    ) -> list[str]:
        events.append("upload_publish_package")
        assert platform_key == module.PLATFORM_KEY
        assert project_dir == resolved_project_dir
        assert build_number == "238"
        assert client_version == "0.1.238"
        assert log_prefix == module.LOG_PREFIX
        return ["uploaded"]

    monkeypatch.setattr(module, "upload_publish_package", fake_upload_publish_package)

    return {
        "events": events,
        "publish_output_dir": publish_output_dir,
        "tail_output": tail_output,
    }


def test_main_dry_run_executes_main_flow_without_side_effects(
    build_script_module,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证 dry-run 执行跳过破坏性步骤但保留完整的主流程调用顺序。"""
    module, platform_key, log_prefix = build_script_module
    context = install_flow_fakes(monkeypatch, module, dry_run=True)

    assert module.main() == 0

    output = capsys.readouterr().out
    assert f"{log_prefix} ===== Step 1/7: parse args =====" in output
    assert f"{log_prefix} host_os=mac" in output
    assert f"{log_prefix} clientVersion=0.1.238" in output
    assert f"{log_prefix} debugBuild=false" in output
    assert f"{log_prefix} ===== Step 4/7: reset output dir =====" in output
    assert f"{log_prefix} dry-run enabled, skip clearing publish output directory" in output
    assert f"{log_prefix} ===== Step 7/7: upload client package =====" in output
    assert f"{log_prefix} dry-run enabled, skip client package upload" in output
    assert f"{log_prefix} build finished successfully" in output
    assert context["events"] == [
        "configure_live_console_output",
        "resolve_build_metadata",
        "detect_host_os",
        "ensure_platform_allowed",
        "resolve_unity_executable",
        "resolve_project_dir",
        "get_execute_method",
        "get_log_path",
        "build_batchmode_command",
        "run_batchmode",
    ]
    assert platform_key == module.PLATFORM_KEY


def test_main_non_dry_run_clears_output_and_uploads_artifact(
    build_script_module,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证非 dry-run 执行会清理输出目录并上传构建好的客户端母包。"""
    module, _, log_prefix = build_script_module
    context = install_flow_fakes(monkeypatch, module, dry_run=False)

    assert module.main() == 0

    output = capsys.readouterr().out
    assert f"{log_prefix} publishOutputDir={context['publish_output_dir']}" in output
    assert f"{log_prefix} debugBuild=false" in output
    assert context["events"] == [
        "configure_live_console_output",
        "resolve_build_metadata",
        "detect_host_os",
        "ensure_platform_allowed",
        "resolve_unity_executable",
        "resolve_project_dir",
        "get_execute_method",
        "get_log_path",
        "clear_publish_package_dir",
        "build_batchmode_command",
        "run_batchmode",
        "upload_publish_package",
    ]


def test_main_reads_log_tail_and_raises_on_batchmode_failure(
    build_script_module,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证 batchmode 失败时输出日志尾部内容并抛出执行错误。"""
    module, platform_key, _ = build_script_module
    context = install_flow_fakes(monkeypatch, module, dry_run=False, return_code=17)

    with pytest.raises(module.UnityBatchModeError, match=f"exit_code=17"):
        module.main()

    output = capsys.readouterr().out
    assert context["tail_output"] in output
    assert f"{module.LOG_PREFIX} debugBuild=false" in output
    assert context["events"] == [
        "configure_live_console_output",
        "resolve_build_metadata",
        "detect_host_os",
        "ensure_platform_allowed",
        "resolve_unity_executable",
        "resolve_project_dir",
        "get_execute_method",
        "get_log_path",
        "clear_publish_package_dir",
        "build_batchmode_command",
        "run_batchmode",
        "read_log_tail",
    ]
    assert platform_key == module.PLATFORM_KEY


def test_main_threads_debug_build_flag_to_unity_command(
    build_script_module,
    monkeypatch: pytest.MonkeyPatch,
    capsys,
) -> None:
    """验证 debug 母包任务会把共享开关透传给 Unity BatchMode 命令。"""
    module, platform_key, log_prefix = build_script_module
    context = install_flow_fakes(monkeypatch, module, dry_run=True, debug_build="true")

    assert module.main() == 0

    output = capsys.readouterr().out
    assert f"{log_prefix} debugBuild=true" in output
    assert context["events"] == [
        "configure_live_console_output",
        "resolve_build_metadata",
        "detect_host_os",
        "ensure_platform_allowed",
        "resolve_unity_executable",
        "resolve_project_dir",
        "get_execute_method",
        "get_log_path",
        "build_batchmode_command",
        "run_batchmode",
    ]
    assert platform_key == module.PLATFORM_KEY