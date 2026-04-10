from __future__ import annotations

"""Tests for the shared ClientRes version pointer helper.

These tests pin the ``clientRes_{platform}/version.info`` protocol shape so CI changes do not silently break
the runtime DevOps file-server entry.
"""

from types import SimpleNamespace

import pytest

from Common.client_resource_version_manifest import (
    ClientResourceVersionManifest,
    ClientResourceVersionManifestError,
    build_client_resource_version_remote_path,
    parse_client_resource_version_manifest,
    publish_client_resource_version_manifest,
    publish_table_version_manifests,
    resolve_manifest_build_label,
)


def test_parse_client_resource_version_manifest_round_trip() -> None:
    manifest = parse_client_resource_version_manifest("101.202.303")

    assert manifest == ClientResourceVersionManifest(
        code_version="101",
        assetbundle_version="202",
        table_version="303",
    )
    assert manifest.to_text() == "101.202.303"


def test_parse_client_resource_version_manifest_rejects_invalid_shape() -> None:
    with pytest.raises(ClientResourceVersionManifestError):
        parse_client_resource_version_manifest("101.202")


def test_build_client_resource_version_remote_path_uses_lowercase_root() -> None:
    assert build_client_resource_version_remote_path("ios") == "clientRes_ios/version.info"


def test_resolve_manifest_build_label_prefers_ci_build_number() -> None:
    assert resolve_manifest_build_label("238", "0.1.238") == "238"
    assert resolve_manifest_build_label(None, "0.1.238") is None


def test_publish_client_resource_version_manifest_updates_single_component(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    captured: dict[str, object] = {}
    settings = SimpleNamespace(base_url="http://fileserver")

    monkeypatch.setattr(
        "Common.client_resource_version_manifest.load_client_resource_version_manifest",
        lambda *args, **kwargs: ClientResourceVersionManifest("100", "200", "300"),
    )
    monkeypatch.setattr(
        "Common.client_resource_version_manifest.save_client_resource_version_manifest",
        lambda platform, manifest, **kwargs: captured.update({"platform": platform, "manifest": manifest, **kwargs}),
    )

    updated_manifest = publish_client_resource_version_manifest(
        "ios",
        component_kind="assetbundle",
        build_label="240",
        settings=settings,
    )

    assert updated_manifest == ClientResourceVersionManifest("100", "240", "300")
    assert captured == {
        "platform": "ios",
        "manifest": ClientResourceVersionManifest("100", "240", "300"),
        "settings": settings,
        "timeout_seconds": 600,
    }


def test_publish_table_version_manifests_updates_all_runtime_platforms(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
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

    assert captured_platforms == ["android", "ios", "windows"]
    assert manifests["ios"].table_version == "777"