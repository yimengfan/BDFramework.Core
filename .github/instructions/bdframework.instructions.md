---
description: "编辑 Packages/com.popo.bdframework 下的运行时、Editor、AOT、测试、asmdef、启动流程或资源更新逻辑时使用。"
applyTo: "Packages/com.popo.bdframework/**"
---

# BDFramework 包编码规范

本文件包含编辑 `Packages/com.popo.bdframework/**` 下任何文件时自动生效的编码规则。包架构理解见 `Packages/com.popo.bdframework/AGENTS.md`。

## Editor/Runtime 隔离

- Runtime 代码放在 `Runtime/` 或 `Runtime.AOT/`；Editor-only 代码放在 `Editor/`
- Runtime 程序集禁止引用 `UnityEditor`、禁止依赖 Editor 程序集
- Editor 可引用 Runtime，不可反向
- 确需共存时，必须用 `#if UNITY_EDITOR` / `#if !UNITY_EDITOR` 条件编译隔离
- 始终考虑 Editor、Player、BatchMode/CI 三种行为

## asmdef 管理

- 每个逻辑模块一个独立 asmdef，不合并无关模块
- 测试 asmdef（`.Test` 后缀）只引用被测模块 + 测试框架
- 新增 asmdef 前确认模块归属和现有边界

## 代码变更原则

- 新增抽象前，优先复用现有实现
- 修改公共 API 前，检查调用链、序列化字段、Inspector 和反射使用点
- 涉及线程、文件 IO、持久化或生命周期的代码，必须考虑并发、重入、异常和释放

## 测试覆盖率要求

修改本包下任何源码时，须满足以下测试要求：

- 受影响编译链内可测代码的自动化测试覆盖率须 ≥ 90%
  - "可测代码"排除：纯 POCO/DTO 无逻辑属性、Unity 序列化行为、第三方框架自身行为、纯 getter/setter
  - 覆盖率 = 被测代码路径数 / 总可测代码路径数，按方法维度计算
- Bug 修复必须附带复现用例 + 修复验证用例，至少覆盖：触发条件、修复前行为、修复后预期行为
- C1 条件门判定：覆盖率不足 90% 时视为未通过，须补充测试或说明不可测原因及替代验证

## 测试覆盖缺口（按优先级）

### P0 零覆盖关键模块

| 模块 | 源文件数 | 缺失测试要点 |
|------|---------|-------------|
| UI/State（Store/Reducer/StateFactory） | 8+ | 状态创建/订阅/取消生命周期、Reducer 方法解析（IL2CPP）、并发 dispatch |
| UI/Component（绑定/适配器） | 12+ | AutoAssign/ButtonOnclick 属性解析失败路径、适配器查找缺失、IL2CPP 反射 |
| Event/DataListener | 8 | 监听器注册/注销/内存泄漏、dispatch 中异常传播、并发修改 |
| ScreenNavigation | 3 | 导航栈状态机转换、非法转换拒绝 |

### P1 薄覆盖核心模块

| 模块 | 现有测试 | 缺失测试要点 |
|------|---------|-------------|
| AssetsManager/ArtAsset | E2E only | LoaderFactory 未知类型、LoadTask 超时/并发、manifest 损坏、依赖追踪卸载 |
| Config | 纯逻辑 | 文件 IO 失败、Processor 加载、配置合并冲突 |
| Data/Sql | 单元+基准 | 事务回滚、并发访问、表结构迁移 |
| EditorPipeline/BuildHotfix | 无 | 适配器生成边界、代码剥离过度、AOT 注册失败 |
| EditorPipeline/BuildTable | 极少 | Excel 解析异常、Schema 迁移、代码生成保留字冲突 |

### P2 需加固模块

| 模块 | 缺失测试要点 |
|------|-------------|
| Utils/Logs | 日志轮转边界、磁盘写满、并发写入安全 |
| EditorPipeline/BuildAssetBundle | 资源图节点链路、粒度规则冲突 |
| Utils/ObjectPool | 获取/释放生命周期、池耗尽 |
| Utils/Extensions | 边界输入、空值处理 |

## 测试策略归属

本包覆盖全部测试层：单元层（Runtime 核心契约）、集成层（Editor pipeline + 资源加载 + 启动链路）、E2E 层（Host E2E）、门禁层（Debug/Release 行为矩阵）。具体子模块归属见各 instruction。

## 模块细分规则

编辑特定子模块时，对应的 instruction 会通过 applyTo 自动加载：

- UI 框架 → `bdframework-ui.instructions.md`
- 资源加载 → `bdframework-resource.instructions.md`
- Editor Pipeline → `bdframework-editor-pipeline.instructions.md`
- 测试框架 → `bdframework-testing.instructions.md`

## 日志模块特殊规则

修改 `Runtime/Utils/Logs/` 时，必须整体看待 `BDebug`、`Editor_UnityLogHook`、`Persistence`、`PersistenceSettings`、`LogReader`、`LogCrypto` 和 `SerializedLogEntry`：
- Editor 下：`BDebug` 负责 Console 输出，序列化日志由 `Editor_UnityLogHook` 处理
- Player 下：`BDebug` 负责二进制日志持久化，不依赖 `Editor_UnityLogHook`
- 文件命名、格式头、记录结构、加密或清理策略变化时，必须同步更新写入、读取、导出和测试
