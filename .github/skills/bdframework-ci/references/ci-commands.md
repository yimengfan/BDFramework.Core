# CI 命令与脚本速查

## Unity 命令模板

```bash
<Unity> -batchmode -quit \
  -projectPath <project_dir> \
  -executeMethod <类.方法> \
  -clientVersion <ver> \
  -logFile <log> \
  [额外参数]
```

!!! danger "三个硬约束"
    | 约束 | 原因 |
    |------|------|
    | **必须 `-quit`** | Unity 2021.3.58f1 在 batchmode 主循环因 License 签名校验崩溃（`CheckLicenseActivated()` segfault） |
    | **禁用 `-force-open`** | 本机 Bus Error |
    | **额外参数插到 `-quit` 之前** | `insert_command_argument` 约定；`-quit` 缺失时追加到末尾 |

## 参数矩阵

| 参数 | 值 | 适用任务 | C# 解析点 |
|------|-----|---------|-----------|
| `-clientVersion` | `major.minor.buildNumber` | 全部 | `BuildTools_ClientPackage.ClientVersionBatchArgName` |
| `-buildDebug` | `true` / `false` | Code / Package | `BatchModeCommandLine.GetBoolArg` |
| `-buildMode` | `Debug` / `DebugForProfiler` / `Release` / `ReleaseForTest` | Package | `ResolveClientPackageBuildModeForBatchMode` |
| `-buildTarget` | `Android` / `iOS` / `Win64` | ClientRes / Verify | **Python 侧读取；C# 侧不解析** |
| `-ciOutputRoot` | 目录路径 | ClientRes / Table | `BuildTools_Assets.CIOutputRootBatchArgName` |
| `-fileServerUrl` | 含 `/files` 的 URL | Verify | `AssetsVersionController.FileServerUrlBatchArgName` |
| `-expectedCodeVersion` | 版本号 | Verify | 同名 const |
| `-expectedAssetbundleVersion` | 版本号 | Verify | 同名 const |
| `-expectedTableVersion` | 版本号 | Verify | 同名 const |
| `-talosForceE2E` | flag | E2E | `TalosE2EBatchBridge` |

**`-buildTarget` 映射**：`android→Android`、`ios→iOS`、`windows→Win64`。

!!! warning "ClientRes 任务必须显式传 `-buildTarget`"
    否则用当前 Editor 平台。**Table 任务只传 `-ciOutputRoot`**，上传平台由宿主映射：

```python
TABLE_OUTPUT_PLATFORM_BY_HOST = {"mac": "osx", "windows": "windows", "linux": "linux"}
```

## Python 脚本参数矩阵

| 脚本 | `-executeMethod` | 必选 | 可选 |
|------|-----------------|------|------|
| `BuildClientResAssetbundle/build_android.py` | `BuildAssetbundleAndroid` | `--client-version` | `--build-name` `--build-number` `--unity-version` `--project-dir` `--debug-build` `--phase` `--dry-run` |
| `BuildClientResAssetbundle/build_ios.py` | `BuildAssetbundleIOS` | 同上 | 同上 |
| `BuildClientResAssetbundle/build_windows.py` | `BuildAssetbundleWindows` | 同上 | 同上 |
| `BuildClientResCode/build_{android,ios,windows}.py` | `BuildCode*` | `--client-version` | 同上 |
| `BuildClientResTable/build_table.py` | `BuildTable` | （`--client-version` 可选） | 同上（无 `--debug-build`） |
| `BuildClientPackage/build_{android,ios,windows}.py` | `BuildClientPackage*` | `--client-version` | `--build-name` `--build-number` `--unity-version` `--project-dir` `--debug-build` `--build-mode` `--file-server-url` `--dry-run` |
| `VerifyClientRes/verify_{android,ios,windows}.py` | `VerifyClientRes*` | `--client-version` `--expected-*` `--server-url` `--config` | … |
| `VerifyClientRes/test_client_res.py` | — | — | 子命令 `resolve-builds` / `wait-builds` / `queue-verify-build` |

`--phase` 取值：`all` / `build` / `upload`。

## 本机验证命令

```bash
# 构建管线纯逻辑
"$UNITY_PATH" -batchmode -quit \
  -projectPath "/Users/naipaopao/Documents/GitHub/BDFramework.Core" \
  -executeMethod BDFramework.EditorTest.DevOps.PublishPipeLineCITest.RunBatchVerification \
  -logFile /tmp/PublishPipeLineCITest.log

# 版本控制协议
"$UNITY_PATH" -batchmode -quit \
  -projectPath "/Users/naipaopao/Documents/GitHub/BDFramework.Core" \
  -executeMethod BDFramework.EditorTest.AssetsManager.AssetsVersionControllerDevOpsBatchVerification.RunBatchVerification \
  -logFile /tmp/AssetsVersionControllerDevOpsBatchVerification.log

# Python 构建工具测试
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/ -q
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/ --run-remote-artifact-tests

# Talos E2E 工具测试
python -m pytest Packages/com.talosai.e2e/Playwright~/tools/tests/ -q
```

## 远端目录规则

```text
ClientPackage_{platform}/{build_label}/...
ClientRes_Code_{platform}/{build_label}/...
ClientRes_Assetbundle_{platform}/{build_label}/...
ClientRes_Table/{build_label}/...
clientRes_{platform}/version.info          # {code}.{assetbundle}.{table}
global_version.info                        # JSON，按平台 version_num
```

## 上传协议

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
      （Python 侧 base_url 不含 /files，传给 Unity 时追加 /files）
```

## 本地 CI 输出

```text
<projectDir>/Library/CIOutputs/<build_kind>/<build_name>/<build_number>[/<platform>]
<projectDir>/Library/CIOutputs/logs
```

| 阶段 | 清理行为 |
|------|---------|
| `build` | 先清空（`prepare_clean_ci_output_root`） |
| `upload` | **不清空** |

`build_kind` ∈ `clientres_code` / `clientres_assetbundle` / `clientres_table`。

## 平台隔离

| 任务 | 隔离 | 机制 |
|------|------|------|
| Assetbundle | ✓ | TeamCity `checkoutDir = ClientRes/{platform}`；`prepare_platform_ci_project_dir` 优先复现 `already_isolated`（父目录名 == platform），否则 `git worktree add --force --detach` 到 `../<platform>/<repo-name>` |
| Code | ✗ | `ciProjectIsolation=skipped` |
| Table | ✗ | 同上 |
| Verify | ✗ | 同上 |

!!! warning "iOS BatchMode SBP 首败重试"
    重导纹理 + 清 `Temp/ContentBuildData`；**仅当工程不是平台隔离目录时**才额外 `BuildCache.PurgeCache(false)`。

## TeamCity 参数

| 参数 | 默认 | 说明 |
|------|------|------|
| `build.client.version` | `0.1` | 客户端版本前缀 |
| `build.extra.args` | — | 追加到 Python 命令末尾 |
| `ci.python.command` | `python` | Python 可执行 |
| `build.debugBuild` | — | Code 任务的 Debug 开关 |

## 相关 skill

- `teamcity` —— 触发/等待构建、查看日志、Versioned Settings
- `bdframework-build-pipeline` —— 构建内容本身
