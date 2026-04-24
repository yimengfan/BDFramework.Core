# TeamCity Skill

`.github/skills/teamcity/` 统一承载 TeamCity Copilot skill 的脚本、测试与维护文档；旧的 `.test-DevOps/teamcityskill/` 目录不再继续维护。

## 目录职责

- `.github/skills/teamcity/SKILL.md`：Copilot 入口说明，保留摘要流程和使用边界。
- `.github/skills/teamcity/README.md`：长期维护手册，记录命令、排障与维护约定。
- `.github/skills/teamcity/scripts/update_project_settings.py`：主脚本，负责 Versioned Settings 查询/导出/更新，以及构建触发。
- `.github/skills/teamcity/scripts/tc_latest_branch_report.py`：补充性的只读日志分析脚本。
- `.github/skills/teamcity/tests/`：脚本回归测试。
- `.test-DevOps/.teamcity/.env.example`：本地环境模板。
- `.test-DevOps/.teamcity/.env`：本地私有凭据文件，不提交到仓库。

## 维护约定

- 当命令行参数、环境变量、默认路径、BuildType 示例或排障流程变化时，必须在同一改动中同步更新以下文件：
  - `.github/skills/teamcity/SKILL.md`
  - `.github/skills/teamcity/README.md`
  - `.github/skills/teamcity/scripts/update_project_settings.py`
  - `.github/skills/teamcity/tests/test_env_paths.py`（如果默认 env 位置或加载方式变化）
  - `.test-DevOps/.teamcity/.env.example`
- 长篇维护说明只保留在本文件；不要把新的排障细节再复制回 `SKILL.md`。
- 默认输出目录现在相对 `.github/skills/teamcity/` 解析，不再相对旧的 `teamcityskill/` 目录解析。

## 环境配置

复制模板并填入本地凭据：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
cp .test-DevOps/.teamcity/.env.example .test-DevOps/.teamcity/.env
```

推荐优先使用：

- `TEAMCITY_TOKEN`

如果 TeamCity 仅支持 Basic Auth，再填写：

- `TEAMCITY_USERNAME`
- `TEAMCITY_PASSWORD`

认证优先级：`TEAMCITY_TOKEN` > `TEAMCITY_USERNAME` + `TEAMCITY_PASSWORD`。

重要说明：

- `TEAMCITY_BASE_URL` 只控制本地 TeamCity helper 访问哪一个 TeamCity HTTP 入口，以及控制台里重写后的公开 `webUrl`；它不会自动改写 `DevOps/CI/BuildTools/buildtools.toml` 里的 `[ci_server]` 或 `[artifact_file_server]`。
- 如果 TeamCity 服务器通过 Web API 返回的是内网 `webUrl`，helper 现在会额外按当前 `TEAMCITY_BASE_URL` 重写出一个可直接访问的 `webUrl`，并把原始服务端地址保留为 `serverWebUrl` 便于对照。

默认环境文件位置：

- `.test-DevOps/.teamcity/.env`

默认输出目录位置：

- `.github/skills/teamcity/output/`

如果需要改输出目录，可在 `.test-DevOps/.teamcity/.env` 中设置：

- `TEAMCITY_OUTPUT_DIR=tc_output`

该路径始终相对 `.github/skills/teamcity/` 解析。

## 加载环境并执行脚本

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py <command> [options]
```

## 支持命令

- `show-project`
- `verify-vcs`
- `export-current`
- `apply`
- `run-build`
- `run-build-group`
- `run-talos-baseflow-chain`

## 常用命令

### 查看项目与当前 Versioned Settings

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project
```

如果更关心“当前 project 是否已从 VCS 设置加载”，直接执行：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py verify-vcs
```

### 导出当前 Versioned Settings

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py export-current
```

默认输出到：

- `.github/skills/teamcity/output/current-versioned-settings.json`

### 预览并更新 Versioned Settings

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
cp .github/skills/teamcity/output/current-versioned-settings.json .github/skills/teamcity/desired-versioned-settings.local.json
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload .github/skills/teamcity/desired-versioned-settings.local.json --dry-run
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload .github/skills/teamcity/desired-versioned-settings.local.json
```

