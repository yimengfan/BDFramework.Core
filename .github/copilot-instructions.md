# BDFramework Agent 强制规范

本文件是本仓库唯一的全局 Agent 入口，负责强制工作链路、公共规范与模块路由。集中模块规则放在 `.github/talos-docs/`；README 与 skill 文档只在路由命中后提供详细用法。

## 1. 强制工作链路

除非用户明确要求只读分析或禁止某个阶段，否则所有实现类任务都必须按下面链路推进。每个阶段都是门禁；前一个必需门禁没有通过，不得跳到后一个门禁，也不得把任务标记为完成。

整体链路：

`需求分析 -> 模块路由 -> 编码方案 -> 编码施工 -> 补全测试 -> 本地测试通过 -> commit/push -> TeamCity 远端验证通过 -> 完成前检查 -> 带证据汇报`

1. **需求分析门禁**
   - 明确用户要交付的结果、限制条件、验收方式和不应触碰的范围。
   - 编辑前检查当前工作树和已有脏文件。
   - 不回滚、不覆盖、不提交与当前任务无关的用户改动。
   - 如果需求、权限或验收条件存在高风险歧义，先用最少问题确认；可以合理假设时继续推进并在结果中说明。

2. **模块路由门禁**
   - 使用本文件的模块路由表判断受影响模块。
   - 只读取命中的 `.github/talos-docs` 模块规则、package 根 `AGENTS.md`、附近 README、附近测试和实现文件。
   - 默认不要加载所有模块文档。
   - 在动手前判断影响范围：业务逻辑、测试、构建工具链、CI/CD、UI 框架、资源加载、Talos E2E、TeamCity DSL 或文档维护。

3. **编码方案门禁**
   - 先确定最小实现路径、需要补的测试、需要跑的本地验证和可能需要触发的 TeamCity buildType。
   - 如果任务多步骤、CI 耗时长或跨会话，先读取并维护 `.agent_memory/todolist.md`。
   - 发现当前编译链外的代码异味时，按 `.agent_memory/code_smells.md` 规则记录，不扩大本次改动。

4. **编码施工门禁**
   - 改动只覆盖用户请求和受影响编译链。
   - 优先复用现有模式、工具函数、asmdef 边界和已记录入口。
   - 行为、入口、日志、公开契约或配置变化时，同步更新测试与必要文档。
   - 不把临时调试、一次性脚本或本地机器状态提交成正式行为。

5. **补全测试门禁**
   - 每条新增或变更代码路径都必须新增或更新最近的自动化测试。
   - 优先补单元测试；跨模块、跨运行时、资源更新、构建或启动流程变化时，补集成、BatchMode、smoke 或 E2E 验证。
   - 测试要覆盖具体行为和失败路径，不能只验证“不抛异常”。
   - 如果确实无法补自动化测试，必须说明原因，并给出最接近的替代验证。

6. **本地验证门禁**
   - 按命中模块运行最近的单元测试、pytest、Unity Test Framework、BatchMode、dry-run 或 smoke test。
   - 本地验证失败时，回到编码施工或补全测试阶段修复，直到相关本地验证通过。
   - 如果推荐检查无法运行，记录原因，并执行最接近的可行验证；不能把“未运行”伪装成“通过”。

7. **提交推送门禁**
   - 端到端实现任务默认在本地验证通过后提交并推送；用户明确禁止提交/推送时除外。
   - 提交前复查 diff，只暂存当前任务相关文件。
   - 不要把无关脏文件放进提交。
   - `.test-DevOps` 是独立仓库；TeamCity DSL 或 Versioned Settings 变化时需要在该仓库单独提交并推送。
   - 记录主仓库和必要子仓库的 commit SHA。

8. **TeamCity 远端验证门禁**
   - 改动影响代码、BuildTools、TeamCity DSL、资源上传、母包构建、Talos E2E、CI 日志、启动链路或设备/Player 行为时，必须触发受影响 TeamCity 构建。
   - 远端验证只跑本次影响相关的最小必要 buildType；涉及设备/Player/E2E/资源更新时，必须覆盖对应远程真机或 Player 用例。
   - 触发 TeamCity 前确认相关本地验证已通过，且 TeamCity checkout 能拿到已推送 commit。
   - TeamCity 命令和排障细节以 `.github/skills/teamcity/SKILL.md` 为准。
   - 不要假设自动触发已经足够；必须主动触发、等待完成并记录 build ID、URL 和状态。
   - **制品验证要求**：构建 SUCCESS 不等于验证通过。必须检查以下至少一项证据，确认构建实际执行了编译/打包/测试，而非仅完成 checkout 后空跑退出：
     - 构建日志中包含编译步骤输出（例如 Tundra/Csc 编译记录、`items updated` 大于 0、受影响程序集名称出现）。
     - 构建日志中包含目标 ExecuteMethod 的执行记录和正常退出。
     - 制品上传步骤完成且 `integrity=verified`，上传文件数和字节数符合预期。
     - 构建时长明显异常（例如热更代码构建不到 30 秒）时，必须下载日志确认原因（增量编译命中缓存是正常原因，但需有 Tundra 输出佐证）。
     - 如果 buildType 设计为只产出日志不产出部署制品（例如 BuildCode），则以日志中的编译输出和 upload 步骤的 `integrity=verified` 为准。

