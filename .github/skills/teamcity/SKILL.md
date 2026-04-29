---
name: teamcity
description: 'TeamCity Web API 操作技能。使用场景：通过 Copilot 触发构建、等待 build 完成、查看构建日志、查看或更新 Versioned Settings、验证 VCS 配置、检查 agent 状态、批量并行或串行分发构建。关键字：TeamCity、run-build、run-build-group、versioned settings、show-project、verify-vcs、export-current、apply、build queue、agent dispatch、构建日志。'
---

# TeamCity Web API 操作技能

## ⚠️ CRITICAL: Agent Execution Pattern / 关键：Agent 执行模式

**必须遵循的模式：**

使用本 skill 触发构建时，`run-build --wait`、`run-build-group --wait` 和 `run-talos-baseflow-chain` 是唯一等待入口。

1. Agent 只启动一个本地 TeamCity helper 命令，并确保命令带有 `--wait`，或使用本身会等待远端链路完成的 `run-talos-baseflow-chain`。
2. TeamCity 状态轮询只允许发生在该 helper 进程内部。
3. Agent 使用当前终端/任务工具自带的进程等待能力等待 helper 命令退出。
4. helper 命令退出后，Agent 只读取一次最终输出，并据此汇报 build ID、URL、状态和失败日志摘要。

**禁止模式：**

-  不得自己写 TeamCity 等待循环。
-  不得在 helper 等待期间反复读取终端输出当作进度轮询。
-  不得使用 `pylanceRunCodeSnippet`、`time.sleep()`、shell/Python 循环、TeamCity REST 查询循环或 `tc_build_poller.sh` 做外层等待。
-  不得在 `run-build --wait` 已经运行时并行启动第二套 TeamCity 轮询。
-  不得先无 `--wait` 触发构建，再自己轮询该 build ID，除非用户明确要求只触发不等待。
-  不得修改 Teamcity 的 Versioned Settings模式，CI任务更新必须通过正常的 `apply` 命令更新 Versioned Settings 并验证 VCS 加载。

**正确示例：**

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build \
    --build-type-id BDFrameworkCore_TestClientRes \
    --branch v4/v-4.0.0 \
    --comment "TestClientRes android" \
    --tag clientres-verify \
    --property build.client.version=0.1 \
    --property test.clientres.platform=android \
    --property env.TEAMCITY_TOKEN="$TEAMCITY_TOKEN" \
    --wait \
    --timeout-seconds 7200
```

执行契约：启动上面的单个进程并等待它退出；不要在外层再查 TeamCity 状态。

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

For Talos BaseFlow regressions, the guarded entrypoint is now `run-talos-baseflow-chain`, not a plain `run-build` call. The chain enforces a local Talos batchmode gate first, then rebuilds the package, then queues the remote BaseFlow run with the real package build id.

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
    --property env.TEAMCITY_TOKEN="$TEAMCITY_TOKEN" \
    --wait
```

关键参数：
- `--wait`：等待构建完成，失败时自动打印日志尾部
- `--timeout-seconds`：等待超时（默认 900 秒）
- `--poll-interval-seconds`：helper 进程内部状态检查间隔（默认 5 秒），不是 Agent 外层轮询许可
- `--log-tail-lines`：失败时显示日志行数（默认 80 行）

Agent 等待纪律：

- `run-build --wait`、`run-build-group --wait` 和 `run-talos-baseflow-chain` 是 Agent 等待 TeamCity 的唯一入口。
- Agent 只启动一个本地 helper 进程，并用当前终端/任务工具自带的进程等待能力等它退出。
- TeamCity 状态轮询只允许发生在 helper 进程内部；Agent 不要再写外层轮询。
- 等待期间不要反复读取终端输出当作进度轮询，也不要并行启动第二套 TeamCity REST、shell 或 Python 轮询。
- helper 进程退出后，再读取最终输出并汇报 build ID、URL、状态和失败日志摘要。

### 手工诊断轮询工具

`tc_build_poller.sh` 只保留给人工手工诊断或特殊脚本集成，不是 Agent 的默认等待入口。Agent 已经启动 `run-build --wait` 后，不得再使用该工具并行等待同一个 build。

