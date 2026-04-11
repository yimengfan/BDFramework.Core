# DevOps CI

`AI_RULES_INDEX.md` 维护 `DevOps/CI/` 的 AI 模块规范索引、阅读顺序和验证入口；本文继续承担公共规范与模块目录说明。

`DevOps/CI/` 下的公共规范、文档入口和模块索引统一维护在这里。

每次实现或修改 `DevOps/CI/` 内功能时，先读本文档，再读具体模块 README；如果改动涉及 TeamCity，再继续读 `.test-DevOps/README.md` 和 `.github/skills/teamcity/README.md`。

## 阅读顺序

1. 先读本文档，确认公共规范和验证要求。
2. 再读当前模块 README，确认模块职责、主流程、测试入口和模块补充约束。
3. 如果改动涉及 TeamCity DSL、构建命令、参数透传、CI 日志或远端执行验证，再读 `../../.test-DevOps/README.md` 和 `../../.github/skills/teamcity/README.md`。

## 公共规范

### 1. 注释与流程日志

- 代码注释要全，尤其是流程编排、CI 边界、宿主差异、失败兜底和回退策略。
- 流程型脚本必须保留清晰的阶段日志，让 CI 日志可以直接对照代码定位问题。
- 关键函数和关键分支优先解释“为什么这样做”，不要只写变量字面含义。

### 2. 外部配置统一来源

- BuildTools 里的业务无关外部配置统一称为 external integration config，按用途拆成 external service config、external signing config、external test config。
- 文件服务器、CI 服务器、签名/证书元数据、remote smoke test 参数都必须写在 `DevOps/CI/BuildTools/buildtools.toml` 或 `buildtools.toml.example`，不能在脚本里散落新的配置源。
- Python 与 shell 入口读取这些配置时，必须复用 `DevOps/CI/BuildTools/Common/buildtools_config.py` 或其公共封装，不要在各模块再复制一份 TOML 解析逻辑。
- `.github/hooks/buildtools-config-guard.json` 会对 agent 的写操作做确定性拦截，禁止在 `DevOps/CI/BuildTools` 源码里重新引入 ad hoc TOML 解析或直接散读共享 config section。
- 变更 section 名称、键名、优先级或默认值时，必须同时更新代码、README、`buildtools.toml(.example)` 和 pytest。

### 3. 测试分层

- 核心逻辑必须实现单元测试，至少覆盖成功路径、关键边界和主要失败路径。
- 只要模块存在真实执行链路，就必须提供可执行的 e2e / smoke / remote 测试；模块 README 里必须写清楚执行命令、前置条件和跳过条件。
- 逻辑修改后，README、测试断言、步骤日志和脚本行为必须保持一致。

### 4. TeamCity 验证

- 如果改动模块已经接入 TeamCity，或者本次修改会影响 TeamCity DSL、构建参数、脚本入口、步骤日志、输出目录、上传逻辑，就必须执行受影响的 TeamCity 测试。
- 验证时至少确认：任务最终状态、关键流程日志、产物路径或远端上传目录。
- 通过 TeamCity Web API 或辅助脚本手动触发验证任务时，必须带上 `Comment` 和 `Tags`；`Comment` 至少要明确这次测试对应的目标 buildType。
- TeamCity 执行与排查入口统一维护在 `../../.test-DevOps/README.md` 和 `../../.github/skills/teamcity/README.md`。

### 5. 文档拆分原则

- 公共规范只维护在本文档，不要在每个模块 README 里重复拷贝同一套规则。
- 模块 README 只保留模块职责、主流程、配置、测试命令、TeamCity 入口和模块特有约束。
- 新增 CI 模块时，必须同时补充：模块 README、单元测试、执行测试入口，以及本文档中的索引记录。

### 6. 构建管线强制规范

