---
description: "编辑 Packages/com.popo.bdframework 下的运行时、Editor、AOT、测试、asmdef、启动流程或资源更新逻辑时使用。"
applyTo: "Packages/com.popo.bdframework/**"
---

# BDFramework 包编码规范

本文件包含编辑 `Packages/com.popo.bdframework/**` 下任何文件时自动生效的编码规则。包架构理解见 `Packages/com.popo.bdframework/AGENTS.md`。

## Editor/Runtime 隔离

- Runtime 代码放在 `Runtime/` 或 `Runtime.AOT/`；Editor-only 代码放在 `Editor/`
- Runtime 程序集禁止引用 `UnityEditor`、禁止依赖 Editor 程序集
- Editor 可引用 Runtime，不可反向
- 确需共存时，必须用 `#if UNITY_EDITOR` / `#if !UNITY_EDITOR` 条件编译隔离
- 始终考虑 Editor、Player、BatchMode/CI 三种行为

## asmdef 管理

- 每个逻辑模块一个独立 asmdef，不合并无关模块
- 测试 asmdef（`.Test` 后缀）只引用被测模块 + 测试框架
- 新增 asmdef 前确认模块归属和现有边界

## 代码变更原则

- 新增抽象前，优先复用现有实现
- 修改公共 API 前，检查调用链、序列化字段、Inspector 和反射使用点
- 涉及线程、文件 IO、持久化或生命周期的代码，必须考虑并发、重入、异常和释放

## 测试策略归属

本包覆盖全部测试层：单元层（Runtime 核心契约）、集成层（Editor pipeline + 资源加载 + 启动链路）、E2E 层（Host E2E）、门禁层（Debug/Release 行为矩阵）。具体子模块归属见各 instruction。

## 模块细分规则

编辑特定子模块时，对应的 instruction 会通过 applyTo 自动加载：

- UI 框架 → `bdframework-ui.instructions.md`
- 资源加载 → `bdframework-resource.instructions.md`
- Editor Pipeline → `bdframework-editor-pipeline.instructions.md`
- 测试框架 → `bdframework-testing.instructions.md`

## 日志模块特殊规则

修改 `Runtime/Utils/Logs/` 时，必须整体看待 `BDebug`、`Editor_UnityLogHook`、`Persistence`、`PersistenceSettings`、`LogReader`、`LogCrypto` 和 `SerializedLogEntry`：
- Editor 下：`BDebug` 负责 Console 输出，序列化日志由 `Editor_UnityLogHook` 处理
- Player 下：`BDebug` 负责二进制日志持久化，不依赖 `Editor_UnityLogHook`
- 文件命名、格式头、记录结构、加密或清理策略变化时，必须同步更新写入、读取、导出和测试
