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
- `build_xcode.shell`
- `_unity_batchmode_shared.py`
- `_unity_batchmode_paths.py`
- `_unity_batchmode_logs.py`
- `unity3d_batchmode.py`
- `package_artifacts.py`
- `config/settings.py`
- `common.py`（废弃兼容占位，不再承载流程）

## 任务说明与覆盖流程

- 任务说明：该模块对应 TeamCity `ClientPackage` 页签下的三端母包构建任务，需与 `PublishPipeLineCI` 里的母包 BatchMode 入口及其 `CI(Des)` 注释保持一致。
- 覆盖流程：`--client-version` 与 CI 元数据透传、Unity `BuildClientPackage*` executeMethod 调用、Android/iOS/Windows 三端分支、iOS Xcode 后处理、上传目录 `ClientPackage_{platform}/{buildnum}`、dry-run 与真实构建/上传出口。

## TeamCity 页面描述

- TeamCity 页签：`BDFramework.Core / ClientPackage`
- 聚合任务：`BuildClientPackage`
- 子任务：`BuildClientPackage_android`、`BuildClientPackage_ios`、`BuildClientPackage_windows`
- TeamCity 上的任务描述应该强调：聚合任务只负责三端构建编排，平台子任务才是实际的母包构建与上传执行单元。

## 共享层维护标准

- `build_android.py` / `build_ios.py` / `build_windows.py` 继续作为平台流程主体，负责步骤编排、阶段日志、失败出口和上传时机。
- `unity3d_batchmode.py` 只保留稳定导出接口与职责说明，不再新增具体实现。
- Unity 路径、宿主机差异、工程目录推导统一放进 `_unity_batchmode_paths.py`。
- Unity 进程调用、`-logFile` 增量输出和日志尾部读取统一放进 `_unity_batchmode_logs.py`。
- 配置读取、异常、CI 元数据、日志路径拼装、命令参数拼装等纯共享 helper 放进 `_unity_batchmode_shared.py`。
- 新增共享逻辑时，如果它需要知道平台业务、上传策略或流程顺序，就不要放进共享层。
- 共享层行为有变化时，必须同步更新 pytest、README 中的行为描述，以及受影响的步骤日志断言。

## 验证命令

```bash
python -m pytest DevOps/CI/BuildTools/tests/test_buildclientpackage_helpers.py DevOps/CI/BuildTools/tests/test_buildclientpackage_batchmode.py DevOps/CI/BuildTools/tests/test_buildclientpackage_main_flow.py -q
```

推荐验证顺序：

1. 先跑上面的 pytest。
2. 再执行 README 末尾的三端 dry-run，确认命令拼接、日志路径和 CI 参数正常。
3. 本地通过后，再在 TeamCity 的 `ClientPackage` 页签触发对应平台 smoke test。

## TeamCity 自动化映射

测试用 TeamCity Kotlin DSL 位于：

- `.test-DevOps/.teamcity/settings.kts`

Python 脚本和 TeamCity 任务的索引文档位于：

- `.test-DevOps/README.md`

当前约定：

- `build_android.py` → `BuildClientPackage_android`
- `build_ios.py` → `BuildClientPackage_ios`
- `build_windows.py` → `BuildClientPackage_windows`
- 聚合任务：`BuildClientPackage`（位于 TeamCity `ClientPackage` 子项目下）

TeamCity 页面中，这四个母包相关任务统一位于 `ClientPackage` 子项目（页签）下。

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
- `--debug-build`
- `--file-server-url`
- `--unity-version`
- `--project-dir`
- `--dry-run`

`--debug-build true` now forwards `-buildDebug true` into Unity BatchMode, so `PublishPipeLineCI` can switch the package build to the debug path and inject Talos E2E compilation symbols.

`--file-server-url` overrides the artifact upload target resolved by `package_artifacts.py`. Use it when a TeamCity agent must upload through a temporary proxy or a public file-server route while `buildtools.toml` still points at an internal address.

TeamCity 侧通过 `%build.extra.args%` 透传这些可选参数，例如：

