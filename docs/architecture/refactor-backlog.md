# 结构问题与重构清单

> 本文记录在代码结构梳理过程中发现的**已知不一致**，作为后续重构的待办清单。
> 每条都标注了严重度和建议动作，**不代表当前会导致故障**——多数是历史演进留下的命名/职责漂移。
>
> 分级：**P1** 会影响正确性或排查效率 ｜ **P2** 职责/命名错误，增加理解成本 ｜ **P3** 死代码与清理项

## P1 —— 正确性风险

### 1. `BResources.UnloadAsset` 的参数被忽略 { #ref-1-unloadasset }

```csharp
public static void UnloadAsset(string assetPath, bool isForceUnload = false, Type type = null)
```

`isForceUnload` 与 `type` **都没有透传到 `AssetBundleMgrV2.UnloadAsset`**（实现只调 `UnUseAssetBundle(loadPath)`）。

**影响**：调用方以为可以强制卸载/指定类型卸载，实际行为与默认一致。
**建议**：要么实现参数语义，要么在签名上标注 `[Obsolete]` 并移除参数。

### 2. `ScreenViewLayer.BeginNavBack()` 的越界条件 { #ref-2-beginnavback }

```csharp
if (currentViewIndex == 0 || currentViewIndex - 1 >= navViews.Count) { BDebug.LogError("别闹，前方没有view"); }
```

`currentViewIndex == -1` 时 `-2 >= count` 为 `false`，会执行 `currentViewIndex--` 变成 `-2`，随后访问 `navViews[-2]` **抛异常**。

**建议**：改为 `if (currentViewIndex <= 0) return;`。

### 3. `UIManager.Back()` 的游标方向 { #ref-3-uimanager-back }

`Forward()` / `Back()` 只改游标后调 `ShowWindow(..., isAddToHistory: false)`，但 `Back()` 内部用的是 `curForwardBackUIIdx++`（方向与语义相反）。

**建议**：核对后统一为 `Back()` 递减、`Forward()` 递增，并补一个导航历史测试。

### 4. `ComponentBindAdaptorManager.AnalysisRenderDataChanged` 的比较对象 { #ref-4-analysisrenderdatachanged }

值类型分支里写的是 `newRenderData.Equals(LastValue)`，比较的是 **RenderData 自身**而不是缓存的上一次值。这会让"值未变化"被判为变化（或反之）。

**建议**：改为 `LastValue == null || !LastValue.Equals(newValue)`，并补差异刷新测试。

### 5. `StatusListenerService.Name` 永远是 `null` { #ref-5-statuslistenerservice-name }

`Create(name)` / `GetService(name)` 内部**没有给 `Name` 赋值**（字典的 key 才是真名）。

**影响**：日志里无法区分不同的 listener service。
**建议**：在 `StatusListenerServer` 构造路径里补 `service.Name = name`。

### 6. AOT 日志子系统的运行时耦合 { #ref-6-bdebug-runtime-coupling }

`BDebug` 是全局命名空间下的 `MonoBehaviour`，静态实例 `Inst` 在非 PlayMode 下会 `FindObjectOfType` 或**直接 `new GameObject`**。`DisableLog` / `EnableLog` 用的是强制实例化的 `Inst`（而非宽松的 `inst`），在 BatchMode/纯逻辑路径里会凭空创建对象。

**建议**：让 `DisableLog`/`EnableLog` 也走宽松路径，或在实例缺失时降级为无操作。

## P2 —— 职责与命名

### 7. 文件名与类型名不一致 { #ref-7-filename-type-mismatch }

| 文件 | 实际类型 | 问题 |
|------|---------|------|
| `Runtime/UI/View/Windows/AWindowProp.cs` | `AWindow<TP>` | 文件名暗示"带 Props 的窗口"，实际是**所有窗口的基类** |
| `Runtime/Navigation/ScreenViewCenter.cs` | `ScreenViewLayer` | 文件名叫 Center，**没有 `ScreenViewCenter` 类型** |
| `Editor/UI/UIManager.Editor.cs` | `UIManagerEditor` | 不是 `UIManager` 的 partial，名字误导 |
| `Runtime/HotfixData/Sql/TableQueryForILRuntime.cs` | 当前唯一查询构建器 | 名字来自已移除的 ILRuntime |
| `Runtime/AssetsManager/VersionController/AssetsVersionController.DevOpsPureLogic.cs` | 文件服务器协议 | "PureLogic" 命名与 `GameConfigStartupPureLogic` 混淆 |

