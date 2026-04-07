# DevOps CI

`DevOps/CI/` 下的公共规范、文档入口和模块索引统一维护在这里。

每次实现或修改 `DevOps/CI/` 内功能时，先读本文档，再读具体模块 README；如果改动涉及 TeamCity，再继续读 `.test-DevOps/README.md` 和 `.test-DevOps/teamcityskill/README.md`。

## 阅读顺序

1. 先读本文档，确认公共规范和验证要求。
2. 再读当前模块 README，确认模块职责、主流程、测试入口和模块补充约束。
3. 如果改动涉及 TeamCity DSL、构建命令、参数透传、CI 日志或远端执行验证，再读 `../../.test-DevOps/README.md` 和 `../../.test-DevOps/teamcityskill/README.md`。

## 公共规范

### 1. 注释与流程日志

- 代码注释要全，尤其是流程编排、CI 边界、宿主差异、失败兜底和回退策略。
- 流程型脚本必须保留清晰的阶段日志，让 CI 日志可以直接对照代码定位问题。
- 关键函数和关键分支优先解释“为什么这样做”，不要只写变量字面含义。

### 2. 测试分层

- 核心逻辑必须实现单元测试，至少覆盖成功路径、关键边界和主要失败路径。
- 只要模块存在真实执行链路，就必须提供可执行的 e2e / smoke / remote 测试；模块 README 里必须写清楚执行命令、前置条件和跳过条件。
- 逻辑修改后，README、测试断言、步骤日志和脚本行为必须保持一致。

### 3. TeamCity 验证

- 如果改动模块已经接入 TeamCity，或者本次修改会影响 TeamCity DSL、构建参数、脚本入口、步骤日志、输出目录、上传逻辑，就必须执行受影响的 TeamCity 测试。
- 验证时至少确认：任务最终状态、关键流程日志、产物路径或远端上传目录。
- TeamCity 执行与排查入口统一维护在 `../../.test-DevOps/README.md` 和 `../../.test-DevOps/teamcityskill/README.md`。

### 4. 文档拆分原则

- 公共规范只维护在本文档，不要在每个模块 README 里重复拷贝同一套规则。
- 模块 README 只保留模块职责、主流程、配置、测试命令、TeamCity 入口和模块特有约束。
- 新增 CI 模块时，必须同时补充：模块 README、单元测试、执行测试入口，以及本文档中的索引记录。

## 模块索引

| 目录 | 职责 | 说明文档 | 必跑验证 |
| --- | --- | --- | --- |
| `BuildTools/` | CI Python 脚本与公共模块目录 | `BuildTools/README.md` | 按子模块 README 执行对应 pytest、smoke test 与 TeamCity 验证 |
| `BuildTools/BuildClientPackage/` | Unity 母包构建入口 | `BuildTools/BuildClientPackage/README.md` | `python -m pytest DevOps/CI/BuildTools/tests/test_buildclientpackage_helpers.py DevOps/CI/BuildTools/tests/test_buildclientpackage_batchmode.py DevOps/CI/BuildTools/tests/test_buildclientpackage_main_flow.py -q`；若改动影响 TeamCity，再执行受影响的 TeamCity 构建 |
| `BuildTools/Common/` | 公共上传模块与配置解析 | `BuildTools/Common/README.md` | `python -m pytest -q DevOps/CI/BuildTools/tests/test_artifact_uploader.py`；若改动影响真实上传链路，再执行 remote smoke test |

## 变更检查清单

1. 先读公共规范和目标模块规范。
2. 改代码时补足流程注释和阶段日志。
3. 运行单元测试。
4. 运行执行测试、remote smoke test 或 TeamCity 验证。
5. 回写 README 中的行为说明、命令和约束，确保文档与实现一致。