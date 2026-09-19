# 归档

本分区存放**迁移前的原始文档**，不参与站点导航，也不再维护。

## 原始 Wolai 导出

位置：`docs/archive/wolai/`

| 项 | 数量/大小 |
|----|----------|
| Markdown 文件 | 89 篇 |
| 本地图片 | 116 张（约 10 MB，分布在 38 个 `image/` 目录） |
| 外链图床图（`cdn.nlark.com`） | 25 张（分布在 6 个文件） |
| 附件 | 1 个（`DB Browser for SQLite_*.zip`，约 19 MB，**已通过 `.gitignore` 排除**） |

!!! warning "归档内容不再维护"
    `mkdocs.yml` 通过 `exclude_docs: archive/wolai/**` 把它排除在站点构建之外。需要引用其中信息时，应**改写进正式文档**。

    它保留的原因：史料价值 + 迁移对照可追溯。

!!! note "第三方二进制附件未纳入版本管理"
    原 `表格操作：Sqlite-` 页附带一个 DB Browser for SQLite 安装包（19 MB）。
    它是第三方二进制安装程序、非文档内容，已在 `.gitignore` 中排除
    （`docs/archive/**/file/*.zip`）。需要时请从 <https://sqlitebrowser.org/> 获取。

## 迁移对照

原始目录树 → 当前站点页面的映射。

### 安装与上手

| 原始路径 | 去向 |
|---------|------|
| `安装/安装.md` + `安装/UPM版本安装引导/` | [安装与依赖](../guide/installation.md) |
| `Start/1.资源目录结构/` | [目录结构与约定](../guide/project-structure.md) |
| `Start/2.框架配置/` | 合并进 [配置中心 GameConfig](../api/game-config.md) |
| `Start/3.资源加载规则/` | [资源加载寻址](../guide/asset-load-path.md) |
| `Start/编码规范建议/编码建议/` | [编码规范](../guide/coding-guidelines.md) |

### API

| 原始路径 | 去向 |
|---------|------|
| `API参考/业务管理：ManagerBase/` | [管理器体系 ManagerBase](../api/manager-base.md) |
| `API参考/事件、数据监听/` | [事件总线 EventBus](../api/event-bus.md) |
| `API参考/协程/` | 合并进 [服务容器与日志](../api/utils.md) |
| `API参考/网络协议/1.Protobuf规范/` | 待补（见下方缺口） |
| `API参考/表格操作：Sqlite-/` | [表格 SQLite](../api/sqlite.md) |
| `API参考/资源加载：BResource-/` | [资源加载 BResources](../api/resources.md) |
| `API参考/配置中心：GameConfig-/` | [配置中心 GameConfig](../api/game-config.md) |
| `API参考/页面导航：ScreenView/` | [屏幕导航 ScreenView](../api/screen-navigation.md) |
| `API参考/日志系统-/` | **空页，丢弃**（内容改写进 [服务容器与日志](../api/utils.md)） |

### UI（UFlux）

| 原始路径 | 去向 |
|---------|------|
| `UI工作流/前言/` + `UFlux整体流程/` + `用MVC去理解UFlux/` | [UFlux 架构总览](../uflux/uflux-overview.md) |
| `UI工作流/View简单使用/窗口AWindows/` | [窗口 Window](../uflux/window.md) |
| `UI工作流/View简单使用/子窗口SubWindow/` | [子窗口 SubWindow](../uflux/sub-window.md) |
| `UI工作流/View简单使用/消息派发、监听/` | [消息 UIMessage](../uflux/ui-message.md) |
| `UI工作流/View简单使用/基本组件：Component/` | [组件 Component](../uflux/component.md) |
| `UI工作流/Props-View渲染状态/` + `Props值绑定/` | [渲染数据 RenderData](../uflux/render-data.md) |
| `UI工作流/View元素自动赋值/` + 4 个 Attribute 子页 | [自动赋值属性](../uflux/auto-assign-attributes.md) |
| `UI工作流/状态(State)管理/Reducer、State、Store/` | [状态管理 Reducer/Store](../uflux/state-management.md) |
| `UI工作流/依赖注入/` | [依赖注入](../uflux/dependency-injection.md) |
| `UI工作流/Demo合集/` | [Demo 解读](../tutorials/demos.md) |
| `UI工作流/FairyGUI支持/` | **丢弃**（本仓库已无 FGUI 集成） |

### 管线

| 原始路径 | 去向 |
|---------|------|
| `构建工作流/1.DLL打包/1.脚本打包/` + `HyCLR前置操作/` | [热更代码 HybridCLR](../pipeline/build-hotfix-dll.md) |
| `构建工作流/1.DLL打包/2.热更代码调试（ILRuntime）/` | **丢弃**（ILRuntime 已移除） |
| `构建工作流/2.AssetBundle打包/1~4` | [AssetBundle 打包](../pipeline/build-assetbundle.md) |
| `构建工作流/2.AssetBundle打包/5.节点扩展/` | [AssetGraph 节点扩展](../pipeline/build-assetbundle-extension.md) |
| `构建工作流/3.表格打包/` | [表格打包](../pipeline/build-table.md) |
| `构建工作流/4.包体构建/` + `发布工作流/2.母包发布/` | [母包构建](../pipeline/build-package.md) |
| `发布工作流/1.资源发布/` | [资源发布](../pipeline/publish-assets.md) |
| `Devops/CI相关操作/` + `GitHook自动发布/` + `给CI的一点建议/` | [DevOps 与 CI](../pipeline/devops-ci.md) |
| `工作流管线/资产管理/` | 合并进 [AssetBundle 打包](../pipeline/build-assetbundle.md) |

