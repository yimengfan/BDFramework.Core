# BuildClientResAssetbundle

三端热更 Assetbundle CI Python 入口，位于 `DevOps/CI/BuildTools/BuildClientResAssetbundle/`。

## 设计原则

1. TeamCity 只调度脚本入口，具体构建和上传逻辑统一落在 BuildTools Python 层。
2. 三个平台脚本只负责传入平台、日志前缀和 Unity `executeMethod`；公共流程统一复用 `Common/client_resource_flow.py`。
3. 真实构建前必须清理隔离输出目录，默认写到 `Library/CIOutputs/clientres_assetbundle/<build_name>/<build_number>/<platform>/`。
4. 上传前只保留 Assetbundle 相关目录和配置文件，不把热更代码或表格产物混进上传源。
5. BatchMode 的 Assetbundle CI 在进入 Unity 构建前会先清理 SBP / AssetGraph 相关缓存，并在 Unity SBP 阶段禁用 `BuildCache`，避免 TeamCity agent 上的 `WriteSerializedFiles` 读取失效 `CAB-*` 缓存文件。

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q
```

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

- `art_assets/`
- `package_build.info`
- `assets.info`
- `assets_subpack.info`（如果本次 Unity 输出里存在）

整理完成后还会执行两层校验：

- 本地 staging 语义校验：`art_assets/` 里必须有真实 payload，且 `assets.info` / `assets_subpack.info` 声明的 `art_assets/*` 不能缺失
- 远端上传结果校验：递归拉取文件服务器目录，校验整批文件路径和大小都与本地 staging 一致

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientResAssetbundle/build_android.py --client-version 0.1 --build-name local_asset_android --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResAssetbundle/build_ios.py --client-version 0.1 --build-name local_asset_ios --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResAssetbundle/build_windows.py --client-version 0.1 --build-name local_asset_windows --build-number 123 --dry-run
```