---
description: "编辑 DevOps/CI 下的 CI、BuildTools、Python 构建脚本、TeamCity 集成、制品上传 helper、客户端资源流程或 pytest 覆盖时使用。"
applyTo: "DevOps/CI/**"
---

# DevOps CI 编码规范

本文件包含编辑 `DevOps/CI/**` 下任何文件时自动生效的规则。

## Python 编码规范

- 被触碰的 Python 模块必须保持模块 docstring 最新
- 被触碰的 Python 类和非平凡函数必须说明角色、契约、兜底或副作用

## 外部配置

- 业务无关外部配置（文件服务器、CI 服务器、签名/证书元数据、远端测试配置）统一放在 `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/buildtools.toml`
- 通过 `Common/buildtools_config.py` 读取，不在其外新增临时 TOML 解析

## CI 流程结构

- 主 CI 流程集中在明确 coordinator 函数或入口脚本，带阶段注释和匹配阶段日志
- 阶段日志标明平台、build target、client version、输出路径、executeMethod 和关键阶段开始/完成

## TeamCity 执行纪律

### 唯一等待入口

`run-build --wait`、`run-build-group --wait` 和 `run-talos-baseflow-chain` 是 Agent 等待 TeamCity 的唯一合法入口：
1. Agent 只启动一个本地 helper 进程，确保命令带 `--wait`
2. TeamCity 状态轮询只允许发生在 helper 进程内部
3. Agent 只用终端/任务工具自带的等待能力等待 helper 退出
4. helper 退出后读取一次最终输出，汇报 build ID、URL、状态和失败日志摘要

### 禁止模式

- 不得自己写 TeamCity 等待循环
- 不得在 helper 等待期间反复读取终端输出
- 不得使用 `time.sleep()`、shell/Python 循环或 `tc_build_poller.sh` 做外层等待
- 不得在 `run-build --wait` 运行时并行启动第二套 TeamCity 轮询
- 不得修改 TeamCity Versioned Settings 模式
- 必需远端构建状态未知时，不得标记任务完成

### 制品验证

构建 `status=SUCCESS` 不等于验证通过。至少确认一项证据：
- 构建日志包含编译步骤输出（Tundra/Csc 记录、`items updated` 大于 0）
- 构建日志包含目标 `ExecuteMethod` 的执行记录和正常退出
- 制品上传步骤完成且 `integrity=verified`
- 构建时长无异常短截；异常短必须下载日志确认原因

只汇报 `status=SUCCESS` 而无制品证据时，不得标记远端验证通过。

### DSL 参数防膨胀

TeamCity Kotlin DSL 参数和 `scriptContent` 不得重复 `buildtools.toml` `[talos.e2e]` 段或 `PlatformProfile` 已提供的默认值。DSL 层只保留真正需要 TeamCity 快照依赖、手动覆盖或页面输入的参数。

### TeamCity 操作

详细命令和环境配置见 `.github/skills/teamcity/SKILL.md`。

## 测试策略归属

归属门禁层和集成层：CI 脚本和 BuildTools 逻辑通过 pytest 覆盖（集成层）；TeamCity 构建产物通过远端验证覆盖（门禁层）。

## 同步要求

构建参数、输出布局、上传协议、CI 日志或 TeamCity 契约变化时，必须同步更新代码、README、pytest 断言和 skill 文档。

## 测试覆盖

- 每条变更代码路径必须新增或更新自动化测试
- 相关 pytest、smoke 和 TeamCity 验证必须通过后才算完成
