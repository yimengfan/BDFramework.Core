---
description: "编辑 BDFramework UI 框架代码时使用。涵盖 window、component binding、state/store/reducer helper 和 ScreenNavigation。"
applyTo: "Packages/com.popo.bdframework/Runtime/UI/**"
---

# UI 框架编码规范

## 规则

- 框架 UI API 保持通用。业务专属界面、启动配方和场景编排属于 `Assets/Code/**`
- 保持 Editor / Player / BatchMode 差异清晰，不要把 Editor API 引入运行时 UI 程序集
- 新增抽象前，优先使用现有 window、component binding、state manager、reducer 和 navigation 模式
- 如果 UI 流程依赖固定初始化或销毁顺序，必须在代码注释和测试中说明原因
- Prefab 或 scene 引用必须在 Editor、Player 和 BatchMode 验证中保持稳定

## 测试策略归属

归属集成层和 E2E 层：UI 窗口生命周期、ScreenNavigation 导航栈转换（集成层）；完整 UI 流程和启动序列（E2E 层）。单元测试覆盖 state/reducer 逻辑。

## 包通用规则

同时生效：`bdframework.instructions.md`（Editor/Runtime 隔离、asmdef 约定、命名、反射规则）。

## 验证

- 新增或更新最近的 UI/window/navigation 测试
- 启动、ScreenNavigation 或 window preconfig 行为变化时，运行相关 host E2E 或 BatchMode 验证