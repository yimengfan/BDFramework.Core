# 文档维护规则

作用域：Markdown 文档、`.github/instructions/*.md`、`.github/talos-docs/**`、skill 文档、README 和根 Agent 入口文件。

## 职责

- `.github/copilot-instructions.md`：强制工作链路、公共规则、模块路由、TeamCity 纪律摘要、todolist/code-smell 规则和完成前检查表。
- `.github/talos-docs/modules/*.md`：集中跨包/细分模块规则。只保留模块归属、边界、本地验证和模块专属易错点。
- `Packages/<package>/AGENTS.md`：package 根级规则。不要只做跳转映射。
- README：详细用法、命令、配置、示例、排障和模块映射。
- Skill 文档：工具专属命令契约、环境配置和操作排障。
- `.agent_memory/todolist.md`：只存临时任务状态。
- `.agent_memory/code_smells.md`：只存临时代码异味追踪。

## 语言策略

- 一方工作流和规则文档以中文为主。
- 已有英文通用包文档，特别是 Talos E2E 文档，可以保持英文，除非用户明确要求翻译。
- 代码注释和 docstring 仍按全局规则使用中文在前的中英双语。
- 面向开发者的运行时和 CI 日志可以使用中文。

## 维护规则

- 不再创建独立全局路由索引。模块路由归 `.github/copilot-instructions.md` 管理。
- 不在 package 更深子目录或业务代码目录新增主仓库模块 `AGENTS.md`。跨包/细分模块规则放在 `.github/talos-docs/modules/`。
- package 根 `AGENTS.md` 承载对应 package 的包级规则，可以引用 `.github/talos-docs` 中的细分模块规则，但不要只是简单跳转映射。
- 新增模块规则文件时，必须同步更新 `.github/copilot-instructions.md` 的模块路由表。
- 入口、命令参数、BuildType ID、输出布局、上传协议、测试命令、CI 日志或模块归属变化时，必须在同一改动中更新所有受影响 README、instruction、skill 文档和测试断言。
- 用户纠正 Agent 行为时，检查纠正是否来自需求误解、漏读已有规则、读错模块规则或真实文档缺口。
- 只有纠正具有可复用性、高风险或容易重复发生时，才新增或更新规则。模块专属行为优先更新最接近的集中模块规则；跨模块工作流才更新全局文档。
- 公共策略放全局文档，package 根级策略放 package 根 `AGENTS.md`，跨包/细分模块策略放 `.github/talos-docs/modules/`，命令手册放 README 或 skill 文档。
- 不要把很长的 TeamCity 命令手册复制进全局规则；链接到 `.github/skills/teamcity/SKILL.md`。
- 不要把一次性偏好、临时任务上下文或单次事故修复写成永久策略。
- 不要把当前任务进度、构建状态或一次性调查记录写进永久文档。
- 删除或废弃文件、路径、buildType 或流程步骤时，移除旧引用。

## 验证

- 文档迁移后，用 `rg` 检查被重命名或废弃的路径/术语。
- 文档类改动结束前运行 `git diff --check`。
- TeamCity 或 BuildTools 文档变化时，交叉检查 `.test-DevOps/README.md`、`.github/skills/teamcity/SKILL.md` 和目标 BuildTools README。
