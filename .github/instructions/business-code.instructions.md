---
description: "编辑 Assets/Code/ 下的业务逻辑、游戏界面、玩法流程、热更业务代码或业务测试时使用。"
applyTo: "Assets/Code/**"
---

# 业务代码编码规范

## 代码放置

- 业务代码放在 `Assets/Code/<Module>/` 或 `Assets/Code/<Module>@hotfix/`
- 业务测试放在 `Assets/Code/<Module>/Tests/`、`Assets/Code/<Module>@hotfix/Tests/` 或业务自有 `E2E/` 目录
- 不要把业务专属测试、场景编排或玩法 case 放进 `Packages/com.popo.bdframework/` 或 `Packages/com.talosai.e2e/`

## 编码规则

- 热更代码必须兼容 HybridCLR 和现有 asmdef/hotfix 拆分
- 使用框架公开 API，不要直接依赖框架内部实现
- 修改启动顺序、资源预热、表格加载或 UI 窗口打开流程时，验证相关 framework host 测试或 Talos E2E host flow

## 测试策略归属

归属单元层和集成层：业务逻辑函数（单元层）；框架/业务交互、启动序列和 UI 流程（集成层）。E2E 层通过 Talos E2E host flow 覆盖。

## 验证

- 优先运行最接近的业务测试或 Unity 测试
- 如果改动影响框架/业务集成，运行对应 framework host E2E 或被改模块记录的 Talos batchmode/player flow
- 如果改动影响母包构建、热更 payload 或远端验证，详见 `ci.instructions.md`