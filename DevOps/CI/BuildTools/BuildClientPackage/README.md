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

## 支持的 CI 宿主环境

- Android：`macOS` / `Windows` / `Linux`
- iOS：仅 `macOS`
- Windows：`macOS` / `Windows` / `Linux`

> 说明：Windows 包是否能在非 Windows 宿主上真正完成构建，最终仍取决于 Unity 能力和你的 CI 环境模块安装情况。

## 必填参数

- `--client-version`

## 可选参数

- `--unity-version`
- `--project-dir`
- `--dry-run`

用于只打印 Unity 命令，不真正执行 Unity，适合：

- 本地联调
- CI 脚本自检
- 排查命令拼接问题

## 示例

```bash
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0
python3 DevOps/CI/BuildTools/BuildClientPackage/build_ios.py --client-version 0.1.0
python3 DevOps/CI/BuildTools/BuildClientPackage/build_windows.py --client-version 0.1.0

python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --unity-version 2022.3.74f1
python3 DevOps/CI/BuildTools/BuildClientPackage/build_ios.py --client-version 0.1.0 --project-dir /path/to/UnityProject
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --dry-run
```

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
- 失败时打印可直接定位的问题和后续检查点

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