更新前备份默认输出到：

- `.github/skills/teamcity/output/versioned-settings.before-update.json`

### 触发单个构建并等待结果

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
    --wait
```

说明：

- `--comment` 会保留用户前缀，并自动追加 `测试目标: <buildTypeId>`。
- `--tag` 可重复传入或使用逗号分隔，仅限平台等关键元信息（如 `win64`、`android`）。The helper does not inject any default tag automatically. Keep test-target and branch details in `--comment`.
- 失败时脚本会自动打印末尾构建日志，便于直接定位问题。
- `--wait` 现在除了 `state/status/statusText` 之外，还会输出 TeamCity `running-info` 里的 `progress`、`hanging` 和 `stage`，并在长时间等待时按固定心跳重报一次当前摘要，避免构建已在推进但控制台长时间静默。
- `--wait` 进入轮询前会先做一次轻量 state-only 检查；如果构建已 `finished`，直接打印最终摘要并退出，不再进入详细轮询循环。
- If the build script needs to call TeamCity again while it is running, explicitly forward `--property env.TEAMCITY_TOKEN="$TEAMCITY_TOKEN"`. The remote worker does not inherit the local shell token automatically.

Copilot 轮询纪律 / Copilot polling discipline:

- 对 `run-build --wait`，优先使用 `run_in_terminal` 的 `async` 模式启动，然后等待该终端的完成通知。
- 不要在同一时段再额外启动 `pylanceRunCodeSnippet` + `time.sleep()` 的轮询脚本；这会重复消耗上下文窗口，看起来像“卡住几个小时”。
- 如果必须临时查看进度，读取同一个异步终端的输出即可；不要再并行起第二套轮询链路。
- For `run-build --wait`, prefer launching it via `run_in_terminal` in `async` mode and then wait for that terminal's completion notification.
- Do not start an extra `pylanceRunCodeSnippet` + `time.sleep()` polling loop in parallel; that burns context budget and looks like the session is frozen for hours.
- If a spot progress check is required, read the same async terminal output instead of starting a second polling pipeline.
### Shell 轮询工具 / Shell Polling Tool

当需要轮询构建状态但不想占用 Python 进程时，可使用 shell 轮询工具 `tc_build_poller.sh`：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.github/skills/teamcity/scripts/tc_build_poller.sh <build_id> [poll_interval] [timeout]
```

参数说明：
- `build_id`: TeamCity 构建 ID（必需）
- `poll_interval`: 轮询间隔秒数（可选，默认 30）
- `timeout`: 超时秒数（可选，默认 7200 = 2小时）

示例：
```bash
# 轮询构建 1075，每 60 秒检查一次，最多等待 1 小时
.github/skills/teamcity/scripts/tc_build_poller.sh 1075 60 3600
```

该工具特点：
- 仅在进度变化或每 5 分钟时打印状态，减少输出噪音
- 构建失败时自动打印日志尾部（最后 80 行）
- 支持无 jq 环境（使用 grep/sed 降级解析）
- 退出码：0=成功，1=失败，2=超时，3=参数错误

使用场景：
- Copilot 触发长时间构建后，需要等待完成但不想阻塞会话
- CI/CD 流程中需要监控构建状态
- 调试构建问题时需要持续跟踪进度
排障建议：

- 如果构建早已结束，不要继续只盯着后台终端等待；优先直接查询 `GET /app/rest/builds/id:<buildId>`，再按 buildId 读取 `test-output.log` 或 `downloadBuildLog.html`。
- 如果 TeamCity helper 能访问公网 TeamCity，但远端构建内部仍打印内网 `teamcityBaseUrl` 或 `uploadServerUrl`，应继续检查 `DevOps/CI/BuildTools/buildtools.toml` 和相关外部服务，而不是只改 `.test-DevOps/.teamcity/.env`。

Guardrail for Talos BaseFlow:

- A plain `run-build --build-type-id BDFrameworkCore_TalosAIStep01BaseFlowTest` is not the default regression path anymore.
- That direct call bypasses the local runtime-complete gate, which is how device-only failures slipped through repeated reruns.
- Use `run-talos-baseflow-chain` for the normal Talos BaseFlow regression workflow so the local batchmode gate and the remote TeamCity/device run stay aligned on the same Playwright spec.

