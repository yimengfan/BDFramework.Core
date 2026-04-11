"""ClientRes 共享版控指针 (global_version.info) 单元测试。

测试覆盖范围：
- 版本 token 校验与规范化
- 构建号解析优先级
- ClientResourceVersionManifest 三段式序列化 / 反序列化
- GlobalVersionEntry dataclass 行为
- global_version.info JSON 协议解析 / 序列化
- 平台检索与 upsert 语义
- 文件服务器 I/O（通过 monkeypatch 模拟）
- publish 编排逻辑（单组件更新 + 全平台 table fan-out）
"""

from __future__ import annotations

import json
from types import SimpleNamespace
from typing import Any

import pytest

from Common.artifact_uploader import ArtifactUploadError
from Common.client_resource_version_manifest import (
    ClientResourceVersionManifest,
    ClientResourceVersionManifestError,
    DEFAULT_GAME_SERVER_IP,
    DEFAULT_VERSION_KEY,
    GLOBAL_VERSION_FILENAME,
    GlobalVersionEntry,
    KNOWN_CLIENT_RES_PLATFORMS,
    build_global_version_remote_path,
    find_entry_by_platform,
    load_client_resource_version_manifest,
    load_global_version_info,
    normalize_manifest_version_token,
    parse_client_resource_version_manifest,
    parse_global_version_entries,
    publish_client_resource_version_manifest,
    publish_table_version_manifests,
    resolve_manifest_build_label,
    save_client_resource_version_manifest,
    save_global_version_info,
    serialize_global_version_entries,
    upsert_global_version_entry,
)


class TestNormalizeManifestVersionToken:
    """版本 token 校验：确保写入 manifest 的每个段不包含非法字符。"""

    def test_accepts_valid_token(self) -> None:
        assert normalize_manifest_version_token("238") == "238"

    def test_strips_whitespace(self) -> None:
        assert normalize_manifest_version_token("  238  ") == "238"

    def test_rejects_empty(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="empty"):
            normalize_manifest_version_token("")

    def test_rejects_none(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="empty"):
            normalize_manifest_version_token(None)

    def test_rejects_whitespace_only(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="empty"):
            normalize_manifest_version_token("   ")

    def test_rejects_dot(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="cannot contain '.'"):
            normalize_manifest_version_token("0.1.238")

    def test_rejects_newline(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="invalid characters"):
            normalize_manifest_version_token("23\n8")

    def test_rejects_tab(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="invalid characters"):
            normalize_manifest_version_token("23\t8")

    def test_rejects_forward_slash(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="invalid characters"):
            normalize_manifest_version_token("238/1")

    def test_rejects_backslash(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="invalid characters"):
            normalize_manifest_version_token("238\\1")

    def test_rejects_carriage_return(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="invalid characters"):
            normalize_manifest_version_token("23\r8")


class TestResolveManifestBuildLabel:
    """构建号解析优先级：优先使用 CI 纯数字 build number，其次使用 fallback。"""

    def test_prefers_ci_build_number(self) -> None:
        assert resolve_manifest_build_label("238", "0.1.238") == "238"

    def test_returns_none_when_both_contain_dots(self) -> None:
        assert resolve_manifest_build_label("0.1.238", "0.1.238") is None

    def test_returns_none_when_both_none(self) -> None:
        assert resolve_manifest_build_label(None, None) is None

    def test_returns_none_when_both_empty(self) -> None:
        assert resolve_manifest_build_label("", "") is None

    def test_falls_back_to_clean_fallback(self) -> None:
        assert resolve_manifest_build_label("0.1.238", "238") == "238"

    def test_build_number_none_fallback_dotted_returns_none(self) -> None:
        assert resolve_manifest_build_label(None, "0.1.238") is None

    def test_build_number_clean_fallback_none(self) -> None:
        assert resolve_manifest_build_label("238", None) == "238"

    def test_build_number_empty_fallback_clean(self) -> None:
        assert resolve_manifest_build_label("", "238") == "238"


