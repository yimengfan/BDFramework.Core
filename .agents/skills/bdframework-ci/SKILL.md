---
name: bdframework-ci
description: 'BDFramework CI 与 BatchMode 技能。使用场景：通过 -executeMethod 触发 Unity BatchMode 构建（PublishPipeLineCI 各入口）、排查 BatchMode 崩溃或参数不生效、跑 Python 构建脚本与 pytest、理解 TeamCity Kotlin DSL 与制品上传协议、文件服务器资源验证（VerifyClientRes）、CI 输出目录 Library/CIOutputs、-clientVersion/-buildMode/-buildTarget/-ciOutputRoot/-fileServerUrl 参数。关键字：PublishPipeLineCI、BatchMode、executeMethod、-quit、-batchmode、BuildClientPackageAndroid、BuildCodeAndroid、BuildAssetbundleAndroid、BuildTable、VerifyClientRes、buildtools.toml、artifact_uploader、client_resource_flow、TeamCity、Kotlin DSL、Library/CIOutputs、pytest、ciProjectIsolation。'
---

# BDFramework CI / BatchMode 技能

## 1. 何时使用

命中以下任一情况时加载本技能：

- 用 `-executeMethod` 触发 Unity BatchMode 构建
- BatchMode 崩溃、参数不生效、日志缺失
- 跑 Python 构建脚本 / pytest
- 理解 TeamCity 任务与制品上传
- 资源验证（`VerifyClientRes`）

不适用：**构建内容本身**（用 `bdframework-build-pipeline`）。

## 2. 铁律（先读这 6 条）

| # | 铁律 | 违反后果 |
|---|------|---------|
| 1 | **`-quit` 必需** | 本机 Unity 2021.3.58f1 在 batchmode 主循环因 License 签名校验崩溃（`CheckLicenseActivated()` segfault） |
| 2 | **禁用 `-force-open`** | 本机执行会 Bus Error |
| 3 | **额外参数插到 `-quit` 之前** | Python `insert_command_argument` 的约定 |
| 4 | **BatchMode 下禁用 `TestContext.WriteLine`** | NUnit 依赖 BatchMode 不提供的运行器基础设施，会抛 NRE |
| 5 | **BatchMode 入口日志必须含中文 `测试目的=` 与 `实现手段=`** | 框架强制约定 |
| 6 | **`-fileServerUrl` 必须含 `/files`** | Python 侧 base_url 不含，传给 Unity 时追加 |

## 3. BatchMode `-executeMethod` 入口

全部位于 `BDFramework.Editor.DevOps.PublishPipeLineCI`
（`Packages/com.popo.bdframework/Editor/EditorPipeline/DevOpsPipeline/CI/PublishPipeLineCI.cs`）。

| `-executeMethod` | 转发到 |
|------------------|--------|
| `…PublishPipeLineCI.CheckEditorCode` | `BuildTools_HotfixScript.CheckEditorCode()` |
| `…PublishPipeLineCI.BuildDLL` | **空实现** |
| `…PublishPipeLineCI.BuildTable` | `BuildTools_Excel2SQLite.BuildTableForBatchMode()` |
| `…PublishPipeLineCI.BuildCode{Android,IOS,Windows}` | `BuildTools_Assets.BuildClientResForBatchMode(bt, BuildPackageOption.BuildHotfixCode)` |
| `…PublishPipeLineCI.BuildAssetbundle{Android,IOS,Windows}` | `BuildClientResForBatchMode(bt, BuildPackageOption.BuildArtAssets)` |
| `…PublishPipeLineCI.VerifyClientRes{Android,IOS,Windows}` | `AssetsVersionController.VerifyFileServerAssetsForBatchModeWithRequest` |
| `…PublishPipeLineCI.BuildClientPackage{Android,IOS,Windows}` | 私有 `BuildClientPackageForBatchMode(BuildTarget)`（**完整注入版**） |
| `…PublishPipeLineCI.PublishPackage_{Android,iOS,Windows}{Debug,Release,DebugForProfiler,ReleaseForTest}` | 直接调 `BuildTools_ClientPackage.BuildClientPackageForBatchMode`（**不注入测试程序集**） |

!!! danger "两组母包入口行为不同"
    - `PublishPackage_*`（12 个）：**绕过**测试程序集注入与 `TalosDebugDefineScope`
    - `BuildClientPackage{Android,IOS,Windows}`（3 个）：走完整注入版

    选错会导致 Debug 包里没有测试程序集，或 Release 包里混入测试程序集。

!!! note "Android 验证入口会先准备环境"
    `VerifyClientResAndroid` 先调 `AndroidExternalToolsBatchResolver.EnsureAndroidExternalToolsForBatchMode()`。

