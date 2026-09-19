# 架构总览

本分区回答一个问题：**代码放在哪里，为什么放在那里**。

| 页面 | 内容 |
|------|------|
| [程序集与依赖](assemblies.md) | 5 个一方程序集的边界、依赖方向、asmdef 位置 |
| [启动链路](bootstrap.md) | 从 `RuntimeInitializeOnLoadMethod` 到管理器就绪的精确时序 |
| [Runtime 模块地图](runtime-modules.md) | `BDFramework.Core` 的 8 个模块与文件清单 |
| [Editor 模块地图](editor-modules.md) | `BDFramework.Editor` 的子模块划分 |
| [结构问题与重构清单](refactor-backlog.md) | 当前结构中的不一致、死代码与建议动作 |

## 一句话架构

```text
AOT 层（不可热更）          桥接层                  Core 层（可热更）
─────────────────      ──────────────      ─────────────────────────────
BDLauncher             BDLauncherBridge     BResources / SqliteLoder
ScriptLoderAOT    →    GameConfigLoder  →   ScriptLoder → ManagerInstHelper
（装载 DLL/元数据）     ClientAssetsUtils     ├─ UIManager (UFlux)
                                              ├─ ScreenViewManager
                                              ├─ ComponentBindAdaptorManager
                                              └─ GameConfigManager
```

**设计要点**：

1. **AOT 层与 Core 层互不引用。** `BDLauncher` 无法 `using BDFramework`（循环依赖），因此它访问 Core 全部通过 `AppDomain.CurrentDomain.GetAssemblies()` + `Type.GetType(全名)` 反射。
2. **启动是两阶段。** `ScriptLoder.Init()` 收集类型并注册管理器（**不启动**），业务在更新页完成后调用 `BDLauncherBridge.Launch()` 才真正初始化资源与启动管理器。
3. **管理器系统是唯一的扩展点。** 新模块只要继承 `ManagerBase<T, TAttribute>` 并挂上派生自 `ManagerAttribute` 的属性，就会被自动发现、注册、`Init()` + `Start()`。

## 分层职责表

| 层 | 程序集 | 可热更 | 职责 | 禁止 |
|----|--------|--------|------|------|
| AOT | `BDFramework.AOT` | ✗ | 引导、AOT 补充元数据加载、热更 DLL 装载、剪裁保护 | 引用 `BDFramework.Core` |
| 运行时 | `BDFramework.Core` | ✓ | 资源/表格/配置/UI/导航/事件/服务 | 引用 `BDFramework.Editor` |
| 编辑器 | `BDFramework.Editor` | ✗ | 构建管线、发布管线、CI bridge、编辑器窗口 | 被运行时引用 |
| 测试 | `BDFramework.Test` / `BDFramework.EditorTest` | ✗ | Runtime API 测试 / BatchMode 测试 | 出现在 Release 构建中 |
| CI 脚本 | `Editor.DevOps~` | ✗ | Python/shell 构建与上传 | 被 Unity 编译（`~` 后缀） |

## 与其他框架的对照

| 概念 | BDFramework | 典型对照 |
|------|-------------|---------|
| UI 状态管理 | UFlux（`AStateBase` + `Reducer` + `Store`） | Redux / Vuex |
| 资源寻址 | 显式路径（`Runtime` 目录相对路径） | Addressables（带 Address 注册表） |
| 热更 | HybridCLR（解释执行 + AOT 补充元数据） | ILRuntime（已移除）/ Lua |
| 服务容器 | `ServiceContainer` + `GameServiceStore` | 简易 DI，无生命周期管理 |
| 配置中心 | `GameConfigManager` + `IConfigProcessor` | ScriptableObject 配置（框架用 `.bytes` JSON） |

!!! note "为什么不用 Addressables"
    框架在 `StreamingAssets` 与 `persistentDataPath` 之间做双寻址，并且需要**自建 hash 命名 + 文件服务器增量下载**（见[资源发布](../pipeline/publish-assets.md)）。Addressables 的 catalog 机制与这套自研版控冲突，因此选择显式路径 + 自研 AB 索引（`art_assets.info`）。
