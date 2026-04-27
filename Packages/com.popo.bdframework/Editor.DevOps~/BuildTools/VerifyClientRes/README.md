# VerifyClientRes

三端热更文件服务器验证 CI Python 入口，位于 `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/`。

## 设计原则

1. TeamCity 端到端入口改为 `test_client_res.py`，由它按 buildStep 显式完成“复用/触发资产构建 -> 等待完成 -> 在当前父构建内直接执行本地 VerifyClientRes 校验脚本”三步编排；其中 Step 2 的 `wait-builds` 会把 `--timeout-seconds` 当成整个等待阶段的共享总预算，而不是给三个子构建分别重置一次。脚本内部的进度横幅统一使用 `Phase x/y`，专门和 TeamCity 外层的 `[Step 1/3]`、`[Step 2/3]`、`[Step 3/3]` 区分，避免把“Step 2 正在等待子构建”误看成“Step 3 本地校验还在等子构建”。
2. 三个平台脚本只保留最薄的一层入口，公共参数、日志、Unity 命令和文件服务器地址解析统一复用 `Common/client_resource_flow.py` 与 `Common/artifact_uploader.py`。
3. 验证任务必须显式传入 `-buildTarget` 到目标平台；验证链路不承担 Assetbundle 构建时的跨平台缓存隔离职责，默认直接使用传入的 `--project-dir` 或仓库根目录，不额外创建平台 worktree。
4. 文件服务器地址必须通过 BuildTools external config 或 `--server-url` 统一解析，不能在 TeamCity DSL 里硬编码。
5. Unity 端会强制重置本地 persistent 下载状态，再真实下载并校验 Code / AssetBundle / Table 三类代表性资源；除了全量 hash/存在性检查外，还会分别做一次热更程序集装载、AssetBundle 本地打开和 SQLite 只读打开，避免历史缓存或“文件存在但本地打不开”把验证变成假阳性。
6. 当前 revision 如果已经存在成功或正在执行中的 TeamCity 资产构建，`test_client_res.py` 会直接复用 build id，而不是重复排队同一个版本。
7. `queue-verify-build` 不再复用或排队 `VerifyClientRes_*` 子任务，而是在当前 `TestClientRes` 父构建内直接启动对应平台的 `verify_{platform}.py`；Step 3 的日志、失败码和 Unity 输出都直接留在父任务里，避免“检查任务却还依赖其他任务”的错误编排。
8. Unity Step 3 内的运行时验证日志统一补齐为稳定的 ASCII 前缀 `[CI][VerifyClientRes]` / `[CI][FileServer]`，并在“元数据重建 -> 全量校验 -> 代表性本地加载 -> AssetBundle 按资产加载”阶段输出明确的开始、`测试目的=`、`实现手段=`、进度和完成日志，方便直接判断当前卡在哪个资源或阶段。下载与全量校验仍在线程池里执行，但代表性本地加载会在外层 batchmode 同步桥接收口回 Unity 主线程执行，因此正常路径应看到 `mainThreadDispatch status=already-main-thread`；如果历史线程切换路径被触发，仍会输出 `mainThreadDispatch status=queued|entered|timeout` 并直接报错而不是继续无输出悬挂。Step 2 等待 TeamCity 子构建时，除了 `statusText` 之外，也会输出 TeamCity `running-info` 里的 `progress`、`hanging` 和 `stage`，便于判断当前到底卡在排队、Library 导入、Unity 执行还是其他阶段。超时错误会附带最后一次状态、`webUrl` 和子构建日志尾部，便于直接排查。
9. 包内 Unity 纯逻辑 batch 验证入口同样必须在 suite 开始和每个 check 开始时输出 `测试目的=` / `实现手段=`，并持续输出 `[测试进度]`，这样 TeamCity 或本地 batchmode 不需要依赖 NUnit 宿主也能直接看到当前验证目标与执行进度。
10. `queue-verify-build` 真正启动本地校验脚本后，会先输出 `localVerifyLaunch=started waitingForUnityOutput=true`，随后按 `--poll-interval-seconds` 输出 `localVerifyHeartbeat`。从启动日志到第一条 `[CI][VerifyClientRes]` / `[CI][FileServer]` 之间，通常是 Unity 打开工程、初始化 PackageManager 和加载项目的时间，不代表还在等待子构建。
11. `queue-verify-build` 的 `--timeout-seconds` 现在用于本地校验脚本的总运行超时；默认值仍为 5400 秒。为了避免误判，如果你希望某次排障完全不做硬超时、只保留心跳监控，可以显式传入 `--timeout-seconds 0`。

## 文件说明

- `test_client_res.py`
- `verify_android.py`
- `verify_ios.py`
- `verify_windows.py`

## 测试目标与范围

- 测试目标：验证 TeamCity 在拿到 Code / AssetBundle / Table 三段构建号后，Unity 端会读取远端 `version.info`、执行真实下载，并确认三类代表性资源能够在本地真实打开。
- 测试范围：`--expected-*` 版本号透传、文件服务器地址解析、远端 `clientRes_{platform}/version.info` 校验、本地 persistent 重置、真实下载、本地 `package_build.info` 三段版本回写校验、Code 代表性程序集装载、AssetBundle 严格按 `art_assets.info` 资产列表顺序执行本地打开与 `LoadAsset` 校验、Table 代表性 SQLite 只读打开。

## TeamCity 描述

- TeamCity 页签：`BDFramework.Core / TestPipeline / TestBuildPipeline_ClientRes`
- 调度任务：`TestClientRes`
- 独立排障任务：`VerifyClientRes_android`、`VerifyClientRes_ios`、`VerifyClientRes_windows`
- TeamCity 上的任务描述应该强调：`TestClientRes` 负责按平台复用或触发三类资产构建并等待结束，然后在自身 Step 3 内直接执行对应平台的本地下载校验；`VerifyClientRes_*` 仅保留给单独排障或手工复现，不再是 `TestClientRes` 的运行依赖。

## 验证命令

```bash
python -m pytest Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/test_client_resource_verify.py Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/tests/test_test_client_res.py -q
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
python3 Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/test_client_res.py resolve-builds --platform android --client-version 0.1 --branch v4/v-4.0.0 --vcs-revision abc123 --upstream-build-extra-args ""
python3 Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/test_client_res.py wait-builds --platform android --code-build-id 101 --assetbundle-build-id 202 --table-build-id 303
python3 Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/test_client_res.py queue-verify-build --platform android --client-version 0.1 --branch v4/v-4.0.0 --vcs-revision abc123 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303
python3 Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/verify_android.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_android --build-number 123 --dry-run
python3 Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/verify_ios.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_ios --build-number 123 --dry-run
python3 Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/VerifyClientRes/verify_windows.py --client-version 0.1 --expected-code-version 101 --expected-assetbundle-version 202 --expected-table-version 303 --server-url http://127.0.0.1:20001 --build-name local_verify_windows --build-number 123 --dry-run
```