### 静态构造

```csharp
static PublishPipeLineCI()
{
    if (Application.isBatchMode) BDFrameworkEditorEnvironment.InitEditorEnvironment();
}
```

!!! danger "BatchMode 下环境不会自动初始化"
    `[InitializeOnLoadMethod]` 在 BatchMode 下不保证触发，必须靠上面的静态构造（或自己调 `InitEditorEnvironment()`）。

## 4. 命令行参数

| 参数 | C# 侧解析点 |
|------|-----------|
| `-clientVersion` | `BuildTools_ClientPackage.ClientVersionBatchArgName` |
| `-buildDebug true\|false` | `BuildTools_ClientPackage` / `PublishPipeLineCI` |
| `-buildMode Debug\|DebugForProfiler\|Release\|ReleaseForTest` | `ResolveClientPackageBuildModeForBatchMode` |
| `-ciOutputRoot <path>` | `BuildTools_Assets.CIOutputRootBatchArgName` |
| `-fileServerUrl` | `AssetsVersionController.FileServerUrlBatchArgName` |
| `-expectedCodeVersion` / `-expectedAssetbundleVersion` / `-expectedTableVersion` | 同名 const |
| `-buildTarget` | **Python 侧读取用于拼命令；C# 侧不解析** |
| `-talosForceE2E` | `TalosE2EBatchBridge` |

### 标准命令模板

```bash
<Unity> -batchmode \
        -projectPath <project_dir> \
        -executeMethod <cls.method> \
        -clientVersion <ver> \
        -logFile <log> \
        [-buildDebug <bool>] \
        [-buildTarget Android|iOS|Win64] \
        [-ciOutputRoot <dir>] \
        -quit
```

| 规则 | 说明 |
|------|------|
| 额外参数插到 **`-quit` 之前** | `-quit` 缺失时追加到末尾 |
| `-buildTarget` 映射 | `android→Android`、`ios→iOS`、`windows→Win64` |
| **ClientRes 类任务必须显式传 `-buildTarget`** | 否则用当前 Editor 平台 |
| Table 任务只传 `-ciOutputRoot` | 上传平台由宿主映射 `TABLE_OUTPUT_PLATFORM_BY_HOST = {mac: osx, windows: windows, linux: linux}` |
| Verify 任务传 `-buildTarget` + `-fileServerUrl` + 三段 `-expected*Version` | |
| **不使用 `-nographics`** | 是否使用都不影响 License 崩溃（崩溃在主循环） |
| **禁止在 Editor 内切换 ClientRes 目标平台** | 通过 `-buildTarget` 指定 |

## 5. Python 脚本

位置：`Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/`（`~` 后缀 = Unity 忽略）。

| 脚本 | `-executeMethod` | 必选 | 可选 |
|------|-----------------|------|------|
| `BuildClientResAssetbundle/build_{android,ios,windows}.py` | `BuildAssetbundle*` | `--client-version` | `--build-name` `--build-number` `--unity-version` `--project-dir` `--debug-build` `--phase all\|build\|upload` `--dry-run` |
| `BuildClientResCode/build_{android,ios,windows}.py` | `BuildCode*` | `--client-version` | 同上 |
| `BuildClientResTable/build_table.py` | `BuildTable` | （`--client-version` 可选） | 同上（无 `--debug-build`） |
| `BuildClientPackage/build_{android,ios,windows}.py` | `BuildClientPackage*` | `--client-version` | `--build-name` `--build-number` `--unity-version` `--project-dir` `--debug-build` `--build-mode` `--file-server-url` `--dry-run` |
| `VerifyClientRes/verify_{android,ios,windows}.py` | `VerifyClientRes*` | `--client-version` `--expected-*` `--server-url` `--config` | … |
| `VerifyClientRes/test_client_res.py` | （纯 TeamCity 调度） | — | 子命令 `resolve-builds` / `wait-builds` / `queue-verify-build` |

### 公共 helper

| 文件 | 关键 API |
|------|---------|
| `Common/artifact_uploader.py` | `resolve_file_server_settings`、`upload_single_file`、`upload_to_remote_root`、`upload_client_package`、`build_artifact_remote_root`、`ArtifactType`、`compute_file_sha256` |
| `Common/client_resource_artifacts.py` | `get_ci_output_root`、`prepare_clean_ci_output_root`、`prepare_{code,assetbundle,table}_upload_source`、`upload_client_res_*`、`validate_uploaded_artifacts` |
| `Common/client_resource_flow.py` | `run_platform_resource_build`、`run_table_resource_build`、`run_platform_resource_verify`、`parse_*_args`、`prepare_platform_ci_project_dir` |
| `Common/client_resource_version_manifest.py` | `clientRes_{platform}/version.info` = `code.assetbundle.table` |
| `Common/buildtools_config.py` | typed dataclass 配置入口（**唯一 TOML 读取点**） |
| `Common/buildtools_config_guard.py` | 拦截 ad hoc TOML 解析 |

