"""Talos E2E 宿主依赖边界测试。
Talos E2E host-dependency boundary tests.

覆盖范围：
Coverage:
1. Talos E2E 包不得再直接引用 BDFramework 源码命名空间或 asmdef。
1. The Talos E2E package must not directly reference BDFramework source namespaces or asmdefs.
2. 宿主相关初始化必须回到 BDFramework 自己的 executeMethod 入口，而不是通过 E2E 回调注册。
2. Host-owned initialization must stay on BDFramework-owned executeMethod entries instead of E2E callback registration.
3. 包级规范必须明确禁止 Talos E2E 替宿主或业务方编排流程代码。
3. Package rules must explicitly forbid Talos E2E from composing workflow code for host or business packages.
"""

from __future__ import annotations

from pathlib import Path
import json


REPO_ROOT = Path(__file__).resolve().parents[5]
E2E_EDITOR_TOOLS = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Editor" / "E2EEditorTools.cs"
E2E_DEBUG_BUILD_MARKER = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "TestRunner" / "DebugBuildMarker.cs"
E2E_AUTO_INIT = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "TestRunner" / "E2EAutoInit.cs"
E2E_TEST_RUNNER = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "TestRunner" / "E2ETestRunner.cs"
E2E_EDITOR_ASMDEF = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Editor" / "Talos.E2E.Editor.asmdef"
E2E_RUNTIME_ASMDEF = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Runtime" / "Talos.E2E.Runtime.asmdef"
BD_EDITOR_BRIDGE = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Editor" / "EditorEnvironment" / "TalosE2EBatchBridge.cs"
BD_SCRIPT_LODER = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Runtime" / "Script" / "ScriptLoder.cs"
EDITOR_PLAYER_SCRIPT = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tools" / "test-editorplayer.sh"
BATCHMODE_SCRIPT = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "tools" / "test-batchmode.sh"
PLAYWRIGHT_CONFIG = REPO_ROOT / "Packages" / "com.talosai.e2e" / "Playwright~" / "playwright.config.ts"
E2E_PACKAGE_RULES = REPO_ROOT / "Packages" / "com.talosai.e2e" / "AGENTS.md"
E2E_PACKAGE_INSTRUCTIONS = REPO_ROOT / ".github" / "instructions" / "e2e.instructions.md"

BD_CORE_ASMDEF_GUID = "GUID:90a5d688c87639542bc27aac9d4173c1"
BD_EDITOR_ASMDEF_GUID = "GUID:722bc14663f336f4a91c0b369e2a4f15"


def test_talos_e2e_source_files_drop_direct_bdframework_dependencies() -> None:
    """验证 Talos E2E 关键源码文件不再直接引用 BDFramework。
    Verify that key Talos E2E source files no longer directly reference BDFramework.
    """

    for path in [E2E_EDITOR_TOOLS, E2E_DEBUG_BUILD_MARKER, E2E_AUTO_INIT, E2E_TEST_RUNNER]:
        content = path.read_text(encoding="utf-8")
        assert "using BDFramework" not in content
        assert "BDFramework." not in content
        assert "HostLaunchHandler" not in content
        assert "AdditionalMarkerDirectoriesProvider" not in content


def test_talos_e2e_asmdefs_drop_bdframework_references() -> None:
    """验证 Talos E2E 程序集定义不再反向引用 BDFramework 程序集。
    Verify that Talos E2E asmdefs no longer reference BDFramework assemblies in reverse.
    """

    for path in [E2E_EDITOR_ASMDEF, E2E_RUNTIME_ASMDEF]:
        data = json.loads(path.read_text(encoding="utf-8"))
        references = data.get("references", [])
        assert BD_CORE_ASMDEF_GUID not in references
        assert BD_EDITOR_ASMDEF_GUID not in references


