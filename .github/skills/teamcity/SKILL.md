---
name: teamcity
description: 'TeamCity Web API 操作技能。使用场景：通过 Copilot 触发构建、等待 build 完成、查看构建日志、查看或更新 Versioned Settings、验证 VCS 配置、检查 agent 状态、批量并行或串行分发构建。关键字：TeamCity、run-build、run-build-group、versioned settings、show-project、verify-vcs、export-current、apply、build queue、agent dispatch、构建日志。'
---

# TeamCity Skill / TeamCity 技能

## 1. Agent 执行铁律 / Agent Execution Rules

### 1.1 唯一等待入口

`run-build --wait`、`run-build-group --wait` 和 `run-talos-baseflow-chain` 是 Agent 等待 TeamCity 的**唯一合法入口**。

执行契约：
1. Agent 只启动**一个**本地 helper 进程，并确保命令带 `--wait`。
2. TeamCity 状态轮询**只允许发生在 helper 进程内部**。
3. Agent 使用当前终端/任务工具自带的进程等待能力等待 helper 命令退出。
4. helper 退出后，读取**一次**最终输出，据此汇报 build ID、URL、状态和失败日志摘要。

### 1.2 禁止模式

- ❌ 不得自己写 TeamCity 等待循环。
- ❌ 不得在 helper 等待期间反复读取终端输出当作进度轮询。
- ❌ 不得使用 `pylanceRunCodeSnippet`、`time.sleep()`、shell/Python 循环、TeamCity REST 查询循环或 `tc_build_poller.sh` 做外层等待。
- ❌ 不得在 `run-build --wait` 已运行时并行启动第二套 TeamCity 轮询。
- ❌ 不得先无 `--wait` 触发构建，再自己轮询该 build ID（除非用户明确要求只触发不等待）。
- ❌ 不得修改 TeamCity Versioned Settings 模式；CI 任务更新必须通过 `apply` 命令正常更新并验证 VCS 加载。

## 2. 制品验证要求

构建 `status=SUCCESS` **不等于**验证通过。必须确认构建实际执行了编译/打包/测试，而非仅完成 checkout 后空跑退出。

检查至少一项证据：
- 构建日志包含编译步骤输出（Tundra/Csc 编译记录、`items updated` > 0、受影响程序集名称出现）。
- 构建日志包含目标 `ExecuteMethod` 的执行记录和正常退出。
- 制品上传步骤完成且 `integrity=verified`，上传文件数和字节数符合预期。
- 构建时长无异常短截（如热更代码构建不到 30 秒时，必须下载日志确认原因；增量编译命中缓存是正常原因，但需有 Tundra 输出佐证）。

只汇报 `status=SUCCESS` 而无制品证据时，**不得标记远端验证通过**。

## 3. 环境配置

环境文件位于 `.test-DevOps/.teamcity/`。

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
cp .test-DevOps/.teamcity/.env.example .test-DevOps/.teamcity/.env
# 编辑 .test-DevOps/.teamcity/.env，填入 TEAMCITY_BASE_URL / TEAMCITY_TOKEN 等
```

认证优先级：`TEAMCITY_TOKEN` > `TEAMCITY_USERNAME` + `TEAMCITY_PASSWORD`。

加载环境并执行：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
setopt allexport && source .test-DevOps/.teamcity/.env && setopt noallexport
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py <command> [options]
```

补充细节：
- `TEAMCITY_BASE_URL` 只控制本地 helper 访问哪个 TeamCity HTTP 入口以及控制台重写后的公开 `webUrl`；不会自动改写 `buildtools.toml` 里的 `[ci_server]` 或 `[artifact_file_server]`。
- 如果 TeamCity 服务器通过 Web API 返回的是内网 `webUrl`，helper 会按当前 `TEAMCITY_BASE_URL` 重写出可直接访问的 `webUrl`，原始地址保留为 `serverWebUrl`。
- 默认输出目录：`.github/skills/teamcity/output/`。如需更改，在 `.test-DevOps/.teamcity/.env` 中设置 `TEAMCITY_OUTPUT_DIR=tc_output`（相对 `.github/skills/teamcity/` 解析）。

