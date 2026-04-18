"""Talos TeamCity E2E 编排脚本测试。

覆盖范围：
1. 平台映射与默认上游母包 buildTypeId。
2. 远端排队属性构造中的 debug 开关透传。
3. 文件服务器目录列表中的包体挑选规则。
4. Windows 包体解压后的 Launcher 定位。
5. TeamCity runner 在 agent 缺少凭据变量时，会回退读取仓库内的 .env。

Talos TeamCity E2E orchestration script tests.

Coverage:
1. Platform mapping and default upstream package buildTypeIds.
2. Debug-flag forwarding in remote queue properties.
3. Package selection rules from file-server directory listings.
4. Launcher discovery after extracting Windows packages.
5. Repository .env fallback when TeamCity agents miss injected credentials.
"""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
from types import SimpleNamespace
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
        self.calls: list[dict[str, object]] = []

    def reconfigure(self, *, encoding: str, errors: str, line_buffering: bool, write_through: bool) -> None:
        """记录重配置调用，并按测试场景模拟编码不可用。"""
        self.calls.append(
            {
                "encoding": encoding,
                "errors": errors,
                "line_buffering": line_buffering,
                "write_through": write_through,
            }
        )
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


class FakePopenProcess:
    """模拟逐行输出的平台工具进程，用于验证 TeamCity runner 的实时日志转发。"""

    def __init__(self, lines: list[str], return_code: int) -> None:
        """保存预设输出行和返回码，模拟 Popen 接口。"""
        self.stdout = lines
        self.return_code = return_code

    def wait(self) -> int:
        """返回预设退出码，模拟子进程结束。"""
        return self.return_code


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


