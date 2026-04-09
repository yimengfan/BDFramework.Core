# BuildTools Common

公共 Python 模块目录，当前提供文件服务器上传能力，以及 `ClientRes_*` 任务共享的输出整理和 BatchMode 流程 facade。

实现前先读 [CI 总索引](../../README.md)，再读 [BuildTools 索引](../README.md)。本文只保留 `Common/` 模块规范。

## 文件说明

- `artifact_uploader.py`：上传到 `/.test-DevOps/GameFileServer/` 的公共模块
- `client_resource_artifacts.py`：`ClientRes_Code / ClientRes_Assetbundle / ClientRes_Table` 的隔离输出、产物筛选和上传摘要 helper
- `client_resource_flow.py`：`ClientRes_*` 三类任务复用的 BatchMode 参数、日志和执行主流程
- `__init__.py`：对外导出公共 API

## 设计目标

1. 统一四类制品的远端目录规则，不让调用方散落拼接：
   - `ClientPackage_{平台}/{buildnum}/xxx`
  - `ClientRes_Code_{平台}/{buildnum}/xxx`
  - `ClientRes_Assetbundle_{平台}/{buildnum}/xxx`
  - `ClientRes_Table/{buildnum}/xxx`
2. 既支持上传单文件，也支持把整个目录递归上传。
3. 默认优先读取 `DevOps/CI/BuildTools/buildtools.toml`，整个 BuildTools 共用同一份配置。
4. 支持单独配置“客户端访问文件服务器的 IP”，避免把服务端 `0.0.0.0` 监听地址误当成可访问地址。
5. 上传函数支持进度回调，方便 CI 在长时间上传目录时持续输出阶段日志。
6. `ClientRes_*` 共享 helper 必须先清理隔离输出目录，再筛选当前类型需要的文件，避免把历史构建残留或其他类型产物重复上传。
7. `ClientRes_*` 对应的 Unity CI 入口在执行真正构建前必须显式切换 Editor active build target 到目标平台，不能依赖 TeamCity agent 上一次残留的平台状态。

## 模块加载规范

BuildTools 自己的公共模块和测试不要再复用 `.test-DevOps` 目录侧的动态加载方式。

统一约定：

1. BuildTools 子目录脚本如果要导入 `Common` 模块，先定位 `BuildTools` 根目录，再插入 `sys.path`。
2. 导入统一写成 `from Common.xxx import ...`，不要用 `importlib.util.spec_from_file_location(...)` 侧载文件。
3. BuildTools 单元测试统一放在 `DevOps/CI/BuildTools/tests/`，不要挂到 `.test-DevOps/GameFileServer/tests/` 下。

标准写法如下：

```python
from pathlib import Path
import sys

BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
  sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.artifact_uploader import resolve_file_server_settings, upload_client_package
```

## 文件服务器 IP 配置

推荐在 `DevOps/CI/BuildTools/buildtools.toml` 中配置：

```toml
[artifact_file_server]
ip = "192.168.0.240"
port = 20001
scheme = "http"
```

如果已经有完整地址，也可以直接配置：

```toml
[artifact_file_server]
base_url = "http://192.168.0.240:20001"
```

优先级：

1. `resolve_file_server_settings(server_url=...)`
2. 环境变量 `ARTIFACT_FILE_SERVER_URL`
3. `buildtools.toml -> [artifact_file_server].base_url`
4. `ARTIFACT_FILE_SERVER_IP` / `buildtools.toml -> [artifact_file_server].ip`
5. 回退到 `127.0.0.1:20001`

## 给其他模块调用

如果调用脚本位于 `DevOps/CI/BuildTools/` 的子目录，按上面的模块加载规范导入：

```python
from pathlib import Path
import sys

BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))

from Common.artifact_uploader import resolve_file_server_settings, upload_client_package


settings = resolve_file_server_settings()
upload_client_package(
    "DevOps/PublishPackages/android",
    platform="android",
    build_number="239",
    settings=settings,
)
```

默认会读取：

- `DevOps/CI/BuildTools/buildtools.toml`
- 如果正式配置还没生成，则回退到 `DevOps/CI/BuildTools/buildtools.toml.example`

公共函数：

- `resolve_file_server_settings()`：解析服务地址、token、chunk size
- `build_artifact_remote_root()`：只生成远端根目录
- `build_artifact_remote_path()`：生成完整远端路径
- `upload_artifact()`：通用上传入口
- `upload_client_package()` / `upload_code()` / `upload_asset_bundle()` / `upload_table()`：四类制品快捷入口
- `prepare_clean_ci_output_root()`：生成并清空当前 ClientRes 任务的隔离输出目录
- `prepare_code_upload_source()` / `prepare_assetbundle_upload_source()` / `prepare_table_upload_source()`：把 Unity 输出整理成当前类型真正需要上传的 staging 目录
- `validate_uploaded_artifacts()`：上传后递归比对远端目录，确认整批文件数量、路径、大小都符合预期