- `--unity-version 2022.3.74f1`
- `--file-server-url https://files.example.com/fileserver`
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
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --file-server-url https://files.example.com/fileserver
python3 DevOps/CI/BuildTools/BuildClientPackage/build_android.py --client-version 0.1.0 --dry-run
python3 DevOps/CI/BuildTools/BuildClientPackage/build_windows.py --client-version 0.1.0 --debug-build true --dry-run
```

## iOS Xcode 后置脚本

Unity 导出 iOS Xcode 工程后，会固定调用 `DevOps/CI/BuildTools/BuildClientPackage/build_xcode.shell`，并显式传入 `--project-dir <xcode_output>`；脚本完成 archive / export 后，会在 Xcode 输出目录的同级目录生成同名 `ipa`：例如 Xcode 目录是 `.../ios/com.demo.game`，则默认 ipa 是 `.../ios/com.demo.game.ipa`。

默认行为：

- Unity 正常会传入 `--project-dir`；如果你手动执行脚本，也可以退回到 `DevOps/PublishPackages/ios/` 下自动选择最新的 Unity Xcode 输出目录
- 优先使用 `.xcworkspace`，找不到时回退到 `.xcodeproj`
- scheme 默认优先 `Unity-iPhone`
- configuration 默认 `Release`
- export method 默认 `development`
- 最终 `ipa` 默认生成在 Xcode 输出目录同级，不放进 Xcode 目录内部
- `xcarchive` 和 `export` 中间目录默认放到 `Library/BuildXcode/`，避免污染最终上传目录

配置优先级：

- `build_xcode.shell` 文件头部的脚本默认值
- `DevOps/CI/BuildTools/buildtools.toml -> [ios_xcode]`
- 环境变量
- 命令行参数

`[ios_xcode]` 现在归类为 external signing config，统一通过 `DevOps/CI/BuildTools/Common/buildtools_config.py` 读取；`build_xcode.shell` 不再维护独立的 TOML 解析逻辑。

如果你们团队希望把常用签名参数固化到仓库里，推荐只把签名信息写到 `buildtools.toml` 的 `[ios_xcode]` 段；其余构建行为继续使用脚本默认值，必要时再通过环境变量或命令行覆盖。例如：

```toml
[ios_xcode]
signing_style = "manual"
team_id = "ABCDE12345"
bundle_identifier = "com.demo.game"
code_sign_identity = "Apple Distribution: Team Name (ABCDE12345)"
provisioning_profile_specifier = "Demo AdHoc"
```

常用覆盖项既支持环境变量，也支持直接执行脚本时传命令行参数：

- `BUILDTOOLS_CONFIG` / `BUILD_XCODE_CONFIG` / `--config`
- `BUILD_XCODE_PROJECT_DIR` / `--project-dir`
- `BUILD_XCODE_SCHEME` / `--scheme`
- `BUILD_XCODE_CONFIGURATION` / `--configuration`
- `BUILD_XCODE_EXPORT_METHOD` / `--export-method`
- `BUILD_XCODE_TEAM_ID` / `--team-id`
- `BUILD_XCODE_SIGN_STYLE` / `--signing-style`
- `BUILD_XCODE_CODE_SIGN_IDENTITY` / `--code-sign-identity`
- `BUILD_XCODE_PROVISIONING_PROFILE_SPECIFIER` / `--provisioning-profile-specifier`
- `BUILD_XCODE_PROVISIONING_PROFILE` / `--provisioning-profile`
- `BUILD_XCODE_BUNDLE_IDENTIFIER` / `--bundle-identifier`
- `BUILD_XCODE_EXPORT_OPTIONS_PLIST` / `--export-options-plist`
- `BUILD_XCODE_DRY_RUN=1` / `--dry-run`

`[ios_xcode]` 支持的键：

- `signing_style`
- `team_id`
- `bundle_identifier`
- `code_sign_identity`
- `provisioning_profile_specifier`
- `provisioning_profile`

非签名参数例如 `project_dir`、`configuration`、`export_method`、`destination`、`clean`、`allow_provisioning_updates` 仍然支持环境变量或命令行覆盖，但不再建议写入 `buildtools.toml`。

如果使用手动签名，至少提供：

- `BUILD_XCODE_SIGN_STYLE=manual`
- `BUILD_XCODE_TEAM_ID`
- `BUILD_XCODE_PROVISIONING_PROFILE_SPECIFIER`
- `BUILD_XCODE_BUNDLE_IDENTIFIER`

示例：

```bash
BUILD_XCODE_TEAM_ID=ABCDE12345 \
BUILD_XCODE_EXPORT_METHOD=development \
DevOps/CI/BuildTools/BuildClientPackage/build_xcode.shell --project-dir DevOps/PublishPackages/ios/com.demo.game

