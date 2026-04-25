# BDFramework 仓库 Agent 入口

本仓库使用 `.github/copilot-instructions.md` 作为唯一全局规则入口。

阅读顺序：
1. 读取 `.github/copilot-instructions.md`。
2. 根据其中的模块路由表，决定需要读取哪个 `.github/talos-docs/modules/*.md`、README、instruction 文件或 skill 文件。
3. 只读取命中的本地规则和附近实现/测试文件。

不要把本文件当作完整规则副本。强制工作链路、完成前检查、TeamCity 纪律、todolist、code smell 和模块路由都维护在 `.github/copilot-instructions.md`。跨包/细分模块规则维护在 `.github/talos-docs/`；package 根 `AGENTS.md` 承载包级规则，package 更深子目录不要新增 `AGENTS.md`。
