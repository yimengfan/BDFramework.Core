# Archive

This section holds the **raw documentation from before the migration**. It is not part of the site navigation and is no longer maintained.

## The raw Wolai export

Location: `docs/archive/wolai/`

| Item | Count / size |
|------|--------------|
| Markdown files | 89 |
| Local images | 116 (about 10 MB, spread across 38 `image/` directories) |
| Externally hosted images (`cdn.nlark.com`) | 25 (spread across 6 files) |
| Attachments | 1 (`DB Browser for SQLite_*.zip`, about 19 MB, **excluded through `.gitignore`**) |

!!! warning "The archived content is no longer maintained"
    `mkdocs.yml` excludes it from the site build through `exclude_docs: archive/wolai/**`. When you need to reference information from it, **rewrite it into the formal documentation**.

    The reasons it is kept: archival value + a traceable migration mapping.

!!! note "The third-party binary attachment is not under version control"
    The original `表格操作：Sqlite-` page shipped a DB Browser for SQLite installer (19 MB).
    It is a third-party binary installer, not documentation content, and has been excluded in `.gitignore`
    (`docs/archive/**/file/*.zip`). Fetch it from <https://sqlitebrowser.org/> if you need it.

## Migration mapping

Original directory tree → current site pages.

### Installation and getting started

| Original path | Destination |
|---------------|-------------|
| `安装/安装.md` + `安装/UPM版本安装引导/` | [Installation & Dependencies](../guide/installation.md) |
| `Start/1.资源目录结构/` | [Project Layout & Conventions](../guide/project-structure.md) |
| `Start/2.框架配置/` | Merged into [Configuration (GameConfig)](../api/game-config.md) |
| `Start/3.资源加载规则/` | [Asset Load Paths](../guide/asset-load-path.md) |
| `Start/编码规范建议/编码建议/` | [Coding Guidelines](../guide/coding-guidelines.md) |

### API

| Original path | Destination |
|---------------|-------------|
| `API参考/业务管理：ManagerBase/` | [Managers (ManagerBase)](../api/manager-base.md) |
| `API参考/事件、数据监听/` | [Event Bus](../api/event-bus.md) |
| `API参考/协程/` | Merged into [Service Container & Logging](../api/utils.md) |
| `API参考/网络协议/1.Protobuf规范/` | Still to be written (see the gaps below) |
| `API参考/表格操作：Sqlite-/` | [Tables (SQLite)](../api/sqlite.md) |
| `API参考/资源加载：BResource-/` | [Assets (BResources)](../api/resources.md) |
| `API参考/配置中心：GameConfig-/` | [Configuration (GameConfig)](../api/game-config.md) |
| `API参考/页面导航：ScreenView/` | [Screen Navigation](../api/screen-navigation.md) |
| `API参考/日志系统-/` | **Empty page, discarded** (the content was rewritten into [Service Container & Logging](../api/utils.md)) |

### UI (UFlux)

| Original path | Destination |
|---------------|-------------|
| `UI工作流/前言/` + `UFlux整体流程/` + `用MVC去理解UFlux/` | [UFlux Overview](../uflux/uflux-overview.md) |
| `UI工作流/View简单使用/窗口AWindows/` | [Window](../uflux/window.md) |
| `UI工作流/View简单使用/子窗口SubWindow/` | [SubWindow](../uflux/sub-window.md) |
| `UI工作流/View简单使用/消息派发、监听/` | [Messages (UIMessage)](../uflux/ui-message.md) |
| `UI工作流/View简单使用/基本组件：Component/` | [Component](../uflux/component.md) |
| `UI工作流/Props-View渲染状态/` + `Props值绑定/` | [RenderData](../uflux/render-data.md) |
| `UI工作流/View元素自动赋值/` + 4 Attribute sub-pages | [Auto-Assign Attributes](../uflux/auto-assign-attributes.md) |
| `UI工作流/状态(State)管理/Reducer、State、Store/` | [State Management (Reducer/Store)](../uflux/state-management.md) |
| `UI工作流/依赖注入/` | [Dependency Injection](../uflux/dependency-injection.md) |
| `UI工作流/Demo合集/` | [Demo Walkthrough](../tutorials/demos.md) |
| `UI工作流/FairyGUI支持/` | **Discarded** (this repository has no FGUI integration) |

### Pipeline

| Original path | Destination |
|---------------|-------------|
| `构建工作流/1.DLL打包/1.脚本打包/` + `HyCLR前置操作/` | [Hotfix Code (HybridCLR)](../pipeline/build-hotfix-dll.md) |
| `构建工作流/1.DLL打包/2.热更代码调试（ILRuntime）/` | **Discarded** (ILRuntime has been removed) |
| `构建工作流/2.AssetBundle打包/1~4` | [AssetBundle Building](../pipeline/build-assetbundle.md) |
| `构建工作流/2.AssetBundle打包/5.节点扩展/` | [Custom AssetGraph Nodes](../pipeline/build-assetbundle-extension.md) |
| `构建工作流/3.表格打包/` | [Table Building](../pipeline/build-table.md) |
| `构建工作流/4.包体构建/` + `发布工作流/2.母包发布/` | [Client Package Build](../pipeline/build-package.md) |
| `发布工作流/1.资源发布/` | [Asset Publishing](../pipeline/publish-assets.md) |
| `Devops/CI相关操作/` + `GitHook自动发布/` + `给CI的一点建议/` | [DevOps & CI](../pipeline/devops-ci.md) |
| `工作流管线/资产管理/` | Merged into [AssetBundle Building](../pipeline/build-assetbundle.md) |

