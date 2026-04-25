# Talos Agent 规则目录

`.github/talos-docs` 是本仓库跨包和细分模块 Agent 规则的集中维护目录。package 根级规则直接放在对应 package 根目录的 `AGENTS.md`。

## 阅读方式

1. 从 `.github/copilot-instructions.md` 开始。
2. 根据其中的模块路由表，选择 `modules/` 下最小命中的规则文件。
3. 只有选中的模块规则要求时，才继续读取模块 README、附近源码和附近测试。
4. 修改长期规则或 Markdown 文档前，先读取 `documentation-maintenance.md`。

## 目录结构

- `modules/`：主仓库跨包或细分模块规则。
- `documentation-maintenance.md`：文档维护规则，以及用户纠正何时应沉淀为可复用规范的判断标准。

## 职责边界

- `.github/copilot-instructions.md`：强制工作链路、公共策略和模块路由。
- `.github/talos-docs/modules/*.md`：跨包或细分模块专属规则。
- `Packages/<package>/AGENTS.md`：package 根级规则。
- README：命令、示例、配置、排障和长篇使用说明。
- `.github/skills/**`：工具专属操作契约。
- `.agent_memory/**`：临时任务状态和 code smell 追踪。

不要在主仓库 package 更深子目录或业务代码子目录新增模块 `AGENTS.md`。跨包/细分模块规则集中维护在这里。允许的 `AGENTS.md` 例外是：根目录轻入口、package 根包级规则入口（例如 `Packages/com.popo.bdframework/AGENTS.md`、`Packages/com.talosai.e2e/AGENTS.md`）以及独立子仓库（例如 `.test-DevOps`）。
