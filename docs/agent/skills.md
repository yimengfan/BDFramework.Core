# Skill 索引

按模块拆分的 Agent Skill，位于 `.github/skills/<name>/SKILL.md`。

!!! note "目录规范"
    本项目级 Skill 采用 VS Code 官方 Agent Skills 约定。项目级可选目录：

    | 目录 | 说明 |
    |------|------|
    | `.github/skills/<name>/` | **本仓库使用** |
    | `.agents/skills/<name>/` | 跨工具通用（Claude / Cursor 等也识别） |
    | `.claude/skills/<name>/` | Claude 专用 |

    本仓库统一用 `.github/skills/`，与已有的 `teamcity` skill 保持一致。

## 技能清单

### 核心 Runtime { #core-runtime }

| Skill | 覆盖范围 | 触发关键词 |
|-------|---------|-----------|
| [`bdframework-bootstrap`](#bdframework-bootstrap) | 启动链路、配置中心、日志、协程、路径、单例 | `BDLauncher`、`ScriptLoder`、`GameConfigManager`、`BDebug` |
| [`bdframework-uflux`](#bdframework-uflux) | UFlux UI 框架全栈 | `AWindow`、`ATComponent`、`RenderData`、`Reducer`、`Store` |
| [`bdframework-uimanager`](#bdframework-uimanager) | 屏幕导航、事件总线、管理器、服务容器 | `ScreenView`、`AStatusListener`、`ManagerBase` |
| [`bdframework-resources`](#bdframework-resources) | 资源加载、双寻址、热更下载、对象池 | `BResources`、`AssetsVersionController` |
| [`bdframework-sqlite`](#bdframework-sqlite) | 表格查询、双库模型、导表 | `SqliteHelper`、`TableQueryForILRuntime` |

### 构建与 CI { #build-ci }

| Skill | 覆盖范围 | 触发关键词 |
|-------|---------|-----------|
| [`bdframework-build-pipeline`](#bdframework-build-pipeline) | 四条构建链路 + 发布 + 钩子 | `HyCLREditorTools`、`SetABPack`、`BuildMode` |
| [`bdframework-ci`](#bdframework-ci) | BatchMode 入口、Python 工具、TeamCity | `PublishPipeLineCI`、`-executeMethod`、`pytest` |
| [`teamcity`](https://github.com/yimengfan/BDFramework.Core/blob/v4/v-4.0.0/.github/skills/teamcity/SKILL.md) | TeamCity Web API 操作（已有） | `run-build`、`versioned settings` |

## 详细说明

### `bdframework-bootstrap`

**何时用**：排查"框架起不来"、新增配置模块、日志与协程问题。

**内容**：三段启动时序（AOT → 桥接 → ScriptLoder）、热更 DLL 装载、类型收集白名单、`IConfigProcessor` 契约、`BDebug` 完整 API 与 `[Conditional]` 陷阱、`IEnumeratorTool` 入队语义、`BApplication` 路径重写。

**参考文件**：`references/startup-sequence.md`（逐方法时序、无 MonoBehaviour 路径）

### `bdframework-uflux`

**何时用**：写/改窗口、组件、子窗口；界面不刷新；按钮无响应；扩展自动赋值属性或绑定适配器。

**内容**：`AWindow<T>` / `ATComponent<T>` 生命周期、`UIManager` 全 API、5 个 `AutoAssignAttribute`、`[ComponentValueBind]` 与适配器体系、差异刷新算法、`AReducers<T>` / `Store<S>` / `StoreFactory`、`Require` DI。

**参考文件**：

- `references/auto-assign-attributes.md`
- `references/component-binding.md`
- `references/state-store.md`
- `references/lifecycle.md`

### `bdframework-uimanager`

**何时用**：阶段级界面切换、跨模块事件广播、新增管理器、模块级服务容器。

**内容**：`IScreenView` 生命周期与 `BeginNavTo` 顺序、`AStatusListener` / `ADataListenerT<T>` 双轨与 20 条缓存、`StatusListenerServer` 双字典、`ManagerBase<T,V>` 契约、`ServiceContainer` / `GameServiceStore`。

**参考文件**：

- `references/event-bus-api.md`
- `references/manager-service.md`

!!! warning "本 skill 名称是历史遗留"
    它实际覆盖的是**导航 + 事件 + 管理器 + 服务容器**，不是 `UIManager` 窗口管理（那在 `bdframework-uflux`）。

### `bdframework-resources`

**何时用**：加载/卸载资源、对象池、排查"加载不到资源"、热更下载。

**内容**：`AssetLoadPathType` vs `LoadPathType`、`BResources` 全 API、双寻址 FIRST/SECOND、`DevResourceMgr` vs `AssetBundleMgrV2` 差异、`AssetsVersionController` 下载流程、磁盘布局常量。

**参考文件**：`references/version-controller.md`

### `bdframework-sqlite`

**何时用**：写表查询、排查 DB 问题、新增业务表、导表。

**内容**：双库模型与加密、`SqliteLoder` / `SqliteHelper` / `SQLiteService`、`TableQueryForILRuntime` 全部方法与三个陷阱、表类命名契约、Excel 格式约定。

**参考文件**：`references/table-pipeline.md`

### `bdframework-build-pipeline`

**何时用**：构建热更 DLL / AB / 表格 / 母包、写自定义 AssetGraph 节点、挂管线钩子。

**内容**：四条链路入口、资源总管道、HybridCLR `PreBuild` / `BuildHotfixDLL`、AssetGraph 20 个内置节点、颗粒度与分包、`BuildMode`、`ABDFrameworkPublishPipelineBehaviour` 完整虚方法表。

**参考文件**：`references/build-entrypoints.md`

### `bdframework-ci`

**何时用**：触发 BatchMode 构建、排查 BatchMode 崩溃/参数问题、跑 Python 脚本与 pytest、理解上传协议。

**内容**：`PublishPipeLineCI` 全部入口、命令行参数矩阵、Python 脚本与 CLI、上传协议、CI 输出目录、平台隔离、TeamCity DSL 结构。

**参考文件**：`references/ci-commands.md`

## Skill 设计约定

本仓库的 Skill 遵循以下结构：

```text
.github/skills/<skill-name>/
├── SKILL.md                  # 必需。name 必须与目录名一致
├── references/               # 按需加载的详细 API 参考
│   └── <topic>.md
└── scripts/ / assets/        # 可选（本仓库暂未使用）
```

`SKILL.md` 的组织约定：

| 章节 | 内容 |
|------|------|
| frontmatter `description` | **关键词密集**，列出触发场景与标识符 —— 这是被发现的关键 |
| 1. 何时使用 | 正向触发条件 + 不适用场景 |
| 2. 铁律 | 3–8 条最高频、破坏性最强的约束 |
| 3–N. 分模块内容 | API 速查 + 标准工作流 + 代码示例 |
| N. 故障对照 | 现象 → 根因 → 处理 |
| N. 详细参考 | 指向 `references/` 与在线文档 |
| N. 改动前检查清单 | 可勾选项 |

!!! tip "渐进式加载"
    `description` 用于发现（约 100 tokens）；`SKILL.md` 正文按需加载；`references/*.md` 只在明确引用时加载。

    因此 **`SKILL.md` 要保持精简**，深度 API 细节下沉到 `references/`。

## 与规则文档的关系

| 层级 | 载体 | 加载方式 |
|------|------|---------|
| L0 全局根规范 | `.github/copilot-instructions.md` | 始终 |
| L1 文件级约束 | `.github/instructions/*.instructions.md` | `applyTo` 自动匹配 |
| L2 包架构 | `AGENTS.md`（仓库根 / package 根） | 按需 |
| Skill | `.github/skills/<name>/SKILL.md` | 按需（模型判断） |
| L3 临时记忆 | `.agent_memory/**` | 任务触发 |

Skill 与 instruction 的分工：

| | Instruction | Skill |
|---|-------------|-------|
| 触发 | 编辑某类文件时**自动**加载 | 任务匹配时**按需**加载 |
| 内容 | 编码规则、边界约束 | 领域知识、API 速查、工作流 |
| 粒度 | 按文件路径 | 按业务模块 |

详见[文档治理](doc-governance.md)。
