# DevOps CI 模块规则

作用域：`DevOps/CI/**`、BuildTools Python 模块、pytest 覆盖、Unity batchmode wrapper、制品上传 helper、文件服务器配置和面向 TeamCity 的 CI 流程。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. 本文件
3. `DevOps/CI/README.md`
4. `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/README.md`
5. 编辑代码前读取目标模块 README
6. 涉及 TeamCity API 或远端验证时读取 `.github/skills/teamcity/SKILL.md`

## 规则

- Python 注释和 docstring 必须中文在前、中英双语，遵守全局代码文档策略。
- 被触碰的 Python 模块必须保持模块 docstring 最新。
- 被触碰的 Python 类和非平凡函数必须说明角色、契约、兜底或副作用。
- 主 CI 流程必须集中在明确 coordinator 函数或入口脚本中，并带阶段注释和匹配阶段日志。
- 文件服务器、CI 服务器、签名/证书元数据、远端测试配置等业务无关外部配置必须放在 `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/buildtools.toml`，并通过 `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/Common/buildtools_config.py` 读取。
- 不要在 `Common/buildtools_config.py` 之外新增临时 TOML 解析或直接读取共享 config section。
- 构建参数、输出布局、上传协议、CI 日志或 TeamCity 契约变化时，必须同步更新代码、README 和 pytest 断言。
- 每条变更代码路径都必须新增或更新自动化测试。相关 pytest、smoke 和 TeamCity 验证必须通过后才算完成。

## 验证

- 共享验证策略以 `DevOps/CI/README.md` 为准。
- 精确 pytest、dry-run、smoke test 和 TeamCity 入口以目标模块 README 为准。
- TeamCity DSL、CI 参数、远端上传、执行日志或 BuildTools 契约变化时，本地检查通过后执行相关 TeamCity 验证。
