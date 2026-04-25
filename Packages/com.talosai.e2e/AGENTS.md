# Talos E2E 包级 Agent 规则

作用域：`Packages/com.talosai.e2e/**`。

本文件承载 Talos E2E package 根级规则。不要在本 package 更深目录下新增 `AGENTS.md`。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. 本文件
3. 正在修改的 `Runtime/`、`Editor/`、`Playwright~/` 或测试文件
4. 只有需要 Unity API 细节时才读取 `Documentation~/`

## 不可违背规则

- Playwright 拥有主编排流程，负责启动应用、等待状态检查、打开场景、驱动 UI 动作以及触发 Unity 侧测试。
- Unity 拥有测试执行和结果交付。Unity 代码实现 Playwright 所需接口，运行测试，产出结果并返回。
- Talos E2E 是能力包，不是宿主工作流包。
- 不要在该包内编排宿主启动、框架初始化、资源/数据库准备、场景顺序、executeMethod wrapper 或兜底恢复流程。
- 如果场景依赖项目场景、配置、资产、manager 或业务启动顺序，应在宿主或业务包中定义流程，让 Talos 通过通用 connector 或显式宿主入口消费。
- 不要在这里加入业务方专属测试、配置、启动配方或硬编码逻辑。

## 架构边界

- 编排逻辑放在 `Playwright~/src/`、`Playwright~/tests/` 和 `Playwright~/tools/`。
- Unity runtime 和 editor 职责分别放在 `Runtime/` 和 `Editor/`。
- 只有需要 Unity API 行为细节时，才把 `Documentation~/` 当作本地 Unity API 参考。
- TCP 协议字段、消息类型、重连行为或命令契约变化时，Unity 与 Playwright 两侧必须同步更新。
- 新增 Unity 侧 editor 命令前，优先使用 Unity 官方 API 和缓存反射网关。

## 命名与验证

- 该包内 Markdown 文档可以保持简洁英文；用户明确要求时可翻译。
- 跨平台 Playwright 测试命名使用 `description-e2e.spec.ts`。
- 平台后缀使用 `EditorPlayer`、`Android`、`Windows` 和 `MacOS`。
- Runtime-complete batchmode 验证使用 `Playwright~/tools/test-batchmode.sh`。
- `Playwright~/tools/test-editorplayer.sh` 只用于 editor-command、scene-control 和 PlayMode-control 流程。
- 设备/player 平台流程使用 `Playwright~/tools/test-android.sh` 或 `Playwright~/tools/test-pc.sh`。
- 静态 editor-only 运行可能出现 step-screenshot warning。除非测试明确要求截图成功，否则将其视为能力限制。