### Guarded Talos BaseFlow chain

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
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

What this command guarantees:

- It runs the local runtime-complete Talos batchmode gate first through `Packages/com.talosai.e2e/Playwright~/tools/test-batchmode.sh`.
- It forwards the same `--test-file` into the local TCP batchmode gate and the remote BaseFlow rerun, so both lanes exercise the same Playwright spec by default.
- It rebuilds the target platform package on TeamCity and then reuses that exact package build id in the BaseFlow rerun.
- It keeps the existing TeamCity helper wait/log-tail behaviour for both remote builds.

Mode note:

- `--local-batchmode-mode tcp` is the exact-parity mode and should be the default whenever the local machine can run it.
- `--local-batchmode-mode sync` is a fallback only. The script now makes that opt-in through `--allow-local-sync-fallback` because sync mode runs the exported Talos suite set instead of Playwright-spec filtering.
- If a local machine cannot sustain TCP batchmode (for example due a Unity license/runtime limitation), treat sync mode as a diagnostic fallback rather than proof that the exact remote spec has already passed locally.

### Re-run Talos BaseFlow with a rebuilt Windows package

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build \
  --build-type-id BDFrameworkCore_TalosAIStep01BaseFlowTest \
  --branch v4/v-4.0.0 \
  --comment "Talos BaseFlow on rebuilt Windows package" \
  --tag windows \
  --property build.client.version=0.1 \
  --property build.debugBuild=true \
  --property talos.e2e.package.build.id=<packageBuildId> \
  --property talos.e2e.test.file=tests/testBaseFlow-e2e.spec.ts \
  --property env.TEAMCITY_TOKEN="$TEAMCITY_TOKEN" \
  --wait
```

Notes:

- `talos.e2e.package.build.id` must point to a successful `BDFrameworkCore_BuildClientPackageWindows` build.
- Override `talos.e2e.test.file` explicitly when the Playwright spec naming changes; do not rely on stale defaults from the loaded DSL revision.
- For Talos E2E reruns, pass only the platform tag that matches the job, for example `--tag windows` or `--tag android`.
- Forward `env.TEAMCITY_TOKEN` whenever the Talos build resolves package artifacts or other TeamCity resources from inside the running build.

### 批量触发多个构建并自动决定并发或串行

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build-group \
    --build-type-id BDFrameworkCore_BuildClientPackageIos \
    --build-type-id BDFrameworkCore_BuildClientPackageWindows \
    --comment "发版前双端回归" \
    --tag release-check \
    --wait
```

自动决策规则：

1. 查询所有 `connected + authorized + enabled` 的 agent。
2. 检查每个 agent 当前运行中的 regular build 数量。
3. 如果所有 buildType 都有空闲的兼容 agent，则自动并行。
4. 否则自动串行。

`--dispatch-mode` 支持：

- `auto`：默认，自动判断。
- `parallel`：强制并行。
- `sequential`：强制串行。

## 常用 BuildType ID

其中 `TestClientRes` 和 `VerifyClientRes_*` 位于 TeamCity 页签 `BDFramework.Core / TestPipeline / TestBuildPipeline_ClientRes`。

- `BDFrameworkCore_TestClientRes`
- `BDFrameworkCore_BuildCodeAndroid`
- `BDFrameworkCore_BuildCodeIos`
- `BDFrameworkCore_BuildCodeWindows`
- `BDFrameworkCore_BuildAssetbundleAndroid`
- `BDFrameworkCore_BuildAssetbundleIos`
- `BDFrameworkCore_BuildAssetbundleWindows`
- `BDFrameworkCore_BuildTable`
- `BDFrameworkCore_VerifyClientResAndroid`
- `BDFrameworkCore_VerifyClientResIos`
- `BDFrameworkCore_VerifyClientResWindows`

正常回归入口优先使用 `BDFrameworkCore_TestClientRes`；只有在单独排查本地检查子任务时，才手工触发 `VerifyClientRes_*` 并补齐 `test.clientres.expected.*` 参数。

## Kotlin DSL 本地校验

