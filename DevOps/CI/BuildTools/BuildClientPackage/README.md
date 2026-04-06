# BuildClientPackage

母包 CI Python 入口，当前位于 `DevOps/CI/BuildTools/BuildClientPackage/`。

实现前先读 [CI 总索引](../../README.md)，再读 [BuildTools 索引](../README.md)。本文只保留 `BuildClientPackage/` 模块规范；公共规范和跨模块约束不再在这里重复维护。

## 设计原则

1. `build_android.py` / `build_ios.py` / `build_windows.py` 是执行主体。
2. 不再把完整流程统一塞到一个 generic `common.py` 中。
3. 只有 Unity BatchMode 的共通能力抽到 `unity3d_batchmode.py`。
4. 平台差异只允许收敛在平台 key、日志前缀和 Unity `executeMethod` 这类模块特有配置点。

## 主要流程

三个平台入口脚本都必须保持相同的七步主流程，差异只允许出现在 `PLATFORM_KEY`、日志前缀和平台对应的 Unity `executeMethod`：

1. 解析参数，并校验 `clientVersion`
2. 解析 `buildName` / `buildNumber`，校验宿主机是否允许执行该平台流程
3. 解析 Unity 可执行路径、Unity 工程目录、C# `executeMethod` 和日志路径
4. 清理 `DevOps/PublishPackages/<platform>/` 目录；`--dry-run` 时跳过
5. 组装 Unity BatchMode 命令
6. 执行 Unity；失败时必须输出 Unity 日志尾部后再抛错
7. 上传 `DevOps/PublishPackages/<platform>/`；`--dry-run` 时跳过

对应单元测试位于：`DevOps/CI/BuildTools/tests/test_buildclientpackage_main_flow.py`

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`
- `unity3d_batchmode.py`
- `package_artifacts.py`
- `config/settings.py`
- `common.py`（废弃兼容占位，不再承载流程）

## TeamCity 验证入口

`BuildClientPackage` 已接入 TeamCity。凡是修改脚本参数、主流程、步骤日志、输出目录、上传逻辑或 TeamCity DSL，都必须在结束前补做 TeamCity 验证。

统一入口：

- TeamCity 总流程与任务映射：`../../../../.test-DevOps/README.md`
- TeamCity Web API 触发与排查：`../../../../.test-DevOps/teamcityskill/README.md`

当前常用 buildType id：

- `BDFrameworkCore_BuildClientPackageAndroid`
- `BDFrameworkCore_BuildClientPackageIos`
- `BDFrameworkCore_BuildClientPackageWindows`

## 支持的 CI 宿主环境

- Android：`macOS` / `Windows` / `Linux`
- iOS：`macOS` / `Windows` / `Linux`
- Windows：`macOS` / `Windows` / `Linux`

> 说明：iOS / Windows 包是否能在非原生宿主上真正完成构建，最终仍取决于 Unity 能力和你的 CI 环境模块安装情况。

## 必填参数

- `--client-version`

## 可选参数

- `--build-name`
- `--build-number`
- `--unity-version`
- `--project-dir`
- `--dry-run`

TeamCity 侧通过 `%build.extra.args%` 透传这些可选参数，例如：

- `--unity-version 2022.3.74f1`
- `--project-dir /path/to/UnityProject`
- `--dry-run`

如果某台 TeamCity Agent 上默认 Python 命令不是 `python`，请同步覆盖 `%ci.python.command%`，例如改成 `python3`、`py -3` 或绝对路径。

用于只打印 Unity 命令，不真正执行 Unity，适合：

- 本地联调
- CI 脚本自检
- 排查命令拼接问题

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0
python3 DevOps/CI/BuildTools/BuildClientPackage/build_ios.py --client-version 0.1.0
python3 DevOps/CI/BuildTools/BuildClientPackage/build_windows.py --client-version 0.1.0

python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --build-name local_android --build-number 123
python3 DevOps/CI/BuildTools/BuildClientPackage/build_ios.py --client-version 0.1.0 --build-name local_ios --build-number 123
python3 DevOps/CI/BuildTools/BuildClientPackage/build_windows.py --client-version 0.1.0 --build-name local_windows --build-number 123

python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --unity-version 2022.3.74f1
python3 DevOps/CI/BuildTools/BuildClientPackage/build_ios.py --client-version 0.1.0 --project-dir /path/to/UnityProject
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --dry-run
```

## CI 元数据

- Python 脚本统一使用通用命名：`--build-name`、`--build-number`
- 环境变量优先读取通用 CI 名称：`CI_BUILD_NAME`、`CI_BUILD_NUMBER`
- 为兼容已有 TeamCity/Jenkins 环境，脚本仍会兜底读取已有平台环境变量，但文档和 DSL 不再使用 `tc` 前缀参数
- 共享日志根目录由共享层自动决定：TeamCity 下默认 `TCLog`，其他 CI 默认 `CILog`
- 非 dry-run 构建会在 Unity 执行前清空 `DevOps/PublishPackages/<platform>/`，避免旧输出污染本次母包
- 构建成功后，脚本会直接调用 `DevOps/CI/BuildTools/Common/artifact_uploader.py` 对应模块接口上传母包
- 上传目录优先使用 `buildNumber` 作为远端版本段；如果没有 `buildNumber`，则回退到 `clientVersion`
- dry-run 只验证参数和 Unity 命令拼接，不会清空输出目录，也不会触发上传

## 文件服务器上传

Step 7 会调用 `../Common/artifact_uploader.py`，把 `DevOps/PublishPackages/<platform>/` 上传到 `ClientPackage_<platform>/<buildnum>/...`。