## 4. 命令速查

### show-project — 查看项目与 Versioned Settings

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py show-project
```

可临时覆盖项目 ID：`--project-id BDFramework.Core`。脚本先按 `id:` 查询；失败后按 `id / externalId / name` 兜底匹配。

### verify-vcs — 验证 Versioned Settings 是否从 VCS 加载

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py verify-vcs
```

### export-current — 导出当前 Versioned Settings

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py export-current
```

默认输出：`.github/skills/teamcity/output/current-versioned-settings.json`

### apply — 更新 Versioned Settings

推荐流程：`export-current` → 基于真实返回 JSON 修改 → `apply`。不同 TeamCity 版本可能通过不同端点暴露 Versioned Settings，所以始终基于 `export-current` 的真实返回修改。

```bash
# dry-run 先预览
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload desired.json --dry-run
# 正式更新
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py apply --payload desired.json
```

更新前自动备份到 `.github/skills/teamcity/output/versioned-settings.before-update.json`。

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
    --wait \
    --timeout-seconds 7200
```

关键参数：

| 参数 | 说明 | 默认值 |
|---|---|---|
| `--wait` | 等待构建完成，失败时自动打印日志尾部 | 无（不等待） |
| `--timeout-seconds` | 等待超时 | 900 |
| `--poll-interval-seconds` | helper 内部轮询间隔（非 Agent 外层轮询许可） | 5 |
| `--log-tail-lines` | 失败时显示日志行数 | 80 |
| `--comment` | 构建注释，自动追加 `测试目标: <buildTypeId>` | — |
| `--tag` | 可重复传入，仅限平台等关键元信息 | — |
| `--property` | 构建属性覆盖，`name=value` 格式，可重复 | — |

凭据透传：
- `run-build` 自动把当前 `TEAMCITY_TOKEN` 透传给远端构建（当 `env.TEAMCITY_TOKEN` 未显式传入时）。
- 需要不同 token 或 basic-auth 时，显式传入 `--property env.TEAMCITY_*` 覆盖。

补充细节：
- `--comment` 保留用户前缀，自动追加 `测试目标: <buildTypeId>`。
- `--tag` 可重复传入或逗号分隔，仅限平台等关键元信息；helper 不再注入默认 tag。
- `--wait` 除 `state/status/statusText` 外，还输出 `running-info` 的 `progress`、`hanging`、`stage`，长等待时按心跳重报摘要。
- `--wait` 进入轮询前先做轻量 state-only 检查；构建已 `finished` 则直接打印最终摘要退出。

### run-build-group — 批量触发构建

```bash
.venv/bin/python .github/skills/teamcity/scripts/update_project_settings.py run-build-group \
    --build-type-id BDFrameworkCore_BuildClientPackageIos \
    --build-type-id BDFrameworkCore_BuildClientPackageWindows \
    --comment "双端回归" \
    --tag release-check \
    --wait
```

自动决策规则：
1. 查询所有 connected + authorized + enabled 的 agent。
2. 检查每个 agent 当前运行中的构建数。
3. 所有 buildType 都有空闲兼容 agent → 自动并行；否则 → 自动串行。

`--dispatch-mode`：`auto`（默认）/ `parallel`（强制并行）/ `sequential`（强制串行）。

### run-talos-baseflow-chain — 守护式 Talos BaseFlow 链路

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

链路保证：
1. **本地 gate**：先运行 `test-batchmode.sh`，失败则立即中止。
2. **母包构建**：本地 gate 通过后，在 TeamCity 重建目标平台母包。
3. **远端 BaseFlow**：用母包 build id 排队 `BDFrameworkCore_TalosAIStep01BaseFlowTest`。

