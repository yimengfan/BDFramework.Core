# Editor Pipeline 模块规则

作用域：`Packages/com.popo.bdframework/Editor/EditorPipeline/**`、PublishPipeline、DevOpsPipeline、BuildHotfix、BuildAssetBundle、BuildTable、母包构建入口和 BatchMode CI bridge。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. `Packages/com.popo.bdframework/AGENTS.md`
3. 本文件
4. 附近 editor pipeline 源码和测试
5. 影响 CI 脚本或 TeamCity 契约时读取 `.github/talos-docs/modules/devops-ci.md`

## 规则

- Editor pipeline 代码可以使用 UnityEditor API；runtime 程序集不得依赖它们。
- 重要构建流程必须锚定在明确 coordinator 或入口方法，方便 CI 日志映射到代码阶段。
- BatchMode 入口必须非交互、确定性、独立于当前打开场景和手工 Inspector 状态。
- `PublishPipeLineCI` 入口、`CI(Des)` 描述、BuildTools 脚本、TeamCity DSL、README 文本和测试必须保持同步。
- TeamCity DSL 和 pipeline 层只负责任务调度、参数和依赖；业务构建逻辑属于 `DevOps/CI/BuildTools/**` 或一方 editor pipeline 代码。
- Debug build 行为、热更测试程序集注入、母包制品和上传路径都是公开 CI 契约；行为变化时更新测试和文档。
- 阶段日志必须标明平台、build target、client version、输出路径、executeMethod 和关键阶段开始/完成。

## 验证

- 运行最近的 editor pipeline 测试或 BatchMode bridge 验证。
- 如果影响 BuildTools 脚本或上传布局，运行目标模块 README 中的 pytest 和 dry-run。
- 如果 TeamCity DSL、构建参数、输出布局、上传协议或 CI 日志变化，本地检查通过后执行受影响 TeamCity 验证。