class TestClientResourceVersionManifest:
    """ClientResourceVersionManifest 三段式版本号 dataclass 行为测试。"""

    def test_default_values(self) -> None:
        manifest = ClientResourceVersionManifest()
        assert manifest.code_version == "0"
        assert manifest.assetbundle_version == "0"
        assert manifest.table_version == "0"
        assert manifest.to_text() == "0.0.0"

    def test_to_text_serializes_components(self) -> None:
        manifest = ClientResourceVersionManifest("101", "202", "303")
        assert manifest.to_text() == "101.202.303"

    def test_with_component_code(self) -> None:
        base = ClientResourceVersionManifest("100", "200", "300")
        updated = base.with_component("code", "999")
        assert updated == ClientResourceVersionManifest("999", "200", "300")

    def test_with_component_assetbundle(self) -> None:
        base = ClientResourceVersionManifest("100", "200", "300")
        updated = base.with_component("assetbundle", "888")
        assert updated == ClientResourceVersionManifest("100", "888", "300")

    def test_with_component_table(self) -> None:
        base = ClientResourceVersionManifest("100", "200", "300")
        updated = base.with_component("table", "777")
        assert updated == ClientResourceVersionManifest("100", "200", "777")

    def test_with_component_rejects_unknown_kind(self) -> None:
        base = ClientResourceVersionManifest("100", "200", "300")
        with pytest.raises(ClientResourceVersionManifestError, match="Unsupported"):
            base.with_component("unknown", "999")

    def test_frozen_immutability(self) -> None:
        manifest = ClientResourceVersionManifest("1", "2", "3")
        with pytest.raises(AttributeError):
            manifest.code_version = "9"  # type: ignore[misc]


class TestParseClientResourceVersionManifest:
    """三段式 version_num (code.assetbundle.table) 解析与反序列化测试。"""

    def test_round_trip(self) -> None:
        manifest = parse_client_resource_version_manifest("101.202.303")
        assert manifest == ClientResourceVersionManifest("101", "202", "303")
        assert manifest.to_text() == "101.202.303"

    def test_rejects_empty_string(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError):
            parse_client_resource_version_manifest("")

    def test_rejects_whitespace_only(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError):
            parse_client_resource_version_manifest("   ")

    def test_rejects_none(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError):
            parse_client_resource_version_manifest(None)  # type: ignore[arg-type]

    def test_rejects_two_segments(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError):
            parse_client_resource_version_manifest("101.202")

    def test_rejects_four_segments(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError):
            parse_client_resource_version_manifest("101.202.303.404")

    def test_rejects_empty_middle_segment(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError):
            parse_client_resource_version_manifest("101..303")

    def test_strips_whitespace_around_segments(self) -> None:
        manifest = parse_client_resource_version_manifest(" 101 . 202 . 303 ")
        assert manifest == ClientResourceVersionManifest("101", "202", "303")


class TestGlobalVersionEntry:
    """GlobalVersionEntry dataclass：对应 global_version.info 中一条 JSON 记录。"""

    def test_default_values(self) -> None:
        entry = GlobalVersionEntry()
        assert entry.key == DEFAULT_VERSION_KEY
        assert entry.platform == ""
        assert entry.version_num == "0.0.0"
        assert entry.game_server_ip == DEFAULT_GAME_SERVER_IP

    def test_custom_values(self) -> None:
        entry = GlobalVersionEntry(
            key="custom",
            platform="ios",
            version_num="101.202.303",
            game_server_ip="10.0.0.1",
        )
        assert entry.key == "custom"
        assert entry.platform == "ios"
        assert entry.version_num == "101.202.303"
        assert entry.game_server_ip == "10.0.0.1"

    def test_to_dict(self) -> None:
        entry = GlobalVersionEntry(platform="ios", version_num="101.202.303")
        d = entry.to_dict()
        assert d == {
            "key": DEFAULT_VERSION_KEY,
            "platform": "ios",
            "version_num": "101.202.303",
            "game_server_ip": DEFAULT_GAME_SERVER_IP,
        }

    def test_to_dict_round_trips_through_parse(self) -> None:
        original = GlobalVersionEntry(
            key="test",
            platform="android",
            version_num="401.502.603",
            game_server_ip="10.0.0.2",
        )
        serialized = json.dumps([original.to_dict()])
        parsed = parse_global_version_entries(serialized)
        assert len(parsed) == 1
        assert parsed[0] == original

    def test_frozen_immutability(self) -> None:
        entry = GlobalVersionEntry(platform="ios")
        with pytest.raises(AttributeError):
            entry.platform = "android"  # type: ignore[misc]