重要规则：
- `--local-batchmode-mode tcp`（默认）保持与远端 Playwright spec 精确一致。
- `--local-batchmode-mode sync` 是弱一致性回退，必须配合 `--allow-local-sync-fallback` 显式 opt-in。
- **不得**用裸 `run-build` 触发 `BDFrameworkCore_TalosAIStep01BaseFlowTest` 作为标准回归路径（绕过本地 gate）。
- Playwright spec 文件名变化时，必须显式传入 `--property talos.e2e.test.file=...`，不要依赖服务器端默认值。
- 只传与任务匹配的平台 tag（如 `--tag windows` 或 `--tag android`），helper 不再注入默认 tag。
- 本地 gate 与远端运行默认使用相同 `--test-file`，确保两侧执行相同 Playwright spec。
- 本地无法运行 TCP batchmode 时，sync 模式仅作诊断回退，不作为远端 spec 已本地通过的证明。

#### Re-run Talos BaseFlow with a rebuilt package

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

注意：`talos.e2e.package.build.id` 必须指向成功的母包构建；构建内部需要访问 TeamCity 资源时必须透传 `env.TEAMCITY_TOKEN`。

### tc_build_poller.sh — 手工诊断轮询工具

**仅限人工诊断或特殊脚本集成**，不是 Agent 默认等待入口。Agent 已启动 `run-build --wait` 后，不得再并行使用此工具。

```bash
.github/skills/teamcity/scripts/tc_build_poller.sh <build_id> [poll_interval] [timeout]
```

| 参数 | 说明 | 默认值 |
|---|---|---|
| `build_id` | TeamCity 构建 ID（必需） | — |
| `poll_interval` | 内部检查间隔秒数 | 30 |
| `timeout` | 超时秒数 | 7200 |

退出码：0=成功，1=失败，2=超时，3=参数错误。

## 5. BuildType ID 速查表

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

### 母包构建

| BuildType ID | 说明 |
|---|---|
| `BDFrameworkCore_BuildClientPackageAndroid` | Android 母包构建 |
| `BDFrameworkCore_BuildClientPackageWindows` | Windows 母包构建 |
| `BDFrameworkCore_BuildClientPackageIos` | iOS 母包构建 |

### ClientRes 验证

| BuildType ID | 说明 |
|---|---|
| `BDFrameworkCore_TestClientRes` | ClientRes 端到端测试入口（**推荐优先使用**） |
| `BDFrameworkCore_VerifyClientResAndroid` | Android ClientRes 验证（仅排查用） |
| `BDFrameworkCore_VerifyClientResIos` | iOS ClientRes 验证（仅排查用） |
| `BDFrameworkCore_VerifyClientResWindows` | Windows ClientRes 验证（仅排查用） |

### Talos E2E 测试

| BuildType ID | 说明 |
|---|---|
| `BDFrameworkCore_TalosAIStep01BaseFlowTest` | Talos BaseFlow 测试（**必须通过 `run-talos-baseflow-chain` 触发**） |
| `BDFrameworkCore_TalosAIStep02FrameworkBusinessTest` | FrameworkBusiness E2E 测试 |

## 6. DSL 参数防膨胀规则

TeamCity Kotlin DSL 参数和 `scriptContent` 不得重复 `buildtools.toml` `[talos.e2e]` 段或 `PlatformProfile` 已提供的默认值。

原则：DSL 层只保留真正需要 TeamCity 快照依赖、手动覆盖或页面输入的参数。

1. `buildtools.toml [talos.e2e]` 已定义的默认值不得再出现在 DSL `params` 块或 `scriptContent` 中。
2. `PlatformProfile` 按平台补齐的默认值同样不得重复声明。
3. DSL `params` 只保留：固定值参数、需要快照依赖传递的参数、需要人工在 TeamCity 页面覆盖的参数。
4. `scriptContent` 只传递 DSL 层保留的参数；其他参数由 Python runner 从 `buildtools.toml` → `PlatformProfile` → argparse defaults 自动获取。
5. 新增 E2E DSL 参数前，先确认默认值是否已在 `buildtools.toml` 或 `PlatformProfile` 中提供；如果是，不要在 DSL 层重复声明。