## 6. 上传协议

```text
PUT /api/files/{remote_path}?overwrite=true|false
Headers:
    Content-Length
    Content-Type
    X-Checksum-Sha256
    Authorization
期望响应: 201
服务端 5xx → try_recover_failed_upload()

下载: GET /files/{remote_path}
```

远端目录规则：

```text
ClientPackage_{platform}/{build_label}/...
ClientRes_Code_{platform}/{build_label}/...
ClientRes_Assetbundle_{platform}/{build_label}/...
ClientRes_Table/{build_label}/...
clientRes_{platform}/version.info          # {code}.{assetbundle}.{table}
global_version.info                        # JSON，按平台 version_num
```

### 配置优先级

`resolve_file_server_settings`：

```text
显式参数 > 环境变量 ARTIFACT_FILE_SERVER_URL
        > buildtools.toml[artifact_file_server].base_url
        > ARTIFACT_FILE_SERVER_IP / ip
        > 127.0.0.1:20001
```

配置路径：

```text
config_path / --config > BUILDTOOLS_CONFIG > 旧模块级环境变量 > buildtools.toml > buildtools.toml.example
```

### 配置文件

| 文件 | 内容 |
|------|------|
| `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/buildtools.toml` | **唯一来源**：`[artifact_file_server]`、`[ci_server]`、`[ios_xcode]`、`[tests.remote_artifact]` |
| `DevOps/CI/buildtools.toml` | 内容重复的副本（见重构清单） |
| `DevOps/CI/talos_e2e_config.toml` | `[talos.e2e]` |
| `DevOps/Config/BDFrameworkSetting.conf` | 编辑器设置 |
| `DevOps/Config/HotfixFile.conf` | 热更文件过滤规则 |

## 7. CI 输出与平台隔离

```text
<projectDir>/Library/CIOutputs/<build_kind>/<build_name>/<build_number>[/<platform>]
```

`build_kind` ∈ `clientres_code` / `clientres_assetbundle` / `clientres_table`；日志在 `Library/CIOutputs/logs`。

| 阶段 | 行为 |
|------|------|
| `build` | 先清空（`prepare_clean_ci_output_root`） |
| `upload` | **不清空** |

!!! warning "只有 Assetbundle 强制平台隔离 checkout"
    | 任务 | 隔离 |
    |------|------|
    | Assetbundle | ✓ `ClientRes/{platform}`（`git worktree add --force --detach`） |
    | Code / Table / Verify | ✗ `ciProjectIsolation=skipped` |

原因：AB 打包依赖平台相关的 `Library/` 与 SBP 缓存。

**iOS BatchMode SBP 首败重试**：重导纹理 + 清 `Temp/ContentBuildData`；**仅当工程不是平台隔离目录时**才额外 `BuildCache.PurgeCache(false)`。

## 8. pytest

```bash
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/ -q

# 含远端制品测试
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/ --run-remote-artifact-tests

# Talos E2E 工具测试
python -m pytest Packages/com.talosai.e2e/Playwright~/tools/tests/ -q
```

| 测试文件 | 覆盖 |
|---------|------|
| `test_buildclientpackage_helpers.py` | 打包产物生成 / ZIP 条目 |
| `test_buildclientpackage_batchmode.py` | 命名路径、project dir 校验、Unity 可执行解析 |
| `test_buildclientpackage_main_flow.py` | 主流程（fake flow） |
| `test_buildtools_config.py` | external integration config |
| `test_artifact_uploader.py` | 本地上传 HTTP server（成功/错误/恢复） |
| `test_artifact_uploader_remote.py` | 远端 smoke（需 marker） |
| `test_client_resource_artifacts.py` | 输出根清理、staging、manifest 校验 |
| `test_client_resource_flow.py` | 三端 wrapper 委派、flow 步骤、dry-run |
| `test_client_resource_verify.py` | 验证 wrapper |
| `test_client_resource_version_manifest.py` | `version.info` 解析/校验 |
| `test_test_client_res.py` | TeamCity build 复用/等待逻辑 |

## 9. Unity 侧纯逻辑验证

