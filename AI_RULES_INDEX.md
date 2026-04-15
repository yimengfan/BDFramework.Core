# AI Rules Index

本文件维护仓库内 AI 规则入口、作用域和阅读顺序，避免模块规则散落后发生遗漏、重复维护或作用域误判。

## 全局阅读顺序

1. 先读 `.github/copilot-instructions.md`，确认仓库级强制规范。
2. 再按目标路径匹配对应的模块 instructions。
3. 再读对应包级 `AGENTS.md`、README 与最近的测试/实现文件。

## 模块索引

| 目标路径 | 模块 instructions | 包级补充 | 主要验证入口 |
| --- | --- | --- | --- |
| `Packages/com.popo.bdframework/**` | `.github/instructions/bdframework.instructions.md` | `Packages/com.popo.bdframework/AGENTS.md` | Unity Test Framework 与相关 BatchMode 驗证入口 |
| `Packages/com.talosai.e2e/**` | `.github/instructions/e2e.instructions.md` | `Packages/com.talosai.e2e/AGENTS.md` | `Playwright~/tools/test-batchmode.sh`、`Playwright~/tools/test-editorplayer.sh` |
| `DevOps/CI/**` | `.github/instructions/ci.instructions.md` | `DevOps/CI/README.md` | 模块 README 指定的 pytest、smoke test 与 TeamCity 验证 |