已优化参考：`TalosAIBuildAndRunE2ETest.kt`（6 参数）、`TalosAIStep01BaseFlowTest.kt`（7 参数）、`TalosAIStep02FrameworkBusinessTest.kt`（7 参数）。

## 7. 前置校验

修改 `.test-DevOps/.teamcity/` Kotlin DSL 后，先本地 Maven 校验：

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core/.test-DevOps/.teamcity
mvn teamcity-configs:generate
```

Maven 补充：
1. 仓库已提供 `.mvn/maven.config` 和 `.mvn/local-settings.xml` 禁用 Maven 3.9+ 默认 HTTP blocker。
2. `pom.xml` 优先使用公共 DSL 仓库 `http://svn.funtoo.games/app/dsl-plugins-repository`，内网 `http://192.168.0.240:20000/app/dsl-plugins-repository` 作为备用。
3. 如果 `mvn teamcity-configs:generate` 仍无法解析 `configs-dsl-kotlin-parent`，先检查两个仓库 URL 的可达性。

远端触发前确认：
1. `.test-DevOps` 仓库改动已提交并推送。
2. 主仓库业务改动已推送到 TeamCity 实际 checkout 的分支。
3. `show-project` 与 `verify-vcs` 返回正常。
4. Talos BaseFlow 远端验证必须通过 `run-talos-baseflow-chain` 或等效本地 batchmode gate。

## 8. 维护约定

命令行参数、环境变量、默认输出目录、BuildType ID 示例或执行流程变化时，必须在同一改动中同步更新以下文件：

- `.github/skills/teamcity/SKILL.md`（本文件）
- `.github/skills/teamcity/scripts/update_project_settings.py`
- `.github/skills/teamcity/tests/test_env_paths.py`（如果默认路径或加载方式变化）
- `.test-DevOps/.teamcity/.env.example`

## 9. 目录职责

| 路径 | 职责 |
|---|---|
| `SKILL.md` | Copilot 入口与唯一文档，精准摘要 + 决策规则 + 排障 |
| `scripts/update_project_settings.py` | 主脚本，Versioned Settings 查询/导出/更新与构建触发 |
| `scripts/tc_latest_branch_report.py` | 补充性只读日志分析脚本 |
| `scripts/tc_build_poller.sh` | 手工诊断轮询工具，非 Agent 默认入口 |
| `tests/` | 脚本回归测试 |
| `.test-DevOps/.teamcity/.env.example` | 本地环境模板 |
| `.test-DevOps/.teamcity/.env` | 本地私有凭据，不提交 |

## 10. 本地测试

```bash
cd /Users/naipaopao/Documents/GitHub/BDFramework.Core
.venv/bin/python -m pytest .github/skills/teamcity/tests -q
```

## 11. 排障

### 构建已结束但终端仍在等待

优先直接查询 `GET /app/rest/builds/id:<buildId>`，再按 buildId 读取 `test-output.log` 或 `downloadBuildLog.html`，而不是盯着后台终端等待。

### 内网 URL 未改写

远端构建内部仍打印内网 `teamcityBaseUrl` 或 `uploadServerUrl` 时，应检查 `buildtools.toml` 和相关外部服务配置，而不是只改 `.test-DevOps/.teamcity/.env`。

### 新 BuildType ID 在服务器查询不到

TeamCity 还没加载新 DSL。确认 `.test-DevOps` 仓库改动已推送，然后检查 `verify-vcs` 返回是否正常。

## 12. 安全说明

1. 不要把真实 `.env` 提交到 Git。
2. 不要把真实 token、密码写进脚本。
3. 如需分享 payload，先确认其中没有敏感 VCS 信息。

## 13. 关联文档

- 总流程与排查方法：`.test-DevOps/README.md`
- Python 构建脚本说明：`Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/BuildClientPackage/README.md`