- TeamCity DSL、Jenkinsfile、shell pipeline 之类的管线层只负责调度参数、任务依赖和执行入口，不承载业务构建逻辑。
- 具体业务实现统一落在 `DevOps/CI/BuildTools/` 下的 Python 脚本里；如果是新的构建类型，必须新建独立目录，不要继续把不同类型流程塞进旧目录。
- 同一种构建类型允许拆成多个平台脚本，但共享逻辑要尽量下沉到 `Common/` 或该类型目录内部的共享模块，避免复制粘贴长流程。
- 平台相关 checkout / `--project-dir` / 缓存根目录必须通过公共参数或运行时目录名推导，统一使用 `{platform}/{project-leaf}` 这类组合协议；禁止在 DSL、脚本和 README 里写死 `ios/BDFramework.Core` 这类具体项目名路径。
- Assetbundle 构建是当前唯一要求强制平台隔离 checkout 的任务类型：当 TeamCity 之类的宿主已经把工程 checkout 到平台目录时，Assetbundle BuildTools 必须直接复用该目录作为 Unity `-projectPath`；只有上游仍给出共享 checkout 时，才允许回退到 sibling worktree 来隔离 `Library/Temp`。其他构建或验证任务只有在任务文档明确声明缓存污染风险时才允许启用同类隔离。
- TeamCity DSL 变更后，必须把“目标 buildType 的 compatible agents 是否仍大于 0”作为远端验证前置检查；尤其不要为 `checkoutDir` 再引入只做路径拼装的自定义参数，以免 TeamCity 兼容性计算异常。
- 每次真实构建前，必须清理对应的 CI 输出目录，避免旧产物混入本次上传。推荐把隔离输出写到 `Library/CIOutputs/<build_kind>/...` 或模块 README 指定的专用目录。
- 任何上传目录命名、产物筛选规则、步骤日志或参数协议变化，都必须同步更新 README、pytest 断言和 TeamCity DSL，不能只改实现。

## 模块索引

| 目录 | 职责 | 说明文档 | 必跑验证 |
| --- | --- | --- | --- |
| `BuildTools/` | CI Python 脚本与公共模块目录 | `BuildTools/README.md` | 按子模块 README 执行对应 pytest、smoke test 与 TeamCity 验证 |
| `BuildTools/BuildClientPackage/` | Unity 母包构建入口 | `BuildTools/BuildClientPackage/README.md` | `python -m pytest DevOps/CI/BuildTools/tests/test_buildclientpackage_helpers.py DevOps/CI/BuildTools/tests/test_buildclientpackage_batchmode.py DevOps/CI/BuildTools/tests/test_buildclientpackage_main_flow.py -q`；若改动影响 TeamCity，再执行受影响的 TeamCity 构建 |
| `BuildTools/BuildClientResCode/` | 三端热更代码构建与上传入口 | `BuildTools/BuildClientResCode/README.md` | `python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q`；若改动影响 TeamCity，再执行 `ClientRes_Code` 相关任务 |
| `BuildTools/BuildClientResAssetbundle/` | 三端热更 Assetbundle 构建与上传入口 | `BuildTools/BuildClientResAssetbundle/README.md` | `python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q`；若改动影响 TeamCity，再执行 `ClientRes_Assetbundle` 相关任务 |
| `BuildTools/BuildClientResTable/` | 统一表格构建与上传入口 | `BuildTools/BuildClientResTable/README.md` | `python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q`；若改动影响 TeamCity，再执行 `ClientRes_Table` 相关任务 |
| `BuildTools/VerifyClientRes/` | 三端热更文件服务器下载验证入口 | `BuildTools/VerifyClientRes/README.md` | `python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_verify.py -q`；若改动影响 TeamCity，再在 `BDFramework.Core / TestPipeline / TestBuildPipeline_ClientRes` 下执行 `ClientRes_Verify` / `VerifyClientRes_*` 相关任务 |
| `BuildTools/Common/` | 公共上传模块与配置解析 | `BuildTools/Common/README.md` | `python -m pytest -q DevOps/CI/BuildTools/tests/test_artifact_uploader.py`；若改动影响真实上传链路，再执行 remote smoke test |

## 变更检查清单

1. 先读公共规范和目标模块规范。
2. 改代码时补足流程注释和阶段日志。
3. 运行单元测试。
4. 运行执行测试、remote smoke test 或 TeamCity 验证。
5. 回写 README 中的行为说明、命令和约束，确保文档与实现一致。