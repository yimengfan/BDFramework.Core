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
- `--tag` 可重复传入或使用逗号分隔，脚本会和默认 tags 合并去重。
- 失败时脚本会自动打印末尾构建日志，便于直接定位问题。

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

补充说明：

1. 仓库内已经提供 `.test-DevOps/.teamcity/.mvn/maven.config` 和 `.test-DevOps/.teamcity/.mvn/local-settings.xml`，用于禁用 Maven 3.9+ 默认的 HTTP blocker。
2. `.test-DevOps/.teamcity/pom.xml` 当前优先使用公网 DSL 仓库 `http://svn.funtoo.games/app/dsl-plugins-repository`，内网 `http://192.168.0.240:20000/app/dsl-plugins-repository` 作为次级兜底。
3. 如果 `mvn teamcity-configs:generate` 仍报 `configs-dsl-kotlin-parent` 无法解析，优先检查这两个仓库地址可达性。

## 远端触发前置条件

在 TeamCity 上远端触发任务前，先确认：

1. `.test-DevOps` 仓库改动已提交并推送，Versioned Settings 能加载到最新 revision。
2. 主仓库业务改动也已提交并推送到 TeamCity 实际 checkout 的 GitHub 分支。
3. `show-project` 与 `verify-vcs` 返回正常。
4. 新的 buildType ID 已能在服务器查询到；如果仍然 missing，说明 TeamCity 还没加载新 DSL。

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