class TestBuildGlobalVersionRemotePath:
    """远程路径生成：确保返回不带平台子文件夹的平铺文件名。"""

    def test_returns_filename(self) -> None:
        assert build_global_version_remote_path() == GLOBAL_VERSION_FILENAME
        assert build_global_version_remote_path() == "global_version.info"


class TestParseGlobalVersionEntries:
    """global_version.info JSON 数组解析：覆盖正常 / 空值 / 非法格式 / 缺失字段等场景。"""

    def test_parses_json_array(self) -> None:
        content = json.dumps([
            {"key": "default", "platform": "ios", "version_num": "101.202.303", "game_server_ip": "10.0.0.1"},
            {"key": "default", "platform": "android", "version_num": "401.502.603", "game_server_ip": "10.0.0.2"},
        ])
        entries = parse_global_version_entries(content)

        assert len(entries) == 2
        assert entries[0] == GlobalVersionEntry(
            key="default", platform="ios", version_num="101.202.303", game_server_ip="10.0.0.1",
        )
        assert entries[1] == GlobalVersionEntry(
            key="default", platform="android", version_num="401.502.603", game_server_ip="10.0.0.2",
        )

    def test_returns_empty_for_empty_content(self) -> None:
        assert parse_global_version_entries("") == []
        assert parse_global_version_entries("  ") == []

    def test_returns_empty_for_empty_array(self) -> None:
        assert parse_global_version_entries("[]") == []

    def test_rejects_invalid_json(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="not valid JSON"):
            parse_global_version_entries("not json")

    def test_rejects_non_array(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="JSON array"):
            parse_global_version_entries('{"key": "value"}')

    def test_rejects_string_root(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="JSON array"):
            parse_global_version_entries('"hello"')

    def test_rejects_number_root(self) -> None:
        with pytest.raises(ClientResourceVersionManifestError, match="JSON array"):
            parse_global_version_entries("42")

    def test_skips_non_dict_entries(self) -> None:
        content = json.dumps([
            "string_entry",
            42,
            None,
            True,
            {"platform": "ios", "version_num": "101.202.303"},
        ])
        entries = parse_global_version_entries(content)
        assert len(entries) == 1
        assert entries[0].platform == "ios"

    def test_uses_defaults_for_missing_fields(self) -> None:
        content = json.dumps([{"platform": "ios"}])
        entries = parse_global_version_entries(content)
        assert len(entries) == 1
        assert entries[0].key == DEFAULT_VERSION_KEY
        assert entries[0].platform == "ios"
        assert entries[0].version_num == "0.0.0"
        assert entries[0].game_server_ip == DEFAULT_GAME_SERVER_IP

    def test_uses_defaults_for_null_fields(self) -> None:
        content = json.dumps([{"key": None, "platform": "ios", "version_num": None, "game_server_ip": None}])
        entries = parse_global_version_entries(content)
        assert len(entries) == 1
        assert entries[0].key == DEFAULT_VERSION_KEY
        assert entries[0].version_num == "0.0.0"
        assert entries[0].game_server_ip == DEFAULT_GAME_SERVER_IP

    def test_uses_defaults_for_empty_string_fields(self) -> None:
        content = json.dumps([{"key": "", "platform": "ios", "version_num": "", "game_server_ip": ""}])
        entries = parse_global_version_entries(content)
        assert len(entries) == 1
        assert entries[0].key == DEFAULT_VERSION_KEY
        assert entries[0].version_num == "0.0.0"
        assert entries[0].game_server_ip == DEFAULT_GAME_SERVER_IP


