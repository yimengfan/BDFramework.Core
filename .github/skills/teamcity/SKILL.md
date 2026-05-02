---
name: teamcity
description: 'TeamCity Web API 操作技能。使用场景：通过 Copilot 触发构建、等待 build 完成、查看构建日志、查看或更新 Versioned Settings、验证 VCS 配置、检查 agent 状态、批量并行或串行分发构建。关键字：TeamCity、run-build、run-build-group、versioned settings、show-project、verify-vcs、export-current、apply、build queue、agent dispatch、构建日志。'
---

# TeamCity Skill / TeamCity 技能

## 1. 入口定位

本文件既是 Copilot/Agent 的 TeamCity 入口文档，也是该 skill 的唯一维护文档：

- 给出唯一合法等待入口和禁止模式。
- 区分同步命令、等待命令、人工诊断脚本和内部分析脚本。
- 提供最小必要示例、详细命令说明、BuildType 速查、本地验证和排障入口。

## 2. Agent 执行铁律

### 2.1 唯一等待入口

`run-build --wait`、`run-build-group --wait` 和 `run-talos-baseflow-chain` 是 Agent 等待 TeamCity 的唯一合法入口。

执行契约：
1. Agent 只启动一个本地 helper 进程，并确保命令带 `--wait`，或直接使用 `run-talos-baseflow-chain`。
2. TeamCity 状态轮询只允许发生在 helper 进程内部。
3. Agent 只用终端/任务工具自带的等待能力等待 helper 退出。
4. helper 退出后读取一次最终输出，据此汇报 build ID、URL、状态和失败日志摘要。

### 2.2 禁止模式

- 不得自己写 TeamCity 等待循环。
- 不得在 helper 等待期间反复读取终端输出当作进度轮询。
- 不得使用 `pylanceRunCodeSnippet`、`time.sleep()`、shell/Python 循环、TeamCity REST 查询循环或 `tc_build_poller.sh` 做外层等待。
- 不得在 `run-build --wait` 已运行时并行启动第二套 TeamCity 轮询。
- 不得先无 `--wait` 触发构建，再自己轮询该 build ID，除非用户明确要求只触发不等待。
- 不得修改 TeamCity Versioned Settings 模式；CI 任务更新必须通过 `apply` 命令正常更新并验证 VCS 加载。

## 3. 制品验证要求

构建 `status=SUCCESS` 不等于验证通过。必须确认构建实际执行了编译、打包或测试，而非只完成 checkout 后空跑退出。

至少确认一项证据：
- 构建日志包含编译步骤输出，例如 Tundra/Csc 记录、`items updated` 大于 0、受影响程序集名称出现。
- 构建日志包含目标 `ExecuteMethod` 的执行记录和正常退出。
- 制品上传步骤完成且 `integrity=verified`，上传文件数和字节数符合预期。
- 构建时长无异常短截；如果异常短，必须下载日志确认原因。

只汇报 `status=SUCCESS` 而无制品证据时，不得标记远端验证通过。

## 4. 命令分流

### 4.1 官方公开入口

| 类别 | 命令 | 何时使用 | Agent 默认可用 |
|---|---|---|---|
| 同步只读 | `show-project` | 查看项目与 Versioned Settings 当前状态 | 是 |
| 同步只读 | `verify-vcs` | 验证 Versioned Settings 是否从 VCS 加载 | 是 |
| 同步导出 | `export-current` | 导出当前 Versioned Settings JSON | 是 |
| 同步更新 | `apply` | dry-run 或正式更新 Versioned Settings | 是 |
| 等待型单构建 | `run-build --wait` | 触发并等待单个构建 | 是 |
| 等待型多构建 | `run-build-group --wait` | 自动并行或串行分发多个构建 | 是 |
| 守护式链路 | `run-talos-baseflow-chain` | 先做本地 Talos gate，再重建母包并触发 BaseFlow | 是 |

### 4.2 人工诊断入口

| 脚本 | 角色 | 何时使用 | Agent 默认可用 |
|---|---|---|---|
| `scripts/tc_build_poller.sh` | 手工诊断轮询工具 | 人工诊断一个已存在的 build ID，或特殊 shell 集成 | 否 |
| `scripts/tc_poll_existing_build.py` | 已存在 build 轮询器 | 只轮询已经入队的 build，不负责任何排队动作 | 否 |

