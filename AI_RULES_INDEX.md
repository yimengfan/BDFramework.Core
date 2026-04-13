# AI Rules Index

本文件维护仓库内 AI 规则入口、作用域和阅读顺序，避免模块规则散落后发生遗漏、重复维护或作用域误判。

## 全局阅读顺序

1. 先读 `.github/copilot-instructions.md`，确认仓库级强制规范。
2. 再按目标路径匹配对应的模块 instructions。
3. 再读对应包级 `AGENTS.md`、README 与最近的测试/实现文件。
4. 如果改动涉及镜像规则同步，同时更新 `.github/copilot-instructions.md` 与 `Packages/com.popo.bdframework/.talos/AGENTS.md`。

## 模块索引

| 目标路径 | 模块 instructions | 包级补充 | 主要验证入口 |
| --- | --- | --- | --- |
| `Packages/com.popo.bdframework/**` | `.github/instructions/bdframework.instructions.md` | `Packages/com.popo.bdframework/AGENTS.md` | Unity Test Framework 与相关 BatchMode 验证入口 |
| `Packages/com.talosai.e2e/**` | `.github/instructions/e2e.instructions.md` | `Packages/com.talosai.e2e/AGENTS.md` | `Playwright~/tools/test-batchmode.sh`、`Playwright~/tools/test-editorplayer.sh` |
| `DevOps/CI/**` | `.github/instructions/ci.instructions.md` | `DevOps/CI/README.md` | 模块 README 指定的 pytest、smoke test 与 TeamCity 验证 |

## 镜像关系

- `.github/copilot-instructions.md` 与 `Packages/com.popo.bdframework/.talos/AGENTS.md` 是仓库级强制规范镜像，语义必须保持一致。
- `Packages/com.popo.bdframework/AGENTS.md` 只保留 BDFramework 包级补充规则，不再承载仓库级镜像内容。
- `Packages/com.talosai.e2e/AGENTS.md` 只保留 Talos E2E 包级补充规则；作用域和阅读顺序由 `.github/instructions/e2e.instructions.md` 负责绑定。