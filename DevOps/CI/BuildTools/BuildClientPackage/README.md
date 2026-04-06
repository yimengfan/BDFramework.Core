# BuildClientPackage

母包 CI Python 入口，当前位于 `DevOps/CI/BuildTools/BuildClientPackage/`。

## 设计原则

1. `build_android.py` / `build_ios.py` / `build_windows.py` 是执行主体。
2. 不再把完整流程统一塞到一个 generic `common.py` 中。
3. 只有 Unity BatchMode 的共通能力抽到 `unity3d_batchmode.py`。
4. 脚本内必须保留充分注释、流程日志和边界处理，方便 CI 排查问题。

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`
- `unity3d_batchmode.py`
- `config/settings.py`
- `common.py`（废弃兼容占位，不再承载流程）

## TeamCity 自动化映射

测试用 TeamCity Kotlin DSL 位于：

- `.test-DevOps/.teamcity/settings.kts`

Python 脚本和 TeamCity 任务的索引文档位于：

- `.test-DevOps/README.md`

当前约定：

- `build_android.py` → `BuildClientPackage_android`
- `build_ios.py` → `BuildClientPackage_ios`
- `build_windows.py` → `BuildClientPackage_windows`
- 根聚合任务：`BuildClientPackage`

> 规则：根任务名称允许单独维护；子任务名称、脚本路径、参数入口必须和这里的 Python 脚本保持一致。

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
- 共享日志根目录由 `CI_LOG_ROOT_NAME` 控制；未设置时默认使用 `CILog`

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

## 脚本编写规范

### 1. 注释规范

- 文件头写清楚职责边界
- 关键函数写明“为什么这样做”
- 对平台限制、版本参数、日志路径等关键点补注释

### 2. 日志规范

- 使用明确阶段日志，例如：`Step 1/5`、`Step 2/5`
- 打印：宿主系统、目标平台、clientVersion、Unity 路径、C# executeMethod、日志文件路径
- 失败时打印可直接定位的问题、Unity 日志尾部和后续检查点

### 3. 边界情况

脚本至少要处理：

- `clientVersion` 为空
- `clientVersion` 含异常空白字符
- 宿主系统不支持目标平台
- `UNITY_PATH` 指向不存在路径
- 候选 Unity 路径全部不存在
- Unity BatchMode 返回非 0 退出码

## 维护建议

- 新平台脚本继续保持 `build_xxx.py` 为流程主体
- 新增共通逻辑时，先判断它是否真的只属于 Unity 调用层
- 如果不是纯 Unity 调用辅助，不要继续塞进共享模块

## 与 TeamCity 同步时必须检查的项

以下内容有任何改动，都必须同步更新：

1. `.test-DevOps/.teamcity/settings.kts`
2. `.test-DevOps/.teamcity/BUILD_CLIENT_PACKAGE_INDEX.md`
3. `.test-DevOps/README.md`

重点检查：

- Python 脚本文件名是否变化
- 参数入口是否变化
- `config/settings.py` 中的 `allowed_hosts` 是否变化
- `config/settings.py` 中的 `method` 是否变化
- TeamCity 子任务名称是否仍满足 `BuildClientPackage_xxx` 约定

强制要求：只要修改了 TeamCity DSL、脚本参数入口、`artifactRules` 或 TeamCity 相关环境参数，就必须重新触发受影响的 TeamCity 任务，并确认最终任务状态与预期一致；不能只看 DSL 已加载或本地 dry-run 通过。

补充提醒：`.test-DevOps/.teamcity/settings.kts` 是 Kotlin Script，脚本级常量请使用普通 `val`，不要使用脚本级 `const val`；如果某个 TeamCity 实例会引用脚本级成员，也优先写成 `val xxx = BuildType({ ... })`，不要写成命名 `object`，否则 TeamCity 服务端可能回退到 last known good settings。

