# UI 框架模块规则

作用域：`Packages/com.popo.bdframework/Runtime/UI/**`、`Runtime/ScreenNavigation/**`、UI manager、window、component binding、state/store/reducer helper 和 UI 框架相关测试。

## 阅读顺序

1. `.github/copilot-instructions.md`
2. `Packages/com.popo.bdframework/AGENTS.md`
3. 本文件
4. 附近 UI 源码、测试和 runtime host E2E 文件
5. 如果改动是业务界面专属，再读取 `.github/talos-docs/modules/business-code.md`

## 规则

- 框架 UI API 保持通用。业务专属界面、启动配方和场景编排属于 `Assets/Code/**`。
- 保持 Editor / Player / BatchMode 差异清晰，不要把 Editor API 引入运行时 UI 程序集。
- 新增抽象前，优先使用现有 window、component binding、state manager、reducer 和 navigation 模式。
- 公开 UI 框架 API 必须保持中文在前的双语 XML 注释。
- 如果 UI 流程依赖固定初始化或销毁顺序，必须在代码注释和测试中说明原因。
- Prefab 或 scene 引用必须在 Editor、Player 和 BatchMode 验证中保持稳定。
- 业务-facing UI 流程不得使用反射。框架反射必须有兼容性或扩展点原因说明。

## 验证

- 新增或更新最近的 UI/window/navigation 测试。
- 启动、ScreenNavigation 或 window preconfig 行为变化时，运行相关 host E2E 或 BatchMode 验证。
- 如果 UI 改动触碰 `Assets/Code/**` 下的业务界面，同时遵守 `.github/talos-docs/modules/business-code.md`。
