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

### DSL 参数防膨胀

TeamCity Kotlin DSL 参数和 `scriptContent` 不得重复 `buildtools.toml` `[talos.e2e]` 段或 `PlatformProfile` 已提供的默认值。DSL 层只保留真正需要 TeamCity 快照依赖、手动覆盖或页面输入的参数。新增 E2E DSL 参数前，先确认默认值是否已在 `buildtools.toml` 或 `PlatformProfile` 中提供。详见 `.github/skills/teamcity/SKILL.md` 的 DSL 参数防膨胀规则。

### Release/Debug 测试程序集分离

测试程序集（`BDFramework.Test`、`BDFramework.HostE2E`）只能出现在 Debug 构建（`-buildDebug`）中，严禁进入 Release 构建的母包（AOT）或热更 DLL 产物。

- **Debug 构建**：`HotfixTestAssemblyInjector.InjectTestAssemblies()` 将测试程序集注入 HybridCLR `hotUpdateAssemblies`，`TalosDebugDefineScope` 临时注入 `ENABLE_E2ETEST` 和 `DEBUG` 宏。
- **Release 构建**：`HotfixTestAssemblyInjector.EnsureTestAssembliesRemoved()` 从配置中移除所有测试程序集，不注入调试宏。
- 测试程序集作为热更 DLL 通过 HybridCLR 加载，不参与 AOT 编译。
- `PublishPipeLineCI` 的 BatchMode 入口（`BuildClientPackageForBatchMode`、`BuildClientResHotfixCodeForBatchMode`）已内置 debug/release 守卫；新增构建入口必须遵守同一分离策略。
- 旧的直接菜单入口（`PublishPackage_*Debug/Release`）不走 CI 守卫路径，仅限本地手动使用，CI 不得调用这些入口。
- Release 构建结果必须验证不包含 `BDFramework.Test.dll`、`BDFramework.HostE2E.dll` 或其 `.bytes` 变体。

## 验证

- 共享验证策略以 `DevOps/CI/README.md` 为准。
- 精确 pytest、dry-run、smoke test 和 TeamCity 入口以目标模块 README 为准。
- TeamCity DSL、CI 参数、远端上传、执行日志或 BuildTools 契约变化时，本地检查通过后执行相关 TeamCity 验证。
