# 菜单与工具索引

全部 `[MenuItem]` 路径索引，便于快速定位功能入口。

## `BDFrameWork工具箱/` —— 框架主菜单

### 引导与设置

| 菜单路径 | 排序 | 处理类 |
|---------|------|--------|
| `BDFrameWork工具箱/框架引导 <BDLauncher.FrameworkVersion>` | 0 | `EditorWindow_BDFrameworkStart` |
| `BDFrameWork工具箱/框架设置` | 1 | `EditorWindow_BDFrameworkConfig` |

### 构建管线

| 菜单路径 | 排序 | 处理类 |
|---------|------|--------|
| `BDFrameWork工具箱/Odin BuildPipeline` | — | `EditorMenuItem.Open` → `EditorWindow_BuildPipeline` |
| `BDFrameWork工具箱/1.DLL打包` | 52 | `EditorMenuItem.OpenDLL` → `EditorWindow_PublishAssets` |
| `BDFrameWork工具箱/2.AssetBundle打包` | 53 | `EditorMenuItem.OpenAB` → `EditorWindow_PublishAssets` |
| `BDFrameWork工具箱/5.构建包体` | 58 | `EditorMenuItem.NULL` → `EditorWindow_BuildPipeline` |
| `BDFrameWork工具箱/4.网络协议/Protobuf->生成Class` | 57 | `Protobuf2ClassTools` |
| `BDFrameWork工具箱/测试` | — | `BuildTools_AssetBundleV2.Test` |

### 表格

| 菜单路径 | 排序 | 处理类 |
|---------|------|--------|
| `BDFrameWork工具箱/3.表格/表格预览` | 54 | `EditorMenuItem.OpenSQL` → `EditorWindow_Table` |
| `BDFrameWork工具箱/3.表格/表格->生成SQLite` | 55 | `BuildTools_Excel2SQLite.ExecuteGenSqlite` |
| `BDFrameWork工具箱/3.表格/表格->生成SQLite[Server]` | 56 | `BuildTools_Excel2SQLite.ExecuteJsonToSqlite` |
| `BDFrameWork工具箱/3.表格/表格->生成Class[程序目录]` | 54 | `Excel2CodeTools` |

### 发布 / 热更 / DevOps

| 菜单路径 | 排序 | 处理类 |
|---------|------|--------|
| `BDFrameWork工具箱/PublishPipeline/1.发布资源` | 101 | `EditorWindow_PublishAssets.Open` |
| `BDFrameWork工具箱/HotfixPipeline/1.配置热更文件` | 111 | `EditorWindow_HotfixFileSetting` |
| `BDFrameWork工具箱/DevOps/CI - API预览` | 121 | `EditorWindow_CICD.Open` |

### Hotfix 程序集管理

| 菜单路径 | 处理类 |
|---------|--------|
| `BDFrameWork工具箱/Hotfix/查看热更程序集配置` | `HotfixTestAssemblyInjector.PrintHotfixAssemblyConfig` |
| `BDFrameWork工具箱/Hotfix/注入测试程序集 (Debug)` | `HotfixTestAssemblyInjector.ManualInjectTestAssemblies` |
| `BDFrameWork工具箱/Hotfix/移除测试程序集 (Release)` | `HotfixTestAssemblyInjector.ManualRemoveTestAssemblies` |
| `BDFrameWork工具箱/Hotfix/自动配置测试程序集` | `HotfixTestAssemblyInjector.AutoConfigureTestAssemblies` |

### HybridCLR 测试入口

| 菜单路径 | 处理类 |
|---------|--------|
| `BDFrameWork工具箱/Test/HyCLREditorTools.BuildHotfixDLL` | `HyCLREditorTools.BuildHotfixDLL_Test` |
| `BDFrameWork工具箱/Test/HyCLREditorTools.PreBuild` | `HyCLREditorTools.PreBuild_Test` |

