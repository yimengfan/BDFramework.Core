"""Talos E2E 独立配置加载器。

Talos E2E standalone configuration loader.

职责：
Responsibilities:
1. 从 E2E 自己的 TOML 配置文件读取参数，不复用 BuildTools 配置体系。
1. Read parameters from E2E's own TOML config files, not reusing the BuildTools config system.
2. 实现 fallback 规则：环境变量 → DevOps/CI 覆盖 → 包内默认 → 硬编码默认值。
2. Implement fallback: env var → DevOps/CI override → package defaults → hardcoded defaults.
3. 每次读取配置时输出日志，便于排查配置来源。
3. Log config source on every read for easy troubleshooting.

配置优先级（由高到低）：
Config priority (highest to lowest):
1. CLI 显式传参（由 teamcity_e2e_runner.py 的 argparse 处理）
   CLI explicit arguments (handled by teamcity_e2e_runner.py argparse)
2. 环境变量 TALOS_E2E_CONFIG 指定的配置文件
   Config file specified by TALOS_E2E_CONFIG env var
3. DevOps/CI/talos_e2e_config.toml（CI 环境覆盖）
   DevOps/CI/talos_e2e_config.toml (CI environment override)
4. 本包 Playwright~/tools/talos_e2e_config.toml（包内默认值）
   Package Playwright~/tools/talos_e2e_config.toml (package defaults)
5. 代码中的硬编码默认值（TalosE2EConfig dataclass 字段默认值）
   Hardcoded defaults (TalosE2EConfig dataclass field defaults)
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any


TOOL_DIR = Path(__file__).resolve().parent
REPO_ROOT = Path(__file__).resolve().parents[4]
# E2E 包内默认配置路径。
# Package-default config path.
PACKAGE_DEFAULT_CONFIG_PATH = TOOL_DIR / "talos_e2e_config.toml"
# CI 环境覆盖配置路径。
# CI environment override config path.
DEVOPS_CI_CONFIG_PATH = REPO_ROOT / "DevOps" / "CI" / "talos_e2e_config.toml"
# 环境变量名：指定 E2E 配置文件的绝对路径。
# Env var name: absolute path to the E2E config file.
ENV_VAR_CONFIG_PATH = "TALOS_E2E_CONFIG"

LOG_PREFIX = "[TalosE2EConfig]"


class TalosE2EConfigError(RuntimeError):
    """Talos E2E 配置加载失败时抛出的错误。

    Error raised when Talos E2E config loading fails.
    """


@dataclass(frozen=True)
class TalosE2EConfig:
    """Talos E2E 编排默认参数。

    Talos E2E orchestration default parameters.
    当 TeamCity DSL 调用 teamcity_e2e_runner.py 时，
    这些参数提供合理的默认值，减少 DSL 行长度与维护成本。
    These parameters provide sensible defaults when TeamCity DSL invokes teamcity_e2e_runner.py,
    reducing DSL line length and maintenance cost.
    """

    client_version: str = "0.1"
    build_debug: str = "true"
    timeout_seconds: int = 5400
    poll_interval_seconds: int = 10
    download_timeout_seconds: int = 600
    unity_host: str = "127.0.0.1"
    unity_port: int = 10002
    # Android 专属默认值：模拟器类型、MuMu 自动启动开关、ADB connect 目标列表。
    # Android-specific defaults: emulator type, MuMu auto-start flag, ADB connect targets.
    emulator_type: str = "mumu"
    mumu_auto_start: str = "true"
    adb_connect_targets: str = "127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555"


def _log(message: str) -> None:
    """输出配置加载日志到标准输出。

    Print config loading log to stdout.
    """
    print(f"{LOG_PREFIX} {message}")


def _try_load_toml_file(path: Path) -> dict[str, Any] | None:
    """尝试加载 TOML 文件；文件不存在时返回 None，格式错误时抛异常。

    Try to load a TOML file; returns None if file does not exist, raises on format errors.
    """
    if not path.exists():
        return None

    try:
        import tomllib
    except ModuleNotFoundError:  # pragma: no cover - Python < 3.11 fallback
        try:
            import tomli as tomllib  # type: ignore[no-redef]
        except ModuleNotFoundError:  # pragma: no cover - TeamCity old Python fallback
            tomllib = None  # type: ignore[assignment]

    if tomllib is not None:
        with path.open("rb") as handle:
            return tomllib.load(handle)

    # 极端回退：使用 BuildTools 的 minimal TOML parser。
    # Extreme fallback: use BuildTools' minimal TOML parser.
    _log(f"tomllibNotAvailable, falling back to minimal TOML parser")
    from Common.buildtools_config import load_minimal_toml  # type: ignore[import-untyped]

    return load_minimal_toml(path.read_text(encoding="utf-8"))


def _get_config_section(config_data: dict[str, Any], section_name: str) -> dict[str, Any]:
    """从 TOML 数据中安全读取嵌套表。

    Safely read a dotted TOML table path from config data.
    """
    current: Any = config_data
    for part in section_name.split("."):
        if not isinstance(current, dict):
            return {}
        current = current.get(part, {})
    return current if isinstance(current, dict) else {}


def _coerce_optional_string(value: Any) -> str | None:
    """将配置值归一化为可选的去除空白字符串。

    Normalize a config value into an optional stripped string.
    """
    if value is None:
        return None
    normalized = str(value).strip()
    return normalized or None


def _coerce_optional_int(value: Any) -> int | None:
    """将配置值归一化为可选整数。

    Normalize a config value into an optional integer.
    """
    if value in (None, ""):
        return None
    return int(value)


def _merge_config_from_toml(
    base: TalosE2EConfig,
    config_data: dict[str, Any],
) -> TalosE2EConfig:
    """用 TOML 数据中的 [talos.e2e] 段覆盖基础配置的非空值。

    Override base config with non-empty values from the [talos.e2e] section in TOML data.
    """
    section = _get_config_section(config_data, "talos.e2e")

    overrides: dict[str, Any] = {}
    field_map: dict[str, type] = {
        "client_version": str,
        "build_debug": str,
        "timeout_seconds": int,
        "poll_interval_seconds": int,
        "download_timeout_seconds": int,
        "unity_host": str,
        "unity_port": int,
        "emulator_type": str,
        "mumu_auto_start": str,
        "adb_connect_targets": str,
    }

    for field_name, field_type in field_map.items():
        raw_value = section.get(field_name)
        if raw_value is None:
            continue
        if field_type is int:
            coerced = _coerce_optional_int(raw_value)
        else:
            coerced = _coerce_optional_string(raw_value)
        if coerced is not None:
            overrides[field_name] = coerced

    if not overrides:
        return base

    return TalosE2EConfig(
        client_version=overrides.get("client_version", base.client_version),
        build_debug=overrides.get("build_debug", base.build_debug),
        timeout_seconds=overrides.get("timeout_seconds", base.timeout_seconds),
        poll_interval_seconds=overrides.get("poll_interval_seconds", base.poll_interval_seconds),
        download_timeout_seconds=overrides.get("download_timeout_seconds", base.download_timeout_seconds),
        unity_host=overrides.get("unity_host", base.unity_host),
        unity_port=overrides.get("unity_port", base.unity_port),
        emulator_type=overrides.get("emulator_type", base.emulator_type),
        mumu_auto_start=overrides.get("mumu_auto_start", base.mumu_auto_start),
        adb_connect_targets=overrides.get("adb_connect_targets", base.adb_connect_targets),
    )


def resolve_talos_e2e_config_path() -> Path | None:
    """按 fallback 规则解析 E2E 配置文件路径，并输出日志。

    Resolve the E2E config file path using fallback rules, with logging.

    Fallback 顺序：
    Fallback order:
    1. 环境变量 TALOS_E2E_CONFIG
    2. DevOps/CI/talos_e2e_config.toml（CI 环境覆盖）
    3. 包内 Playwright~/tools/talos_e2e_config.toml（包内默认值）

    返回 None 表示没有找到任何配置文件。
    Returns None when no config file is found.
    """
    # 1. 环境变量指定的路径（必须存在，否则报错）。
    # 1. Path specified by env var (must exist, otherwise error).
    env_value = os.environ.get(ENV_VAR_CONFIG_PATH, "").strip()
    if env_value:
        env_path = Path(env_value).expanduser().resolve()
        if not env_path.exists():
            raise TalosE2EConfigError(
                f"Config file specified by {ENV_VAR_CONFIG_PATH} does not exist: {env_path}"
            )
        _log(f"configSource=env, path={env_path}")
        return env_path

    # 2. DevOps/CI 覆盖配置。
    # 2. DevOps/CI override config.
    if DEVOPS_CI_CONFIG_PATH.exists():
        _log(f"configSource=devops-ci, path={DEVOPS_CI_CONFIG_PATH}")
        return DEVOPS_CI_CONFIG_PATH

    # 3. 包内默认配置。
    # 3. Package-default config.
    if PACKAGE_DEFAULT_CONFIG_PATH.exists():
        _log(f"configSource=package-default, path={PACKAGE_DEFAULT_CONFIG_PATH}")
        return PACKAGE_DEFAULT_CONFIG_PATH

    _log("configSource=none, using hardcoded defaults")
    return None


def load_talos_e2e_config() -> TalosE2EConfig:
    """加载 Talos E2E 配置，按 fallback 规则合并，并输出关键参数日志。

    Load Talos E2E config, merge using fallback rules, and log key parameters.

    加载流程：
    Loading flow:
    1. 以硬编码默认值为基础。
       Start with hardcoded defaults.
    2. 如果存在包内默认配置文件，用其覆盖基础值。
       If package-default config exists, overlay on top of defaults.
    3. 如果存在 DevOps/CI 覆盖配置文件，用其再次覆盖。
       If DevOps/CI override config exists, overlay again.
    4. 如果环境变量指定了配置文件，用其做最终覆盖。
       If env var specifies a config file, overlay as the final override.
    5. 输出合并后的关键参数日志。
       Log merged key parameters.
    """
    config = TalosE2EConfig()
    _log("loadedDefaults=hardcoded")

    # 先加载包内默认配置（最低优先级的文件配置）。
    # Load package-default config first (lowest-priority file config).
    package_data = _try_load_toml_file(PACKAGE_DEFAULT_CONFIG_PATH)
    if package_data is not None:
        config = _merge_config_from_toml(config, package_data)
        _log(f"overlay=package-default, path={PACKAGE_DEFAULT_CONFIG_PATH}")

    # 再加载 DevOps/CI 覆盖配置（中等优先级）。
    # Then load DevOps/CI override config (medium priority).
    devops_data = _try_load_toml_file(DEVOPS_CI_CONFIG_PATH)
    if devops_data is not None:
        config = _merge_config_from_toml(config, devops_data)
        _log(f"overlay=devops-ci, path={DEVOPS_CI_CONFIG_PATH}")

    # 最后加载环境变量指定的配置（最高文件优先级）。
    # Finally load env-var-specified config (highest file priority).
    env_value = os.environ.get(ENV_VAR_CONFIG_PATH, "").strip()
    if env_value:
        env_path = Path(env_value).expanduser().resolve()
        if not env_path.exists():
            raise TalosE2EConfigError(
                f"Config file specified by {ENV_VAR_CONFIG_PATH} does not exist: {env_path}"
            )
        env_data = _try_load_toml_file(env_path)
        if env_data is not None:
            config = _merge_config_from_toml(config, env_data)
            _log(f"overlay=env, path={env_path}")

    # 输出合并后的关键参数，便于排查配置来源问题。
    # Log merged key parameters for config-source troubleshooting.
    _log(
        f"finalConfig: "
        f"client_version={config.client_version}, "
        f"build_debug={config.build_debug}, "
        f"timeout_seconds={config.timeout_seconds}, "
        f"poll_interval_seconds={config.poll_interval_seconds}, "
        f"download_timeout_seconds={config.download_timeout_seconds}, "
        f"unity_host={config.unity_host}, "
        f"unity_port={config.unity_port}, "
        f"emulator_type={config.emulator_type}, "
        f"mumu_auto_start={config.mumu_auto_start}, "
        f"adb_connect_targets={config.adb_connect_targets}"
    )

    return config


__all__ = [
    "DEVOPS_CI_CONFIG_PATH",
    "ENV_VAR_CONFIG_PATH",
    "LOG_PREFIX",
    "PACKAGE_DEFAULT_CONFIG_PATH",
    "REPO_ROOT",
    "TOOL_DIR",
    "TalosE2EConfig",
    "TalosE2EConfigError",
    "load_talos_e2e_config",
    "resolve_talos_e2e_config_path",
]