这些脚本不是 Agent 默认等待入口；只有在人工诊断或明确脚本集成场景下才使用。

### 4.3 内部分析脚本

| 脚本 | 角色 | 说明 |
|---|---|---|
| `scripts/tc_latest_branch_report.py` | 临时或内部日志分析脚本 | 不属于稳定公开命令面，不要把它当作 Copilot 的默认入口 |

## 5. 环境与调用入口

环境文件位于 `.test-DevOps/.teamcity/`。

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
cp .test-DevOps/.teamcity/.env.example .test-DevOps/.teamcity/.env
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
```

统一调用入口：

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py <command> [options]
```

认证优先级：`TEAMCITY_TOKEN` 优先于 `TEAMCITY_USERNAME` + `TEAMCITY_PASSWORD`。

补充约束：
- `TEAMCITY_BASE_URL` 只控制 helper 访问哪个 TeamCity HTTP 入口，以及日志里重写后的公开 `webUrl`。
- 默认输出目录是 `.github/skills/teamcity/output/`。
- 如果 shell 里已存在同名环境变量，`.env` 只补缺，不覆盖 shell 当前值。

## 6. 最小命令示例

### show-project

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project
```

### apply

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload desired.json --dry-run
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload desired.json
```

### run-build

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

### run-build-group

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build-group \
  --build-type-id BDFrameworkCore_BuildClientPackageIos \
  --build-type-id BDFrameworkCore_BuildClientPackageWindows \
  --comment "双端回归" \
  --tag release-check \
  --wait
```

### run-talos-baseflow-chain

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
  --timeout-seconds 7200
```

重要规则：
1. `run-talos-baseflow-chain` 先做本地 gate，再排队远端 BaseFlow。
2. `--local-batchmode-mode tcp` 是默认和推荐模式；`sync` 只允许显式 `--allow-local-sync-fallback` 后作为诊断回退。
3. 不得用裸 `run-build` 直接触发 `BDFrameworkCore_TalosAIStep01BaseFlowTest` 作为标准回归路径。

## 7. 常用参数要点

### 公共等待参数

| 参数 | 适用命令 | 说明 |
|---|---|---|
| `--wait` | `run-build`、`run-build-group` | 等待构建结束并在失败时打印日志尾部 |
| `--timeout-seconds` | `run-build`、`run-build-group`、`run-talos-baseflow-chain` | helper 内部等待超时 |
| `--poll-interval-seconds` | `run-build`、`run-build-group`、`run-talos-baseflow-chain` | helper 内部轮询间隔 |
| `--log-tail-lines` | `run-build`、`run-build-group`、`run-talos-baseflow-chain` | 失败时打印的日志尾部行数 |

### 常用排队参数

| 参数 | 说明 |
|---|---|
| `--comment` | 保留用户前缀，helper 自动追加测试目标信息 |
| `--tag` | 可重复传入或逗号分隔，只传最小必要元信息 |
| `--property name=value` | 构建属性覆盖，可重复传入 |
| `--dispatch-mode` | `run-build-group` 专用，`auto` / `parallel` / `sequential` |

### 7.1 show-project / verify-vcs

可临时覆盖项目 ID：

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project --project-id BDFramework.Core
```

脚本会先尝试按 `id:` 查询；失败后按 `id / externalId / name` 兜底匹配，打印最终解析结果。

这两个命令是同步只读命令，不涉及等待链路；排查 Versioned Settings、VCS root、buildType 是否已加载时优先使用它们。

### 7.2 apply — Versioned Settings 更新流程

推荐流程始终为 `export-current` → 基于真实返回 JSON 修改 → `apply`，因为不同 TeamCity 版本可能通过不同端点暴露 Versioned Settings。

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
cp .github/skills/teamcity/output/current-versioned-settings.json .github/skills/teamcity/desired-versioned-settings.local.json
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload .github/skills/teamcity/desired-versioned-settings.local.json --dry-run
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload .github/skills/teamcity/desired-versioned-settings.local.json
```

更新前自动备份到 `.github/skills/teamcity/output/versioned-settings.before-update.json`。

### 7.3 run-build — 补充说明

