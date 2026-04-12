# VerifyClientRes

三端热更文件服务器验证 CI Python 入口，位于 `DevOps/CI/BuildTools/VerifyClientRes/`。

## 设计原则

1. TeamCity 端到端入口改为 `test_client_res.py`，由它按 buildStep 显式完成“复用/触发资产构建 -> 等待完成 -> 在当前父构建内直接执行本地 VerifyClientRes 校验脚本”三步编排。
2. 三个平台脚本只保留最薄的一层入口，公共参数、日志、Unity 命令和文件服务器地址解析统一复用 `Common/client_resource_flow.py` 与 `Common/artifact_uploader.py`。
3. 验证任务必须显式传入 `-buildTarget` 到目标平台；验证链路不承担 Assetbundle 构建时的跨平台缓存隔离职责，默认直接使用传入的 `--project-dir` 或仓库根目录，不额外创建平台 worktree。
4. 文件服务器地址必须通过 BuildTools external config 或 `--server-url` 统一解析，不能在 TeamCity DSL 里硬编码。
5. Unity 端会强制重置本地 persistent 下载状态，再真实下载并校验 Code / AssetBundle / Table 三类代表性资源；除了全量 hash/存在性检查外，还会分别做一次热更程序集装载、AssetBundle 本地打开和 SQLite 只读打开，避免历史缓存或“文件存在但本地打不开”把验证变成假阳性。
6. 当前 revision 如果已经存在成功或正在执行中的 TeamCity 资产构建，`test_client_res.py` 会直接复用 build id，而不是重复排队同一个版本。
7. `queue-verify-build` 不再复用或排队 `VerifyClientRes_*` 子任务，而是在当前 `TestClientRes` 父构建内直接启动对应平台的 `verify_{platform}.py`；Step 3 的日志、失败码和 Unity 输出都直接留在父任务里，避免“检查任务却还依赖其他任务”的错误编排。
8. Unity Step 3 内的运行时验证日志统一补齐为稳定的 ASCII 前缀 `[CI][VerifyClientRes]` / `[CI][FileServer]`，并在“元数据重建 -> 全量校验 -> 代表性本地加载”阶段输出明确的开始、进度和完成日志，方便直接判断当前卡在哪个资源或阶段。下载与全量校验仍在线程池里执行，但代表性本地加载会在外层 batchmode 同步桥接收口回 Unity 主线程执行，因此正常路径应看到 `mainThreadDispatch status=already-main-thread`；如果历史线程切换路径被触发，仍会输出 `mainThreadDispatch status=queued|entered|timeout` 并直接报错而不是继续无输出悬挂。

## 文件说明

- `test_client_res.py`
- `verify_android.py`
- `verify_ios.py`
- `verify_windows.py`

## 测试目标与范围

- 测试目标：验证 TeamCity 在拿到 Code / AssetBundle / Table 三段构建号后，Unity 端会读取远端 `version.info`、执行真实下载，并确认三类代表性资源能够在本地真实打开。
- 测试范围：`--expected-*` 版本号透传、文件服务器地址解析、远端 `clientRes_{platform}/version.info` 校验、本地 persistent 重置、真实下载、`package_build.info` 回写校验、Code 代表性程序集装载、AssetBundle 代表性 bundle 本地打开、Table 代表性 SQLite 只读打开。

## TeamCity 描述

- TeamCity 页签：`BDFramework.Core / TestPipeline / TestBuildPipeline_ClientRes`
- 调度任务：`TestClientRes`
- 独立排障任务：`VerifyClientRes_android`、`VerifyClientRes_ios`、`VerifyClientRes_windows`
- TeamCity 上的任务描述应该强调：`TestClientRes` 负责按平台复用或触发三类资产构建并等待结束，然后在自身 Step 3 内直接执行对应平台的本地下载校验；`VerifyClientRes_*` 仅保留给单独排障或手工复现，不再是 `TestClientRes` 的运行依赖。

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_verify.py DevOps/CI/BuildTools/tests/test_test_client_res.py -q
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

- `test_client_res.py` → `TestClientRes`
- `verify_android.py` → `VerifyClientRes_android`（单独排障入口）
- `verify_ios.py` → `VerifyClientRes_ios`（单独排障入口）
- `verify_windows.py` → `VerifyClientRes_windows`（单独排障入口）
- `TestClientRes` 的三个 step：
	1. 复用或触发平台的 Code / AssetBundle / Table 构建任务
	2. 等待三个 TeamCity 构建结束并导出三段 build.number
	3. 在当前父构建内直接执行对应平台的 `verify_{platform}.py` 本地检查脚本

执行方法：

- 调度任务：`test_client_res.py resolve-builds` / `wait-builds` / `queue-verify-build`
- 其中 `queue-verify-build` 虽然沿用旧命令名，但实际语义已经改为“当前构建内直接执行本地校验脚本”，不再与 TeamCity 子任务交互
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

- `test_client_res.py resolve-builds`: `--platform`、`--client-version`、`--vcs-revision`
- `test_client_res.py wait-builds`: `--platform`、`--code-build-id`、`--assetbundle-build-id`、`--table-build-id`
- `test_client_res.py queue-verify-build`: `--platform`、`--client-version`、`--vcs-revision`、`--expected-code-version`、`--expected-assetbundle-version`、`--expected-table-version`
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
python3 DevOps/CI/BuildTools/VerifyClientRes/test_client_res.py resolve-builds --platform android --client-version 0.1 --branch v4/v-4.0.0 --vcs-revision abc123 --upstream-build-extra-args ""
python3 DevOps/CI/BuildTools/VerifyClientRes/test_client_res.py wait-builds --platform android --code-build-id 101 --assetbundle-build-id 202 --table-build-id 303
python3 DevOps/CI/BuildTools/VerifyClientRes/test_client_res.py queue-verify-build --platform android --client-version 0.1 --branch v4/v-4.0.0 --vcs-revision abc123 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303
python3 DevOps/CI/BuildTools/VerifyClientRes/verify_android.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_android --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/VerifyClientRes/verify_ios.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_ios --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/VerifyClientRes/verify_windows.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_windows --build-number 123 --dry-run
```