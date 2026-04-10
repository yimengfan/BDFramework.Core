"""BuildTools 外部配置规范的 Workspace PreToolUse hook。

为什么需要这个 hook：
1. `DevOps/CI/BuildTools/buildtools.toml` 是业务无关外部配置的唯一共享来源。
2. `DevOps/CI/BuildTools/Common/buildtools_config.py` 是唯一允许解析和建模这份配置的入口。
3. 这个 hook 用来阻止后续改动再次把 TOML 解析散落到其他 BuildTools 源文件里。

hook 的工作方式：
1. VS Code 会把当前工具调用的 JSON 通过 stdin 传给 hook。
2. hook 只关心可能引入新源码的写操作：`apply_patch` 和 `create_file`。
3. 对 `apply_patch`，它只检查新增行，避免旧代码或上下文行误伤当前改动。
4. 对 `create_file`，它会检查整个新文件内容。
5. 一旦发现违规模式，hook 会为 `PreToolUse` 返回 `deny`；否则返回 `continue: true`。
"""

from __future__ import annotations

import json
import os
import sys
from typing import Any

# hook 运行时当前目录就是 workspace 根目录。
# 这里推导出 BuildTools 根目录，是为了直接复用 pytest 也在使用的 guard 规则，
# 避免在这个脚本里再维护第二份正则或策略实现。
REPO_ROOT = os.getcwd()
BUILD_TOOLS_ROOT = os.path.join(REPO_ROOT, "DevOps", "CI", "BuildTools")
if BUILD_TOOLS_ROOT not in sys.path:
    sys.path.insert(0, BUILD_TOOLS_ROOT)

from Common.buildtools_config_guard import scan_buildtools_source_content


def emit_allow() -> int:
    """在当前工具调用不命中规则时返回放行结果。"""
    json.dump({"continue": True}, sys.stdout)
    sys.stdout.write("\n")
    return 0


def emit_deny(reason: str) -> int:
    """在发现违规编辑时，为 PreToolUse 返回拒绝结果。"""
    # PreToolUse 需要一个结构化的权限判定结果。
    # 在这里返回 `deny`，可以在文件真正被修改之前就阻止这次工具调用。
    payload = {
        "systemMessage": reason,
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": reason,
        },
    }
    json.dump(payload, sys.stdout)
    sys.stdout.write("\n")
    return 0


def extract_tool_name(payload: Any) -> str:
    """尽量从 hook 的 stdin 载荷里提取当前工具名。"""
    # 不同工具调用带来的 hook payload 形状可能略有差异，
    # 所以这里会探测几个常见字段，而不是假设只有一种固定 schema。
    candidates = []
    if isinstance(payload, dict):
        candidates.extend(
            [
                payload.get("toolName"),
                payload.get("tool_name"),
                payload.get("name"),
            ]
        )
        hook_input = payload.get("hookSpecificInput")
        if isinstance(hook_input, dict):
            candidates.extend([hook_input.get("toolName"), hook_input.get("tool_name")])
        tool_info = payload.get("tool")
        if isinstance(tool_info, dict):
            candidates.extend([tool_info.get("name"), tool_info.get("toolName")])

    for candidate in candidates:
        if isinstance(candidate, str) and candidate.strip():
            return candidate.strip()
    return ""


def extract_tool_input(payload: Any) -> dict[str, Any]:
    """尽量从 hook 的 stdin 载荷里提取工具参数对象。"""
    if not isinstance(payload, dict):
        return {}

    # 不同工具包装层可能把参数放在不同字段下。
    # 这里先做一次归一化，后面的扫描逻辑就可以保持简单。
    candidates: list[Any] = [
        payload.get("toolInput"),
        payload.get("tool_input"),
        payload.get("arguments"),
        payload.get("args"),
    ]
    hook_input = payload.get("hookSpecificInput")
    if isinstance(hook_input, dict):
        candidates.extend(
            [
                hook_input.get("toolInput"),
                hook_input.get("tool_input"),
                hook_input.get("arguments"),
                hook_input.get("args"),
            ]
        )

    for candidate in candidates:
        if isinstance(candidate, dict):
            return candidate
    return {}


def scan_content(relative_path: str, content: str) -> str | None:
    """扫描目标 BuildTools 源文件内容，并返回第一个违规原因。"""
    # 真正的策略实现都放在 `Common/buildtools_config_guard.py` 里；
    # 这个包装层只负责绑定当前 workspace 根目录，让返回路径更易读。
    return scan_buildtools_source_content(relative_path, content, workspace_root=REPO_ROOT)


def scan_apply_patch(patch_text: str) -> str | None:
    """检查 apply_patch 载荷，并拒绝违规的 BuildTools 源码编辑。"""
    current_path: str | None = None
    added_lines: list[str] = []

    def flush_pending() -> str | None:
        """校验当前文件块里已经缓冲的新增行。"""
        if current_path is None:
            return None
        added_content = "\n".join(added_lines)
        if not added_content:
            return None
        return scan_content(current_path, added_content)

    # 一个 patch 可能同时改多个文件。
    # 这里对每个文件块只缓存 `+` 新增行，因为 guard 关心的是新引入的代码，
    # 而不是 patch 里展示出来的旧上下文。
    for raw_line in patch_text.splitlines():
        if raw_line.startswith("*** Update File: ") or raw_line.startswith("*** Add File: "):
            violation = flush_pending()
            if violation is not None:
                return violation
            current_path = raw_line.split(":", 1)[1].strip()
            added_lines = []
            continue

        if raw_line.startswith("*** Delete File: "):
            violation = flush_pending()
            if violation is not None:
                return violation
            current_path = None
            added_lines = []
            continue

        if current_path is None:
            continue
        if raw_line.startswith("+") and not raw_line.startswith("+++"):
            added_lines.append(raw_line[1:])

    return flush_pending()


def scan_create_file(tool_input: dict[str, Any]) -> str | None:
    """检查 create_file 载荷，并拒绝违规的 BuildTools 源码编辑。"""
    file_path = tool_input.get("filePath")
    content = tool_input.get("content")
    if not isinstance(file_path, str) or not isinstance(content, str):
        return None
    # 新文件没有旧上下文可参考，因此必须对整份内容做完整校验。
    return scan_content(file_path, content)


def main() -> int:
    """评估当前工具调用，并拒绝违反 BuildTools 配置规范的编辑。"""
    try:
        # Hook 命令通过 stdin 接收 JSON。
        # 如果 stdin 缺失或格式损坏，这里选择 fail-open，避免因为脆弱的解析器报错而误阻塞无关工作。
        payload = json.load(sys.stdin)
    except Exception:
        return emit_allow()

    # Phase 1: 先把 hook 载荷归一化成“工具名 + 参数对象”。
    tool_name = extract_tool_name(payload)
    tool_input = extract_tool_input(payload)

    # Phase 2: 只拦截可能引入新源码的写路径。
    if "apply_patch" in tool_name:
        patch_text = tool_input.get("input")
        if isinstance(patch_text, str):
            violation = scan_apply_patch(patch_text)
            if violation is not None:
                return emit_deny(violation)
        return emit_allow()

    if "create_file" in tool_name:
        violation = scan_create_file(tool_input)
        if violation is not None:
            return emit_deny(violation)
        return emit_allow()

    # Phase 3: 其他非写工具与这条规范无关，直接放行。
    return emit_allow()


if __name__ == "__main__":
    raise SystemExit(main())