9. **失败回环门禁**
   - 本地或远端验证失败时，先分析根因，再回到对应阶段修复。
   - 修复代码后补齐或修正测试，重跑相关本地验证，通过后再提交/推送并重跑受影响 TeamCity。
   - 长任务、失败根因和下一步必须同步更新 `.agent_memory/todolist.md`。
   - 必需检查仍失败、状态未知或没有证据时，不得标记任务完成。

10. **纠正反馈复盘门禁**
   - 用户纠正 Agent 行为时，先判断问题来自需求误解、漏读已有规则、读错模块规则，还是全局/模块文档确有缺口。
   - 如果已有规则已覆盖该纠正，应按已有规则执行，并在有帮助时简要说明漏读了哪条规则。
   - 如果纠正暴露出可复用、高风险或容易重复发生的缺口，且用户期望继续实现，应在同一任务中更新合适的全局或模块规范。
   - 不要把一次性偏好、临时任务细节或很窄的单次事故写成永久规范。
   - 对用户只汇报结论和文档更新，不展开内部推理过程。

11. **完成收口门禁**
   - 逐项对照“完成前检查列表”；必需项未满足时，回到对应门禁处理。
   - 汇报改动范围、本地测试结果、适用时的 commit SHA、远端 build ID/URL/status 和制品验证证据（编译输出摘要、上传文件数/字节数/integrity）、未运行项原因和剩余风险。
   - 如果某项检查不适用或无法运行，必须说明原因和已执行的替代验证。

## 2. 模块路由

先读 `.github/copilot-instructions.md`，再按下表读取最小命中的本地规则。多个模块同时命中时，读取所有相关规则。

| 目标 | 命中条件 | 必读规则 | 主要验证 |
| --- | --- | --- | --- |
| 业务代码 | `Assets/Code/**`、游戏界面、玩法、热更业务流程 | `.github/talos-docs/modules/business-code.md`、附近源码/测试 | `Assets/Code/<Module>/Tests/` 下的业务测试，相关 Unity 测试或 Talos host flow |
| BDFramework 包 | `Packages/com.popo.bdframework/**` | `Packages/com.popo.bdframework/AGENTS.md` | Unity Test Framework、包内 BatchMode、附近测试 |
| UI 框架 | `Packages/com.popo.bdframework/Runtime/UI/**`、`Runtime/ScreenNavigation/**`、窗口/导航/状态框架 | `.github/talos-docs/modules/ui-framework.md`、包规则 | UI/window 测试；启动或导航流程变化时跑 host E2E |
| 资源加载 | `Runtime/AssetsManager/**`、资源版本/更新/加载协议 | `.github/talos-docs/modules/resource-loading.md`、包规则 | AssetsManager 测试、资源更新 BatchMode、服务器协议变化时跑 ClientRes 验证 |
| 构建业务 | `Editor/EditorPipeline/**`、PublishPipeline、BuildHotfix、BuildAssetBundle、BuildTable、母包构建入口 | `.github/talos-docs/modules/editor-pipeline.md`；面向 CI 时读 CI 文档 | Unity BatchMode bridge 测试；面向 CI 时跑 BuildTools pytest/dry-run |
| 框架测试 | `Runtime.Test/**`、包测试 asmdef | `.github/talos-docs/modules/testing.md`、包规则 | 目标 Unity 测试程序集；相关时跑 BatchMode |
| DevOps CI | `DevOps/CI/**`、`Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/**`、BuildTools Python、上传 helper、文件服务器配置、pytest | `.github/talos-docs/modules/devops-ci.md`、`DevOps/CI/README.md`、`Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/README.md`、目标模块 README | 目标 pytest、dry-run/smoke test；契约/日志/上传/DSL 变化时跑 TeamCity |
| TeamCity DSL | `.test-DevOps/.teamcity/**`、buildType 映射、Versioned Settings | `.test-DevOps/AGENTS.md`、`.github/skills/teamcity/SKILL.md` | Maven DSL 生成、TeamCity REST 检查、受影响远端构建 |
| Talos E2E 包 | `Packages/com.talosai.e2e/**` | `Packages/com.talosai.e2e/AGENTS.md` | `Playwright~/tools/test-batchmode.sh`、`test-editorplayer.sh`、相关平台工具；Runtime 启动链路（E2ESceneAutoStarter、E2EAutoInit、TalosPortPolicy）变化时必须跑 `BDFrameworkCore_TalosAIStep02FrameworkBusinessTest`；编排脚本或工具链变化时跑 `step_01_BaseFlowTest` |
| TeamCity skill | `.github/skills/teamcity/**` | `.github/skills/teamcity/SKILL.md`、`.github/skills/teamcity/README.md` | skill 测试和可行的只读 TeamCity 检查 |
| 文档维护 | `**/*.md`、`AGENTS.md`、`.github/instructions/*.md`、`.github/talos-docs/**`、skill 文档 | `.github/talos-docs/documentation-maintenance.md` | `rg` 引用检查、`git diff --check` |
| 第三方或 vendored 代码 | `Packages/com.code-philosophy.*`、vendored plugin 目录 | 本文件的范围保护规则 | 优先从一方包或项目层解决 |