### Editor

| 原始路径 | 去向 |
|---------|------|
| `Editor API参考/Editor核心类总览/` | [Editor 核心类](../editor/core-classes.md) |
| `Editor API参考/Editor下Http支持/` | [Editor Http 服务](../editor/http-server.md) |
| `Editor API参考/Editor事件监听/` | [管线回调钩子](../editor/publish-hooks.md) |

### 测试

| 原始路径 | 去向 |
|---------|------|
| `测试工作流（TDD）/BDFrame的单元测试/` | [测试体系总览](../testing/overview.md) |

### 教程

| 原始路径 | 去向 |
|---------|------|
| `教程给你/视频&demo/` | [视频与外部资源](../tutorials/videos.md) |

### 丢弃 / 归档

| 原始路径 | 处理 |
|---------|------|
| `开发计划/**`（12 页） | 归档，不迁移 |
| `开发计划表V-4.0.0/` | **空页，丢弃** |
| `Unity插件/` | **丢弃**（推荐的 Asset Store 插件与框架能力无关） |
| `2.热更代码调试（ILRuntime）/` | **丢弃**（ILRuntime 已移除） |
| `FairyGUI支持/` | **丢弃**（无对应集成） |
| `日志系统-/` | **空页，丢弃** |
| 19 个纯链接索引页 | **丢弃**（改由站点侧边栏导航） |

## 原始文档的主要问题

迁移时发现并已修正的问题：

| 类别 | 具体问题 |
|------|---------|
| **术语过期** | `APropsBase`→`ARenderDataBase`、`props`→`RenderData`、`CommitProps`→`CommitRenderData`、`AutoInitComponentAttribute`→`AutoAssignAttribute`、`ComponentPathAttribute`→`UfluxComponentPathAttribute`、`Awake`→`AWindow`、`SetABName`→`BuildABPack`、`CodeRunMode`→`HotfixCodeRunMode` |
| **技术栈过期** | 大量 ILRuntime（CLRBinding / Adaptor / 调试插件）内容已完全不适用 |
| **版本过期** | 包版本 2.4.2、Unity 2019.4、`com.ourpalm.ilruntime` scoped registry |
| **死链** | 5 处 Wolai 站内页 ID；13 处语雀链接（其中 1 个被 5 页当作唯一实现说明引用）；25 张 `cdn.nlark.com` 外链图；4 处 ILRuntime 官方文档；约 12 处指向 upstream `master` 分支而非本仓库 |
| **格式损坏** | 1 处加粗标记错位导致的畸形链接；20+ 页的锚点式 TOC 在静态站点生成器下全部失效 |
| **命名不可用于 URL** | 全角 `（）`、`：`、`、`、`【】`；`[ ]` 会破坏 Markdown 链接解析；尾随 `-`；`视频&demo` 的 `&`；`UI工作流（UIPipeline)` 括号混用；`Jekins` 拼写错误 |
| **结构缺陷** | 19 个零正文索引页（占 21% 节点数）；18 处"同名页与同名目录并存"造成 URL 自我重复；4 个空页；多处有标题无内容的残缺章节 |
| **内容泄漏** | `typeof(没有标记修改字段)` 占位符、非法的 `public TestWidow:Awindows<Props_TestView>` 语法、重复错乱句 |
| **编号错乱** | `1.DLL打包/1.脚本打包` 两层都从 1 编号；各子目录独立重排；`Demo合集` 跳号（0、1、4、5） |
| **导出残渣** | 大量 `\*\*` 转义加粗、`&#x20;` HTML 空格、`&#xA;` 换行实体出现在标题与链接 title 中 |
| **PM 内容混放** | `开发计划/**` 与产品文档同级 |

## 内容缺口

迁移中识别出但**尚未补齐**的页面：

| 缺口 | 说明 |
|------|------|
| `api/network-protobuf` | Protobuf 规范（proto 放置、生成类路径、热更归属、`MessageParser` 用法、避免 `map`） |
| 日志持久化详情 | 原始 `日志系统-` 是空页；当前内容已并入 [服务容器与日志](../api/utils.md)，但导出/查看工具链未单独成页 |
| 网络协议栈 | 仓库有 `Assets/Code/Game/NetProtocol/Protobuf/`，但无独立文档页 |
| 多语言（L2） | `L2Type` 只是枚举，`L2Text` / `L2Image` 是空壳，无可用文档 |
| FairyGUI 集成 | 若未来恢复支持需重写（当前无集成） |

## 相关页面

- [文档治理](../agent/doc-governance.md) —— 分层、边界与维护约定
- [结构问题与重构清单](../architecture/refactor-backlog.md) —— 代码结构待办
- [Agent 集成](../agent/index.md)