如果这次改动涉及 `.test-DevOps/.teamcity/` Kotlin DSL，先在本机完成一次 Maven 校验：

```bash
brew install maven
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core/.test-DevOps/.teamcity
mvn -version
mvn teamcity-configs:generate
```

Supplemental notes:

1. The repository already provides `.test-DevOps/.teamcity/.mvn/maven.config` and `.test-DevOps/.teamcity/.mvn/local-settings.xml` to disable the Maven 3.9+ default HTTP blocker.
2. `.test-DevOps/.teamcity/pom.xml` currently prefers the public DSL repository `http://svn.funtoo.games/app/dsl-plugins-repository`, with the intranet fallback `http://192.168.0.240:20000/app/dsl-plugins-repository` as a secondary fallback.
3. If `mvn teamcity-configs:generate` still fails to resolve `configs-dsl-kotlin-parent`, check reachability to those two repository URLs first.

## 远端触发前置条件

在 TeamCity 上远端触发任务前，先确认：

1. `.test-DevOps` 仓库改动已提交并推送，Versioned Settings 能加载到最新 revision。
2. 主仓库业务改动也已提交并推送到 TeamCity 实际 checkout 的 GitHub 分支。
3. `show-project` 与 `verify-vcs` 返回正常。
4. 新的 buildType ID 已能在服务器查询到；如果仍然 missing，说明 TeamCity 还没加载新 DSL。
5. Talos BaseFlow validation must use `run-talos-baseflow-chain` or an equivalent local batchmode gate before the remote TeamCity/device leg is queued.

## 本地测试

脚本相关测试可直接执行：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
.venv/bin/python -m pytest .github/skills/teamcity/tests -q
```

然后按下面规则决策：

1. TeamCity 的单个 build agent 实例默认一次只能执行一个 regular build。
2. 如果所有目标 buildType 都能匹配到不同的 idle compatible agents，则自动并发触发。
3. 如果 idle compatible agents 不够，则自动切换为串行触发，避免把“并发”误当成真实可同时执行。

如需人工覆盖：

- `--dispatch-mode auto`：默认，自动判断。
- `--dispatch-mode parallel`：强制并发；如果 agent 容量不足会直接报错。
- `--dispatch-mode sequential`：强制串行。

Operational note:
- For builds that call TeamCity again from inside the build scripts, `run-build` now forwards the current TeamCity token automatically when `env.TEAMCITY_TOKEN` is not provided explicitly.
- If a rerun must use a different token or a basic-auth pair, pass the matching `env.TEAMCITY_*` property explicitly and the helper will preserve that override.
- Talos BaseFlow reruns should still override `--property talos.e2e.test.file=tests/testBaseFlow-e2e.spec.ts` when the Playwright spec naming has changed, instead of relying on server-side defaults.

## 常见只读验证

### 1. 验证 project 解析是否正确

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project --project-id BDFramework.Core
```

### 2. 验证当前配置是否更偏向 VCS

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py verify-vcs
```

## 输出与兼容说明

不同 TeamCity 版本下：

- 有些服务器提供独立 `/versionedSettings` 端点。
- 有些通过 `projectFeatures(type=versionedSettings)` 暴露。

因此更稳妥的流程始终是：

1. `export-current`
2. 基于真实返回 JSON 修改
3. 再 `apply`

## 参数优先级

以下优先级从高到低：

1. CLI 参数，例如 `--project-id`
2. `.env` / 环境变量
3. 脚本默认值

例如可以临时覆盖项目：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project --project-id BDFramework.Core
```

脚本会先尝试直接按 `id:` 查询；如果失败，会再按 `id / externalId / name` 兜底匹配，并打印最终解析结果。

## 安全说明

1. 不要把真实 `.env` 提交到 Git。
2. 不要把真实 token、密码写进脚本。
3. 如需分享 payload，先确认其中没有敏感 VCS 信息。

## 关联文档

- 总流程与排查方法：`/.test-DevOps/README.md`
- Python 构建脚本说明：`/Users/naipaopao/Documents/GitHub/BDFramework.Core/DevOps/CI/BuildTools/BuildClientPackage/README.md`

