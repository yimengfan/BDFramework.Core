---
description: "编辑 Packages/com.talosai.e2e 下的 Talos E2E 运行时、Editor bridge、Playwright 编排、文档或测试资源时使用。"
applyTo: "Packages/com.talosai.e2e/**"
---

# Talos E2E 编码规范

本文件包含编辑 `Packages/com.talosai.e2e/**` 下任何文件时自动生效的规则。包架构理解见 `Packages/com.talosai.e2e/AGENTS.md`。

## 职责边界（不可违背）

- Playwright 拥有主编排流程：启动应用、等待状态检查、打开场景、驱动 UI 动作及触发 Unity 侧测试
- Unity 拥有测试执行和结果交付：实现 Playwright 所需接口，运行测试，产出结果并返回
- Talos E2E 是能力包，不是宿主工作流包

## 禁止放入 E2E 包的内容

- 宿主启动流程、框架初始化、资源/数据库准备
- 场景顺序、executeMethod wrapper 或兜底恢复流程
- 业务方专属测试、配置、启动配方或硬编码逻辑

以上应在宿主或业务包中定义，让 Talos 通过通用 connector 或显式宿主入口消费。

## 分层规则

- 编排逻辑：`Playwright~/src/`、`Playwright~/tests/`、`Playwright~/tools/`
- Unity Runtime：`Runtime/`
- Unity Editor：`Editor/`
- 只有需要 Unity API 行为细节时才读取 `Documentation~/`

## TCP 协议

- 字段、消息类型、重连行为或命令契约变化时，Unity 与 Playwright 两侧必须同步更新
- 新增 Unity 侧 editor 命令前，优先使用 Unity 官方 API 和缓存反射网关

## 验证入口

- Runtime-complete batchmode：`Playwright~/tools/test-batchmode.sh`
- Editor-command / scene-control / PlayMode-control：`Playwright~/tools/test-editorplayer.sh`
- 设备/player 平台：`Playwright~/tools/test-android.sh` 或 `Playwright~/tools/test-pc.sh`

## 命名约定

- 跨平台 Playwright 测试：`description-e2e.spec.ts`
- 平台后缀：`EditorPlayer`、`Android`、`Windows`、`MacOS`
- 该包内 Markdown 文档可保持英文；用户明确要求时可翻译

## 测试策略归属

归属 E2E 层：Playwright 编排、TCP 协议通信、设备/编辑器端到端流程。内部工具函数通过 Playwright~/tools/tests/ 单元测试覆盖。

## 日志规范

- 静态 editor-only 运行可能出现 step-screenshot warning；除非测试明确要求截图成功，否则视为能力限制
