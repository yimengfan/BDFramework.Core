# BuildClientResAssetbundle

三端热更 Assetbundle CI Python 入口，位于 `DevOps/CI/BuildTools/BuildClientResAssetbundle/`。

## 设计原则

1. TeamCity 只调度脚本入口，具体构建和上传逻辑统一落在 BuildTools Python 层。
2. 三个平台脚本只负责传入平台、日志前缀和 Unity `executeMethod`；公共流程统一复用 `Common/client_resource_flow.py`。
3. 真实构建前必须清理隔离输出目录，默认写到 `Library/CIOutputs/clientres_assetbundle/<build_name>/<build_number>/<platform>/`。
4. 上传前只保留 Assetbundle 相关目录和配置文件，不把热更代码或表格产物混进上传源。
5. BatchMode 的 Assetbundle CI 会在 Unity 命令行里显式追加 `-buildTarget` 到目标平台，不在 Editor 内切换平台；这是当前唯一要求强制做平台工程隔离的 ClientRes 任务，因为 Assetbundle 会受跨平台 Unity 缓存复用影响。TeamCity 侧应把 `checkoutDir` 设为 `/{platform}/%ci.project.checkout.leaf%` 并显式透传 `--project-dir "%teamcity.build.checkoutDir%"`。如果上游 CI 仍在使用共享 checkout，共享 flow 才回退到原工程同级的 `/{platform}/{repo-leaf}/` 隔离 git worktree，让每个平台拥有独立工程目录和 `Library/Temp`。

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`

## 任务说明与覆盖流程

- 任务说明：该模块对应 TeamCity `ClientRes_Assetbundle` 页签下的三端热更 AssetBundle 构建任务，需与 `PublishPipeLineCI.BuildAssetbundle*` 入口及其 `CI(Des)` 注释保持一致。
- 覆盖流程：BatchMode `BuildAssetbundle*` executeMethod、`art_assets/*` payload 产出、`assets.info` / `art_assets.info` 回退逻辑、上传目录 `ClientRes_Assetbundle_{platform}/{buildnum}`、远端目录递归校验、dry-run 与真实上传。

## TeamCity 页面描述

- TeamCity 页签：`BDFramework.Core / ClientRes_Assetbundle`
- 聚合任务：`ClientRes_Assetbundle`
- 子任务：`BuildAssetbundle_android`、`BuildAssetbundle_ios`、`BuildAssetbundle_windows`
- TeamCity 上的任务描述应该强调：这里是热更美术资源 payload 构建、整理与上传一致性任务，不是单纯 Unity 打包成功。

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q
```

推荐验证顺序：

1. 先跑上面的 pytest。
2. 再执行三端 dry-run，确认平台隔离 checkout/worktree、日志和上传前 staging 规则正确。
3. 只有当前 Unity 与包版本能正常编译时，才继续 TeamCity 真实构建与上传验证。

## TeamCity 自动化映射

- `build_android.py` → `BuildAssetbundle_android`
- `build_ios.py` → `BuildAssetbundle_ios`
- `build_windows.py` → `BuildAssetbundle_windows`
- 聚合任务：`ClientRes_Assetbundle`

执行方法：

- Android: `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildAssetbundleAndroid`
- iOS: `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildAssetbundleIOS`
- Windows: `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildAssetbundleWindows`

远端目录：

- `ClientRes_Assetbundle_android/{buildnum}/...`
- `ClientRes_Assetbundle_ios/{buildnum}/...`
- `ClientRes_Assetbundle_windows/{buildnum}/...`

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
- `assets.info` 中声明的 `HashName` 文件，包括 `package_build.info`、`art_assets/*` 等对应的服务器 hash payload

整理完成后还会执行两层校验：

- 本地 staging 语义校验：优先以顶层 `assets.info` 作为资源清单，把 `LocalPath -> HashName` 整理成服务器布局；如果它没有带出真实 `art_assets/*` payload，则回退读取 `art_assets/art_assets.info` 补齐并重写 staging 的 `assets.info`；`assets_subpack.info` 仅原样上传，不参与资源存在性判断；hash 文件集合必须与 `assets.info` 一致，且声明中必须存在真实 `art_assets/*` payload
- 远端上传结果校验：递归拉取文件服务器目录，校验整批文件路径和大小都与本地 staging 一致

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientResAssetbundle/build_android.py --client-version 0.1 --build-name local_asset_android --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResAssetbundle/build_ios.py --client-version 0.1 --build-name local_asset_ios --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResAssetbundle/build_windows.py --client-version 0.1 --build-name local_asset_windows --build-number 123 --dry-run
```