### Editor

| Original path | Destination |
|---------------|-------------|
| `Editor API参考/Editor核心类总览/` | [Editor Core Classes](../editor/core-classes.md) |
| `Editor API参考/Editor下Http支持/` | [Editor Http Server](../editor/http-server.md) |
| `Editor API参考/Editor事件监听/` | [Pipeline Hooks](../editor/publish-hooks.md) |

### Testing

| Original path | Destination |
|---------------|-------------|
| `测试工作流（TDD）/BDFrame的单元测试/` | [Testing Overview](../testing/overview.md) |

### Tutorials

| Original path | Destination |
|---------------|-------------|
| `教程给你/视频&demo/` | [Videos & External Resources](../tutorials/videos.md) |

### Discarded / archived

| Original path | Handling |
|---------------|----------|
| `开发计划/**` (12 pages) | Archived, not migrated |
| `开发计划表V-4.0.0/` | **Empty page, discarded** |
| `Unity插件/` | **Discarded** (the recommended Asset Store plugins are unrelated to framework capabilities) |
| `2.热更代码调试（ILRuntime）/` | **Discarded** (ILRuntime has been removed) |
| `FairyGUI支持/` | **Discarded** (no corresponding integration) |
| `日志系统-/` | **Empty page, discarded** |
| 19 pure link-index pages | **Discarded** (the site sidebar now handles navigation) |

## Main problems in the original documents

Problems found during the migration and already fixed:

| Category | Concrete problem |
|----------|------------------|
| **Outdated terminology** | `APropsBase`→`ARenderDataBase`, `props`→`RenderData`, `CommitProps`→`CommitRenderData`, `AutoInitComponentAttribute`→`AutoAssignAttribute`, `ComponentPathAttribute`→`UfluxComponentPathAttribute`, `Awake`→`AWindow`, `SetABName`→`BuildABPack`, `CodeRunMode`→`HotfixCodeRunMode` |
| **Outdated tech stack** | A large amount of ILRuntime content (CLRBinding / Adaptor / debugging plugins) is now completely inapplicable |
| **Outdated versions** | Package version 2.4.2, Unity 2019.4, the `com.ourpalm.ilruntime` scoped registry |
| **Dead links** | 5 Wolai in-site page IDs; 13 Yuque links (one of which 5 pages cited as their only implementation reference); 25 externally hosted `cdn.nlark.com` images; 4 links to the official ILRuntime documentation; about 12 links pointing at the upstream `master` branch instead of this repository |
| **Broken formatting** | One malformed link caused by misplaced bold markers; the anchor-style TOCs on 20+ pages all break under a static site generator |
| **Names unusable in URLs** | Full-width `（）`, `：`, `、`, `【】`; `[ ]` breaks Markdown link parsing; trailing `-`; the `&` in `视频&demo`; mixed bracket styles in `UI工作流（UIPipeline)`; the misspelling `Jekins` |
| **Structural defects** | 19 zero-body index pages (21% of all nodes); 18 cases of "a page and a directory of the same name coexisting", which duplicate URLs; 4 empty pages; several truncated sections that have a heading but no content |
| **Content leakage** | The placeholder `typeof(没有标记修改字段)`, the illegal syntax `public TestWidow:Awindows<Props_TestView>`, duplicated and garbled sentences |
| **Scrambled numbering** | `1.DLL打包/1.脚本打包` numbers from 1 at both levels; every subdirectory restarts its own numbering; `Demo合集` skips numbers (0, 1, 4, 5) |
| **Export residue** | Many `\*\*` escaped-bold sequences, `&#x20;` HTML spaces and `&#xA;` newline entities in headings and link titles |
| **PM content mixed in** | `开发计划/**` sits at the same level as the product documentation |

## Content gaps

Pages identified during the migration but **not yet written**:

| Gap | Notes |
|-----|-------|
| `api/network-protobuf` | The Protobuf conventions (where the proto files live, the generated class paths, hotfix ownership, `MessageParser` usage, avoiding `map`) |
| Log persistence details | The original `日志系统-` is an empty page; the current content has been merged into [Service Container & Logging](../api/utils.md), but the export/view toolchain does not have a page of its own |
| Network protocol stack | The repository has `Assets/Code/Game/NetProtocol/Protobuf/` but no dedicated documentation page |
| Localisation (L2) | `L2Type` is only an enum and `L2Text` / `L2Image` are empty shells, so there is no usable documentation |
| FairyGUI integration | Would have to be rewritten if support is restored in the future (there is no integration today) |

## Related pages

- [Documentation Governance](../agent/doc-governance.md) — layering, boundaries and maintenance conventions
- [Structure Issues & Refactor Backlog](../architecture/refactor-backlog.md) — code structure to-dos
- [Agent](../agent/index.md)
