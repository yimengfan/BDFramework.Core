# 业务代码模块规则

作用域：`Assets/Code/**` 业务层代码、热更业务代码、游戏 demo、游戏 UI 界面、玩法流程和业务侧测试。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. 本文件
3. 本次改动引用的附近源码、测试、prefab/scene
4. 修改 UI 使用方式时读取 `.github/talos-docs/modules/ui-framework.md`
5. 修改资源加载或更新使用方式时读取 `.github/talos-docs/modules/resource-loading.md`

## 规则

- 业务代码放在 `Assets/Code/<Module>/` 或 `Assets/Code/<Module>@hotfix/`。
- 业务测试放在 `Assets/Code/<Module>/Tests/`、`Assets/Code/<Module>@hotfix/Tests/` 或业务自有 `E2E/` 目录。
- 不要把业务专属测试、场景编排或玩法 case 放进 `Packages/com.popo.bdframework/` 或 `Packages/com.talosai.e2e/`。
- Unity3D 业务层代码不得使用反射。
- 热更代码必须兼容 HybridCLR 和现有 asmdef/hotfix 拆分。
- 使用框架公开 API，不要直接依赖框架内部实现。
- 修改启动顺序、资源预热、表格加载或 UI 窗口打开流程时，验证相关 framework host 测试或 Talos E2E host flow。
- 运行时日志可以使用中文；触碰代码时，注释和测试文档必须中文在前、中英双语。

## 验证

- 优先运行最接近的业务测试或 Unity 测试。
- 如果改动影响框架/业务集成，运行对应 framework host E2E 或被改模块记录的 Talos batchmode/player flow。
- 如果改动影响母包构建、热更 payload 或远端验证，按 `.github/copilot-instructions.md` 的 TeamCity 路由执行。
