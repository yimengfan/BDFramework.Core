---
description: "编辑 BDFramework 测试代码时使用。涵盖 Runtime.Test、Unity Test Framework、host E2E suite、测试放置和编写规范。"
applyTo: "Packages/com.popo.bdframework/Runtime.Test/**"
---

# 测试编码规范

## 测试放置

- 框架单元测试：`Runtime.Test/Runtime/`
- 框架 editor-only 测试：`Runtime.Test/Editor/`
- 框架宿主 E2E（含原 HostE2E）：`Runtime.Test/Runtime/E2E/`
- 业务测试：`Assets/Code/<Module>/Tests/` 或 `Assets/Code/<Module>@hotfix/Tests/`
- 业务自有 E2E：`Assets/Code/<Module>/E2E/`

## 编写规则

- 有明确被测源码时，测试文件遵循 `source-file-name + Test.cs`
- 遵循 copilot-instructions.md §2 测试策略：覆盖具体行为和失败路径，不能只验证"不抛异常"
- 需要在打包 player/device 上运行的 runtime-facing 测试必须放在 runtime-capable 程序集
- 如果已有 runtime 测试归属，不要继续堆积 editor wrapper
- 磁盘 IO 测试必须使用临时路径并清理
- 面向自动化和 BatchMode 的测试日志遵循 copilot-instructions.md §2 命名规则：包含中文 `测试目的=` 和 `实现手段=` 开始标记
- 测试 asmdef 引用保持最小，并与被测程序集边界一致

## 测试策略归属

本模块是测试框架本身，不归入测试金字塔层。修改测试框架代码时，用框架自带的测试验证（Runtime.Test 内自测）。

## 包通用规则

同时生效：`bdframework.instructions.md`（Editor/Runtime 隔离、asmdef 约定、命名、反射规则）。

## 验证

- 可行时运行目标 Unity 测试程序集
- BatchMode bridge 或 host E2E 变化时，运行文档记录的 BatchMode 入口
- 如果测试会在 debug package 验证中注入热更程序集，验证 debug-only 注入路径，并确认 release build 不包含测试程序集