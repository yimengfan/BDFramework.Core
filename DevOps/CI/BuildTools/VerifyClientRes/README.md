# VerifyClientRes

三端热更文件服务器验证 CI Python 入口，位于 `DevOps/CI/BuildTools/VerifyClientRes/`。

## 设计原则

1. TeamCity 只负责调度 `verify_android.py` / `verify_ios.py` / `verify_windows.py`，具体验证逻辑统一收敛在 BuildTools Python 与 Unity 公开入口。
2. 三个平台脚本只保留最薄的一层入口，公共参数、日志、Unity 命令和文件服务器地址解析统一复用 `Common/client_resource_flow.py` 与 `Common/artifact_uploader.py`。
3. 验证任务必须显式传入 `-buildTarget` 到目标平台；验证链路不承担 Assetbundle 构建时的跨平台缓存隔离职责，默认直接使用传入的 `--project-dir` 或仓库根目录，不额外创建平台 worktree。
4. 文件服务器地址必须通过 BuildTools external config 或 `--server-url` 统一解析，不能在 TeamCity DSL 里硬编码。
5. Unity 端会强制重置本地 persistent 下载状态，再真实下载并校验 Code / AssetBundle / Table 三类代表性资源，避免历史缓存或 StreamingAssets 残留把验证变成假阳性。

## 文件说明

- `verify_android.py`
- `verify_ios.py`
- `verify_windows.py`

## 测试目标与范围

- 测试目标：验证 TeamCity 在拿到 Code / AssetBundle / Table 三段构建号后，Unity 端会读取远端 `version.info`、执行真实下载，并确认三类代表性资源可用。
- 测试范围：`--expected-*` 版本号透传、文件服务器地址解析、远端 `clientRes_{platform}/version.info` 校验、本地 persistent 重置、真实下载、`package_build.info` 回写校验、Code/AssetBundle/Table 代表性 payload 可用性。

## TeamCity 描述

- TeamCity 页签：`BDFramework.Core / TestPipeline / TestBuildPipeline_ClientRes`
- 聚合任务：`ClientRes_Verify`
- 子任务：`VerifyClientRes_android`、`VerifyClientRes_ios`、`VerifyClientRes_windows`
- TeamCity 上的任务描述应该强调：这些任务不是构建入口，而是依赖前序 build.number 进行远端下载与资源可用性验证的测试入口。

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_verify.py -q
```

推荐验证顺序：

1. 先跑上面的 pytest。
2. 如果改动涉及 Unity 参数装配或运行时验证逻辑，再跑下方两个 Unity batch 纯逻辑入口。
3. 本地通过后，最后在 TeamCity `TestPipeline / TestBuildPipeline_ClientRes` 页签触发三端验证。

如果改动同时影响 Unity 端参数装配或运行时纯逻辑验证，再补跑下面两个 batchmode 入口：

```bash
export UNITY_PATH="/Applications/Unity2021.3.58f1/Unity.app/Contents/MacOS/Unity" # 替换成当前机器实际 Unity 可执行路径
"$UNITY_PATH" -batchmode -quit -projectPath "/Users/naipaopao/Documents/GitHub/BDFramework.Core" -executeMethod BDFramework.EditorTest.DevOps.PublishPipeLineCITest.RunBatchVerification -logFile /tmp/PublishPipeLineCITest.log
"$UNITY_PATH" -batchmode -quit -projectPath "/Users/naipaopao/Documents/GitHub/BDFramework.Core" -executeMethod BDFramework.EditorTest.AssetsManager.AssetsVersionControllerDevOpsBatchVerification.RunBatchVerification -logFile /tmp/AssetsVersionControllerDevOpsBatchVerification.log
```

先完成上面的本地验证，再触发 TeamCity 三端验证。

## TeamCity 自动化映射

TeamCity 页签路径：`BDFramework.Core / TestPipeline / TestBuildPipeline_ClientRes`

- `verify_android.py` → `VerifyClientRes_android`
- `verify_ios.py` → `VerifyClientRes_ios`
- `verify_windows.py` → `VerifyClientRes_windows`
- 聚合任务：`ClientRes_Verify`

执行方法：

- Android: `BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResAndroid`
- iOS: `BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResIOS`
- Windows: `BDFramework.Editor.DevOps.PublishPipeLineCI.VerifyClientResWindows`

验证协议：

- 共享版控入口：`clientRes_{platform}/version.info`
- 代码目录：`ClientRes_Code_{platform}/{buildnum}/...`
- AssetBundle 目录：`ClientRes_Assetbundle_{platform}/{buildnum}/...`
- 表格目录：`ClientRes_Table/{buildnum}/...`

## 支持的 CI 宿主环境

- Android：`macOS / Windows / Linux`
- iOS：`macOS / Windows / Linux`
- Windows：`macOS / Windows / Linux`

## 参数

必填参数：

- `--client-version`
- `--expected-code-version`
- `--expected-assetbundle-version`
- `--expected-table-version`

可选参数：

- `--server-url`
- `--config`
- `--build-name`
- `--build-number`
- `--unity-version`
- `--project-dir`
- `--dry-run`

## 示例

```bash
python3 DevOps/CI/BuildTools/VerifyClientRes/verify_android.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_android --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/VerifyClientRes/verify_ios.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_ios --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/VerifyClientRes/verify_windows.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_windows --build-number 123 --dry-run
```