ClientRes 类型专项校验：

- Code：`prepare_code_upload_source()` 会校验 `script/` 目录存在真实 payload，并且 `assets.info` / `assets_subpack.info` 中声明的 `script/*` 全部已经落到 staging
- Assetbundle：`prepare_assetbundle_upload_source()` 会校验 `art_assets/` 存在真实 payload，并且 `assets.info` / `assets_subpack.info` 中声明的 `art_assets/*` 全部已经落到 staging
- Table：`prepare_table_upload_source()` 会校验 `local.db` / `server.db` 存在且非空，再重命名为 `client.db` / `server.db` 上传

上传入口都支持两个可选回调：

- `on_uploading(index, total, local_path, remote_path)`：单个文件开始上传前触发
- `on_uploaded(index, total, result)`：单个文件上传成功后触发

这两个回调适合给 TeamCity、Jenkins 这类 CI 持续打印上传进度。

例如 BuildClientPackage 现在会在真实构建完成后：

1. 扫描 `DevOps/PublishPackages/<platform>/`
2. 输出 `uploadFileCount`、`uploadTotalBytes`、`uploadRemoteRoot`
3. 通过 `on_uploading` / `on_uploaded` 持续打印上传进度
4. 最终把目录上传到 `ClientPackage_<platform>/<buildnum>/...`

## 命令行示例

单文件上传：

```bash
python3 DevOps/CI/BuildTools/Common/artifact_uploader.py \
  DevOps/PublishPackages/android/Launcher.apk \
  --artifact-type client-package \
  --platform android \
  --build-number 239
```

目录递归上传：

```bash
python3 DevOps/CI/BuildTools/Common/artifact_uploader.py \
  DevOps/PublishAssets/android/AssetBundles \
  --artifact-type asset-bundle \
  --platform android \
  --build-number 239
```

可选参数：

- `--remote-relative-path`：为单文件指定远端文件名，或为目录指定额外子目录
- `--server-url`：直接覆盖服务地址
- `--token`：直接覆盖 Bearer Token
- `--config`：显式指定 `fileserver.toml`
- `--overwrite true|false`：控制覆盖策略

环境变量补充：

- `ARTIFACT_FILE_SERVER_URL`：直接指定完整 URL
- `ARTIFACT_FILE_SERVER_IP`：只指定客户端访问 IP
- `ARTIFACT_FILE_SERVER_PORT`
- `ARTIFACT_FILE_SERVER_SCHEME`

## 单元测试

`artifact_uploader.py` 的单元测试位于：`DevOps/CI/BuildTools/tests/test_artifact_uploader.py`

执行命令：

```bash
python -m pytest -q DevOps/CI/BuildTools/tests/test_artifact_uploader.py
```

## 远程集成测试

如果需要验证“真实文件已提交到当前配置的文件服务器，且远程列表接口里能看到该文件”，使用：

- `DevOps/CI/BuildTools/tests/test_artifact_uploader_remote.py`

执行命令：

```bash
python -m pytest -q -s DevOps/CI/BuildTools/tests/test_artifact_uploader_remote.py --run-remote-artifact-tests
```

说明：

- 这个测试会读取 `DevOps/CI/BuildTools/buildtools.toml`，并真实上传文件到当前配置的远端服务器。
- 测试会把文件写到 `ClientRes_Table/remote-smoke-tests/<run-id>/artifact_uploader_remote_test.txt`。
- 测试会轮询 `GET /api/files?prefix=...&recursive=false`，确认远程列表里已经出现该文件，并再次下载远端文件校验内容和 SHA256。
- 某些已部署服务可能会在文件已落盘后仍返回 500；这个 smoke test 以“远程列表可见且下载内容一致”作为最终成功依据。
- 文件服务器默认禁用 API 删除，所以这些 smoke test 产物不会自动清理；如果要清理，只能在运维机器上手工删除。

## 模块补充规范

在遵守 [CI 公共规范](../../README.md) 的前提下，本模块额外要求：

1. 远端目录规则、配置解析、上传回调或错误恢复逻辑变更后，必须同步更新并执行 `DevOps/CI/BuildTools/tests/test_artifact_uploader.py`。
2. 真实上传、远端列表校验或下载回读逻辑变更后，必须执行 `python -m pytest -q -s DevOps/CI/BuildTools/tests/test_artifact_uploader_remote.py --run-remote-artifact-tests`。
3. 如果将来把本模块直接接入 TeamCity 构建，必须同步把 TeamCity 验证入口补到 `../../README.md` 与本文档，避免规则散落到多个位置。
4. 新增 ClientRes 共享逻辑时，只允许抽取真正跨类型复用的部分；如果 helper 已经开始耦合具体平台业务或 TeamCity 规则，就应该回收到对应构建类型目录。