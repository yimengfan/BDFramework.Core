"""Talos E2E 独立配置加载器测试。

覆盖范围：
1. 硬编码默认值在无任何配置文件时正确返回。
2. 包内默认 TOML 配置正确覆盖硬编码默认值。
3. DevOps/CI 覆盖配置正确覆盖包内默认值。
4. 环境变量指定的配置文件具有最高文件优先级。
5. resolve_talos_e2e_config_path 的 fallback 顺序。
6. 环境变量指定不存在的文件时报错。
7. 配置加载日志输出。

Talos E2E standalone configuration loader tests.

Coverage:
1. Hardcoded defaults are returned when no config file exists.
2. Package-default TOML correctly overrides hardcoded defaults.
3. DevOps/CI override config correctly overrides package defaults.
4. Env-var-specified config file has the highest file priority.
5. resolve_talos_e2e_config_path fallback order.
6. Error when env var points to a non-existent file.
7. Config loading log output.
"""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys

import pytest


TOOLS_ROOT = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS_ROOT / "talos_e2e_config.py"
MODULE_NAME = "talos_e2e_config"

MODULE_SPEC = importlib.util.spec_from_file_location(MODULE_NAME, MODULE_PATH)
if MODULE_SPEC is None or MODULE_SPEC.loader is None:
    raise RuntimeError(f"无法加载测试目标模块: {MODULE_PATH}")

config_module = importlib.util.module_from_spec(MODULE_SPEC)
sys.modules[MODULE_NAME] = config_module
MODULE_SPEC.loader.exec_module(config_module)