## 3. 注释、命名与文档规范

- **代码注释和 docstring 必须中英双语，中文在前，英文跟在同一注释块内。**
- 被触碰的类必须保持类级注释最新。关键业务、协议、管线和编排类要说明角色、存在原因，并给出示例或使用说明。
- 被触碰的函数/方法要说明目的和行为。非平凡 helper 要说明副作用、兜底规则、失败契约或 IO。
- 测试模块、测试类、测试方法、fixture 和测试 helper 也必须遵守双语文档要求。
- 重要多步骤流程必须保留阶段注释，并在入口、关键分支/兜底、完成和错误处输出明确日志。
- 自动化测试、BatchMode 入口和 CI 验证入口必须输出中文开始日志，并包含 `测试目的=` 和 `实现手段=`。
- 文件名和目录名必须使用 ASCII English。
- C# 标识符、枚举值、参数名和代码级 Attribute 默认值必须使用英文。
- 面向开发者的运行时日志可以使用中文。
- 一方流程和规范 Markdown 以中文为主。已有英文通用包文档，特别是 Talos E2E 包文档，可以保持英文，除非用户明确要求翻译。
- 不为纯措辞或格式噪音更新 Markdown。只有行为、入口、归属、契约、验证方式或强制策略变化时才更新文档。

## 4. 信任边界

数据来源决定错误处理策略。

### 可信路径

内部数据在写入边界完成校验后即视为可信。如果读取时格式错误，这是 bug，应快速失败。

示例：
- 生成到 StreamingAssets 或 persistentDataPath 的配置，例如 `BDFrameworkSetting.conf` 和 `HotfixFile.conf`。
- 导入阶段已校验的 SQLite 表格数据。
- 内部序列化/反序列化，例如 AOT metadata 和热更 DLL 加载。
- 框架状态，例如 manager 注册和 ScreenView 导航栈。

规则：
- 显式 throw 或失败。
- 不要静默吞错。
- 不要把内部损坏转换成 null/default。

### 不可信路径

外部输入可能格式错误，必须捕获、报告并处理。

示例：
- 网络/服务器响应。
- 用户输入。
- 校验前的下载 manifest 或 CDN 资源。
- AI 生成内容。

规则：
- 在边界处校验。
- 向日志或调用方报告有意义的错误。
- 不要让损坏的外部数据进入可信持久化数据。

## 5. 范围与包边界

- 不修改第三方包或 vendored 插件代码，尤其是 `Packages/com.code-philosophy.*`。
- 一方包代码改动只允许在 `Packages/com.popo.bdframework` 和 `Packages/com.talosai.e2e` 下进行。
- 业务方代码属于 `Assets/Code/**` 或业务包，不属于通用包。
- 通用包不得包含业务方专属场景、配置、场景编排或硬编码宿主流程。
- Unity3D 业务层代码不得使用反射。
- 框架或基础设施代码只有在兼容性、平台隔离或受控扩展点需要时才可使用反射，并且必须在注释中说明原因。

- 测试程序集（`BDFramework.Test`）的 DLL 只能走热更路径、只在 Debug 模式生效，Release/Profiler 构建中 AOT 和热更产物均不得包含。Release 构建检测到泄漏必须抛异常中断，不得降级为警告。新增测试程序集时必须同步更新 `HotfixTestAssemblyInjector.TestAssemblyNames` 列表；新增构建入口时必须对 Release/Profiler 调用 `EnsureTestAssembliesRemoved()` 和 `ValidateNoTestAssembliesInOutput()`。详细行为矩阵和验收条件见 `.github/talos-docs/modules/editor-pipeline.md`。