当用户明确要求轮询一个已经存在的 build ID，且没有正在运行的 `run-build --wait` helper 时，可以手工使用：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.github/skills/teamcity/scripts/tc_build_poller.sh <build_id> [poll_interval] [timeout]
```

参数说明：
- `build_id`: TeamCity 构建 ID（必需）
- `poll_interval`: 手工诊断工具内部检查间隔秒数（可选，默认 30）
- `timeout`: 超时秒数（可选，默认 7200 = 2小时）

示例：
```bash
# 轮询构建 1079，每 60 秒检查一次，最多等待 30 分钟
.github/skills/teamcity/scripts/tc_build_poller.sh 1079 60 1800
```

该工具特点：
- 仅在进度变化或每 5 分钟时打印状态，减少输出噪音
- 构建失败时自动打印日志尾部（最后 80 行）
- 支持无 jq 环境（使用 grep/sed 降级解析）
- 退出码：0=成功，1=失败，2=超时，3=参数错误

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

### run-talos-baseflow-chain — guard Talos BaseFlow with a local runtime gate

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-talos-baseflow-chain \
    --platform android \
    --unity-path "$UNITY_PATH" \
    --branch v4/v-4.0.0 \
    --comment "Talos BaseFlow sqlite validation" \
    --tag android \
    --tag baseflow-sqlite \
    --test-file tests/testBaseFlow-e2e.spec.ts \
    --adb-serial 127.0.0.1:62001 \
    --adb-connect-targets 127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555 \
    --emulator-type nox \
    --timeout-seconds 7200 \
    --poll-interval-seconds 10
```

Key contract:
- The command runs `Packages/com.talosai.e2e/Playwright~/tools/test-batchmode.sh --test-file <spec>` first and aborts immediately if the local gate fails.
- Only after the local gate passes does it queue the package build for the selected platform and then queue `BDFrameworkCore_TalosAIStep01BaseFlowTest` with the produced package build id.
- Use `--local-batchmode-mode tcp` for exact Playwright-spec parity between local and remote validation.
- `--local-batchmode-mode sync` is an explicit fallback only. It runs the exported Talos suite set instead of Playwright-spec filtering, so you must opt in with `--allow-local-sync-fallback` and treat it as a weaker parity mode.
- For Talos BaseFlow regressions, do not use a plain `run-build` call as the default regression path, because it bypasses the local runtime gate entirely.

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

## DSL 参数防膨胀规则

TeamCity Kotlin DSL 参数和 `scriptContent` 不得重复 `buildtools.toml` `[talos.e2e]` 段或 `PlatformProfile` 已提供的默认值。

**原则：DSL 层只保留真正需要 TeamCity 快照依赖、手动覆盖或页面输入的参数。**

具体规则：
1. `buildtools.toml [talos.e2e]` 已定义的默认值（`build_debug`、`timeout_seconds`、`unity_host`、`unity_port`、`emulator_type`、`mumu_auto_start`、`adb_connect_targets` 等）不得再出现在 DSL `params` 块或 `scriptContent` 中。
2. `PlatformProfile`（`teamcity_e2e_runner.py`）按平台补齐的默认值（`default_unity_port`、`default_emulator_type`、`default_adb_connect_targets`）同样不得重复声明。
3. DSL `params` 只保留以下类型的参数：
   - 固定值参数（如 `talos.e2e.platform = "windows"`）
   - 需要快照依赖传递的参数（如 `talos.e2e.package.build.id`、`talos.e2e.package.build.number`）
   - 需要人工在 TeamCity 页面覆盖的参数（如 `build.extra.args`、`ci.python.command`）
4. `scriptContent` 只传递 DSL 层保留的参数；所有其他参数由 Python runner 从 `buildtools.toml` → `PlatformProfile` → argparse defaults 链路自动获取。
5. 新增 E2E DSL 参数前，先确认默认值是否已在 `buildtools.toml` 或 `PlatformProfile` 中提供；如果是，不要在 DSL 层重复声明。

已优化参考：`TalosAIBuildAndRunE2ETest.kt`（6 参数，3 步骤）、`TalosAIStep01BaseFlowTest.kt`（7 参数，4 步骤）、`TalosAIStep02FrameworkBusinessTest.kt`（7 参数，4 步骤）。

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
4. Talos BaseFlow remote validation must go through `run-talos-baseflow-chain` or an equivalent local batchmode gate before the remote build is queued.

## 详细文档

完整使用文档、维护约定和排障流程参见 `.github/skills/teamcity/README.md`。
