---
description: "编辑仓库 Markdown、AGENTS.md、instruction 文件、skill 文档、模块规则或 README 时使用。涵盖文档类型分类、路由规则、维护策略和语言约定。"
applyTo: "**/*.md"
---

# 文档维护标准

本文件包含编辑任何 Markdown 文件时自动生效的规则。

## 文档类型分类

| 层级 | 类型 | 文件名 | 位置 | 触发方式 | 唯一职责 | 预算 | 退役条件 |
|------|------|--------|------|---------|---------|------|---------|
| L0 | Agent 全局入口 | `copilot-instructions.md` | `.github/` | 始终加载 | 范式、门禁、跨模块约束 | ≤200 行 | — |
| L1 | Instruction | `*.instructions.md` | `.github/instructions/` | `applyTo` 自动匹配 | 被 applyTo 命中文件的编码约束 | ≤150 行 | 对应模块移除 |
| L2 | Agent 轻入口 | `AGENTS.md` | 仓库根、package 根、独立子仓库根 | 始终加载 | 包架构理解、模块划分 | ≤50 行 | package 移除 |
| L3 | Skill | `SKILL.md` | `.github/skills/<name>/` | `description` 匹配 + 斜杠命令 | 可执行步骤、命令、脚本 | 快速参考 ≤200 行 | 工具弃用 |
| L4 | README | `README.md` | 任意目录 | 手动读取 | 人类可读的用法、示例、排障 | — | — |
| — | 模块深度规则 | `*.md` | `.github/talos-docs/modules/` | instruction 引用 | 行为矩阵、验收条件 | — | 模块移除 |
| L5 | 临时记忆 | `*.md` | `.agent_memory/` | 任务触发 | todolist、code smells | 任务结束清理 | 任务结束/合并 |

## 创建决策

```
要创建文档 →
├─ 全局工作链路/规范/完成检查？ → copilot-instructions.md
├─ 包架构理解？ → AGENTS.md（根/package 根/子仓库根，≤50 行）
├─ 编辑某类文件时的编码规范？ → .instructions.md（必须含 applyTo/description + 实质规则）
├─ 可执行操作步骤 + 脚本？ → SKILL.md（skills/<name>/ 目录）
├─ 深度模块规则/矩阵/验收条件？ → talos-docs/modules/<module>.md
├─ 用法/命令/示例/排障？ → README.md（就近放置）
├─ 临时任务状态/代码异味？ → .agent_memory/
└─ 以上都不是 → 不创建
```

## 放置规则

| 内容范围 | 放置位置 |
|---------|---------|
| 全局（跨所有模块） | `copilot-instructions.md` |
| 按文件路径触发 | `.github/instructions/<name>.instructions.md` |
| 按包架构 | `Packages/<name>/AGENTS.md` |
| 按任务/workflow | `.github/skills/<name>/SKILL.md` |
| 按模块深度规则 | `.github/talos-docs/modules/<module>.md` |
| 按目录用法 | 该目录的 `README.md` |
| 临时 | `.agent_memory/` |

**禁止创建的位置：**
- package 更深子目录创建 `AGENTS.md`
- 业务代码目录创建 `AGENTS.md` 或 `*.instructions.md`
- `.github/` 根目录创建新的 `.md`（除 copilot-instructions.md）

## 引用规则

### 正向引用（允许）

```
copilot-instructions.md
  └─ 引用 → .github/instructions/*.instructions.md      （全局规范引用）

.instructions.md
  ├─ 引用 → copilot-instructions.md                     （获取全局上下文）
  └─ 引用 → talos-docs/modules/<module>.md              （获取深度规则）

AGENTS.md (package 级)
  ├─ 引用 → copilot-instructions.md                     （获取全局上下文）
  └─ 引用 → talos-docs/modules/<module>.md              （获取模块细则）
```

### 禁止引用

```
❌ talos-docs/modules/*.md → 引用 AGENTS.md             （模块文档不反向引用包级文档）
❌ skill → 引用 instruction                             （skill 不依赖 instruction）
❌ .agent_memory/*.md → 引用永久文档                     （临时记忆不引用长期文档）
❌ README → 引用 instruction                            （README 是给人看的）
```

## 内容原则

### Instruction 必须

- 每条都是可直接执行的约束，不是「去别处找答案」
- 引用其他文档时，本文件已包含足够规则让 agent 独立工作
- 「需要深入理解时再看 X」是合法的——前提是本文件已有实质内容

### 不得出现

- 纯阅读顺序列表（"1. 读 A 2. 读 B 3. 读 C"）而无任何实质规则
- 把长命令手册复制进全局规则
- 把临时任务记录写进永久文档
- 新增模块规则却不更新引用它的 instruction
- 保留已废弃路由文件或旧路径

## 语言策略

- 一方工作流和规则文档以中文为主
- 已有英文通用包文档可保持英文，除非用户明确要求翻译
- 代码注释和 docstring 中文在前、中英双语
- 面向开发者的运行时和 CI 日志可使用中文

## 变更同步

入口、命令参数、BuildType ID、输出布局、上传协议、测试命令、CI 日志或模块归属变化时，必须在同一改动中更新所有受影响文档。

## DRY 与预算制

- 全局规则声明一次（copilot-instructions.md），各 instruction 只写偏差（无偏差不重复）
- 同一规则跨文件出现 3+ 次 → 合并到声明点
- instruction 超预算 → 拆分子模块或提取范式到 copilot-instructions.md
- 文档迁移后用 `rg` 检查被重命名或废弃的路径/术语

## 用户纠正处理

用户纠正 Agent 行为时：
- 先判断：需求误解、漏读规则、读错模块规则，还是文档确有缺口
- 只有纠正可复用、高风险或容易重复发生时，才新增或更新规则
- 模块专属行为优先更新最接近的 instruction
- 跨模块工作流才更新 `copilot-instructions.md`
- 不为一次性偏好或单次事故创建永久策略

## 命名约定

| 类型 | 格式 | 示例 |
|------|------|------|
| Instruction | `<scope>.instructions.md` | `bdframework.instructions.md` |
| Skill 目录 | `<name>/`，小写 + 连字符 | `teamcity/` |
| Skill 文件 | `SKILL.md`（固定） | `skills/teamcity/SKILL.md` |
| 模块文档 | `<module>.md` | `editor-pipeline.md` |
| AGENTS.md | `AGENTS.md`（固定） | 根、package 根 |

## 验证

- 文档迁移后用 `rg` 检查被重命名或废弃的路径/术语
- 文档类改动结束前运行 `git diff --check`
- TeamCity 或 BuildTools 文档变化时交叉检查 `.test-DevOps/README.md`、`skills/teamcity/SKILL.md` 和目标 BuildTools README
