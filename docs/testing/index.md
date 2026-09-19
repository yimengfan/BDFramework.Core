# 测试

BDFramework 有**两套并行的测试体系**，服务于不同目的。

| 体系 | 位置 | 框架 | 用途 |
|------|------|------|------|
| **NUnit 测试套件** | `Packages/com.popo.bdframework/Runtime.Test/` | Unity Test Framework (NUnit) | 框架 API 契约、E2E 流程、BatchMode 验证 |
| **自研精简单测** | `Assets/Code/BDFramework.UnitTest/` | 自研 `UnitTestAttribute` | **可在热更层执行**的测试 |

## 页面

| 页面 | 内容 |
|------|------|
| [测试体系总览](overview.md) | 两套体系的分工、放置规范、编写要求 |
| [Runtime 单元测试](runtime-tests.md) | `BDFramework.Test` / `BDFramework.EditorTest` 程序集与测试文件清单 |
| [BatchMode 与 CI 测试](batchmode-tests.md) | 命令行入口、pytest、构建管线验证 |
| [E2E（Talos）](e2e.md) | Talos E2E 编排与设备测试 |

## 快速命令

```bash
# Python 侧构建工具测试
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/ -q

# Unity BatchMode 纯逻辑验证
"$UNITY_PATH" -batchmode -quit \
  -projectPath "<repo>" \
  -executeMethod BDFramework.EditorTest.DevOps.PublishPipeLineCITest.RunBatchVerification \
  -logFile /tmp/PublishPipeLineCITest.log
```

!!! danger "BatchMode 必须带 `-quit`"
    本机 Unity 在 batchmode 主循环会因 License 签名校验崩溃。`-quit` 让 Unity 在进入主循环前退出。**不要用 `-force-open`**（会 Bus Error）。

## 日志规范

自动化测试与 BatchMode 入口的日志**必须**包含中文：

```text
测试目的=<做什么>
实现手段=<怎么做>
```

这是框架的强制约定，便于 CI 日志检索与失败归因。