- `--comment` 会保留用户前缀，并自动追加 `测试目标: <buildTypeId>`。
- `--tag` 可重复传入或逗号分隔，仅限平台等关键元信息；helper 不再注入默认 tag。
- `--wait` 除了 `state/status/statusText` 外，还会输出 TeamCity `running-info` 的 `progress`、`hanging` 和 `stage`，并在长时间等待时按心跳重报当前摘要。
- `--wait` 进入轮询前先做一次轻量 state-only 检查；如果构建已 `finished`，直接打印最终摘要退出。

如果只是排队、不等待，可以省略 `--wait`；但省略后不要再由 Agent 自己补一层外部轮询。

### 7.4 run-build-group — 补充说明

- 默认 `--dispatch-mode auto`：只有当空闲兼容 agent 足以覆盖所有 buildType 时才并行，否则自动串行。
- 强制 `parallel` 但 agent 容量不足时，helper 会直接报错，不会静默退回串行。
- 这仍属于官方等待入口；等待逻辑由 helper 内部负责，不要在外层再套 `tc_build_poller.sh` 或自写 REST 轮询。

### 7.5 run-talos-baseflow-chain — 补充说明

链路保证的详细步骤：

1. 本地 gate：通过 `test-batchmode.sh` 运行 `--test-file` 指定的 Playwright spec。
2. 母包重建：在 TeamCity 重建目标平台母包。
3. 远端 BaseFlow：用母包 build id 排队 `BDFrameworkCore_TalosAIStep01BaseFlowTest`。

本地 gate 与远端运行默认使用相同 `--test-file`，确保两侧执行相同 Playwright spec。

如果本地无法运行 TCP batchmode，如 Unity license 限制，`sync` 模式仅作为诊断回退，不作为远端 spec 已本地通过的证明。

`run-talos-baseflow-chain` 属于官方等待入口，但它不是通用轮询器；它只用于 Talos BaseFlow 标准链路。

### 7.6 Re-run Talos BaseFlow with a rebuilt package

需要用已有母包 build id 手动重新触发 BaseFlow 时：

```bash
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

注意事项：

- `talos.e2e.package.build.id` 必须指向一个成功的 `BDFrameworkCore_BuildClientPackageWindows` 构建。
- Playwright spec 文件名变化时，显式传入 `talos.e2e.test.file`，不要依赖 DSL 默认值。
- 只传与任务匹配的平台 tag，如 `--tag windows` 或 `--tag android`。
- 构建内部需要访问 TeamCity 资源时，必须透传 `env.TEAMCITY_TOKEN`。

### 7.7 tc_poll_existing_build.py — 适用边界

这个脚本只适合人工诊断一个已经存在的 build ID，例如：

- 远端已经由别的系统排队好了 build，需要补做只读跟踪。
- shell 脚本需要对既有 build 做单独监控。

它不创建构建，也不替代 `run-build --wait` / `run-build-group --wait`。

### 7.8 tc_latest_branch_report.py — 适用边界

这是内部或临时日志分析脚本，用于快速扫特定分支构建日志中的上传关键字。不要把它视为稳定 CLI 契约，也不要把它写入 Copilot 的默认执行路径。

## 8. BuildType ID 速查表

### 代码与资源构建

| BuildType ID | 说明 |
|---|---|
| `BDFrameworkCore_BuildCodeAndroid` | Android 代码构建 |
| `BDFrameworkCore_BuildCodeIos` | iOS 代码构建 |
| `BDFrameworkCore_BuildCodeWindows` | Windows 代码构建 |
| `BDFrameworkCore_BuildAssetbundleAndroid` | Android AssetBundle 构建 |
| `BDFrameworkCore_BuildAssetbundleIos` | iOS AssetBundle 构建 |
| `BDFrameworkCore_BuildAssetbundleWindows` | Windows AssetBundle 构建 |
| `BDFrameworkCore_BuildTable` | 共享表格构建 |

### 母包与验证

| BuildType ID | 说明 |
|---|---|
| `BDFrameworkCore_BuildClientPackageAndroid` | Android 母包构建 |
| `BDFrameworkCore_BuildClientPackageWindows` | Windows 母包构建 |
| `BDFrameworkCore_BuildClientPackageIos` | iOS 母包构建 |
| `BDFrameworkCore_TestClientRes` | ClientRes 端到端测试入口 |
| `BDFrameworkCore_TalosAIStep01BaseFlowTest` | Talos BaseFlow 测试，必须通过 `run-talos-baseflow-chain` 触发 |
| `BDFrameworkCore_TalosAIStep02FrameworkBusinessTest` | FrameworkBusiness E2E 测试 |

## 9. DSL 参数防膨胀规则

TeamCity Kotlin DSL 参数和 `scriptContent` 不得重复 `buildtools.toml` `[talos.e2e]` 段或 `PlatformProfile` 已提供的默认值。

原则：
1. DSL 层只保留真正需要 TeamCity 快照依赖、手动覆盖或页面输入的参数。
2. 已在 `buildtools.toml` 或 `PlatformProfile` 提供默认值的参数，不得在 DSL `params` 或 `scriptContent` 中重复声明。
3. 新增 E2E DSL 参数前，先确认默认值是否已在配置层提供。

## 10. 本地验证

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
.venv/bin/python -m pytest .github/skills/teamcity/tests -q
```

