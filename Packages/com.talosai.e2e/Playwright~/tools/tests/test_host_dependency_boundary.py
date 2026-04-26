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
BD_LAUNCHER = REPO_ROOT / "Packages" / "com.popo.bdframework" / "Runtime.AOT" / "AOTLauncher" / "BDLauncher.cs"
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


def test_bdframework_launcher_owns_debug_talos_bridge() -> None:
    """验证 BDLauncher 持有 Debug 模式 Talos 启动桥接，而 ScriptLoder 不再承担 app 启动入口。
    Verify that BDLauncher owns the Debug-mode Talos startup bridge while ScriptLoder no longer carries the app startup entry.
    """

    script_loader_content = BD_SCRIPT_LODER.read_text(encoding="utf-8")
    launcher_content = BD_LAUNCHER.read_text(encoding="utf-8")

    assert "TryStartE2EFramework" not in script_loader_content
    assert "Talos.E2E.E2EAutoInit.CheckAndLaunch();" not in script_loader_content
    assert '[Conditional("DEBUG")]' in launcher_content
    assert "TryLaunchTalosE2EInDebugBuild()" in launcher_content
    assert 'GetMethod("CheckAndLaunch", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)' in launcher_content


def test_bdframework_script_loader_prewarms_bapplication_on_main_thread() -> None:
    """验证 ScriptLoder 会在主线程预热 BApplication，避免后台线程抢先触发静态构造。
    Verify that ScriptLoder prewarms BApplication on the main thread so a background thread cannot win the static-constructor race.
    """

    script_loader_content = BD_SCRIPT_LODER.read_text(encoding="utf-8")

    assert "Warm up BApplication on the main thread" in script_loader_content
    assert "BDFramework.Core.Tools.BApplication.persistentDataPath" in script_loader_content


def test_bdframework_script_loader_does_not_compose_host_suite_execution() -> None:
    """验证 ScriptLoder 只桥接 Talos 框架入口，而不直接编排宿主 suite。
    Verify that ScriptLoder only bridges the Talos framework entry and does not directly compose host suites.
    """

    script_loader_content = BD_SCRIPT_LODER.read_text(encoding="utf-8")

    assert "TryPrimeTalosHostE2ESuites" not in script_loader_content
    assert '"BDFramework.HostE2E.LaunchFlowHostTests"' not in script_loader_content
    assert '"BDFramework.HostE2E.BaseFlowHostRuntimeTests"' not in script_loader_content
    assert 'Type.GetType($"{typeName}, BDFramework.HostE2E", false)' not in script_loader_content
    assert "BDFramework.Core.Tools.BApplication.persistentDataPath" in script_loader_content


def test_talos_e2e_runner_keeps_public_type_fallback_for_player_discovery() -> None:
    """验证 E2ETestRunner 会在程序集部分类型加载失败时继续补扫公共类型。
    Verify that E2ETestRunner continues with a public-type fallback when an assembly only partially loads its types.
    """

    runner_content = E2E_TEST_RUNNER.read_text(encoding="utf-8")

    assert "ScanCandidateTypes(assembly.GetTypes(), scannedTypeNames);" in runner_content
    assert "ScanCandidateTypes(ex.Types, scannedTypeNames);" in runner_content
    assert "ScanCandidateTypes(assembly.ExportedTypes, scannedTypeNames);" in runner_content
    assert "程序集 {assembly.GetName().Name} 类型加载异常" in runner_content


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

    assert "Talos E2E 是能力包，不是宿主工作流包。" in rules_content
    assert "不要在该包内编排宿主启动、框架初始化、资源/数据库准备、场景顺序、executeMethod wrapper 或兜底恢复流程。" in rules_content
    assert "不要在这里加入业务方专属测试、配置、启动配方或硬编码逻辑。" in rules_content

    assert "本文件只作为 `Packages/com.talosai.e2e/**` 的 Copilot applyTo 触发入口。" in instructions_content
    assert "Packages/com.talosai.e2e/AGENTS.md" in instructions_content
    assert "不要在本文件重复 Talos 模块规则。" in instructions_content