def test_bdframework_owns_talos_e2e_execute_method_entries() -> None:
    """验证宿主相关集成已经回到 BDFramework 自己的 executeMethod 入口。
    Verify that host-owned integration has moved back to BDFramework-owned executeMethod entries.
    """

    bridge_content = BD_EDITOR_BRIDGE.read_text(encoding="utf-8")
    assert "LaunchTalosE2EEditorOnly" in bridge_content
    assert "RunTalosE2EAndExport" in bridge_content
    assert "E2EEditorTools.LaunchE2EEditorOnly" in bridge_content
    assert "E2EEditorTools.RunE2EAndExport" in bridge_content

    script_loader_content = BD_SCRIPT_LODER.read_text(encoding="utf-8")
    assert "TryRegisterE2EDebugMarkerDirectoryProvider" not in script_loader_content
    assert "AdditionalMarkerDirectoriesProvider" not in script_loader_content


def test_playwright_scripts_call_bdframework_owned_execute_methods() -> None:
    """验证本地 Playwright 启动脚本已经切到 BDFramework 自己的 executeMethod 入口。
    Verify that local Playwright launcher scripts now call BDFramework-owned executeMethod entries.
    """

    editor_player_content = EDITOR_PLAYER_SCRIPT.read_text(encoding="utf-8")
    batchmode_content = BATCHMODE_SCRIPT.read_text(encoding="utf-8")

    assert "BDFramework.Editor.Environment.TalosE2EBatchBridge.LaunchTalosE2EEditorOnly" in editor_player_content
    assert "BDFramework.Editor.Environment.TalosE2EBatchBridge.LaunchTalosE2EEditorOnly" in batchmode_content
    assert "BDFramework.Editor.Environment.TalosE2EBatchBridge.RunTalosE2EAndExport" in batchmode_content
    assert "Talos.E2E.Editor.E2EEditorTools.LaunchE2EEditorOnly" not in editor_player_content
    assert "Talos.E2E.Editor.E2EEditorTools.RunE2EAndExport" not in batchmode_content


def test_unityplayer_project_is_limited_to_editorplayer_specs() -> None:
    """验证 unityplayer 项目只承载 EditorPlayer 专项用例，不再混跑跨平台运行时套件。
    Verify that the unityplayer project carries only EditorPlayer specialty cases rather than reusable runtime suites.
    """

    config_content = PLAYWRIGHT_CONFIG.read_text(encoding="utf-8")

    assert "const editorPlayerTestMatch = /-EditorPlayer-e2e\\.spec\\.ts$/;" in config_content
    assert "name: 'unityplayer'" in config_content
    assert "testMatch: editorPlayerTestMatch" in config_content
    assert "testMatch: [crossPlatformTestMatch, /-EditorPlayer-e2e\\.spec\\.ts$/]" not in config_content


def test_policy_docs_forbid_host_and_business_workflow_composition_inside_talos() -> None:
    """验证规范文档明确禁止 Talos E2E 承接宿主或业务流程编排代码。
    Verify that policy docs explicitly forbid Talos E2E from taking over host or business workflow composition.
    """

    rules_content = E2E_PACKAGE_RULES.read_text(encoding="utf-8")
    instructions_content = E2E_PACKAGE_INSTRUCTIONS.read_text(encoding="utf-8")

    assert "Talos E2E is a capability package, not a host workflow package." in rules_content
    assert "Do not compose host-owned startup, initialization, scene sequencing, executeMethod wrappers, or fallback recovery workflows inside this package." in rules_content
    assert "Do not add business-party multi-step scenario scripts, launch recipes, or one-off recovery logic under `Playwright~/`, `Editor/`, or `Runtime/` just to make a single project pass." in rules_content

    assert "Talos E2E is a capability package, not a host workflow package." in instructions_content
    assert "If a scenario depends on project or business scenes, config, assets, manager initialization, or launch order, define that flow in the host or business package" in instructions_content
    assert "Do not add business-party scenario scripts, one-project launch recipes, or host-only acceptance choreography under `Playwright~/` just to help another package pass." in instructions_content