!!! note "被注释掉（不生效）的菜单"
    `Editor Coroutine Example`、`---Build Pipeline----`、`---Publish Pipeline----`、`---Test Pipeline----`、`3.表格/表格->生成Class[策划目录]`、`Test/TestGitCmd`、`xxx/xxxx`、`TestPipeline/Sqlite/json序列化`。

## `Assets/` 右键菜单

| 菜单路径 | 处理类 |
|---------|--------|
| `Assets/【BD工具箱】ExcelTools/Excel导入到数据库` | `BuildTools_Excel2SQLite.MenuItem_Excel2Sqlite` |
| `Assets/【BD工具箱】ExcelTools/表格转换工具` | `EditorWindow_ExcelExchange` |
| `Assets/【BD工具箱】ExcelTools/Excel导出脚本[程序目录]` | `Excel2CodeTools` |
| `Assets/【BD工具箱】ExcelTools/Excel生成脚本[程序目录]` | `Excel2CodeTools` |
| `Assets/Find Asset Dependencise` | `EditorMenuItem.ShowAssetDependencies` |

## `GameObject/` 菜单

| 菜单路径 | 排序 | 处理类 |
|---------|------|--------|
| `GameObject/UI/Text` | — | `OverrideUIComponent`（默认 `raycastTarget = false`） |
| `GameObject/UI/Image` | — | 同上 |
| `GameObject/UI/Raw Image` | — | 同上 |
| `GameObject/UI工作流/1.创建UIPrefab` | 1 | `Editor/UI/Workflow/MenuItems.cs` |
| `GameObject/UI工作流/2.创建SubWindow节点` | 2 | **空实现** |

## 测试菜单

| 菜单路径 | 优先级 | 处理类 |
|---------|-------|--------|
| `BDFramework/测试/SQLite 集成测试` | 100 | `Runtime.Test/Editor/Sqlite/SqliteIntegrationTestRunner.cs` |
| `BDFramework/测试/SQLite优化性能基准 ▶` | — | `EditorPipeline/SqliteBenchmark/SqliteOptimizationBenchmark.cs` |
| `BDFrameWork工具箱/TestPipeline/打开TestRunner` | — | `Assets/Code/BDFramework.UnitTest/Editor/TestRunner/TestRunnerEditor.cs` |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-DLL` | — | 同上 |
| `BDFrameWork工具箱/TestPipeline/执行UnitTest-ILRuntime` | — | 同上（历史遗留命名） |
| `BDFrameWork工具箱/TestPipeline/执行逻辑测试-ILRuntime(Rebuild DLL)` | — | 同上 |

## 业务方菜单

| 菜单路径 | 处理类 |
|---------|--------|
| `BDFrame开发辅助/导出主工程资源` | `Assets/Code/Game/Editor/ExpoterMainProjectAsset.cs` |

## 其他

| 菜单路径 | 处理类 |
|---------|--------|
| `Tools/GUIStyle 样例窗口` | `Editor/EditorWindows/Menuitems/EditorBuiltinStyle.cs` |

## 第三方菜单（非一方，备查）

| 分组 | 内容 |
|------|------|
| `HybridCLR/*` | `Generate/*`、`CompileDll/*`、`Installer...`、`Settings...`、`Documents/*`、`ObfuzExtension/*` |
| `Obfuz/*` | 混淆相关 |
| `Window/NuGet/*`、`Assets/NuGet/*` | NuGet 包管理 |
| `Window/UniTask Tracker` | UniTask 追踪 |
| `Window/MessagePack/CodeGenerator` | MessagePack 代码生成 |
| `Reporter/Create` | Logs Viewer |
| `Talos/E2E Test/创建 DEBUG 标记` / `移除 DEBUG 标记` / `检查 DEBUG 状态` | Talos E2E |

## 相关页面

- [Editor 核心类](core-classes.md)
- [构建与发布](../pipeline/index.md)
- [测试体系总览](../testing/overview.md)