## 6. 测试与验证策略

- 除非用户明确要求只改文档，否则每条新增或变更代码路径都必须新增或更新自动化测试。
- 优先补最近的单元测试；行为跨模块或跨运行时边界时，再补集成、BatchMode、smoke 或 TeamCity 验证。
- 测试必须验证具体行为，不能只验证“不抛异常”。
- 磁盘 IO 测试必须使用临时路径并清理。
- 需要在 player/device 上运行的 runtime-facing API 和集成测试必须放在 runtime-capable 测试程序集。
- 如果改动影响启动、资源更新、构建管线、母包构建或 CI 契约，必须同步验证相关文档、日志、本地测试和 TeamCity 入口。

## 7. TeamCity 执行纪律

所有 TeamCity Web API 操作都使用 TeamCity skill。

权威来源：
- `.github/skills/teamcity/SKILL.md`

长构建强制执行模式：
1. 通过 TeamCity skill 启动一个本地 helper 进程，例如 `run-build --wait`、`run-build-group --wait` 或 `run-talos-baseflow-chain`。
2. 等待责任属于这个 helper 进程；Agent 只使用当前终端/任务工具自带的进程等待能力等待该命令退出。
3. helper 进程退出后，再读取一次最终输出并汇报 build ID、URL、状态和失败日志摘要。

禁止模式：
- 不得由 Agent 自己实现 TeamCity 等待循环。
- 等待期间不得反复读取终端输出当作进度轮询。
- 不得使用 `pylanceRunCodeSnippet`、`time.sleep()`、shell/Python 循环、TeamCity REST 查询循环或 `tc_build_poller.sh` 作为外层等待链路。
- 不得在 `run-build --wait` 已经运行时并行启动第二套 TeamCity 轮询。
- 必需远端构建状态未知时，不得标记任务完成。

远端验证检查：
- 根据改动文件判断受影响 buildType。
- 远端运行前确认相关本地测试已通过。
- 确认主仓库改动已推送到 TeamCity checkout 的分支。
- Versioned Settings 或 DSL 变化时，确认 `.test-DevOps` 改动已推送。
- 触发时带有意义的 comment 和最小必要 tag。
- 汇报 build ID、状态和 URL。
- 构建完成后执行制品验证：下载构建日志确认编译步骤实际执行，制品上传 `integrity=verified`，构建时长无异常短截。只汇报 `status=SUCCESS` 而无制品证据时，不得标记远端验证通过。

## 8. 任务追踪

多步骤或跨会话任务使用 `.agent_memory/todolist.md`。

以下情况创建或更新：
- 任务有多个依赖步骤。
- CI/TeamCity 验证耗时长。
- 失败需要根因追踪。
- 用户要求一直处理到问题全部解决。

以下情况立即更新：
- 子任务完成或失败。
- 构建完成。
- 根因确认。
- 计划变化。

不要因为常规读文件或立即解决的临时失败更新 todolist。

必要内容：
- 当前任务摘要。
- 可用时记录分支、commit 或 build 证据。
- completed、failed、in-progress 和 next-action 状态。
- 失败的根因和下一步调查动作。

临时任务状态不要写入长期文档。只有当经验会改变未来行为时，才沉淀到相关规则文件。

## 9. 代码异味追踪

发现问题不等于立刻修复问题。

规则：
- **当前编译链内：** 如果阻塞编译/测试或属于当前改动范围，就修。
- **当前编译链外：** 记录到 `.agent_memory/code_smells.md`，不要扩大 PR。

追踪格式：

```markdown
# Code Smell 追踪

| # | 文件路径 | 行号 | 违反规则 | 简述 |
|---|---|---|---|---|
| 1 | Runtime/Core/Example.cs | L42 | 静默吞错误 | catch 块为空，异常被吞 |
```

规则：
- 最多保留 10 条。
- 按严重程度排序。
- 修复后删除对应条目。
- 如果存在条目，任务结束前说明剩余数量。

优先级示例：
- 高：静默 catch、可信路径优雅降级、用默认值隐藏数据损坏。
- 中：命名误导、Editor/Runtime 职责泄漏。
- 低：死代码、未使用 using、局部风格漂移。

## 10. 文档维护

编辑规则或 Markdown 文档前先读 `.github/talos-docs/documentation-maintenance.md`。