如果改动涉及 `.test-DevOps/.teamcity/` Kotlin DSL，再运行：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core/.test-DevOps/.teamcity
mvn teamcity-configs:generate
```

补充说明：

1. 仓库已提供 `.mvn/maven.config` 和 `.mvn/local-settings.xml` 禁用 Maven 3.9+ 默认 HTTP blocker。
2. `pom.xml` 优先使用公共 DSL 仓库 `http://svn.funtoo.games/app/dsl-plugins-repository`，内网 `http://192.168.0.240:20000/app/dsl-plugins-repository` 作为备用。
3. 如果 `mvn teamcity-configs:generate` 仍无法解析 `configs-dsl-kotlin-parent`，先检查两个仓库 URL 的可达性。

## 11. 排障

### 11.1 构建已结束但终端仍在等待

优先直接查询 `GET /app/rest/builds/id:<buildId>`，再按 buildId 读取 `test-output.log` 或 `downloadBuildLog.html`，而不是盯着后台终端等待。

### 11.2 内网 URL 未改写

如果远端构建内部仍打印内网 `teamcityBaseUrl` 或 `uploadServerUrl`，应检查 `buildtools.toml` 和相关外部服务配置，而不是只改 `.test-DevOps/.teamcity/.env`。

### 11.3 新 BuildType ID 在服务器查询不到

TeamCity 还没加载新 DSL。确认 `.test-DevOps` 仓库改动已推送，然后检查 `verify-vcs` 返回是否正常。

### 11.4 命令行为与预期不一致

优先检查三件事：

1. 当前 shell 是否已经残留旧的 `TEAMCITY_*` 环境变量。
2. `show-project` / `verify-vcs` 是否能正常返回。
3. 本地 helper 的帮助文本和本文件是否处于同一版本。

## 12. 安全说明

1. 不要把真实 `.env` 提交到 Git。
2. 不要把真实 token、密码写进脚本。
3. 如需分享 payload，先确认其中没有敏感 VCS 信息。

## 13. 维护约定

命令行参数、环境变量、默认输出目录、BuildType ID 示例、支持脚本定位或执行流程变化时，必须在同一改动中同步更新：

- `.github/skills/teamcity/SKILL.md`
- `.github/skills/teamcity/scripts/update_project_settings.py`
- `.github/skills/teamcity/tests/test_env_paths.py`（如果 env 位置或加载方式变化）
- `.test-DevOps/.teamcity/.env.example`

## 14. 目录职责

| 路径 | 职责 |
|---|---|
| `SKILL.md` | Copilot 入口、长篇说明、排障与维护约定 |
| `scripts/update_project_settings.py` | 主脚本，Versioned Settings 查询/导出/更新与构建触发 |
| `scripts/tc_build_poller.sh` | 手工诊断轮询工具，非 Agent 默认入口 |
| `scripts/tc_poll_existing_build.py` | 已存在 build 轮询脚本，非 Agent 默认入口 |
| `scripts/tc_latest_branch_report.py` | 内部或临时日志分析脚本 |
| `tests/` | TeamCity skill 回归测试 |

## 15. 关联文档

- `.test-DevOps/README.md`
- `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/BuildClientPackage/README.md`