**建议**：`AWindowProp.cs` → `AWindow.Generic.cs`；`ScreenViewCenter.cs` → `ScreenViewLayer.cs`；`UIManager.Editor.cs` → `UIManagerEditor.cs`。

### 8. `ConfigEditorUtil` 位于 Runtime 程序集 { #ref-8-configeditorutil }

`Runtime/GameConfig/ConfigEditorUtil.cs` 内容被 `#if UNITY_EDITOR` 包裹，但类型声明在 `BDFramework.Core` 中。

**后果**：`GameConfigManager` 无法直接复用它，只好**自己复制了一份 `DefaultEditorConfigPath` 常量**。

**建议**：把 `ConfigEditorUtil` 移到 `Editor/` 下，或在 Runtime 侧抽一个不含 UnityEditor 依赖的常量类供两边引用。

### 9. `AssetsManager/UnitTest/*` 是手工 GUI 测试 { #ref-9-assetsmanager-unittest }

`Runtime/AssetsManager/UnitTest/` 下是一批**需要手工挂载到场景**的 `MonoBehaviour` 基准脚本（`AssetBundleBenchmark01`、`AssetBundleTestLoad`、对象池 Examples），混在 Runtime 程序集里，不会被自动化测试覆盖。

**建议**：迁到 `Runtime.Test/` 或 `Assets/Code/BDFramework.UnitTest/Runtime/`。

### 10. 全局命名空间下的类 { #ref-10-global-namespace }

`BDebug`、`IEnumeratorTool`、`PageList`、`L2Text`、`L2Image` 都在**全局命名空间**（无 `namespace`）。

**影响**：业务代码里 `Text` / `Image` / `Log` 之类短名容易与 Unity 类型冲突，且无法被 `using` 收窄。

**建议**：至少给 `PageList` / `L2Text` / `L2Image` 加上 `BDFramework.UI` 命名空间。

### 11. 空壳类型与未使用接口 { #ref-11-empty-types }

| 类型 | 状态 |
|------|------|
| `Runtime/UI/View/Component/ISubComponent.cs` | 接口 `RegisterSubComponent` **无任何实现** |
| `StateManager/Reducer/Attribute/AsyncReducerAttribute.cs` | 只有字段、**无构造函数**，未被使用 |
| `Component/Localization/L2Text.cs` / `L2Image.cs` | 空壳 `MonoBehaviour`，**无任何逻辑** |
| `Runtime/GameConfig/Config.cs` | `Config : MonoBehaviour` **字段为空**，只剩枚举宿主价值 |
| `Runtime/Core/` | 目录存在但**无 `.cs`** |
| `PublishPipeLineCI.BuildDLL()` | `[CI]` 入口标记但**空实现** |

**建议**：删除或补实现。`Config.cs` 至少应改名为 `FrameworkEnums.cs` 以免误导。

### 12. `PageList` 与 UFlux 脱节 { #ref-12-pagelist }

`Runtime/UI/Component/PageList/PageList.cs` 是**全局命名空间的 `MonoBehaviour`**，内部硬编码 `Resources.Load`，并且自带 `TestScene.unity` 示例。

**建议**：要么改造为 `AComponent` + `BResources` 加载，要么从框架包移出到业务侧。

## P3 —— 清理项

### 13. ILRuntime 残留 { #ref-13-ilruntime-residue }

| 位置 | 状态 |
|------|------|
| `EditorPipeline/.../BuildHotfixScriptEditor/Unity3dRoslynBuildTools.cs` | 整段注释，`ENABLE_ILRUNTIME` 门控 |
| `EditorPipeline/.../BuildHotfixScriptEditor/ILRuntime/` | 遗留编辑器工具 |
| `ScriptLoder.CreateHotfixInstance` | ILRuntime 分支注释 |
| `ScriptLoder.Dispose()` | `AppDomain?.Dispose()` 注释 |
| `TableQueryForILRuntime` | 旧 API 名 |

**建议**：在本仓库确认无 `ENABLE_ILRUNTIME` 构建后整目录删除；`ScriptLoder` 内的注释块清掉。

### 14. 常量重复 { #ref-14-duplicated-constants }

`HYCLR_AOT_PATCH_PATH` / `HOTFIX_DLL_PATH` / `HOT_DLL_EXTENSION` 在 `Runtime/HotfixScript/ScriptLoder.cs` 与 `Runtime.AOT/ScriptLoderAOT.cs` **各有一份**。

