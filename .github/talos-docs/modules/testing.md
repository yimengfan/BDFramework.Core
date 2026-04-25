# BDFramework 测试模块规则

作用域：`Packages/com.popo.bdframework/Runtime.Test/**`、`Packages/com.popo.bdframework/Runtime.HostE2E/**`、包测试 asmdef、Unity Test Framework 覆盖和框架自有 host E2E suite。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. `Packages/com.popo.bdframework/AGENTS.md`
3. 本文件
4. 被测源码和同相对模块下的既有测试

## 测试放置

- 框架单元测试：`Packages/com.popo.bdframework/Runtime.Test/Runtime/`。
- 框架 editor-only 测试：`Packages/com.popo.bdframework/Runtime.Test/Editor/`。
- 框架自有 host E2E：`Packages/com.popo.bdframework/Runtime.HostE2E/`。
- 业务测试：`Assets/Code/<Module>/Tests/` 或 `Assets/Code/<Module>@hotfix/Tests/`。
- 业务自有 E2E：`Assets/Code/<Module>/E2E/`。

## 规则

- 有明确被测源码时，测试文件遵循 `source-file-name + Test.cs`。
- 测试必须断言具体行为和预期失败路径，不只验证“不抛异常”。
- 需要在打包 player/device 上运行的 runtime-facing 测试必须放在 runtime-capable 程序集。
- 如果已有 runtime 测试归属，不要继续堆积 editor wrapper。
- 磁盘 IO 测试必须使用临时路径并清理。
- 面向自动化和 BatchMode 的测试日志必须包含中文 `测试目的=` 和 `实现手段=` 开始标记。
- 测试 asmdef 引用保持最小，并与被测程序集边界一致。

## 验证

- 可行时运行目标 Unity 测试程序集。
- BatchMode bridge 或 host E2E 变化时，运行文档记录的 BatchMode 入口。
- 如果测试会在 debug package 验证中注入热更程序集，验证 debug-only 注入路径，并确认 release build 不包含测试程序集。
