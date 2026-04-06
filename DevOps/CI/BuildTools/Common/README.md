# BuildTools Common

公共 Python 模块目录，当前先提供文件服务器上传能力。

## 文件说明

- `artifact_uploader.py`：上传到 `/.test-DevOps/GameFileServer/` 的公共模块
- `__init__.py`：对外导出公共 API

## 设计目标

1. 统一四类制品的远端目录规则，不让调用方散落拼接：
   - `ClientPackage_{平台}/{buildnum}/xxx`
  - `ClientRes_Code_{平台}/{buildnum}/xxx`
  - `ClientRes_AssetBundle_{平台}/{buildnum}/xxx`
  - `ClientRes_table/{buildnum}/xxx`
2. 既支持上传单文件，也支持把整个目录递归上传。
3. 默认优先读取 `DevOps/CI/BuildTools/buildtools.toml`，整个 BuildTools 共用同一份配置。
4. 支持单独配置“客户端访问文件服务器的 IP”，避免把服务端 `0.0.0.0` 监听地址误当成可访问地址。
5. 上传函数支持进度回调，方便 CI 在长时间上传目录时持续输出阶段日志。

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
- 测试会把文件写到 `ClientRes_table/remote-smoke-tests/<run-id>/artifact_uploader_remote_test.txt`。
- 测试会轮询 `GET /api/files?prefix=...&recursive=false`，确认远程列表里已经出现该文件，并再次下载远端文件校验内容和 SHA256。
- 某些已部署服务可能会在文件已落盘后仍返回 500；这个 smoke test 以“远程列表可见且下载内容一致”作为最终成功依据。
- 文件服务器默认禁用 API 删除，所以这些 smoke test 产物不会自动清理；如果要清理，只能在运维机器上手工删除。