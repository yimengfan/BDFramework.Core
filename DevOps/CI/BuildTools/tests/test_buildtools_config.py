"""BuildTools 共享外部配置加载器测试。

测试覆盖范围：
1. 外部集成配置解析：验证文件服务器、CI 服务器、iOS 签名和远程测试配置被正确读取。
2. 配置路径解析：验证环境变量覆盖配置路径的优先级。
3. iOS Xcode 签名桥接：验证只导出支持的签名键并保持稳定顺序。
4. 源码守卫扫描：验证 BuildTools 源码文件不会重新引入临时外部配置解析器。
"""

from __future__ import annotations

from pathlib import Path

import pytest

from Common.buildtools_config import (
    BuildToolsIosXcodeSigningConfig,
    iter_ios_xcode_shell_pairs,
    load_buildtools_external_config,
    resolve_buildtools_config_path,
)
from Common.buildtools_config_guard import is_guarded_buildtools_source, scan_buildtools_source_file


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]


def test_load_buildtools_external_config_reads_external_integration_sections(tmp_path: Path) -> None:
    """验证共享加载器能同时读取文件服务器、CI 服务器、签名和远程测试配置段。"""
    config_path = tmp_path / "buildtools.toml"
    config_path.write_text(
        """
[artifact_file_server]
base_url = "https://files.example.com:8443"
token = "file-token"
upload_chunk_size_kb = 512
hash_chunk_size_kb = 256

[ci_server]
provider = "teamcity"
base_url = "https://ci.example.com"
project_name = "BDFramework.Core"

[ios_xcode]
signing_style = "manual"
team_id = "ABCDE12345"
bundle_identifier = "com.demo.game"
code_sign_identity = "Apple Distribution: Team Name (ABCDE12345)"
provisioning_profile_specifier = "Demo AdHoc"

[tests.remote_artifact]
enabled = true
build_number = "ci-remote-smoke"
filename = "custom_remote_test.txt"
request_timeout_seconds = 30
listing_timeout_seconds = 18
poll_interval_seconds = 2
""".strip(),
        encoding="utf-8",
    )

    config = load_buildtools_external_config(config_path=config_path)

    assert config.config_path == config_path.resolve()
    assert config.file_server.base_url == "https://files.example.com:8443"
    assert config.file_server.token == "file-token"
    assert config.file_server.upload_chunk_size_kb == 512
    assert config.file_server.hash_chunk_size_kb == 256
    assert config.ci_server.provider == "teamcity"
    assert config.ci_server.base_url == "https://ci.example.com"
    assert config.ci_server.project_name == "BDFramework.Core"
    assert config.ios_xcode.signing_style == "manual"
    assert config.ios_xcode.team_id == "ABCDE12345"
    assert config.remote_artifact_test.enabled is True
    assert config.remote_artifact_test.build_number == "ci-remote-smoke"
    assert config.remote_artifact_test.filename == "custom_remote_test.txt"
    assert config.remote_artifact_test.request_timeout_seconds == 30
    assert config.remote_artifact_test.listing_timeout_seconds == 18
    assert config.remote_artifact_test.poll_interval_seconds == 2


def test_resolve_buildtools_config_path_prefers_shared_env(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    """验证 BUILDTOOLS_CONFIG 环境变量覆盖对所有外部配置消费者生效。"""
    config_path = tmp_path / "buildtools.toml"
    config_path.write_text("[artifact_file_server]\nbase_url = \"http://127.0.0.1:20001\"\n", encoding="utf-8")

    monkeypatch.setenv("BUILDTOOLS_CONFIG", str(config_path))

    assert resolve_buildtools_config_path(None) == config_path.resolve()


def test_iter_ios_xcode_shell_pairs_emits_supported_signing_keys() -> None:
    """验证 Xcode shell 桥接只导出支持的签名键并保持稳定的输出顺序。"""
    signing_config = BuildToolsIosXcodeSigningConfig(
        signing_style="manual",
        team_id="ABCDE12345",
        bundle_identifier="com.demo.game",
        code_sign_identity="Apple Distribution: Team Name (ABCDE12345)",
        provisioning_profile_specifier="Demo AdHoc",
        provisioning_profile=None,
    )

    assert list(iter_ios_xcode_shell_pairs(signing_config)) == [
        ("signing_style", "manual"),
        ("team_id", "ABCDE12345"),
        ("bundle_identifier", "com.demo.game"),
        ("code_sign_identity", "Apple Distribution: Team Name (ABCDE12345)"),
        ("provisioning_profile_specifier", "Demo AdHoc"),
    ]


def test_buildtools_source_files_do_not_reintroduce_ad_hoc_external_config_parsers() -> None:
    """验证受保护的 BuildTools 源码文件只通过 Common/buildtools_config.py 读取共享外部配置，不会重新引入临时解析器。"""
    workspace_root = str(BUILD_TOOLS_ROOT.parents[2])
    violations = []

    for path in BUILD_TOOLS_ROOT.rglob("*"):
        if not path.is_file():
            continue
        if not is_guarded_buildtools_source(str(path), workspace_root=workspace_root):
            continue
        violation = scan_buildtools_source_file(path, workspace_root=workspace_root)
        if violation is not None:
            violations.append(violation)

    assert violations == []