class TestSerializeGlobalVersionEntries:
    """GlobalVersionEntry 列表序列化回 JSON 字符串测试。"""

    def test_round_trip(self) -> None:
        entries = [
            GlobalVersionEntry(platform="ios", version_num="101.202.303"),
            GlobalVersionEntry(platform="android", version_num="401.502.603"),
        ]
        serialized = serialize_global_version_entries(entries)
        parsed = parse_global_version_entries(serialized)

        assert len(parsed) == 2
        assert parsed[0].platform == "ios"
        assert parsed[0].version_num == "101.202.303"
        assert parsed[1].platform == "android"
        assert parsed[1].version_num == "401.502.603"

    def test_empty_list_produces_empty_array(self) -> None:
        serialized = serialize_global_version_entries([])
        assert json.loads(serialized) == []

    def test_output_is_valid_json(self) -> None:
        entries = [GlobalVersionEntry(platform="windows", version_num="111.222.333")]
        serialized = serialize_global_version_entries(entries)
        parsed_json = json.loads(serialized)
        assert isinstance(parsed_json, list)
        assert len(parsed_json) == 1
        assert parsed_json[0]["platform"] == "windows"

    def test_preserves_all_fields(self) -> None:
        entry = GlobalVersionEntry(
            key="custom_key",
            platform="ios",
            version_num="101.202.303",
            game_server_ip="10.0.0.5",
        )
        serialized = serialize_global_version_entries([entry])
        parsed_json = json.loads(serialized)
        assert parsed_json[0] == {
            "key": "custom_key",
            "platform": "ios",
            "version_num": "101.202.303",
            "game_server_ip": "10.0.0.5",
        }


class TestFindEntryByPlatform:
    """平台检索：按 platform 字段在 entries 列表中查找匹配记录。"""

    def test_finds_match_case_insensitive(self) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        assert find_entry_by_platform(entries, "IOS") is not None
        assert find_entry_by_platform(entries, "ios").version_num == "101.202.303"
        assert find_entry_by_platform(entries, "Ios") is not None

    def test_returns_none_for_missing(self) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        assert find_entry_by_platform(entries, "android") is None

    def test_returns_none_for_empty_list(self) -> None:
        assert find_entry_by_platform([], "ios") is None

    def test_returns_first_match_when_duplicates(self) -> None:
        entries = [
            GlobalVersionEntry(platform="ios", version_num="101.202.303"),
            GlobalVersionEntry(platform="ios", version_num="999.888.777"),
        ]
        result = find_entry_by_platform(entries, "ios")
        assert result is not None
        assert result.version_num == "101.202.303"

    def test_strips_whitespace(self) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        assert find_entry_by_platform(entries, "  ios  ") is not None


class TestUpsertGlobalVersionEntry:
    """upsert 语义：已有平台更新 version_num，新平台追加记录。"""

    def test_updates_existing_entry(self) -> None:
        entries = [
            GlobalVersionEntry(platform="ios", version_num="101.202.303"),
            GlobalVersionEntry(platform="android", version_num="401.502.603"),
        ]
        updated = upsert_global_version_entry(entries, "ios", "999.888.777")

        assert len(updated) == 2
        assert updated[0].version_num == "999.888.777"
        assert updated[1].version_num == "401.502.603"

    def test_inserts_new_entry(self) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        updated = upsert_global_version_entry(entries, "windows", "111.222.333")

        assert len(updated) == 2
        assert updated[0].version_num == "101.202.303"
        assert updated[1].platform == "windows"
        assert updated[1].version_num == "111.222.333"

    def test_inserts_into_empty_list(self) -> None:
        updated = upsert_global_version_entry([], "ios", "101.202.303")

        assert len(updated) == 1
        assert updated[0].platform == "ios"
        assert updated[0].version_num == "101.202.303"

    def test_uses_default_key_and_game_server_ip(self) -> None:
        updated = upsert_global_version_entry([], "ios", "101.202.303")
        assert updated[0].key == DEFAULT_VERSION_KEY
        assert updated[0].game_server_ip == DEFAULT_GAME_SERVER_IP

    def test_uses_custom_key_and_game_server_ip_on_insert(self) -> None:
        updated = upsert_global_version_entry(
            [],
            "ios",
            "101.202.303",
            key="custom",
            game_server_ip="10.0.0.5",
        )
        assert updated[0].key == "custom"
        assert updated[0].game_server_ip == "10.0.0.5"

    def test_preserves_existing_key_and_game_server_ip_on_update(self) -> None:
        entries = [
            GlobalVersionEntry(
                key="original",
                platform="ios",
                version_num="101.202.303",
                game_server_ip="10.0.0.5",
            ),
        ]
        updated = upsert_global_version_entry(
            entries,
            "ios",
            "999.888.777",
            key="new_key",
            game_server_ip="10.0.0.9",
        )
        assert len(updated) == 1
        assert updated[0].key == "original"
        assert updated[0].game_server_ip == "10.0.0.5"
        assert updated[0].version_num == "999.888.777"

    def test_case_insensitive_platform_match(self) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        updated = upsert_global_version_entry(entries, "IOS", "999.888.777")
        assert len(updated) == 1
        assert updated[0].version_num == "999.888.777"

    def test_normalizes_platform_to_lowercase_on_insert(self) -> None:
        updated = upsert_global_version_entry([], "iOS", "101.202.303")
        assert updated[0].platform == "ios"

    def test_does_not_mutate_original_list(self) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        updated = upsert_global_version_entry(entries, "ios", "999.888.777")
        assert entries[0].version_num == "101.202.303"
        assert updated[0].version_num == "999.888.777"


