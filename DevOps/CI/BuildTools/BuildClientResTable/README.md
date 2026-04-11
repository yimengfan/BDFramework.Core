# BuildClientResTable

统一表格 CI Python 入口，位于 `DevOps/CI/BuildTools/BuildClientResTable/`。

## 设计原则

1. TeamCity 只调度 `build_table.py`，真正的 Unity BatchMode 参数、隔离输出和上传逻辑统一放在 BuildTools Python 层。
2. 表格任务只保留一个子任务 `BuildTable`，由 Python 脚本统一构建 `client.db / server.db`。
3. 真实构建前必须清理隔离输出目录，默认写到 `Library/CIOutputs/clientres_table/<build_name>/<build_number>/`。
4. 上传前会把 Unity 产出的 `local.db` 规范化重命名成 `client.db`，确保文件服务器目录与业务约定一致。
5. `local.db` 在 Unity 输出根下仍然按当前活动 BuildTarget 落到平台子目录，因此上传阶段必须从实际输出里解析这一个平台目录，不能只按 TeamCity agent 宿主 OS 猜目录名。

## 文件说明

- `build_table.py`

## 任务说明与覆盖流程

- 任务说明：该模块对应 TeamCity `ClientRes_Table` 页签下的共享表格构建任务，需与 `PublishPipeLineCI.BuildTable` 入口及其 `CI(Des)` 注释保持一致。
- 覆盖流程：BatchMode `BuildTable` executeMethod、隔离输出目录清理、`client.db` / `server.db` / `package_build.info` 整理、上传目录 `ClientRes_Table/{buildnum}`、dry-run 与真实上传。

## TeamCity 页面描述

- TeamCity 页签：`BDFramework.Core / ClientRes_Table`
- 聚合任务：`ClientRes_Table`
- 子任务：`BuildTable`
- TeamCity 上的任务描述应该强调：该任务处理的是共享表格制品链路，不按 Android/iOS/Windows 划分远端根。

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_client_resource_artifacts.py DevOps/CI/BuildTools/tests/test_client_resource_flow.py -q
```

推荐验证顺序：

1. 先跑上面的 pytest。
2. 再执行 `build_table.py --dry-run`，确认目录发现、日志和远端路径参数正确。
3. 本地通过后，再在 TeamCity `ClientRes_Table` 页签触发真实 smoke test。

## TeamCity 自动化映射

- `build_table.py` → `BuildTable`
- 聚合任务：`ClientRes_Table`

执行方法：

- `BDFramework.Editor.DevOps.PublishPipeLineCI.BuildTable`

远端目录：

- `ClientRes_Table/{buildnum}/...`

## 支持的 CI 宿主环境

- `macOS / Windows / Linux`

## 参数

可选参数：

- `--client-version`
- `--build-name`
- `--build-number`
- `--unity-version`
- `--project-dir`
- `--dry-run`

`--build-number` 是远端目录名的首选来源；如果本地 dry-run 或手工执行没有传 `--build-number`，脚本会回退到 `--client-version`，再回退到固定标签 `table` 作为本地日志和上传摘要标识。

## 上传内容

上传前会整理以下内容：

- `client.db`
- `server.db`
- `package_build.info`

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientResTable/build_table.py --build-name local_table --build-number 123 --dry-run
python3 DevOps/CI/BuildTools/BuildClientResTable/build_table.py --client-version 0.1 --build-name local_table --build-number 123 --dry-run
```