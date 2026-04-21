---
name: teamcity
description: 'TeamCity Web API 操作技能。使用场景：通过 Copilot 触发构建、等待 build 完成、查看构建日志、查看或更新 Versioned Settings、验证 VCS 配置、检查 agent 状态、批量并行或串行分发构建。关键字：TeamCity、run-build、run-build-group、versioned settings、show-project、verify-vcs、export-current、apply、build queue、agent dispatch、构建日志。'
---

# TeamCity Web API 操作技能

## 作用

- 本技能是 Copilot 对 TeamCity 自动化操作的统一入口说明。
- 维护资产集中在 `.github/skills/teamcity/`：脚本、测试和维护文档不再分散在 `.test-DevOps/teamcityskill/`。
- 主脚本位于 `scripts/update_project_settings.py`，详细维护文档位于 `README.md`。

## 适用场景

当用户需要通过 TeamCity Web API 执行以下操作时使用本技能：

- 查看项目信息与 Versioned Settings 状态
- 导出/更新 Versioned Settings
- 触发单个构建并等待完成
- 批量触发多个构建，自动判断并行或串行
- 检查 agent 兼容性与空闲状态
- 查看构建日志与排查失败原因

## 维护约定

- 当命令行参数、环境变量、默认输出目录、BuildType ID 示例或执行流程变化时，必须在同一改动中同步更新以下文件：
    - `.github/skills/teamcity/SKILL.md`
    - `.github/skills/teamcity/README.md`
    - `.github/skills/teamcity/scripts/update_project_settings.py`
    - `.github/skills/teamcity/tests/test_env_paths.py`（如果默认路径或加载方式变化）
    - `.test-DevOps/.teamcity/.env.example`
- 本文件只保留 Copilot 触发所需的摘要说明；长篇背景、排障步骤和完整命令示例统一维护在 README 中。

## 环境配置

环境配置文件统一位于 `.test-DevOps/.teamcity/`：

```bash
# 1. 在仓库根目录复制模板并填入凭据
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
cp .test-DevOps/.teamcity/.env.example .test-DevOps/.teamcity/.env

# 2. 编辑 .test-DevOps/.teamcity/.env，填入 TEAMCITY_BASE_URL / TEAMCITY_TOKEN 等
```

认证优先级：`TEAMCITY_TOKEN` > `TEAMCITY_USERNAME` + `TEAMCITY_PASSWORD`。

## 核心脚本

- `scripts/update_project_settings.py`：主入口，负责 Versioned Settings 查询/导出/更新，以及单个或批量构建触发。
- `scripts/tc_latest_branch_report.py`：补充性的只读分析脚本，用于抓取固定分支构建日志中的关键上传线索。

### 加载环境并执行命令

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py <command> [options]
```

## 命令参考

### show-project — 查看项目与 Versioned Settings

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project
```

### verify-vcs — 验证 Versioned Settings 是否从 VCS 加载

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py verify-vcs
```

### export-current — 导出当前 Versioned Settings

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py export-current
```

默认输出到 `.github/skills/teamcity/output/current-versioned-settings.json`。

### apply — 更新 Versioned Settings

```bash
# dry-run 先预览
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload desired.json --dry-run
# 正式更新
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload desired.json
```

### run-build — 触发单个构建

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build \
    --build-type-id BDFrameworkCore_TestClientRes \
    --branch v4/v-4.0.0 \
    --comment "TestClientRes android" \
    --tag clientres-verify \
    --property build.client.version=0.1 \
    --property test.clientres.platform=android \
    --wait
```

关键参数：
- `--wait`：等待构建完成，失败时自动打印日志尾部
- `--timeout-seconds`：等待超时（默认 900 秒）
- `--poll-interval-seconds`：轮询间隔（默认 5 秒）
- `--log-tail-lines`：失败时显示日志行数（默认 80 行）

Operational note:
- For builds that call TeamCity again from inside the build scripts, `run-build` now forwards the current TeamCity token automatically when `env.TEAMCITY_TOKEN` is not provided explicitly.
- If a rerun must use a different token or basic-auth pair, pass the matching `env.TEAMCITY_*` property explicitly and the helper will preserve that override.
- Talos BaseFlow reruns should also override `--property talos.e2e.test.file=tests/testBaseFlow-e2e.spec.ts` when the Playwright spec naming has changed, instead of relying on server-side defaults.
- Talos E2E reruns should pass only the platform tag that matches the job, for example `--tag windows` or `--tag android`; the helper no longer injects a default source tag.
- `run-build --wait` now prints TeamCity `running-info` progress / hanging / stage fields and emits heartbeat summaries during long waits; when TeamCity returns an intranet `webUrl`, the helper rewrites it onto the configured `TEAMCITY_BASE_URL` and keeps the raw server address as `serverWebUrl` for comparison.

### run-build-group — 批量触发构建

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build-group \
    --build-type-id BDFrameworkCore_BuildClientPackageIos \
    --build-type-id BDFrameworkCore_BuildClientPackageWindows \
    --comment "双端回归" \
    --wait
```

自动决策规则：
1. 查询所有 connected + authorized + enabled 的 agent
2. 检查每个 agent 当前运行中的构建数
3. 如果所有 buildType 都有空闲的兼容 agent → 自动并行
4. 否则 → 自动串行

`--dispatch-mode`：`auto`（默认）/ `parallel`（强制并行）/ `sequential`（强制串行）

## 常用 BuildType ID

| BuildType ID | 说明 |
|---|---|
| `BDFrameworkCore_TestClientRes` | ClientRes 端到端测试入口（推荐优先使用） |
| `BDFrameworkCore_BuildCodeAndroid` | Android 代码构建 |
| `BDFrameworkCore_BuildCodeIos` | iOS 代码构建 |
| `BDFrameworkCore_BuildCodeWindows` | Windows 代码构建 |
| `BDFrameworkCore_BuildAssetbundleAndroid` | Android AssetBundle 构建 |
| `BDFrameworkCore_BuildAssetbundleIos` | iOS AssetBundle 构建 |
| `BDFrameworkCore_BuildAssetbundleWindows` | Windows AssetBundle 构建 |
| `BDFrameworkCore_BuildTable` | 共享表格构建 |
| `BDFrameworkCore_VerifyClientResAndroid` | Android ClientRes 验证（仅排查用） |
| `BDFrameworkCore_VerifyClientResIos` | iOS ClientRes 验证（仅排查用） |
| `BDFrameworkCore_VerifyClientResWindows` | Windows ClientRes 验证（仅排查用） |

## 前置校验

修改 `.test-DevOps/.teamcity/` Kotlin DSL 后，先本地 Maven 校验：

```bash
brew install maven
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core/.test-DevOps/.teamcity
mvn teamcity-configs:generate
```

远端触发前确认：
1. `.test-DevOps` 仓库改动已提交并推送
2. 主仓库业务改动已推送到 TeamCity 实际 checkout 的分支
3. `show-project` 与 `verify-vcs` 返回正常

## 详细文档

完整使用文档、维护约定和排障流程参见 `.github/skills/teamcity/README.md`。
