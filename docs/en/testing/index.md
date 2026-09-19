# Testing

BDFramework has **two parallel testing systems**, serving different purposes.

| System | Location | Framework | Purpose |
|------|------|------|------|
| **NUnit test suite** | `Packages/com.popo.bdframework/Runtime.Test/` | Unity Test Framework (NUnit) | Framework API contracts, E2E flows, BatchMode verification |
| **In-house lightweight unit tests** | `Assets/Code/BDFramework.UnitTest/` | In-house `UnitTestAttribute` | Tests that **can execute in the hotfix layer** |

## Pages

| Page | Content |
|------|------|
| [Testing Overview](overview.md) | How the two systems divide the work, placement rules, authoring requirements |
| [Runtime Unit Tests](runtime-tests.md) | The `BDFramework.Test` / `BDFramework.EditorTest` assemblies and the test file inventory |
| [BatchMode & CI Tests](batchmode-tests.md) | Command line entry points, pytest, build pipeline verification |
| [E2E (Talos)](e2e.md) | Talos E2E orchestration and device testing |

## Quick commands

```bash
# Python 侧构建工具测试
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/ -q

# Unity BatchMode 纯逻辑验证
"$UNITY_PATH" -batchmode -quit \
  -projectPath "<repo>" \
  -executeMethod BDFramework.EditorTest.DevOps.PublishPipeLineCITest.RunBatchVerification \
  -logFile /tmp/PublishPipeLineCITest.log
```

!!! danger "BatchMode must be launched with `-quit`"
    On this machine Unity crashes in the batchmode main loop because of the licence signature check. `-quit` makes Unity exit before entering the main loop. **Do not use `-force-open`** (it causes a Bus Error).

## Logging convention

The logs of automated tests and BatchMode entry points **must** contain the Chinese markers:

```text
测试目的=<做什么>
实现手段=<怎么做>
```

This is a mandatory framework convention that makes CI log searching and failure triage easier.
