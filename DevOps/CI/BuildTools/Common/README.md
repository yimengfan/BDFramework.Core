# BuildTools Common

公共 Python 模块目录，当前提供文件服务器上传能力，以及 `ClientRes_*` 任务共享的输出整理和 BatchMode 流程 facade。

实现前先读 [CI 总索引](../../README.md)，再读 [BuildTools 索引](../README.md)。本文只保留 `Common/` 模块规范。

## 文件说明

- `buildtools_config.py`：BuildTools 外部集成配置的统一术语、TOML 读取和 typed dataclass 入口
- `artifact_uploader.py`：上传到 `/.test-DevOps/GameFileServer/` 的公共模块
- `client_resource_artifacts.py`：`ClientRes_Code / ClientRes_Assetbundle / ClientRes_Table` 的隔离输出、产物筛选和上传摘要 helper
- `client_resource_flow.py`：`ClientRes_*` 构建任务与 `VerifyClientRes_*` 验证任务复用的 BatchMode 参数、日志和执行主流程
- `client_resource_version_manifest.py`：维护 `clientRes_{platform}/version.info` 共享版控清单，格式固定为 `code.assetbundle.table`
- `__init__.py`：对外导出公共 API

## 共享版控指针

`client_resource_version_manifest.py` 的职责不是再上传一份资源，而是维护运行时进入文件服务器版控流程所需的单一入口：

- 路径：`clientRes_{platform}/version.info`
- 内容：`{code build}.{assetbundle build}.{table build}`
- 示例：`clientRes_ios/version.info -> 101.202.303`

运行时会先读这份指针文件，再分别到下面三个目录拉对应版本的资源：

- `ClientRes_Code_{platform}/{build}/...`
- `ClientRes_Assetbundle_{platform}/{build}/...`
- `ClientRes_Table/{build}/...`

## 设计目标

1. 统一四类制品的远端目录规则，不让调用方散落拼接：
   - `ClientPackage_{平台}/{buildnum}/xxx`
  - `ClientRes_Code_{平台}/{buildnum}/xxx`
  - `ClientRes_Assetbundle_{平台}/{buildnum}/xxx`
  - `ClientRes_Table/{buildnum}/xxx`
2. 既支持上传单文件，也支持把整个目录递归上传。
3. 默认优先读取 `DevOps/CI/BuildTools/buildtools.toml`，整个 BuildTools 共用同一份配置。
4. 业务无关的 external service / signing / test config 必须先进入 `buildtools.toml`，再由 `Common/buildtools_config.py` 暴露 typed 配置结构给调用方。
5. 支持单独配置“客户端访问文件服务器的 IP”，避免把服务端 `0.0.0.0` 监听地址误当成可访问地址。
6. 上传函数支持进度回调，方便 CI 在长时间上传目录时持续输出阶段日志。
7. `ClientRes_*` 共享 helper 必须先清理隔离输出目录，再筛选当前类型需要的文件，避免把历史构建残留或其他类型产物重复上传。
8. `ClientRes_*` 对应的 Unity BatchMode 命令必须显式传入 `-buildTarget` 到目标平台，不能在 Editor 内切换平台，也不能依赖 TeamCity agent 上一次残留的平台状态。
9. `ClientRes_*` 共享 flow / artifact helper 的注释和日志规范与 `BuildClientPackage` 保持一致：文件头说明职责边界，关键函数说明为什么这样做，CI 日志按 `Step n/m` 输出宿主系统、目标平台、clientVersion、Unity 路径、executeMethod 和日志路径。
10. `ClientRes_Code / ClientRes_Assetbundle / ClientRes_Table` 上传成功后，会同步刷新 `clientRes_{platform}/version.info`，供运行时的 DevOps 文件服务器版控协议读取三段构建号。
11. `ClientRes_Table` 上传阶段不能把 TeamCity agent 的宿主 OS 当成 Unity 表格输出平台；当宿主提示目录不存在时，公共 helper 必须从当前 `ciOutputRoot` 自动发现唯一包含 `local.db + package_build.info` 的平台目录。
12. `VerifyClientRes_*` 必须通过 `Common/artifact_uploader.py -> resolve_file_server_settings()` 解析文件服务器地址，再把 `-fileServerUrl` 和三段期望 build.number 显式透传给 Unity BatchMode；不要在 DSL 或 wrapper 脚本里硬编码服务器地址或重写 TOML 解析。

## 外部集成配置术语

- external integration config：BuildTools 里所有业务无关的外部系统配置总称。
- external service config：文件服务器、CI 服务器这类外部服务的地址、协议、端口、鉴权。
- external signing config：iOS Xcode 这类外部工具链的签名/证书元数据。
- external test config：会访问外部系统的 smoke test / 集成测试开关、命名和超时参数。
- 这些配置统一放在 `DevOps/CI/BuildTools/buildtools.toml`，统一通过 `Common/buildtools_config.py` 读取；现有上传模块和 `build_xcode.shell` 都已经接到这套入口上。
- `.github/hooks/buildtools-config-guard.json` 会阻止 agent 在其他 BuildTools 源文件里重新引入 `tomllib`、手写 `load_toml` 或直接读取 `[artifact_file_server]` / `[ios_xcode]` / `[ci_server]` / `[tests.remote_artifact]`。

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

全局配置路径优先级：

