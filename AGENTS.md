# BDFramework 仓库 Agent 入口

本仓库使用 `.github/copilot-instructions.md` 作为唯一全局规则入口。

阅读顺序：

1. 读取 `.github/copilot-instructions.md`（全局工作链路和规范）。
2. 编辑代码时，对应 `.github/instructions/*.instructions.md` 通过 `applyTo` 自动加载。
3. 包架构理解见各 package 根 `AGENTS.md`。
4. 只读取命中的规则文件和附近实现/测试文件。

不要把本文件当作完整规则副本。强制工作链路、全局规范、完成前检查、todolist 和 code smell 规则维护在 `.github/copilot-instructions.md`。文件/模块级编码规范维护在 `.github/instructions/`；package 根 `AGENTS.md` 承载包级规则；package 更深子目录不要新增 `AGENTS.md`。
