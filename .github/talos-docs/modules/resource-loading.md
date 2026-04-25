# 资源加载模块规则

作用域：`Packages/com.popo.bdframework/Runtime/AssetsManager/**`、资源加载、AssetBundle 和 DevAssets 流程、资源版本控制、文件服务器更新协议和资源相关测试。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. `Packages/com.popo.bdframework/AGENTS.md`
3. 本文件
4. 附近 AssetsManager 源码和测试
5. 影响文件服务器或 ClientRes 行为时读取 `.github/talos-docs/modules/devops-ci.md`

## 规则

- 资源加载 API 必须显式。新增资源更新协议时，通过明确 public API 暴露；除非需求明确要求，不要让旧 public entrypoint 隐式路由到新协议。
- Runtime 命名使用业务概念，例如 `file server`、`version server` 或 `resource server`；避免新增名为 `BuildTools` 的 runtime 抽象。
- 持久化资源 manifest 和生成配置在构建/写入边界后视为可信数据。可信数据格式错误应快速失败。
- 下载 manifest、文件服务器响应和 CDN 内容在校验前视为不可信数据。
- 不要把 Editor-only 构建逻辑混入 runtime 程序集。
- 文件格式、版本指针、缓存文件、hash payload 或远端路径变化时，必须同步更新写入方、读取方、测试、README 和 CI 断言。
- BatchMode 必须兼容：不弹窗、不依赖人工场景、不隐式依赖本地缓存。

## 验证

- 新增或更新最近的 AssetsManager 单元/集成测试。
- 资源更新或文件服务器协议变化时，运行文档记录的 BatchMode 验证和相关 `DevOps/CI/BuildTools` pytest/dry-run。
- ClientRes 远端上传/下载行为变化时，本地检查通过后触发受影响 TeamCity 验证。