class TestLoadGlobalVersionInfo:
    """从文件服务器读取 global_version.info：通过 monkeypatch 模拟 HTTP 响应。"""

    def _make_fake_connection(self, status: int, body: bytes) -> SimpleNamespace:
        """构造模拟的 HTTP 连接对象，用于 monkeypatch 替换真实的文件服务器连接。"""
        response = SimpleNamespace(status=status, read=lambda: body)
        connection = SimpleNamespace()
        connection.request = lambda method, path, headers=None: None
        connection.getresponse = lambda: response
        connection.close = lambda: None
        return connection

    def test_parses_valid_response(self, monkeypatch: pytest.MonkeyPatch) -> None:
        entries_json = json.dumps([
            {"platform": "ios", "version_num": "101.202.303"},
        ]).encode("utf-8")
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.create_http_connection",
            lambda *args, **kwargs: self._make_fake_connection(200, entries_json),
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.build_download_request_path",
            lambda base_url, remote_path: f"/download/{remote_path}",
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.build_authorization_headers",
            lambda *args, **kwargs: {},
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        entries = load_global_version_info(settings=settings)

        assert len(entries) == 1
        assert entries[0].platform == "ios"

    def test_returns_empty_for_404(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.create_http_connection",
            lambda *args, **kwargs: self._make_fake_connection(404, b""),
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.build_download_request_path",
            lambda base_url, remote_path: f"/download/{remote_path}",
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.build_authorization_headers",
            lambda *args, **kwargs: {},
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        entries = load_global_version_info(settings=settings)
        assert entries == []

    def test_raises_on_server_error(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.create_http_connection",
            lambda *args, **kwargs: self._make_fake_connection(500, b"server error"),
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.build_download_request_path",
            lambda base_url, remote_path: f"/download/{remote_path}",
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.build_authorization_headers",
            lambda *args, **kwargs: {},
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        with pytest.raises(ClientResourceVersionManifestError, match="status=500"):
            load_global_version_info(settings=settings)


class TestSaveGlobalVersionInfo:
    """将 entries 序列化为 JSON 并上传到文件服务器：通过 monkeypatch 模拟上传。"""

    def test_uploads_serialized_json(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured_upload: dict[str, Any] = {}

        def capture_upload(local_path, **kwargs):
            content = local_path.read_text(encoding="utf-8")
            captured_upload.update({"content": content, **kwargs})

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.upload_single_file",
            capture_upload,
        )

        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        settings = SimpleNamespace(base_url="http://fileserver")
        save_global_version_info(entries, settings=settings)

        assert captured_upload["remote_path"] == GLOBAL_VERSION_FILENAME
        assert captured_upload["overwrite"] is True
        parsed = json.loads(captured_upload["content"])
        assert len(parsed) == 1
        assert parsed[0]["platform"] == "ios"


class TestLoadClientResourceVersionManifest:
    """从 global_version.info 中提取指定平台的 version_num 并解析为三段式 manifest。"""

    def _make_fake_load(self, entries: list[GlobalVersionEntry]):
        """构造模拟的 load_global_version_info 函数，返回预设的 entries 列表。"""
        def fake_load(*args, **kwargs):
            return entries
        return fake_load

    def test_returns_manifest_for_existing_platform(self, monkeypatch: pytest.MonkeyPatch) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            self._make_fake_load(entries),
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        manifest = load_client_resource_version_manifest("ios", settings=settings)

        assert manifest == ClientResourceVersionManifest("101", "202", "303")

    def test_returns_default_for_missing_platform(self, monkeypatch: pytest.MonkeyPatch) -> None:
        entries = [GlobalVersionEntry(platform="ios", version_num="101.202.303")]
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            self._make_fake_load(entries),
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        manifest = load_client_resource_version_manifest("android", settings=settings)

        assert manifest == ClientResourceVersionManifest()

    def test_returns_default_for_empty_entries(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            self._make_fake_load([]),
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        manifest = load_client_resource_version_manifest("ios", settings=settings)

        assert manifest == ClientResourceVersionManifest()


class TestSaveClientResourceVersionManifest:
    """将指定平台的 manifest 写回 global_version.info：读取 → upsert → 保存。"""

    def test_upserts_and_saves(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured_load_calls: list[dict[str, Any]] = []
        captured_save_calls: list[dict[str, Any]] = []

        existing_entries = [GlobalVersionEntry(platform="ios", version_num="100.200.300")]

        def fake_load(*args, **kwargs):
            captured_load_calls.append({"args": args, "kwargs": kwargs})
            return existing_entries

        def fake_save(entries, **kwargs):
            captured_save_calls.append({"entries": entries, **kwargs})

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            fake_load,
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.save_global_version_info",
            fake_save,
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        manifest = ClientResourceVersionManifest("999", "200", "300")
        save_client_resource_version_manifest("ios", manifest, settings=settings)

        assert len(captured_save_calls) == 1
        saved_entries = captured_save_calls[0]["entries"]
        assert len(saved_entries) == 1
        assert saved_entries[0].version_num == "999.200.300"

    def test_inserts_new_platform_when_missing(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured_save_calls: list[dict[str, Any]] = []

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            lambda *args, **kwargs: [GlobalVersionEntry(platform="ios", version_num="100.200.300")],
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.save_global_version_info",
            lambda entries, **kwargs: captured_save_calls.append({"entries": entries}),
        )

        settings = SimpleNamespace(base_url="http://fileserver")
        manifest = ClientResourceVersionManifest("100", "200", "777")
        save_client_resource_version_manifest("android", manifest, settings=settings)

        saved_entries = captured_save_calls[0]["entries"]
        assert len(saved_entries) == 2
        assert saved_entries[0].platform == "ios"
        assert saved_entries[1].platform == "android"
        assert saved_entries[1].version_num == "100.200.777"


class TestPublishClientResourceVersionManifest:
    """发布单组件版本更新：读取当前 manifest → 替换指定段 → 写回文件服务器。"""

    def test_updates_single_component(self, monkeypatch: pytest.MonkeyPatch) -> None:
        """验证发布单个组件版本时，会在当前平台 manifest 上只替换目标组件段。"""
        captured: dict[str, object] = {}
        settings = SimpleNamespace(base_url="http://fileserver")

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            lambda *args, **kwargs: [GlobalVersionEntry(platform="ios", version_num="100.200.300")],
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.save_global_version_info",
            lambda entries, **kwargs: captured.update({"entries": entries, **kwargs}),
        )

        updated_manifest = publish_client_resource_version_manifest(
            "ios",
            component_kind="assetbundle",
            build_label="240",
            settings=settings,
        )

        assert updated_manifest == ClientResourceVersionManifest("100", "240", "300")
        assert captured == {
            "entries": [GlobalVersionEntry(platform="ios", version_num="100.240.300")],
            "settings": settings,
            "timeout_seconds": 600,
        }

    def test_updates_code_component(self, monkeypatch: pytest.MonkeyPatch) -> None:
        """验证发布 code 组件版本时，会只覆盖 code 段并保留其他段。"""
        captured: dict[str, object] = {}
        settings = SimpleNamespace(base_url="http://fileserver")

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            lambda *args, **kwargs: [GlobalVersionEntry(platform="android", version_num="100.200.300")],
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.save_global_version_info",
            lambda entries, **kwargs: captured.update({"entries": entries}),
        )

        result = publish_client_resource_version_manifest(
            "android",
            component_kind="code",
            build_label="999",
            settings=settings,
        )
        assert result == ClientResourceVersionManifest("999", "200", "300")
        assert captured["entries"][0].version_num == "999.200.300"

    def test_retries_with_latest_remote_manifest_when_upload_detects_concurrent_overwrite(
        self,
        monkeypatch: pytest.MonkeyPatch,
    ) -> None:
        """验证共享版控上传命中并发覆盖时，会重新加载最新远端 manifest 再合并当前组件版本。"""
        settings = SimpleNamespace(base_url="http://fileserver")
        load_results = iter(
            [
                [GlobalVersionEntry(platform="android", version_num="10.20.30")],
                [GlobalVersionEntry(platform="android", version_num="10.21.30")],
            ]
        )
        saved_entries: list[list[GlobalVersionEntry]] = []

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            lambda *args, **kwargs: next(load_results),
        )

        def fake_save(entries, **kwargs):
            saved_entries.append(entries)
            if len(saved_entries) == 1:
                raise ArtifactUploadError(
                    "File upload failed. local=/tmp/global_version.info, remote=global_version.info, "
                    "status=500, detail=Internal Server Error, recovery_check=metadata sha256_mismatch "
                    "expected=old actual=new"
                )

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.save_global_version_info",
            fake_save,
        )

        result = publish_client_resource_version_manifest(
            "android",
            component_kind="code",
            build_label="99",
            settings=settings,
        )

        assert [entries[0].version_num for entries in saved_entries] == ["99.20.30", "99.21.30"]
        assert result == ClientResourceVersionManifest("99", "21", "30")

    def test_updates_table_component(self, monkeypatch: pytest.MonkeyPatch) -> None:
        """验证发布 table 组件版本时，会只覆盖 table 段并保留其他段。"""
        captured: dict[str, object] = {}
        settings = SimpleNamespace(base_url="http://fileserver")

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_global_version_info",
            lambda *args, **kwargs: [GlobalVersionEntry(platform="windows", version_num="100.200.300")],
        )
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.save_global_version_info",
            lambda entries, **kwargs: captured.update({"entries": entries}),
        )

        result = publish_client_resource_version_manifest(
            "windows",
            component_kind="table",
            build_label="555",
            settings=settings,
        )
        assert result == ClientResourceVersionManifest("100", "200", "555")
        assert captured["entries"][0].version_num == "100.200.555"

    def test_rejects_dotted_build_label(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.load_client_resource_version_manifest",
            lambda *args, **kwargs: ClientResourceVersionManifest(),
        )
        settings = SimpleNamespace(base_url="http://fileserver")
        with pytest.raises(ClientResourceVersionManifestError, match="cannot contain '.'"):
            publish_client_resource_version_manifest(
                "ios",
                component_kind="code",
                build_label="0.1.238",
                settings=settings,
            )