这是**刻意的程序集隔离**（AOT 不能引用 Core），但缺少注释说明。**建议**：补注释交叉引用，避免有人改了一处漏另一处。

### 15. 过时注释 { #ref-15-stale-comments }

- `ScriptLoder.Init()` 的 XML 注释提到"桥接 Talos E2E 自动发现入口"，但**方法体内没有这段逻辑**（实际由 `Packages/com.talosai.e2e/Runtime/TestRunner/E2EAutoInit.cs` 完成）。
- `PublishPipelineTools` 注释写上传路径 `{UPLOAD_FOLDER_SUFFIX}/{platform}/{version}`，**代码实现是 `_ReadyToUpload/<version>/<platform>`**（顺序相反）。
- `EditorPipeline/Behavior/EditorBehavior/BDFrameworkPipelineHelper.cs` 内留有被注释的 `[MenuItem("xxx/xxxx")]` 示例。

### 16. 拼写错误（已在 API 中固化，改动需评估兼容） { #ref-16-typos }

| 位置 | 当前拼写 | 正确 |
|------|---------|------|
| `BResources.UnloadAssetByGouroup` | `Gouroup` | `Group` |
| `TableQueryForILRuntime.EnableSqlCahce` | `Cahce` | `Cache` |
| `BuildTools_Excel2SQLite.BuildSqlite(string ouptputPath, …)` | `ouptput` | `output` |
| `docs` 历史页 `CI相关操作（Jekins，TeamCity）` | `Jekins` | `Jenkins` |

**建议**：加正确拼写的别名并标注 `[Obsolete("拼写错误，请使用 Xxx")]`，下个大版本删除。

### 17. 死参数 { #ref-17-dead-parameters }

- `ScreenViewLayer.BeginNavForward(string name)` 的 `name` **未被使用**。
- `SqliteLoder.Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir)` 的 `assetLoadPathType` / `secondDir` **未参与任何判断**——db 恒定取 `firstDir/local.db`。

**建议**：删参数或补语义。`SqliteLoder.Init` 的签名刻意与 `BResources.Init` 对齐，若要保留一致性应补注释。

### 18. SQLite 门面不对称 { #ref-18-sqlite-facade }

`SqliteHelper.SQLiteService` **没有公开的 `Update` / `Delete` / `Execute`**，但构造时又创建了 `TableQueryForILRuntime`。写库只能通过 `.Connection` 拿底层连接（Editor 构建管线正是这样用的）。

**建议**：把 `Update` / `Delete` 显式暴露出来，或明确声明"服务层只读"。

### 19. `WhereOr` / `WhereAnd` 是覆盖而非追加 { #ref-19-whereor-whereand }

`TableQueryForILRuntime.WhereOr` / `WhereAnd` **整体覆盖 `@where`**，而 `And` / `Or` 属性是**读取即修改**（`get` 里拼接）。这意味着不能写 `if (cond) q.And;` 这类分支代码——会静默产生错误 SQL。

**建议**：至少在 XML 注释里加粗警告；更好的做法是把 `And`/`Or` 改成方法。

## 仓库级问题

### 20. 配置文件重复 { #ref-20-duplicated-config }

| 文件 | 状态 |
|------|------|
| `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/buildtools.toml` | 规定为唯一来源 |
| `DevOps/CI/buildtools.toml` | **内容重复的副本** |

**建议**：删除 `DevOps/CI/buildtools.toml`，或在 CI 里做一致性校验。

### 21. 引用失效路径 { #ref-21-missing-githook-dir }

`DevOpsEditorTasks.UpdateGitHookToLocalStore` 仍引用 `Path.Combine(BApplication.DevOpsCIPath, "githook")`，但 **`DevOps/CI/githook/` 在本仓库不存在**（应由使用方提供）。该任务在每次 Unity 加载/代码重编译时执行。

**建议**：缺目录时降级为 `Log` 而不是静默失败，并在文档中说明该目录由使用方提供。

### 22. TeamCity DSL 位置 { #ref-22-teamcity-dsl-location }

Kotlin DSL 位于 `.test-DevOps/.teamcity/`，**不在 `DevOps/`** 下。所有 CI 文档与脚本路径都更容易被误认为在 `DevOps/CI/`。

**建议**：在 `DevOps/CI/README.md` 里显式写明 DSL 真实位置（当前已有部分说明，建议加强）。

## 相关页面

- [Runtime 模块地图](runtime-modules.md)
- [Editor 模块地图](editor-modules.md)
- [归档：原始 Wolai 导出](../archive/index.md)
