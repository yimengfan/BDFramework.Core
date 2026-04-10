"""BuildTools 外部配置源码规范的共享 enforcement helper。

这个模块的职责：
1. 统一维护 workspace hook 和 pytest 共用的确定性规则。
2. 防止外部配置解析再次散落到 `Common/buildtools_config.py` 之外的 BuildTools 源文件中。
3. 在扫描内存中的文本或磁盘上的源码文件时，返回第一个可读的违规原因。

为什么要集中到这里：
1. workspace hook 需要在真正写入前就拦截违规改动。
2. pytest 需要在仓库回归时复用同一套规则，避免只在 hook 路径上生效。
3. 把规则集中在这里，可以避免 hook 和测试各自维护一份正则，后续出现漂移。
"""

from __future__ import annotations

import re
from pathlib import Path, PurePosixPath


# 只对真正的 BuildTools 源文件做 guard。
# tests 目录刻意排除在外，这样测试文件可以放构造出来的违规样例，而不会被仓库扫描自己打断。
SOURCE_SUFFIXES = {".py", ".sh", ".shell"}

# 共享配置入口本身就是唯一允许建模或解析 BuildTools 外部配置的实现位置。
# 其他 BuildTools 源文件都只能消费它暴露出来的接口或 typed model。
ALLOWED_CONFIG_SOURCE_FILES = {
    "DevOps/CI/BuildTools/Common/buildtools_config.py",
}

# 每条规则都是 `(regex, 可读原因)` 的形式。
# 扫描器只返回第一个命中的原因，这样 hook 拒绝信息和 pytest 失败信息都能保持简洁。
FORBIDDEN_RULES = (
    (
        # 如果叶子源文件重新引入 TOML 解析库，通常就是配置解析开始再次散落的直接信号。
        re.compile(r"\bimport\s+tomllib\b|\bimport\s+tomli\b|\b(?:tomllib|tomli|toml)\.(?:load|loads)\b"),
        "BuildTools external config must not add per-file TOML parsers; use Common/buildtools_config.py.",
    ),
    (
        # 历史上分散实现常见的特征之一是局部解析 helper，这里提前拦住，避免再次扩散。
        re.compile(r"\b(?:def\s+load_toml\s*\(|def\s+parse_simple_value\s*\(|load_toml\s*\(|parse_simple_value\s*\()"),
        "BuildTools source files must not define ad hoc config parsing helpers; use Common/buildtools_config.py.",
    ),
    (
        # 即便叶子文件没有自己解析 TOML，也不允许直接读这些共享 section；
        # 正确路径仍然应该经过共享 typed model，而不是重新散读原始配置表。
        re.compile(
            r"(?:get\(|\[)\s*[\"'](?:artifact_file_server|ci_server|ios_xcode|tests\.remote_artifact)[\"']"
        ),
        "BuildTools config sections must be accessed through Common/buildtools_config.py, not read directly in source files.",
    ),
)


def normalize_buildtools_source_path(path_text: str, *, workspace_root: str | None = None) -> str:
    """把绝对或相对路径标准化成 workspace 相对的 POSIX 形式。"""
    # 统一转换成 workspace 相对 POSIX 路径，可以让 hook、pytest 和不同宿主平台上的诊断信息保持稳定。
    normalized = path_text.replace("\\", "/").strip()
    if workspace_root:
        normalized_root = workspace_root.replace("\\", "/").rstrip("/")
        if normalized.startswith(normalized_root + "/"):
            normalized = normalized[len(normalized_root) + 1 :]
    return str(PurePosixPath(normalized))


def is_guarded_buildtools_source(path_text: str, *, workspace_root: str | None = None) -> bool:
    """判断一个路径是否属于需要执行配置 guard 的 BuildTools 源文件。"""
    relative_path = normalize_buildtools_source_path(path_text, workspace_root=workspace_root)
    suffix = PurePosixPath(relative_path).suffix

    # Phase 1: guard 只作用在 BuildTools 源码范围内，不扫描整个仓库。
    if not relative_path.startswith("DevOps/CI/BuildTools/"):
        return False

    # Phase 2: tests 目录排除在外，允许测试代码保留故意构造的违规样例。
    if relative_path.startswith("DevOps/CI/BuildTools/tests/"):
        return False

    # Phase 3: 只有可执行源文件才和这条规范相关。
    if suffix not in SOURCE_SUFFIXES:
        return False

    # Phase 4: 共享配置模块自身就是允许的实现位置，因此不拦截它。
    return relative_path not in ALLOWED_CONFIG_SOURCE_FILES


def scan_buildtools_source_content(
    path_text: str,
    content: str,
    *,
    workspace_root: str | None = None,
) -> str | None:
    """扫描给定的 BuildTools 源码文本，并返回第一个命中的规范违规信息。"""
    relative_path = normalize_buildtools_source_path(path_text, workspace_root=workspace_root)
    if not is_guarded_buildtools_source(relative_path, workspace_root=workspace_root):
        return None

    # 按行扫描可以把错误定位到一个可操作的位置，避免返回一大段难以处理的 diff 或 regex 结果。
    for line_number, line in enumerate(content.splitlines(), start=1):
        if not line.strip():
            continue
        for pattern, reason in FORBIDDEN_RULES:
            if pattern.search(line):
                return (
                    f"{reason} Offending file: {relative_path}:{line_number}. "
                    f"Line: {line.strip()}"
                )
    return None


def scan_buildtools_source_file(path: Path, *, workspace_root: str | None = None) -> str | None:
    """读取并校验单个 BuildTools 源码文件是否违反共享配置 guard。"""
    # 这个薄包装的意义，是让 pytest 在扫描真实仓库文件时不用在调用侧重复写文件读取逻辑。
    return scan_buildtools_source_content(
        str(path),
        path.read_text(encoding="utf-8"),
        workspace_root=workspace_root,
    )


__all__ = [
    "ALLOWED_CONFIG_SOURCE_FILES",
    "FORBIDDEN_RULES",
    "SOURCE_SUFFIXES",
    "is_guarded_buildtools_source",
    "normalize_buildtools_source_path",
    "scan_buildtools_source_content",
    "scan_buildtools_source_file",
]