class TestPublishTableVersionManifests:
    """全平台 table 版本发布：table 资源跨平台共享，需要 fan-out 到所有运行时平台。"""

    def test_updates_all_runtime_platforms(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured_platforms: list[str] = []

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.publish_client_resource_version_manifest",
            lambda platform, **kwargs: captured_platforms.append(platform)
            or ClientResourceVersionManifest("0", "0", kwargs["build_label"]),
        )

        manifests = publish_table_version_manifests(
            build_label="777",
            settings=SimpleNamespace(base_url="http://fileserver"),
        )

        assert captured_platforms == list(KNOWN_CLIENT_RES_PLATFORMS)
        assert manifests["android"].table_version == "777"
        assert manifests["ios"].table_version == "777"
        assert manifests["windows"].table_version == "777"

    def test_custom_platforms(self, monkeypatch: pytest.MonkeyPatch) -> None:
        captured_platforms: list[str] = []

        monkeypatch.setattr(
            "Common.client_resource_version_manifest.publish_client_resource_version_manifest",
            lambda platform, **kwargs: captured_platforms.append(platform)
            or ClientResourceVersionManifest("0", "0", kwargs["build_label"]),
        )

        manifests = publish_table_version_manifests(
            build_label="100",
            settings=SimpleNamespace(base_url="http://fileserver"),
            platforms=("ios",),
        )

        assert captured_platforms == ["ios"]
        assert manifests["ios"].table_version == "100"

    def test_empty_platforms_produces_empty_result(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setattr(
            "Common.client_resource_version_manifest.publish_client_resource_version_manifest",
            lambda platform, **kwargs: ClientResourceVersionManifest("0", "0", kwargs["build_label"]),
        )

        manifests = publish_table_version_manifests(
            build_label="100",
            settings=SimpleNamespace(base_url="http://fileserver"),
            platforms=(),
        )

        assert manifests == {}
