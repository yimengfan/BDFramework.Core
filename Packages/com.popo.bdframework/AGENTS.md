# BDFramework 包级 Agent 规则

作用域：`Packages/com.popo.bdframework/**`。

本文件承载 BDFramework package 根级规则。更细的模块编码规则通过 `.github/instructions/bdframework-*.instructions.md` 的 `applyTo` 自动加载。不要在本 package 更深目录下新增 `AGENTS.md`。

## 阅读顺序

1. `.github/copilot-instructions.md`（全局工作链路和规范）
2. 本文件（包架构）
3. 编辑特定模块时，对应的 `.github/instructions/bdframework-*.instructions.md` 通过 `applyTo` 自动加载
4. 附近 README、测试和实现文件

## 通用规则

- 只做最小必要改动，避免无关重构、格式化、重命名或 API 抖动。
- 始终考虑 Editor、Player、BatchMode/CI 三种行为。
- 使用明确的平台/运行时边界，例如 `#if UNITY_EDITOR`、`#if !UNITY_EDITOR`、`Application.isPlaying`、`Application.isEditor`、`Application.isBatchMode`。
- Runtime 代码放在 `Runtime/` 或 `Runtime.AOT/`；Editor-only 代码放在 `Editor/`。
- 不要把 Editor API 混入 Runtime 程序集。确需共存时，必须用条件编译隔离。
- 保持现有风格和命名。
- 新增抽象前，优先复用现有实现。
- 修改公共 API 前，检查调用链、序列化字段、Inspector 使用点和反射使用点。
- 涉及线程、文件 IO、持久化或生命周期的代码，必须考虑并发、重入、异常和释放。
- 不要把临时调试行为提交成正式行为。

## 测试规则

- 任何非样式行为变化都应新增或更新测试。
- 测试放在 `Runtime.Test/`，并尽量与被测模块保持相对目录一致。
- 需要在 player/device 上运行的 runtime-facing API 和集成测试必须放在 `Runtime.Test/Runtime/`。
- `Runtime.Test/Editor/` 只放 editor-only 工具测试或非主逻辑 BatchMode bridge。
- 优先使用具体断言，不要只断言“不抛异常”。
- 磁盘 IO 测试必须使用临时目录并清理。

## BatchMode 规则

- 自动化验证优先使用 Unity Test Framework + Unity BatchMode。
- BatchMode 代码不得依赖弹窗、人工点击、当前打开场景、Inspector 状态或隐藏本地缓存。
- BatchMode 失败时，优先检查编译错误、程序集缺失、测试未发现、项目锁、Unity 日志片段和退出码。

## 本地日志模块

修改 `Runtime/Utils/Logs/` 时，要整体看待 `BDebug`、`Editor_UnityLogHook`、`Persistence`、`PersistenceSettings`、`LogReader`、`LogCrypto` 和 `SerializedLogEntry`。

- Editor 下：`BDebug` 负责常规 Console 输出；序列化日志由 `Editor_UnityLogHook` 处理。
- Player/device 下：`BDebug` 负责二进制日志持久化，不依赖 `Editor_UnityLogHook`。
- Player 日志默认开启序列化，支持可选加密，`playerlogs/` 默认保留 20 份归档。
- 文件命名、格式头、记录结构、加密或清理策略变化时，必须同步更新写入、读取、导出和测试。