上传模块的配置来源、远端目录规则、remote smoke test 和公共 API 统一维护在 [BuildTools Common](../Common/README.md)，这里不再重复展开。

## Unity 路径

优先级：

1. 环境变量 `UNITY_PATH`
2. 命令行参数 `--unity-version`
3. `config/settings.py` 中按宿主系统配置的默认 Unity 版本

### Unity 多版本怎么指定

在 `config/settings.py` 中维护：

- `unity.default_version`
- `unity.supported_versions`
- `unity.paths`

其中 `unity.paths` 使用 key-value 形式：

- 第一层：宿主机平台，如 `mac` / `windows` / `linux`
- 第二层：`versioned` / `fallback`
- 第三层：路径名称 => 路径值

例如：

```python
"paths": {
	"mac": {
		"versioned": {
			"hub": "/Applications/Unity{version}/Unity.app/Contents/MacOS/Unity",
		},
		"fallback": {},
	},
}
```

这样做的好处：

- 路径可以按名字索引，便于定位和维护
- 保留声明顺序作为查找优先级
- 新增路径时只需补一个 key-value 条目，不需要维护长列表注释

脚本执行时：

1. 如果设置了 `UNITY_PATH`，直接使用这个路径
2. 如果传了 `--unity-version`，使用指定版本
3. 如果没传，则使用统一的默认版本

例如：

```bash
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --unity-version 2021.3.58f1
```

### 默认 Unity 版本

默认版本定义在：

- `config/settings.py -> SETTINGS["unity"]["default_version"]`

如果你的 CI 机器上常用 Unity 版本变了，优先改这里。

## 工程目录怎么传入

可以通过：

- `--project-dir /your/unity/project/path`

例如：

```bash
python3 DevOps/CI/BuildTools/BuildClientPackage/build_windows.py --client-version 0.1.0 --project-dir /data/build/BDFramework.Core
```

如果不传：

1. 先看 `config/settings.py -> SETTINGS["project"]["default_dir"]`
2. 如果还是 `None`，则从脚本目录自动推导当前仓库的 Unity 工程根目录

在当前精简后的配置里，对应为：

1. `config/settings.py -> SETTINGS["project"]["default_dir"]`
2. 如果还是 `None`，则从脚本目录自动推导当前仓库的 Unity 工程根目录

脚本还会校验该目录是否包含：

- `Assets`
- `Packages`
- `ProjectSettings`

### macOS / Linux

```bash
export UNITY_PATH="/Applications/Unity2021.3.58f1/Unity.app/Contents/MacOS/Unity"
```

### Windows PowerShell

```powershell
$env:UNITY_PATH = 'C:\Program Files\Unity\Hub\Editor\2021.3.58f1\Editor\Unity.exe'
```

## 模块补充规范

在遵守 [CI 公共规范](../../README.md) 的前提下，本模块额外要求：

1. 三个平台入口必须保持同一套七步主流程，平台差异只能出现在 `PLATFORM_KEY`、日志前缀和平台对应的 Unity `executeMethod`。
2. 流程日志必须与 `DevOps/CI/BuildTools/tests/test_buildclientpackage_main_flow.py` 中的步骤断言保持一致。
3. 失败路径必须输出 Unity 日志尾部和下一步检查点，不能只返回非 0 退出码。
4. 下列边界情况至少要被显式处理：

- `clientVersion` 为空
- `clientVersion` 含异常空白字符
- 宿主系统不支持目标平台
- `UNITY_PATH` 指向不存在路径
- 候选 Unity 路径全部不存在
- Unity BatchMode 返回非 0 退出码

## 变更与测试强制规范

以下规则对 `DevOps/CI/BuildTools/BuildClientPackage/` 及其对应测试文件是阻塞性的，不满足就不能结束本次修改：

1. 只要修改主流程、参数入口、步骤日志、Unity 调用、输出目录清理、上传逻辑，就必须同步新增或更新 pytest 单元测试。
2. 主流程相关改动，至少要覆盖：`dry-run` 成功路径、非 `dry-run` 成功路径、Unity 失败后输出日志尾并抛错路径。
3. 每次修改后都必须重新执行并通过以下测试：

```bash
python -m pytest \
	DevOps/CI/BuildTools/tests/test_buildclientpackage_helpers.py \
	DevOps/CI/BuildTools/tests/test_buildclientpackage_main_flow.py \
	-q
```

4. 如果改动影响 TeamCity 映射流程，还必须执行受影响的 TeamCity 构建，并确认最终状态、关键流程日志和远端母包目录符合预期。
5. 如果新增平台入口，必须同步把它纳入主流程参数化测试。
6. 如果修改 README 中记录的步骤、参数或规则，必须保证测试名称和断言仍能对应当前行为。

## 维护建议

- 新平台脚本继续保持 `build_xxx.py` 为流程主体
- 新增共通逻辑时，先判断它是否真的只属于 Unity 调用层
- 如果不是纯 Unity 调用辅助，不要继续塞进共享模块

## 与 TeamCity 同步时必须检查的项

重点检查：

- Python 脚本文件名、参数入口和 `%build.extra.args%` 透传是否仍与脚本保持一致
- `config/settings.py` 中的 `allowed_hosts`、`method` 和默认路径配置是否变化
- TeamCity 任务命令、`%ci.python.command%`、上传远端目录是否仍与当前实现一致
- CI 日志里是否还能看到清空输出目录、Unity 执行、上传开始、上传进度、上传完成这些关键阶段

具体 DSL 文件和排查步骤统一以 `../../../../.test-DevOps/README.md` 为准；不要在本文档里再维护另一份重复的 TeamCity 映射说明。