```bash
"$UNITY_PATH" -batchmode -quit \
  -projectPath "/Users/naipaopao/Documents/GitHub/BDFramework.Core" \
  -executeMethod BDFramework.EditorTest.DevOps.PublishPipeLineCITest.RunBatchVerification \
  -logFile /tmp/PublishPipeLineCITest.log

"$UNITY_PATH" -batchmode -quit \
  -projectPath "/Users/naipaopao/Documents/GitHub/BDFramework.Core" \
  -executeMethod BDFramework.EditorTest.AssetsManager.AssetsVersionControllerDevOpsBatchVerification.RunBatchVerification \
  -logFile /tmp/AssetsVersionControllerDevOpsBatchVerification.log
```

## 10. TeamCity

!!! note "DSL 位置不在 `DevOps/` 下"
    在 **`.test-DevOps/.teamcity/`**。

| 文件 | BuildType / Project |
|------|-------------------|
| `Project.kt` | `BDFrameworkCoreProject` = `BDFramework.Core`；subProject：ClientPackage / ClientResCode / ClientResAssetbundle / ClientResTable / TalosAI / TestPipeline |
| `projects/ClientPackage.kt` | `ClientPackage` |
| `projects/ClientResCode.kt` / `ClientResAssetbundle.kt` / `ClientResTable.kt` | `ClientRes_Code` / `ClientRes_Assetbundle` / `ClientRes_Table` |
| `projects/TestPipeline.kt` + `ClientResVerify.kt` | `TestPipeline/TestBuildPipeline_ClientRes` |
| `projects/TalosAI.kt` + `TalosAIE2E.kt` | `TalosAI/TalosAI.E2E` |
| `buildTypes/BuildClientPackage.kt` + `_android/_ios/_windows.kt` | 聚合 + 三端母包 |
| `buildTypes/BuildCode_{android,ios,windows}.kt` | `BDFrameworkCore_BuildCode*` |
| `buildTypes/BuildAssetbundle_{android,ios,windows}.kt` | `BDFrameworkCore_BuildAssetbundle*` |
| `buildTypes/BuildTable.kt` | `BDFrameworkCore_BuildTable` |
| `buildTypes/TestClientRes.kt` | `BDFrameworkCore_TestClientRes`（3 step） |
| `vcsRoots/Github.kt` | `【Github】BDFramework.core` |

通用参数：`build.client.version`（默认 `0.1`）、`build.extra.args`、`ci.python.command`（默认 `python`）、`build.debugBuild`。

典型 step：

```kotlin
scriptContent = """
    %ci.python.command% "Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/BuildClientResAssetbundle/build_android.py" \
      --client-version "%build.client.version%" \
      --build-name "BuildAssetbundle_android" \
      --build-number "%build.number%" \
      --project-dir "%teamcity.build.checkoutDir%" \
      --phase build %build.extra.args%
""".trimIndent()
```

`artifactRules = "Library/CIOutputs/logs => build-logs"`。

!!! tip "TeamCity 操作细节见 `teamcity` skill"
    触发构建、等待完成、查看日志、Versioned Settings 等操作由 `.github/skills/teamcity/SKILL.md` 覆盖。

## 11. 故障对照

| 现象 | 根因 | 处理 |
|------|------|------|
| BatchMode 段错误崩溃 | 缺 `-quit` | 加上 `-quit` |
| Bus Error | 用了 `-force-open` | 移除该参数 |
| 参数不生效 | 插在了 `-quit` 之后 | 插到 `-quit` 之前 |
| `NullReferenceException` in test | 用了 `TestContext.WriteLine` | 改用 `UnityEngine.Debug.Log` |
| 管理器未初始化 / `BResources.ResLoader == null` | BatchMode 下环境未初始化 | 确认 `PublishPipeLineCI` 静态构造生效 |
| ClientRes 构建用了错误平台 | 未传 `-buildTarget` | 显式传 |
| 上传 4xx | `X-Checksum-Sha256` / `Authorization` 不对 | 检查 `buildtools.toml` |
| 验证失败 | 见 `[CI][VerifyClientRes]` 日志 | 检查三段 `-expected*Version` |
| 找不到 `DevOps/CI/githook/` | 该目录**在本仓库不存在** | 由使用方提供 |

## 12. 详细参考

| 文件 | 内容 |
|------|------|
| [references/ci-commands.md](./references/ci-commands.md) | 命令模板、脚本参数矩阵、远端目录规则速查 |

在线文档：

- [DevOps 与 CI](https://yimengfan.github.io/BDFramework.Core/pipeline/devops-ci.md)
- [BatchMode 与 CI 测试](https://yimengfan.github.io/BDFramework.Core/testing/batchmode-tests.md)
- [资源发布](https://yimengfan.github.io/BDFramework.Core/pipeline/publish-assets.md)
