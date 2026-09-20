# BDFramework 仓库 Agent 入口

本仓库使用 `.github/copilot-instructions.md` 作为唯一全局规则入口。

阅读顺序：

1. 读取 `.github/copilot-instructions.md`（全局工作链路和规范）。
2. 编辑代码时，对应 `.github/instructions/*.instructions.md` 通过 `applyTo` 自动加载。
3. 处理**特定模块**的开发任务时，加载 `.agents/skills/bdframework-*/SKILL.md`（按需，见下表）。
4. 包架构理解见各 package 根 `AGENTS.md`。
5. 只读取命中的规则文件和附近实现/测试文件。

不要把本文件当作完整规则副本。强制工作链路、全局规范、完成前检查、todolist 和 code smell 规则维护在 `.github/copilot-instructions.md`。文件/模块级编码规范维护在 `.github/instructions/`；package 根 `AGENTS.md` 承载包级规则；package 更深子目录不要新增 `AGENTS.md`。

## 模块 Skill 路由

按任务类型选择 Skill（详见 `docs/agent/skills.md`）：

| 任务 | Skill |
|------|-------|
| 启动链路、配置中心、日志、协程、路径 | `.agents/skills/bdframework-bootstrap/SKILL.md` |
| 窗口、组件、子窗口、RenderData、Reducer/Store、DI | `.agents/skills/bdframework-uflux/SKILL.md` |
| 屏幕导航、事件总线、ManagerBase、服务容器 | `.agents/skills/bdframework-uimanager/SKILL.md` |
| 资源加载、双寻址、热更下载、对象池 | `.agents/skills/bdframework-resources/SKILL.md` |
| 表格查询、双库模型、导表 | `.agents/skills/bdframework-sqlite/SKILL.md` |
| 构建热更 DLL / AB / 表格 / 母包、发布、管线钩子 | `.agents/skills/bdframework-build-pipeline/SKILL.md` |
| BatchMode 入口、CI 参数、Python 工具、TeamCity | `.agents/skills/bdframework-ci/SKILL.md` |
| TeamCity API 操作 | `.github/skills/teamcity/SKILL.md` |

模块知识 Skill 统一放 `.agents/skills/<name>/`（VS Code 官方 Agent Skills 约定，跨工具通用）。
`teamcity` 保留在 `.github/skills/`：`.test-DevOps` 子模块（独立仓库）把它作为路径契约引用，迁移会破坏跨仓库引用。

## 面向人的文档

`docs/**` 是发布到 GitHub Pages 的功能文档（结构、模块地图、API 参考、教程），与 Agent 规则分层维护。两层关系与边界见 `docs/agent/doc-governance.md`。

文档改动后必须跑 `mkdocs build --strict`（会把断链升级为错误）。
