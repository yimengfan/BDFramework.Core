---
description: "编辑 BDFramework 资源加载代码时使用。涵盖 AssetsManager、AssetBundle、DevAssets、资源版本控制、文件服务器更新协议。"
applyTo: "Packages/com.popo.bdframework/Runtime/AssetsManager/**"
---

# 资源加载编码规范

## 规则

- 资源加载 API 必须显式。新增资源更新协议时，通过明确 public API 暴露；不要让旧 public entrypoint 隐式路由到新协议
- Runtime 命名使用业务概念（`file server`、`version server`、`resource server`）；避免新增名为 `BuildTools` 的 runtime 抽象
- 资源加载信任边界遵循 copilot-instructions.md §2 契约式设计：持久化 manifest 和生成配置为可信路径，下载 manifest 和远端响应为不可信路径
- 文件格式、版本指针、缓存文件、hash payload 或远端路径变化时，必须同步更新写入方、读取方、测试、README 和 CI 断言
- BatchMode 必须兼容：不弹窗、不依赖人工场景、不隐式依赖本地缓存

## 测试策略归属

归属集成层：资源加载降级/回退路径、文件服务器更新协议、缓存一致性。单元测试覆盖路径解析和 hash 计算；集成测试覆盖完整加载链路和降级逻辑。

## 包通用规则

同时生效：`bdframework.instructions.md`（Editor/Runtime 隔离、asmdef 约定、命名、反射规则）。

## 验证

- 新增或更新最近的 AssetsManager 单元/集成测试
- 资源更新或文件服务器协议变化时，运行文档记录的 BatchMode 验证
- ClientRes 远端上传/下载行为变化时，本地检查通过后触发受影响 TeamCity 验证