def test_resolve_teamcity_runtime_config_falls_back_to_repo_env_file(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    """验证 TeamCity runner 在 agent 缺少凭据变量时，会回退读取仓库内的 .env。
    Verify that the TeamCity runner falls back to the repository .env when the agent does not provide credential environment variables.
    """
    env_file = tmp_path / ".test-DevOps" / ".teamcity" / ".env"
    env_file.parent.mkdir(parents=True)
    env_file.write_text(
        "TEAMCITY_TOKEN=repo-token\nTEAMCITY_USERNAME=repo-user\nTEAMCITY_PASSWORD=repo-pass\n",
        encoding="utf-8",
    )

    external_config = SimpleNamespace(
        ci_server=SimpleNamespace(
            provider="teamcity",
            base_url="http://teamcity.local",
            token=None,
            token_env="TEAMCITY_TOKEN",
        ),
        config_path=None,
    )

    monkeypatch.setattr(runner, "REPO_ROOT", tmp_path)
    monkeypatch.setattr(runner, "DEFAULT_TEAMCITY_ENV_FILE", env_file)
    monkeypatch.setattr(runner, "load_external_config_if_available", lambda _config_path: external_config)
    monkeypatch.delenv("TEAMCITY_TOKEN", raising=False)
    monkeypatch.delenv("TEAMCITY_USERNAME", raising=False)
    monkeypatch.delenv("TEAMCITY_PASSWORD", raising=False)
    monkeypatch.delenv("TEAMCITY_BASE_URL", raising=False)
    monkeypatch.delenv("TEAMCITY_SERVER_URL", raising=False)

    resolved = runner.resolve_teamcity_runtime_config(None)

    assert resolved.base_url == "http://teamcity.local"
    assert resolved.token == "repo-token"
    assert resolved.username == "repo-user"
    assert resolved.password == "repo-pass"


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
    """验证 Windows 包体 zip 会落到短路径解压目录，并返回 Launcher.exe。
    Verify that a Windows package zip is extracted under the shortened path layout and returns Launcher.exe.
    """
    profile = runner.resolve_platform_profile("windows")
    archive_path = tmp_path / "GameRuntime.zip"
    with zipfile.ZipFile(archive_path, "w") as archive:
        archive.writestr("GameRuntime/Launcher.exe", "stub")

    monkeypatch.setattr(runner, "PLAYWRIGHT_DIR", tmp_path / "PlaywrightRoot")
    launcher_path = runner.prepare_local_package(
        profile,
        archive_path,
        package_build_number="35",
        current_build_id="724",
    )

    assert launcher_path.name == "Launcher.exe"
    assert launcher_path.is_file()
    assert launcher_path.relative_to(tmp_path / "PlaywrightRoot").parts[:3] == (
        "test-results",
        "p",
        "w-b35-r724",
    )


def test_normalize_bool_flag_rejects_invalid_value() -> None:
    """验证非法的 debug 开关值会被立即拒绝。"""
    with pytest.raises(runner.TalosTeamCityE2EError, match="unsupported value"):
        runner.normalize_bool_flag("maybe")


def test_configure_console_streams_enables_backslashreplace(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证标准输出流会启用 backslashreplace 和行缓冲，避免远端日志长时间静默。"""
    stdout = FakeTextStream("gbk")
    stderr = FakeTextStream("gbk")

    monkeypatch.setattr(runner.sys, "stdout", stdout)
    monkeypatch.setattr(runner.sys, "stderr", stderr)

    runner.configure_console_streams()

    assert stdout.calls == [
        {
            "encoding": "gbk",
            "errors": "backslashreplace",
            "line_buffering": True,
            "write_through": True,
        }
    ]
    assert stderr.calls == [
        {
            "encoding": "gbk",
            "errors": "backslashreplace",
            "line_buffering": True,
            "write_through": True,
        }
    ]


def test_configure_console_streams_falls_back_to_utf8(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证当前编码不可用时，会回退到 utf-8 并继续保留行缓冲与 backslashreplace。"""
    stdout = FakeTextStream("x-unknown", fail_encodings={"x-unknown"})
    stderr = FakeTextStream("x-unknown", fail_encodings={"x-unknown"})

    monkeypatch.setattr(runner.sys, "stdout", stdout)
    monkeypatch.setattr(runner.sys, "stderr", stderr)

    runner.configure_console_streams()

    assert stdout.calls == [
        {
            "encoding": "x-unknown",
            "errors": "backslashreplace",
            "line_buffering": True,
            "write_through": True,
        },
        {
            "encoding": "utf-8",
            "errors": "backslashreplace",
            "line_buffering": True,
            "write_through": True,
        },
    ]
    assert stderr.calls == [
        {
            "encoding": "x-unknown",
            "errors": "backslashreplace",
            "line_buffering": True,
            "write_through": True,
        },
        {
            "encoding": "utf-8",
            "errors": "backslashreplace",
            "line_buffering": True,
            "write_through": True,
        },
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


def test_normalize_bash_path_converts_windows_drive_prefix(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证 Windows 盘符路径会被转换成 Git Bash 可直接消费的 /d/... 形式。"""
    monkeypatch.setattr(runner, "normalize_shell_path", lambda _path: "D:/TeamcityBuildAgent/work/test-pc.sh")

    normalized = runner.normalize_bash_path(Path("ignored"))

    assert normalized == "/d/TeamcityBuildAgent/work/test-pc.sh"


def test_build_test_command_uses_shell_safe_paths_and_env_test_file(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    """验证 runner 会把 bash 脚本与包体路径归一化为 shell 友好格式，并避免把测试文件直接放进命令行。"""

    class FakeArgs:
        """提供 build_test_command 所需的最小参数集合。"""

        test_file = "tests/testBaseFlow-e2e.spec.ts"
        unity_port = 10002
        unity_host = "127.0.0.1"
        adb_serial = ""

    monkeypatch.setattr(runner, "resolve_bash_command", lambda: "bash")
    profile = runner.resolve_platform_profile("windows")
    package_path = tmp_path / "Build" / "Launcher.exe"

    command = runner.build_test_command(profile, package_path, FakeArgs())

    assert command == [
        "bash",
        runner.normalize_bash_path(runner.TOOL_DIR / "test-pc.sh"),
        "--exe",
        runner.normalize_bash_path(package_path),
        "--port",
        "10002",
        "--host",
        "127.0.0.1",
    ]


def test_run_test_tool_streams_platform_output_line_by_line(monkeypatch: pytest.MonkeyPatch, tmp_path: Path, capsys: pytest.CaptureFixture[str]) -> None:
    """验证 TeamCity runner 会逐行转发平台工具输出，避免长时间等待时主日志空白。"""

    class FakeArgs:
        """提供 run_test_tool 所需的最小参数集合。"""

        test_file = "tests/testBaseFlow-e2e.spec.ts"
        unity_port = 10002
        unity_host = "127.0.0.1"
        adb_serial = ""

    fake_tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )
    fake_tooling.node_home.mkdir(parents=True, exist_ok=True)
    for tooling_path in (fake_tooling.node_bin, fake_tooling.npm_bin):
        tooling_path.write_text("stub", encoding="utf-8")

    monkeypatch.setattr(runner, "ensure_node_tooling", lambda: fake_tooling)
    monkeypatch.setattr(runner, "build_test_command", lambda *_args, **_kwargs: ["bash", "tools/test-pc.sh"])
    monkeypatch.setattr(
        runner.subprocess,
        "Popen",
        lambda *args, **kwargs: FakePopenProcess([">>> first line\n", ">>> second line\n"], 0),
    )

    exit_code = runner.run_test_tool(runner.resolve_platform_profile("windows"), tmp_path / "Launcher.exe", FakeArgs())
    captured = capsys.readouterr()

    assert exit_code == 0
    assert ">>> first line" in captured.out
    assert ">>> second line" in captured.out


def test_build_test_tool_environment_includes_playwright_test_file(tmp_path: Path) -> None:
    """验证 runner 会通过环境变量传递测试文件，避免 Windows bash 命令行编码干扰。"""
    tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )

    environment = runner.build_test_tool_environment(
        tooling,
        test_file="tests/testBaseFlow-e2e.spec.ts",
    )

    assert environment["TALOS_NODEJS_HOME"] == runner.normalize_bash_path(tooling.node_home)
    assert environment["NODE_BIN"] == runner.normalize_bash_path(tooling.node_bin)
    assert environment["NPM_BIN"] == runner.normalize_bash_path(tooling.npm_bin)
    assert environment["PLAYWRIGHT_TEST_FILE"] == "tests/testBaseFlow-e2e.spec.ts"


def test_run_test_tool_streams_platform_output_line_by_line(monkeypatch: pytest.MonkeyPatch, tmp_path: Path, capsys: pytest.CaptureFixture[str]) -> None:
    """验证 TeamCity runner 会逐行转发平台工具输出，避免长时间等待时主日志空白。"""

    class FakeArgs:
        """提供 run_test_tool 所需的最小参数集合。"""

        test_file = "tests/testBaseFlow-e2e.spec.ts"
        unity_port = 10002
        unity_host = "127.0.0.1"
        adb_serial = ""

    fake_tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )
    fake_tooling.node_home.mkdir(parents=True, exist_ok=True)
    for tooling_path in (fake_tooling.node_bin, fake_tooling.npm_bin):
        tooling_path.write_text("stub", encoding="utf-8")

    monkeypatch.setattr(runner, "ensure_node_tooling", lambda: fake_tooling)
    monkeypatch.setattr(runner, "build_test_command", lambda *_args, **_kwargs: ["bash", "tools/test-pc.sh"])
    monkeypatch.setattr(
        runner.subprocess,
        "Popen",
        lambda *args, **kwargs: FakePopenProcess([">>> first line\n", ">>> second line\n"], 0),
    )

    exit_code = runner.run_test_tool(runner.resolve_platform_profile("windows"), tmp_path / "Launcher.exe", FakeArgs())
    captured = capsys.readouterr()

    assert exit_code == 0
    assert ">>> first line" in captured.out
    assert ">>> second line" in captured.out


def test_run_test_tool_injects_node_environment(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    """验证平台工具脚本执行前，runner 会把 Node/npm 环境变量显式注入子进程。"""
    tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )
    captured: dict[str, object] = {}

    def fake_popen(command, cwd, env, stdout, stderr, text, encoding, errors, bufsize):
        """记录 runner 传给 subprocess 的参数，供断言环境变量是否已注入。"""
        captured["command"] = command
        captured["cwd"] = cwd
        captured["env"] = env
        captured["stdout"] = stdout
        captured["stderr"] = stderr
        captured["text"] = text
        captured["encoding"] = encoding
        captured["errors"] = errors
        captured["bufsize"] = bufsize
        return FakePopenProcess(["tool output\n"], 0)

    monkeypatch.setattr(runner, "ensure_node_tooling", lambda: tooling)
    monkeypatch.setattr(runner, "build_test_command", lambda profile, package_path, args: ["bash", "tool.sh"])
    monkeypatch.setattr(runner.subprocess, "Popen", fake_popen)

    class FakeArgs:
        """提供 run_test_tool 透传测试文件所需的最小参数集合。"""

        test_file = "tests/testBaseFlow-e2e.spec.ts"

    exit_code = runner.run_test_tool(runner.resolve_platform_profile("windows"), tmp_path / "Launcher.exe", FakeArgs())

    assert exit_code == 0
    assert captured["cwd"] == runner.REPO_ROOT
    assert captured["stdout"] is runner.subprocess.PIPE
    assert captured["stderr"] is runner.subprocess.STDOUT
    assert captured["text"] is True
    assert captured["encoding"] == "utf-8"
    assert captured["errors"] == "replace"
    assert captured["bufsize"] == 1
    assert captured["env"]["TALOS_NODEJS_HOME"] == runner.normalize_bash_path(tooling.node_home)
    assert captured["env"]["NODE_BIN"] == runner.normalize_bash_path(tooling.node_bin)
    assert captured["env"]["NPM_BIN"] == runner.normalize_bash_path(tooling.npm_bin)
    assert captured["env"]["PLAYWRIGHT_TEST_FILE"] == "tests/testBaseFlow-e2e.spec.ts"


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


def test_parse_build_id_from_url_reads_numeric_tail() -> None:
    """验证 runner 能从 TeamCity 构建详情页 URL 里回推出当前构建 id。"""
    assert runner.parse_build_id_from_url("http://teamcity.local/build/699") == "699"
    assert runner.parse_build_id_from_url("http://teamcity.local/buildConfiguration/Type/699") == "699"
    assert runner.parse_build_id_from_url("http://teamcity.local/buildConfiguration/Type/latest") is None


def test_complete_current_teamcity_build_context_backfills_missing_build_type(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证当前构建缺失 buildTypeId 时，runner 会用当前 buildId 回查并补全 artifact 链接上下文。"""
    context = runner.TeamCityCurrentBuildContext(
        base_url=None,
        build_id=None,
        build_type_id=None,
        build_url="http://teamcity.local/build/699",
    )
    runtime_config = runner.TeamCityRuntimeConfig(
        base_url="http://teamcity.local",
        token="token",
        username=None,
        password=None,
        config_path=None,
    )
    build_handle = runner.BuildHandle(
        build_id=699,
        build_type_id="BDFrameworkCore_TalosAIStep01BaseFlowTest",
        number="14",
        state="finished",
        status="SUCCESS",
        status_text="Success",
        branch_name="v4/v-4.0.0",
        web_url="http://teamcity.local/build/699",
    )

    monkeypatch.setattr(runner, "resolve_teamcity_runtime_config", lambda _config_path: runtime_config)
    monkeypatch.setattr(runner, "get_build", lambda _config, build_id: build_handle if build_id == 699 else None)

    completed = runner.complete_current_teamcity_build_context(context, config_path=None)

    assert completed.base_url == "http://teamcity.local"
    assert completed.build_id == "699"
    assert completed.build_type_id == "BDFrameworkCore_TalosAIStep01BaseFlowTest"
    assert completed.build_url == "http://teamcity.local/build/699"


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


def test_build_test_tool_environment_passes_adb_connect_targets_when_set(tmp_path: Path) -> None:
    """验证 runner 会把 ADB 连接目标列表注入工具脚本环境变量，以支持 MuMu 等宿主机模拟器的自动修复连接。
    Verify that the runner injects TALOS_ADB_CONNECT_TARGETS into the tool script environment
    to support auto-recovery ADB connection for host-local emulators such as MuMu.
    """
    tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )

    environment = runner.build_test_tool_environment(
        tooling,
        adb_connect_targets="127.0.0.1:16384,127.0.0.1:7555",
    )

    assert environment["TALOS_ADB_CONNECT_TARGETS"] == "127.0.0.1:16384,127.0.0.1:7555"


def test_build_test_tool_environment_omits_adb_connect_targets_when_empty(tmp_path: Path) -> None:
    """验证未配置 ADB 连接目标时，runner 不会把 TALOS_ADB_CONNECT_TARGETS 注入环境变量。
    Verify that TALOS_ADB_CONNECT_TARGETS is not injected when adb_connect_targets is empty or unset.
    """
    tooling = runner.NodeTooling(
        node_home=tmp_path / "node-home",
        node_bin=tmp_path / "node-home" / "node.exe",
        npm_bin=tmp_path / "node-home" / "npm.cmd",
    )

    environment = runner.build_test_tool_environment(tooling, adb_connect_targets=None)

    assert "TALOS_ADB_CONNECT_TARGETS" not in environment


def test_main_skip_build_mode_uses_package_build_number_directly(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    """验证传入 --package-build-number 时，无论是否同时传 --package-build-id，runner 均跳过 TeamCity API 直接从文件服务器下载。
    Verify that when --package-build-number is given, the runner bypasses the TeamCity API and
    downloads from the file server directly, regardless of whether --package-build-id is also set.
    """
    profile = runner.resolve_platform_profile("android")
    downloaded_apk = tmp_path / "Launcher.apk"
    downloaded_apk.write_text("stub apk", encoding="utf-8")

    calls: list[str] = []

    def fake_resolve_or_queue(_args, _profile):
        """如果 TeamCity API 被调用则记录，用来验证跳过逻辑。"""
        calls.append("resolve_or_queue_package_build")
        return (999, "99")

    def fake_download_package(*, profile, build_number, args):
        """验证 build_number 已被正确传递，并返回预设 APK 路径。"""
        calls.append(f"download:{build_number}")
        return downloaded_apk

    def fake_prepare_local_package(profile, downloaded_path, **kwargs):
        """直接返回下载路径，绕过解压流程。"""
        return downloaded_apk

    def fake_run_test_tool(_profile, _package_path, _args):
        """跳过实际测试执行，记录调用并返回成功。"""
        calls.append("run_test_tool")
        return 0

    monkeypatch.setattr(runner, "resolve_or_queue_package_build", fake_resolve_or_queue)
    monkeypatch.setattr(runner, "download_package_from_build", fake_download_package)
    monkeypatch.setattr(runner, "prepare_local_package", fake_prepare_local_package)
    monkeypatch.setattr(runner, "run_test_tool", fake_run_test_tool)
    monkeypatch.setattr(runner, "emit_playwright_report_metadata", lambda **_kwargs: None)

    class FakeArgs:
        """模拟 parse_args 产物，指定已知构建版本号，不传 package_build_id。
        Simulates a parsed args namespace with a known build number and no package_build_id.
        """

        platform = "android"
        phase = "all"
        client_version = "0.1"
        build_debug = "true"
        package_build_id = ""
        package_build_number = "60"
        package_build_extra_args = ""
        package_build_type_id = ""
        package_path = ""
        branch = "v4/v-4.0.0"
        config = None
        file_server_url = None
        file_server_token = None
        test_file = ""
        unity_host = "127.0.0.1"
        unity_port = 10002
        adb_serial = ""
        adb_connect_targets = ""
        start_mumu = ""
        mumu_exe_path = ""
        timeout_seconds = 5400
        poll_interval_seconds = 10
        download_timeout_seconds = 600

    monkeypatch.setattr(runner, "parse_args", lambda: FakeArgs())
    monkeypatch.setattr(runner, "configure_console_streams", lambda: None)
    monkeypatch.setattr(runner, "resolve_current_teamcity_build_context", lambda: runner.TeamCityCurrentBuildContext(
        base_url=None, build_id=None, build_type_id=None, build_url=None
    ))

    exit_code = runner.main()

    assert exit_code == 0
    assert "resolve_or_queue_package_build" not in calls, "TeamCity API should not be called in skip-build mode"
    assert "download:60" in calls


def test_main_prepare_phase_emits_prepared_package_path_and_skips_run(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    """验证 prepare 阶段只准备本地包体并回写 TeamCity 参数，不执行平台工具。
    Verify that the prepare phase only prepares the local package, emits the TeamCity parameter, and skips the platform tool.
    """
    prepared_path = tmp_path / "prepared" / "Launcher.exe"
    prepared_path.parent.mkdir(parents=True)
    prepared_path.write_text("stub", encoding="utf-8")
    emitted_parameters: dict[str, str] = {}
    calls: list[str] = []

    class FakeArgs:
        """模拟 prepare 阶段的最小参数集合。
        Simulate the minimal parsed-args shape for the prepare phase.
        """

        phase = "prepare"
        platform = "windows"
        client_version = "0.1"
        build_debug = "true"
        package_build_id = ""
        package_build_number = ""
        package_build_extra_args = ""
        package_build_type_id = ""
        package_path = ""
        branch = "v4/v-4.0.0"
        config = None
        file_server_url = None
        file_server_token = None
        test_file = "tests/testBaseFlow-e2e.spec.ts"
        unity_host = "127.0.0.1"
        unity_port = 10002
        adb_serial = ""
        adb_connect_targets = ""
        start_mumu = ""
        mumu_exe_path = ""
        timeout_seconds = 5400
        poll_interval_seconds = 10
        download_timeout_seconds = 600

    monkeypatch.setattr(runner, "parse_args", lambda: FakeArgs())
    monkeypatch.setattr(runner, "configure_console_streams", lambda: None)
    monkeypatch.setattr(
        runner,
        "resolve_current_teamcity_build_context",
        lambda: runner.TeamCityCurrentBuildContext(
            base_url="http://teamcity.local",
            build_id="901",
            build_type_id="BDFrameworkCore_TalosAIStep01BaseFlowTest",
            build_url=None,
        ),
    )
    monkeypatch.setattr(
        runner,
        "resolve_local_package_path",
        lambda _args, _profile, current_build_id=None: prepared_path,
    )
    monkeypatch.setattr(
        runner,
        "emit_teamcity_parameter",
        lambda name, value: emitted_parameters.__setitem__(name, value),
    )
    monkeypatch.setattr(
        runner,
        "run_test_tool",
        lambda _profile, _package_path, _args: calls.append("run_test_tool") or 0,
    )
    monkeypatch.setattr(
        runner,
        "emit_playwright_report_metadata",
        lambda **_kwargs: calls.append("emit_playwright_report_metadata"),
    )

    exit_code = runner.main()

    assert exit_code == 0
    assert emitted_parameters[runner.PREPARED_PACKAGE_PATH_PARAMETER] == str(prepared_path)
    assert "run_test_tool" not in calls
    assert "emit_playwright_report_metadata" not in calls


def test_main_run_phase_requires_prepared_package_path(monkeypatch: pytest.MonkeyPatch) -> None:
    """验证 run 阶段缺少 packagePath 时会立即失败，而不是重新下载包体。
    Verify that the run phase fails fast when packagePath is missing instead of silently downloading a package again.
    """

    class FakeArgs:
        """模拟 run 阶段缺少 packagePath 的最小参数集合。
        Simulate the minimal parsed-args shape for the run phase with a missing packagePath.
        """

        phase = "run"
        platform = "windows"
        client_version = "0.1"
        build_debug = "true"
        package_build_id = ""
        package_build_number = ""
        package_build_extra_args = ""
        package_build_type_id = ""
        package_path = ""
        branch = "v4/v-4.0.0"
        config = None
        file_server_url = None
        file_server_token = None
        test_file = "tests/testBaseFlow-e2e.spec.ts"
        unity_host = "127.0.0.1"
        unity_port = 10002
        adb_serial = ""
        adb_connect_targets = ""
        start_mumu = ""
        mumu_exe_path = ""
        timeout_seconds = 5400
        poll_interval_seconds = 10
        download_timeout_seconds = 600

    monkeypatch.setattr(runner, "parse_args", lambda: FakeArgs())
    monkeypatch.setattr(runner, "configure_console_streams", lambda: None)
    monkeypatch.setattr(
        runner,
        "resolve_current_teamcity_build_context",
        lambda: runner.TeamCityCurrentBuildContext(
            base_url="http://teamcity.local",
            build_id="901",
            build_type_id="BDFrameworkCore_TalosAIStep01BaseFlowTest",
            build_url=None,
        ),
    )

    with pytest.raises(
        runner.TalosTeamCityE2EError,
        match="run phase requires --package-path",
    ):
        runner.main()


def test_build_test_tool_environment_sets_mumu_auto_start_when_true(
    tmp_path: Path,
) -> None:
    """验证 mumu_auto_start='true' 时 TALOS_MUMU_AUTO_START 被注入到环境变量。
    Verify that TALOS_MUMU_AUTO_START is injected into the environment when mumu_auto_start='true'.
    """
    from unittest.mock import MagicMock

    fake_tooling = MagicMock()
    fake_tooling.node_home = tmp_path / "node-home"
    fake_tooling.node_bin = tmp_path / "node-home" / "node.exe"
    fake_tooling.npm_bin = tmp_path / "node-home" / "npm.cmd"

    env = runner.build_test_tool_environment(fake_tooling, mumu_auto_start="true")
    assert env.get("TALOS_MUMU_AUTO_START") == "true"


def test_build_test_tool_environment_omits_mumu_auto_start_when_empty_or_false(
    tmp_path: Path,
) -> None:
    """验证 mumu_auto_start 为空字符或 'false' 时 TALOS_MUMU_AUTO_START 不注入环境。
    Verify that TALOS_MUMU_AUTO_START is NOT injected when mumu_auto_start is empty or 'false'.
    """
    from unittest.mock import MagicMock

    fake_tooling = MagicMock()
    fake_tooling.node_home = tmp_path / "node-home"
    fake_tooling.node_bin = tmp_path / "node-home" / "node.exe"
    fake_tooling.npm_bin = tmp_path / "node-home" / "npm.cmd"

    for val in ("", None, "false", "  "):
        env = runner.build_test_tool_environment(fake_tooling, mumu_auto_start=val)
        assert "TALOS_MUMU_AUTO_START" not in env, f"Expected key absent for mumu_auto_start={val!r}"


def test_build_test_tool_environment_sets_mumu_exe_path_when_given(
    tmp_path: Path,
) -> None:
    """验证 mumu_exe_path 非空时 TALOS_MUMU_EXE_PATH 被注入到环境变量。
    Verify that TALOS_MUMU_EXE_PATH is injected when mumu_exe_path is a non-empty value.
    """
    from unittest.mock import MagicMock

    fake_tooling = MagicMock()
    fake_tooling.node_home = tmp_path / "node-home"
    fake_tooling.node_bin = tmp_path / "node-home" / "node.exe"
    fake_tooling.npm_bin = tmp_path / "node-home" / "npm.cmd"

    env = runner.build_test_tool_environment(fake_tooling, mumu_exe_path="/d/MuMuPlayer-12.0/shell/MuMuPlayer.exe")
    assert env.get("TALOS_MUMU_EXE_PATH") == "/d/MuMuPlayer-12.0/shell/MuMuPlayer.exe"

    for val in ("", None, "  "):
        env2 = runner.build_test_tool_environment(fake_tooling, mumu_exe_path=val)
        assert "TALOS_MUMU_EXE_PATH" not in env2, f"Expected key absent for mumu_exe_path={val!r}"