1. 调用方显式传入 `config_path` / `--config`
2. 环境变量 `BUILDTOOLS_CONFIG`
3. 兼容旧入口的模块级环境变量，例如 `ARTIFACT_FILE_SERVER_CONFIG`、`BUILD_XCODE_CONFIG`
4. `DevOps/CI/BuildTools/buildtools.toml`
5. `DevOps/CI/BuildTools/buildtools.toml.example`

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
- 也支持统一环境变量覆盖：`BUILDTOOLS_CONFIG`
- 如果正式配置还没生成，则回退到 `DevOps/CI/BuildTools/buildtools.toml.example`

公共函数：

- `load_buildtools_external_config()`：统一读取 external service / signing / test config dataclass
- `iter_ios_xcode_shell_pairs()`：把 `[ios_xcode]` 的共享签名配置转换成 shell 可消费的键值对
- `resolve_file_server_settings()`：解析服务地址、token、chunk size
- `build_artifact_remote_root()`：只生成远端根目录
- `build_artifact_remote_path()`：生成完整远端路径
- `upload_artifact()`：通用上传入口
- `upload_client_package()` / `upload_code()` / `upload_asset_bundle()` / `upload_table()`：四类制品快捷入口
- `prepare_clean_ci_output_root()`：生成并清空当前 ClientRes 任务的隔离输出目录
- `prepare_code_upload_source()` / `prepare_assetbundle_upload_source()` / `prepare_table_upload_source()`：把 Unity 输出整理成当前类型真正需要上传的 staging 目录
- `validate_uploaded_artifacts()`：上传后递归比对远端目录，确认整批文件数量、路径、大小都符合预期；如果文件服务器在 Windows 上复用了历史目录并保留旧的根目录大小写，校验会把 listing 路径归一化回本次 `remote_root`，并额外打印 `uploadVerifiedRootAliases=...`

ClientRes 类型专项校验：

- Code：`prepare_code_upload_source()` 会保留显式 `package_build.info`、`assets.info` 和可选的 `assets_subpack.info`，再按 `assets.info` 的 `HashName <- LocalPath` 关系把 `package_build.info`、`script/*` 等文件整理成服务器 hash 布局；也就是说组件根目录既有显式元数据文件，也有 hash payload 文件，供 VerifyClientRes 和运行时共用同一套目录协议；校验 hash 文件集合与 `assets.info` 一致，且声明中存在真实 `script/*` payload
- Assetbundle：`prepare_assetbundle_upload_source()` 会保留显式 `package_build.info`、`assets.info` 和可选的 `assets_subpack.info`，再按 `assets.info` 的 `HashName <- LocalPath` 关系把 `package_build.info`、`art_assets/*` 等文件整理成服务器 hash 布局；如果顶层 `assets.info` 没带出真实 `art_assets/*` payload，则回退读取 `art_assets/art_assets.info` 补齐并重写 staging 的 `assets.info`；`assets_subpack.info` 只原样上传，不参与资源清单解析；组件根目录同样需要显式元数据文件和 hash payload 并存，校验 hash 文件集合与 `assets.info` 一致，且声明中存在真实 `art_assets/*` payload
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
- `--config`：显式指定 `buildtools.toml`
- `--overwrite true|false`：控制覆盖策略

环境变量补充：

- `BUILDTOOLS_CONFIG`：统一覆盖 BuildTools 外部集成配置文件路径
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

- 这个测试会读取 `DevOps/CI/BuildTools/buildtools.toml` 的 `[artifact_file_server]` 和 `[tests.remote_artifact]`，并真实上传文件到当前配置的远端服务器。
- 默认只有同时满足 `pytest --run-remote-artifact-tests` 和 `[tests.remote_artifact].enabled = true` 时才会真正访问远端服务器。
- 测试会把文件写到 `ClientRes_Table/<build_number>/<run-id>/<filename>`，其中 `build_number` 和 `filename` 都来自 `[tests.remote_artifact]`。
- 测试会轮询 `GET /api/files?prefix=...&recursive=false`，确认远程列表里已经出现该文件，并再次下载远端文件校验内容和 SHA256。
- 某些已部署服务可能会在文件已落盘后仍返回 500；这个 smoke test 以“远程列表可见且下载内容一致”作为最终成功依据。
- 文件服务器默认禁用 API 删除，所以这些 smoke test 产物不会自动清理；如果要清理，只能在运维机器上手工删除。

## 模块补充规范

在遵守 [CI 公共规范](../../README.md) 的前提下，本模块额外要求：

1. 远端目录规则、配置解析、上传回调或错误恢复逻辑变更后，必须同步更新并执行 `DevOps/CI/BuildTools/tests/test_artifact_uploader.py`。
2. 真实上传、远端列表校验或下载回读逻辑变更后，必须执行 `python -m pytest -q -s DevOps/CI/BuildTools/tests/test_artifact_uploader_remote.py --run-remote-artifact-tests`。
3. 如果将来把本模块直接接入 TeamCity 构建，必须同步把 TeamCity 验证入口补到 `../../README.md` 与本文档，避免规则散落到多个位置。
4. 新增 ClientRes 共享逻辑时，只允许抽取真正跨类型复用的部分；如果 helper 已经开始耦合具体平台业务或 TeamCity 规则，就应该回收到对应构建类型目录。