职责：
- `.github/copilot-instructions.md`：强制工作链路、公共规则和模块路由。
- `.github/talos-docs/modules/*.md`：集中跨包/细分模块规则。package 根 `AGENTS.md` 承载对应 package 的包级规则，不要在 package 更深子目录新增 `AGENTS.md`。
- README：详细用法、命令、配置、示例和排障。
- Skill 文档：工具专属命令契约和操作流程。
- `.agent_memory/**`：只存临时任务状态和代码异味追踪。

入口、命令参数、BuildType ID、输出布局、上传协议、测试命令或模块归属变化时，必须在同一改动中更新所有受影响文档。

用户纠正暴露文档缺口时：
- 模块专属行为优先更新 `.github/talos-docs/modules/` 下最接近的集中模块文件。
- 只有跨模块工作流或全仓库策略才更新 `.github/copilot-instructions.md`。
- 命令用法、示例、排障和操作细节更新 README 或 skill 文档。
- 新规则必须短、可复用、可验证；不要为一次性纠正增加臃肿策略。

避免：
- 在全局规则里复制很长的命令手册。
- 把临时任务记录写进永久文档。
- 新增模块规则却不更新上面的模块路由表。
- 在 package 更深子目录或业务代码子目录新增主仓库模块 `AGENTS.md`。允许的例外是根轻入口、package 根包级规则入口、独立子仓库如 `.test-DevOps`。
- 保留已废弃路由文件或旧路径引用。

## 11. 完成前检查列表

完成前检查列表是第 1 节“强制工作链路”的闭环表。每个实现类任务在最终回复前必须逐项确认；不适用项必须能说明原因，必需项未满足时回到对应门禁继续处理。

**需求分析与路由**

- [ ] 已明确交付物、验收方式、限制条件和不应触碰的范围。
- [ ] 已检查工作树，识别并保护无关脏文件。
- [ ] 已根据模块路由表读取最小必要规则，没有默认加载全部模块文档。
- [ ] 已判断影响范围是否触及业务、测试、构建工具链、CI/CD、UI、资源加载、Talos E2E、TeamCity DSL 或文档维护。

**编码与测试**

- [ ] 改动保持最小，只覆盖用户请求和受影响编译链。
- [ ] 未回滚、覆盖、格式化或提交无关用户改动。
- [ ] 代码注释/docstring 符合中文在前的双语规则。
- [ ] 命名、路径和代码级标识符使用 ASCII English。
- [ ] 新增或变更行为已补最近的自动化测试，或已说明无法补测试的原因与替代验证。
- [ ] 需要同步的 docs、README、skill 文档、日志和测试已更新。

**本地验证**

- [ ] 已运行命中模块要求的最近本地测试、pytest、Unity Test Framework、BatchMode、dry-run 或 smoke test。
- [ ] 本地验证全部通过；如果无法运行，已说明原因并执行最接近的可行验证。
- [ ] 本地失败已完成根因分析、修复、补测和重跑，没有把失败状态带到后续门禁。

**提交与推送**

- [ ] 端到端实现任务在本地验证通过后已提交并推送，或用户明确禁止提交/推送并已说明。
- [ ] 提交只包含当前任务相关文件，没有纳入无关脏文件。
- [ ] 涉及 `.test-DevOps` 时，独立仓库改动已单独提交并推送。
- [ ] 已记录必要的主仓库和子仓库 commit SHA。

**TeamCity 远端验证**

- [ ] 已根据改动文件判断受影响 buildType 和是否需要远程真机/Player/E2E 用例。
- [ ] 需要远端验证时，已使用 TeamCity skill 主动触发本次影响相关的最小必要构建。
- [ ] 涉及设备、Player、E2E 或资源更新时，远端验证已覆盖对应真机或 Player 用例。
- [ ] TeamCity 构建已等待完成并通过；已记录 build ID、URL 和状态。
- [ ] 已执行制品验证：构建日志确认编译步骤执行、制品上传 integrity=verified、构建时长无异常短截；只汇报 status=SUCCESS 而无制品证据时不得标记通过。
- [ ] 如果 TeamCity 失败，已回到编码/测试/本地验证/提交阶段修复并重跑；状态未知时没有标记完成。

**收口与记忆**

- [ ] 任务期间的用户纠正已检查是否漏读规则或暴露可复用文档缺口。
- [ ] 使用过的 `.agent_memory/todolist.md` 和 `.agent_memory/code_smells.md` 反映真实当前状态。
- [ ] 最终回复包含改动范围、本地验证结果、适用时的 commit/TeamCity 证据、未运行项原因和剩余风险。