DevOps/CI/BuildTools/BuildClientPackage/build_xcode.shell \
	--project-dir DevOps/PublishPackages/ios/com.demo.game \
	--configuration Release \
	--signing-style manual \
	--bundle-identifier com.demo.game \
	--provisioning-profile-specifier "Demo AdHoc" \
	--export-method ad-hoc
```

## CI 元数据

- Python 脚本统一使用通用命名：`--build-name`、`--build-number`
- 环境变量优先读取通用 CI 名称：`CI_BUILD_NAME`、`CI_BUILD_NUMBER`
- 为兼容已有 TeamCity/Jenkins 环境，脚本仍会兜底读取已有平台环境变量，但文档和 DSL 不再使用 `tc` 前缀参数
- 共享日志根目录由共享层自动决定：TeamCity 下默认 `TCLog`，其他 CI 默认 `CILog`
- 非 dry-run 构建会在 Unity 执行前清空 `DevOps/PublishPackages/<platform>/`，避免旧输出污染本次母包
- 构建成功后，脚本会直接调用 `DevOps/CI/BuildTools/Common/artifact_uploader.py` 对应模块接口上传母包
- 上传目录优先使用 `buildNumber` 作为远端版本段；如果没有 `buildNumber`，则回退到 `clientVersion`
- iOS 上传前会把 `DevOps/PublishPackages/ios/` 下的 Xcode 工程目录压成单个 zip，只上传 zip，不上传 `.ipa`
- Windows 上传前会把可运行目录压成单个 zip；如果输出里存在名为 `不要发布` 的目录，或 Unity Burst 自动生成的 `*_BurstDebugInformation_DoNotShip` 目录，会再分别单独压成 zip 一起上传
- dry-run 只验证参数和 Unity 命令拼接，不会清空输出目录，也不会触发上传

## 文件服务器上传

BuildTools 现在提供公共上传模块：`DevOps/CI/BuildTools/Common/artifact_uploader.py`。

说明：

- `build_android.py` / `build_ios.py` / `build_windows.py` 在真实构建成功后会直接调用这个模块上传母包
- 默认读取 `DevOps/CI/BuildTools/buildtools.toml`
- 已封装四类远端目录：`ClientPackage_{平台}/{buildnum}`、`ClientRes_Code_{平台}/{buildnum}`、`ClientRes_Assetbundle_{平台}/{buildnum}`、`ClientRes_Table/{buildnum}`
- 上传接口支持进度回调，当前 BuildClientPackage 会把每个文件的上传开始/完成事件直接打到 CI 日志

详细说明见：`DevOps/CI/BuildTools/Common/README.md`

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
- `unity3d_batchmode.py` 只做导出层；新增实现直接放到 `_unity_batchmode_*.py`
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

强制要求：只要修改了 TeamCity DSL、脚本参数入口、BuildTools 上传配置、上传远端目录规则或 TeamCity 相关环境参数，就必须重新触发受影响的 TeamCity 任务，并确认最终任务状态、日志中的上传进度以及远端母包目录都与预期一致；不能只看 DSL 已加载或本地 dry-run 通过。

补充提醒：`.test-DevOps/.teamcity/settings.kts` 是 Kotlin Script，脚本级常量请使用普通 `val`，不要使用脚本级 `const val`；如果某个 TeamCity 实例会引用脚本级成员，也优先写成 `val xxx = BuildType({ ... })`，不要写成命名 `object`，否则 TeamCity 服务端可能回退到 last known good settings。

