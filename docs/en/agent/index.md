# Agent

This repository serves both **humans** (the documentation site) and **AI agents** (rules and Skills). This section covers the latter.

| Page | Contents |
|------|----------|
| [Skill Index](skills.md) | The Agent Skill set, split by module |
| [Documentation Governance](doc-governance.md) | Layering, boundaries and maintenance conventions for rule documents |

## Two-layer structure

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

## Why two layers

| Dimension | Human-facing (docs) | Agent-facing (.github) |
|-----------|--------------------|-----------------------|
| Goal | Understand the framework, learn how to use it | Constrain behaviour, enforce the quality gates |
| Organisation | By topic (UI / pipeline / API) | By trigger path (`applyTo` glob) |
| Loading | The user browses actively | Automatic matching / on-demand loading |
| Change frequency | Low | Evolves with the rules |
| Cost of failure | You cannot understand it | You break the code |

!!! note "The boundary between the two"
    - Rules that **can be executed** ("you must add tests", "you must not modify third-party packages") → `.github/`
    - Knowledge that **needs to be understood** ("why UFlux is layered this way") → `docs/`

    The two overlap in content but have **different responsibilities**: `docs` explains "what / why", `.github` constrains "how it must be done".

## Quick navigation

| I want to… | Where to go |
|-----------|-------------|
| Have the agent help me change UI code | [Skill Index](skills.md#core-runtime) |
| Have the agent help me write a build pipeline | [Skill Index](skills.md#build-ci) |
| Understand the agent's coding workflow and gates | `.github/copilot-instructions.md` |
| Understand a package's architecture conventions | `Packages/<name>/AGENTS.md` |
| Understand which layer a rule document belongs in | [Documentation Governance](doc-governance.md) |

## Related pages

- [Documentation Governance](doc-governance.md)
- [Skill Index](skills.md)
- [Archive](../archive/index.md)
