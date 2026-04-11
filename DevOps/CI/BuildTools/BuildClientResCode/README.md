# BuildClientResCode

三端热更代码 CI Python 入口，位于 `DevOps/CI/BuildTools/BuildClientResCode/`。

## 设计原则

1. TeamCity 只负责调度 `build_android.py` / `build_ios.py` / `build_windows.py`，具体业务逻辑不写在 DSL 里。
2. 三个平台脚本只保留最薄的一层入口，通用参数、日志、Unity 命令和上传逻辑统一复用 `Common/client_resource_flow.py` 与 `Common/client_resource_artifacts.py`。
3. 真实构建前必须清理隔离输出目录，默认写到 `Library/CIOutputs/clientres_code/<build_name>/<build_number>/<platform>/`。
4. 上传前只整理热更代码当前需要的文件，不直接整目录上传 Unity 输出根。
5. BatchMode 的 Code CI 会在 Unity 命令行里显式追加 `-buildTarget` 到目标平台，不在 Editor 内切换平台；Code 任务本身不携带 Assetbundle 那类跨平台缓存污染约束，所以共享 flow 默认直接使用传入的 `--project-dir` 或仓库根目录，不额外创建平台 worktree；CI 日志和注释规范与 `BuildClientPackage` 保持一致。

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`

## 任务说明与覆盖流程

- 任务说明：该模块对应 TeamCity `ClientRes_Code` 页签下的三端热更代码构建任务，需与 `PublishPipeLineCI.BuildCode*` 入口及其 `CI(Des)` 注释保持一致。
- 覆盖流程：BatchMode `BuildCode*` executeMethod、`-buildTarget` 透传、隔离输出目录清理、`assets.info` / `assets_subpack.info` / `script/*` hash payload 整理、上传目录 `ClientRes_Code_{platform}/{buildnum}`、dry-run 与真实上传路径。

## TeamCity 页面描述

- TeamCity 页签：`BDFramework.Core / ClientRes_Code`
- 聚合任务：`ClientRes_Code`
- 子任务：`BuildCode_android`、`BuildCode_ios`、`BuildCode_windows`
- TeamCity 上的任务描述应该强调：这里是热更代码 payload 构建与上传任务，不覆盖 AssetBundle 或 Table 资源。

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q
```

推荐验证顺序：

1. 先跑上面的 pytest。
2. 再执行三端 dry-run，确认 `BuildCode_*` 命令行、日志和 staging 目录正确。
3. 本地通过后，再在 TeamCity `ClientRes_Code` 页签触发平台 smoke test。

## TeamCity 自动化映射

- `build_android.py` → `BuildCode_android`
- `build_ios.py` → `BuildCode_ios`
- `build_windows.py` → `BuildCode_windows`
- 聚合任务：`ClientRes_Code`

执行方法：

- Android: `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeAndroid`
- iOS: `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeIOS`
- Windows: `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildCodeWindows`

远端目录：

- `ClientRes_Code_android/{buildnum}/...`
- `ClientRes_Code_ios/{buildnum}/...`
- `ClientRes_Code_windows/{buildnum}/...`

## 支持的 CI 宿主环境

- Android：`macOS / Windows / Linux`
- iOS：`macOS / Windows / Linux`
- Windows：`macOS / Windows / Linux`

## 参数

必填参数：

- `--client-version`

可选参数：

- `--build-name`
- `--build-number`
- `--unity-version`
- `--project-dir`
- `--dry-run`

## 上传内容

上传前会从隔离输出目录里整理以下内容：

- `assets.info`
- `assets_subpack.info`（如果本次 Unity 输出里存在）
- `assets.info` 中声明的 `HashName` 文件，包括 `package_build.info`、`script/*` 等对应的服务器 hash payload

整理完成后还会执行本地 staging 校验：只以 `assets.info` 作为资源清单，把 `LocalPath -> HashName` 整理成服务器布局；`assets_subpack.info` 仅原样上传，不参与资源存在性判断；hash 文件集合必须与 `assets.info` 一致，且声明中必须存在真实 `script/*` payload。

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientResCode/build_android.py --client-version 0.1 --build-name local_code_android --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResCode/build_ios.py --client-version 0.1 --build-name local_code_ios --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResCode/build_windows.py --client-version 0.1 --build-name local_code_windows --build-number 123 --dry-run
```