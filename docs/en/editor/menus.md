# Menus & Tools Index

An index of all `[MenuItem]` paths, for quickly locating a feature's entry point.

## `BDFrameWork工具箱/` — framework main menu

### Bootstrap & settings

| Menu path | Order | Handler class |
|---------|------|--------|
| `BDFrameWork工具箱/框架引导 <BDLauncher.FrameworkVersion>` | 0 | `EditorWindow_BDFrameworkStart` |
| `BDFrameWork工具箱/框架设置` | 1 | `EditorWindow_BDFrameworkConfig` |

### Build pipeline

| Menu path | Order | Handler class |
|---------|------|--------|
| `BDFrameWork工具箱/Odin BuildPipeline` | — | `EditorMenuItem.Open` → `EditorWindow_BuildPipeline` |
| `BDFrameWork工具箱/1.DLL打包` | 52 | `EditorMenuItem.OpenDLL` → `EditorWindow_PublishAssets` |
| `BDFrameWork工具箱/2.AssetBundle打包` | 53 | `EditorMenuItem.OpenAB` → `EditorWindow_PublishAssets` |
| `BDFrameWork工具箱/5.构建包体` | 58 | `EditorMenuItem.NULL` → `EditorWindow_BuildPipeline` |
| `BDFrameWork工具箱/4.网络协议/Protobuf->生成Class` | 57 | `Protobuf2ClassTools` |
| `BDFrameWork工具箱/测试` | — | `BuildTools_AssetBundleV2.Test` |

### Tables

| Menu path | Order | Handler class |
|---------|------|--------|
| `BDFrameWork工具箱/3.表格/表格预览` | 54 | `EditorMenuItem.OpenSQL` → `EditorWindow_Table` |
| `BDFrameWork工具箱/3.表格/表格->生成SQLite` | 55 | `BuildTools_Excel2SQLite.ExecuteGenSqlite` |
| `BDFrameWork工具箱/3.表格/表格->生成SQLite[Server]` | 56 | `BuildTools_Excel2SQLite.ExecuteJsonToSqlite` |
| `BDFrameWork工具箱/3.表格/表格->生成Class[程序目录]` | 54 | `Excel2CodeTools` |

### Publish / Hotfix / DevOps

| Menu path | Order | Handler class |
|---------|------|--------|
| `BDFrameWork工具箱/PublishPipeline/1.发布资源` | 101 | `EditorWindow_PublishAssets.Open` |
| `BDFrameWork工具箱/HotfixPipeline/1.配置热更文件` | 111 | `EditorWindow_HotfixFileSetting` |
| `BDFrameWork工具箱/DevOps/CI - API预览` | 121 | `EditorWindow_CICD.Open` |

### Hotfix assembly management

| Menu path | Handler class |
|---------|--------|
| `BDFrameWork工具箱/Hotfix/查看热更程序集配置` | `HotfixTestAssemblyInjector.PrintHotfixAssemblyConfig` |
| `BDFrameWork工具箱/Hotfix/注入测试程序集 (Debug)` | `HotfixTestAssemblyInjector.ManualInjectTestAssemblies` |
| `BDFrameWork工具箱/Hotfix/移除测试程序集 (Release)` | `HotfixTestAssemblyInjector.ManualRemoveTestAssemblies` |
| `BDFrameWork工具箱/Hotfix/自动配置测试程序集` | `HotfixTestAssemblyInjector.AutoConfigureTestAssemblies` |

### HybridCLR test entry points

| Menu path | Handler class |
|---------|--------|
| `BDFrameWork工具箱/Test/HyCLREditorTools.BuildHotfixDLL` | `HyCLREditorTools.BuildHotfixDLL_Test` |
| `BDFrameWork工具箱/Test/HyCLREditorTools.PreBuild` | `HyCLREditorTools.PreBuild_Test` |

!!! note "Commented-out menus (inactive)"
    `Editor Coroutine Example`, `---Build Pipeline----`, `---Publish Pipeline----`, `---Test Pipeline----`, `3.表格/表格->生成Class[策划目录]`, `Test/TestGitCmd`, `xxx/xxxx`, `TestPipeline/Sqlite/json序列化`.

## `Assets/` context menu

| Menu path | Handler class |
|---------|--------|
| `Assets/【BD工具箱】ExcelTools/Excel导入到数据库` | `BuildTools_Excel2SQLite.MenuItem_Excel2Sqlite` |
| `Assets/【BD工具箱】ExcelTools/表格转换工具` | `EditorWindow_ExcelExchange` |
| `Assets/【BD工具箱】ExcelTools/Excel导出脚本[程序目录]` | `Excel2CodeTools` |
| `Assets/【BD工具箱】ExcelTools/Excel生成脚本[程序目录]` | `Excel2CodeTools` |
| `Assets/Find Asset Dependencise` | `EditorMenuItem.ShowAssetDependencies` |

## `GameObject/` menu

| Menu path | Order | Handler class |
|---------|------|--------|
| `GameObject/UI/Text` | — | `OverrideUIComponent` (defaults to `raycastTarget = false`) |
| `GameObject/UI/Image` | — | Same as above |
| `GameObject/UI/Raw Image` | — | Same as above |
| `GameObject/UI工作流/1.创建UIPrefab` | 1 | `Editor/UI/Workflow/MenuItems.cs` |
| `GameObject/UI工作流/2.创建SubWindow节点` | 2 | **Empty implementation** |

## Test menus

| Menu path | Priority | Handler class |
|---------|-------|--------|
| `BDFramework/测试/SQLite 集成测试` | 100 | `Runtime.Test/Editor/Sqlite/SqliteIntegrationTestRunner.cs` |
| `BDFramework/测试/SQLite优化性能基准 ▶` | — | `EditorPipeline/SqliteBenchmark/SqliteOptimizationBenchmark.cs` |
| `BDFrameWork工具箱/TestPipeline/打开TestRunner` | — | `Assets/Code/BDFramework.UnitTest/Editor/TestRunner/TestRunnerEditor.cs` |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-DLL` | — | Same as above |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-ILRuntime` | — | Same as above (legacy naming) |
| `BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime(Rebuild DLL)` | — | Same as above |

## Business-side menus

| Menu path | Handler class |
|---------|--------|
| `BDFrame开发辅助/导出主工程资源` | `Assets/Code/Game/Editor/ExpoterMainProjectAsset.cs` |

## Other

| Menu path | Handler class |
|---------|--------|
| `Tools/GUIStyle 样例窗口` | `Editor/EditorWindows/Menuitems/EditorBuiltinStyle.cs` |

## Third-party menus (not first-party, for reference)

| Group | Content |
|------|------|
| `HybridCLR/*` | `Generate/*`, `CompileDll/*`, `Installer...`, `Settings...`, `Documents/*`, `ObfuzExtension/*` |
| `Obfuz/*` | Obfuscation-related |
| `Window/NuGet/*`, `Assets/NuGet/*` | NuGet package management |
| `Window/UniTask Tracker` | UniTask tracking |
| `Window/MessagePack/CodeGenerator` | MessagePack code generation |
| `Reporter/Create` | Logs Viewer |
| `Talos/E2E Test/创建 DEBUG 标记` / `移除 DEBUG 标记` / `检查 DEBUG 状态` | Talos E2E |

## Related pages

- [Editor Core Classes](core-classes.md)
- [Build & Release](../pipeline/index.md)
- [Testing Overview](../testing/overview.md)
