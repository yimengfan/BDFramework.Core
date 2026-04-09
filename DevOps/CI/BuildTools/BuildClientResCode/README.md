# BuildClientResCode

三端热更代码 CI Python 入口，位于 `DevOps/CI/BuildTools/BuildClientResCode/`。

## 设计原则

1. TeamCity 只负责调度 `build_android.py` / `build_ios.py` / `build_windows.py`，具体业务逻辑不写在 DSL 里。
2. 三个平台脚本只保留最薄的一层入口，通用参数、日志、Unity 命令和上传逻辑统一复用 `Common/client_resource_flow.py` 与 `Common/client_resource_artifacts.py`。
3. 真实构建前必须清理隔离输出目录，默认写到 `Library/CIOutputs/clientres_code/<build_name>/<build_number>/<platform>/`。
4. 上传前只整理热更代码当前需要的文件，不直接整目录上传 Unity 输出根。

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q
```

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

- `script/`
- `package_build.info`
- `assets.info`
- `assets_subpack.info`（如果本次 Unity 输出里存在）

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientResCode/build_android.py --client-version 0.1 --build-name local_code_android --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResCode/build_ios.py --client-version 0.1 --build-name local_code_ios --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResCode/build_windows.py --client-version 0.1 --build-name local_code_windows --build-number 123 --dry-run
```