def test_load_talos_e2e_config_returns_hardcoded_defaults_when_no_files(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证没有任何配置文件时，加载器返回硬编码默认值。
    Verify that the loader returns hardcoded defaults when no config files exist.
    """
    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", Path("/nonexistent/package/talos_e2e_config.toml"))
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))

    config = config_module.load_talos_e2e_config()

    assert config.client_version == "0.1"
    assert config.build_debug == "true"
    assert config.timeout_seconds == 5400
    assert config.poll_interval_seconds == 10
    assert config.download_timeout_seconds == 600
    assert config.unity_host == "127.0.0.1"
    assert config.unity_port == 10002
    assert config.emulator_type == "mumu"
    assert config.mumu_auto_start == "true"
    assert config.adb_connect_targets == "127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555"


def test_load_talos_e2e_config_overlays_package_default(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证包内默认 TOML 配置正确覆盖硬编码默认值。
    Verify that the package-default TOML correctly overrides hardcoded defaults.
    """
    package_config_path = tmp_path / "package_default.toml"
    package_config_path.write_text(
        """
[talos.e2e]
client_version = "0.2"
build_debug = "false"
timeout_seconds = 7200
unity_host = "192.168.1.100"
unity_port = 20002
""",
        encoding="utf-8",
    )

    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", package_config_path)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))

    config = config_module.load_talos_e2e_config()

    # 包内配置覆盖了以下字段。
    # Fields overridden by package-default config.
    assert config.client_version == "0.2"
    assert config.build_debug == "false"
    assert config.timeout_seconds == 7200
    assert config.unity_host == "192.168.1.100"
    assert config.unity_port == 20002
    # 未覆盖的字段保持硬编码默认值。
    # Non-overridden fields keep hardcoded defaults.
    assert config.poll_interval_seconds == 10
    assert config.download_timeout_seconds == 600


def test_load_talos_e2e_config_overlays_devops_ci(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证 DevOps/CI 覆盖配置正确叠加在包内默认之上。
    Verify that DevOps/CI override config correctly overlays on top of package defaults.
    """
    package_config_path = tmp_path / "package_default.toml"
    package_config_path.write_text(
        """
[talos.e2e]
client_version = "0.2"
build_debug = "false"
timeout_seconds = 7200
unity_host = "192.168.1.100"
unity_port = 20002
""",
        encoding="utf-8",
    )

    devops_config_path = tmp_path / "devops_ci.toml"
    devops_config_path.write_text(
        """
[talos.e2e]
timeout_seconds = 3600
unity_port = 13002
emulator_type = "nox"
""",
        encoding="utf-8",
    )

    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", package_config_path)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", devops_config_path)

    config = config_module.load_talos_e2e_config()

    # DevOps/CI 覆盖了 timeout_seconds 和 unity_port。
    # DevOps/CI overrides timeout_seconds and unity_port.
    assert config.timeout_seconds == 3600
    assert config.unity_port == 13002
    assert config.emulator_type == "nox"
    # 包内默认值保留。
    # Package-default values preserved.
    assert config.client_version == "0.2"
    assert config.build_debug == "false"
    assert config.unity_host == "192.168.1.100"


def test_load_talos_e2e_config_overlays_env_var(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证环境变量指定的配置文件具有最高文件优先级。
    Verify that env-var-specified config file has the highest file priority.
    """
    package_config_path = tmp_path / "package_default.toml"
    package_config_path.write_text(
        """
[talos.e2e]
client_version = "0.2"
build_debug = "false"
""",
        encoding="utf-8",
    )

    devops_config_path = tmp_path / "devops_ci.toml"
    devops_config_path.write_text(
        """
[talos.e2e]
timeout_seconds = 3600
""",
        encoding="utf-8",
    )

    env_config_path = tmp_path / "env_override.toml"
    env_config_path.write_text(
        """
[talos.e2e]
client_version = "0.5"
unity_host = "10.0.0.1"
adb_connect_targets = "127.0.0.1:5555"
""",
        encoding="utf-8",
    )

    monkeypatch.setenv(config_module.ENV_VAR_CONFIG_PATH, str(env_config_path))
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", package_config_path)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", devops_config_path)

    config = config_module.load_talos_e2e_config()

    # 环境变量配置覆盖了 client_version 和 unity_host。
    # Env-var config overrides client_version and unity_host.
    assert config.client_version == "0.5"
    assert config.unity_host == "10.0.0.1"
    assert config.adb_connect_targets == "127.0.0.1:5555"
    # DevOps/CI 覆盖保留。
    # DevOps/CI override preserved.
    assert config.timeout_seconds == 3600
    # 包内默认保留。
    # Package-default preserved.
    assert config.build_debug == "false"


def test_load_talos_e2e_config_raises_on_missing_env_var_file(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证环境变量指定的文件不存在时抛出 TalosE2EConfigError。
    Verify that TalosE2EConfigError is raised when the env-var-specified file does not exist.
    """
    monkeypatch.setenv(config_module.ENV_VAR_CONFIG_PATH, "/nonexistent/env_config.toml")
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", Path("/nonexistent/package/talos_e2e_config.toml"))
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))

    with pytest.raises(config_module.TalosE2EConfigError, match="does not exist"):
        config_module.load_talos_e2e_config()


def test_resolve_talos_e2e_config_path_returns_env_var_path(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证环境变量设置时，resolve_talos_e2e_config_path 返回环境变量指定的路径。
    Verify that resolve_talos_e2e_config_path returns the env-var-specified path.
    """
    env_config_path = tmp_path / "env_config.toml"
    env_config_path.write_text("[talos.e2e]\n", encoding="utf-8")

    monkeypatch.setenv(config_module.ENV_VAR_CONFIG_PATH, str(env_config_path))

    result = config_module.resolve_talos_e2e_config_path()

    assert result == env_config_path.resolve()


def test_resolve_talos_e2e_config_path_returns_devops_ci_path(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证无环境变量但有 DevOps/CI 配置时，返回 DevOps/CI 路径。
    Verify that when no env var is set but DevOps/CI config exists, the DevOps/CI path is returned.
    """
    devops_config_path = tmp_path / "devops_ci.toml"
    devops_config_path.write_text("[talos.e2e]\n", encoding="utf-8")

    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", devops_config_path)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", Path("/nonexistent/package/talos_e2e_config.toml"))

    result = config_module.resolve_talos_e2e_config_path()

    assert result == devops_config_path


def test_resolve_talos_e2e_config_path_returns_package_default_path(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证只有包内默认配置时，返回包内默认路径。
    Verify that when only package-default config exists, its path is returned.
    """
    package_config_path = tmp_path / "package_default.toml"
    package_config_path.write_text("[talos.e2e]\n", encoding="utf-8")

    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", package_config_path)

    result = config_module.resolve_talos_e2e_config_path()

    assert result == package_config_path


def test_resolve_talos_e2e_config_path_returns_none_when_no_files(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证无任何配置文件时，resolve_talos_e2e_config_path 返回 None。
    Verify that resolve_talos_e2e_config_path returns None when no config files exist.
    """
    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", Path("/nonexistent/package/talos_e2e_config.toml"))

    result = config_module.resolve_talos_e2e_config_path()

    assert result is None


def test_resolve_talos_e2e_config_path_raises_on_missing_env_var_file(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """验证环境变量指定不存在的文件时，resolve_talos_e2e_config_path 抛错。
    Verify that resolve_talos_e2e_config_path raises when env var points to a non-existent file.
    """
    monkeypatch.setenv(config_module.ENV_VAR_CONFIG_PATH, "/nonexistent/env_config.toml")

    with pytest.raises(config_module.TalosE2EConfigError, match="does not exist"):
        config_module.resolve_talos_e2e_config_path()


def test_load_talos_e2e_config_logs_config_source(
    monkeypatch: pytest.MonkeyPatch,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """验证配置加载过程会输出配置来源日志。
    Verify that the config loading process logs the config source.
    """
    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", Path("/nonexistent/package/talos_e2e_config.toml"))
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))

    config_module.load_talos_e2e_config()
    captured = capsys.readouterr()

    assert "loadedDefaults=hardcoded" in captured.out
    assert "finalConfig:" in captured.out
    assert "client_version=" in captured.out


def test_load_talos_e2e_config_logs_overlay_layers(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    """验证多层配置覆盖时日志会输出每一层的信息。
    Verify that logs include each overlay layer when multiple config layers are applied.
    """
    package_config_path = tmp_path / "package_default.toml"
    package_config_path.write_text(
        """
[talos.e2e]
client_version = "0.2"
""",
        encoding="utf-8",
    )

    devops_config_path = tmp_path / "devops_ci.toml"
    devops_config_path.write_text(
        """
[talos.e2e]
timeout_seconds = 3600
""",
        encoding="utf-8",
    )

    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", package_config_path)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", devops_config_path)

    config_module.load_talos_e2e_config()
    captured = capsys.readouterr()

    assert "overlay=package-default" in captured.out
    assert "overlay=devops-ci" in captured.out
    assert "finalConfig:" in captured.out


def test_load_talos_e2e_config_ignores_empty_toml_values(
    monkeypatch: pytest.MonkeyPatch,
    tmp_path: Path,
) -> None:
    """验证 TOML 中的空字符串值不会覆盖已有配置。
    Verify that empty string values in TOML do not override existing config.
    """
    package_config_path = tmp_path / "package_default.toml"
    package_config_path.write_text(
        """
[talos.e2e]
client_version = "0.2"
unity_host = ""
emulator_type = ""
""",
        encoding="utf-8",
    )

    monkeypatch.delenv(config_module.ENV_VAR_CONFIG_PATH, raising=False)
    monkeypatch.setattr(config_module, "PACKAGE_DEFAULT_CONFIG_PATH", package_config_path)
    monkeypatch.setattr(config_module, "DEVOPS_CI_CONFIG_PATH", Path("/nonexistent/devops/talos_e2e_config.toml"))

    config = config_module.load_talos_e2e_config()

    # client_version 被覆盖；unity_host 和 emulator_type 为空字符串，保持默认。
    # client_version is overridden; unity_host and emulator_type are empty strings, defaults preserved.
    assert config.client_version == "0.2"
    assert config.unity_host == "127.0.0.1"
    assert config.emulator_type == "mumu"


def test_talos_e2e_config_is_frozen() -> None:
    """验证 TalosE2EConfig 是 frozen dataclass，防止意外修改。
    Verify that TalosE2EConfig is a frozen dataclass, preventing accidental mutation.
    """
    config = config_module.TalosE2EConfig()
    with pytest.raises(AttributeError):
        config.client_version = "9.9"  # type: ignore[misc]


def test_talos_e2e_config_error_is_runtime_error() -> None:
    """验证 TalosE2EConfigError 继承 RuntimeError。
    Verify that TalosE2EConfigError inherits RuntimeError.
    """
    assert issubclass(config_module.TalosE2EConfigError, RuntimeError)
