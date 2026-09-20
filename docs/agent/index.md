# Agent 集成

本仓库同时服务**人**（文档站点）和 **AI Agent**（规则与 Skill）。本分区说明后者。

| 页面 | 内容 |
|------|------|
| [Skill 索引](skills.md) | 按模块拆分的 Agent Skill 集 |
| [文档治理](doc-governance.md) | 规则文档的分层、边界与维护约定 |

## 两层结构

```text
面向人                          面向 Agent
─────────────────────────      ────────────────────────────────────
docs/**  (本站)                  .github/copilot-instructions.md  ← L0 全局根规范
  ├─ guide/                      .github/instructions/*.md        ← L1 文件级约束
  ├─ architecture/               AGENTS.md (仓库根 / package 根)  ← L2 包架构
  ├─ uflux/ api/ pipeline/        .agents/skills/<name>/SKILL.md   ← Skill（按需加载）
  ├─ editor/ testing/            .agent_memory/**                 ← L3 临时记忆
  ├─ tutorials/
  └─ agent/
```

## 为什么分成两层

| 维度 | 面向人（docs） | 面向 Agent（.github） |
|------|--------------|---------------------|
| 目标 | 理解框架、学习用法 | 约束行为、保证质量门禁 |
| 组织 | 按主题（UI / 管线 / API） | 按触发路径（`applyTo` glob） |
| 加载 | 用户主动浏览 | 自动匹配 / 按需加载 |
| 变化频率 | 低 | 随规则演进 |
| 失败代价 | 看不懂 | 改坏代码 |

!!! note "两层的边界"
    - **能被执行**的规则（"必须补测试"、"不得修改第三方包"）→ `.github/`
    - **需要被理解**的知识（"UFlux 为什么这样分层"）→ `docs/`

    两者内容会重叠，但**职责不同**：`docs` 解释"是什么/为什么"，`.github` 约束"必须怎么做"。

## 快速导航

| 我想… | 去哪里 |
|------|--------|
| 让 Agent 帮我改 UI 代码 | [Skill 索引](skills.md#core-runtime) |
| 让 Agent 帮我写构建管线 | [Skill 索引](skills.md#build-ci) |
| 了解 Agent 的编码流程与门禁 | `.github/copilot-instructions.md` |
| 了解某个 package 的架构约定 | `Packages/<name>/AGENTS.md` |
| 了解规则文档该写在哪一层 | [文档治理](doc-governance.md) |

## 相关页面

- [文档治理](doc-governance.md)
- [Skill 索引](skills.md)
- [归档](../archive/index.md)
