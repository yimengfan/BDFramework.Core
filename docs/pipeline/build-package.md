# 母包构建

母包（客户端安装包）构建入口，支持 Android / iOS / Windows 三平台。

## 入口

```csharp
namespace BDFramework.Editor.BuildPipeline

public static class BuildTools_ClientPackage
{
    // 完整签名
    static public bool Build(BuildMode buildMode, string buildScene, string buildConfig,
                             bool isGenAssets, string outdir, BuildTarget buildTarget,
                             BuildTools_Assets.BuildPackageOption buildOption = BuildAll,
                             string clientVersion = null);

    // 简化重载（自动选场景）
    static public bool Build(BuildMode buildMode, bool isGenAssets, string outdir,
                             BuildTarget buildTarget, …);

    // 平台专用
    public static bool BuildAPK(BuildMode mode, string outdir);      // Android
    public static bool BuildIpa(BuildMode mode, string outdir);      // iOS
    public static bool BuildExe(BuildMode mode, string outdir);      // Windows

    // BatchMode
    public static void BuildClientPackageForBatchMode(BuildTarget buildTarget, BuildMode buildMode);
    public static string GetClientVersionForBatchMode();
    public static string GetDefaultClientVersion();

    internal const string ClientVersionBatchArgName = "-clientVersion";
    public const string DefaultClientVersion = "0.1.0";
}
```

场景常量：

```csharp
SCENE_PATH    = "Assets/Scenes/BDFrame.unity"
QA_SCENE_PATH = "Assets/Scenes/BDFrameForQA.unity"
```

## `BuildMode` 四级

| 模式 | BuildOptions | 测试程序集 |
|------|-------------|-----------|
| `Debug` | `CompressWithLz4HC \| Development \| AllowDebugging` | **注入** |
| `DebugForProfiler` | 上述 + `ConnectWithProfiler \| EnableDeepProfilingSupport` | **不注入** |
| `Release` | `CompressWithLz4HC` | 不注入 |
| `ReleaseForTest` | `CompressWithLz4HC` | **注入** |

辅助判定：`IsDebugBuildMode`、`IsReleaseBuildMode`、`ShouldInjectTestAssemblies`。

!!! danger "`DebugForProfiler` 不注入测试程序集"
    这是刻意的——profiler 构建要接近真实性能表现。

## 构建流程

```text
Build(...)
 ① OnBeginBuildPackage(BuildTarget, outputpath, clientVersion)
 ② LoadConfig 注入 clientVersion
 ③ HybridCLR PreBuild + BuildPlayerSettingsScope
 ④ (可选) BuildAll  ← 由 isGenAssets / buildOption 决定
 ⑤ 拷贝 DevOps → StreamingAssets
 ⑥ BuildAPK / BuildIpa / BuildExe
 ⑦ OnEndBuildPackage
 ⑧ 测试程序集产物校验
 ⑨ 删除 StreamingAssets（清理临时拷贝）
 finally 还原所有被覆盖的设置
```

## 三平台差异

### Android

```csharp
BuildAPK(BuildMode, string outdir)
```

| 项 | 值 |
|----|-----|
| 产物 | `DevOps/PublishPackages/android/<Application.identifier>.apk` |
| 符号 | `androidCreateSymbolsZip = true` |
| keystore | **必填**，否则 `throw "请注意设置apk keystore账号密码"` |
| 版本号 | `PlayerSettings.Android.bundleVersionCode++` |
| HybridCLR 特化 | `gcIncremental = false` + `ApiCompatibilityLevel.NET_4_6` |

### iOS

```csharp
BuildIpa(BuildMode, string outdir)
```

| 项 | 值 |
|----|-----|
| 产物（中间） | `DevOps/PublishPackages/ios/<identifier>/`（Xcode 工程） |
| 产物（最终） | `DevOps/PublishPackages/ios/<identifier>.ipa`（由后置脚本产出） |
| 增量模式 | 已有 `Info.plist` 且 `Application.platform == OSXEditor` → 追加 `BuildOptions.AcceptExternalModificationsToPlayer` |
| Xcode 构建 | 调用 `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/BuildClientPackage/build_xcode.shell <xcode_dir> <mode...>`，`exitCode != 0` 抛异常 |
| 版本号 | `PlayerSettings.iOS.buildNumber++`，`bundleVersion = config.GetClientVersionNumForIOS()` |

### Windows

```csharp
BuildExe(BuildMode, string outdir)
```

| 项 | 值 |
|----|-----|
| 产物 | `DevOps/PublishPackages/windows/<identifier>/Launcher.exe` |
| 清理 | 先 `Directory.Delete(outdir, true)` |
| 后置 | `EnsureHybridClrHotUpdateAssembliesCopiedToManaged(outputPath)` |

!!! danger "Windows 构建后必须补拷贝热更程序集"
    否则首场景的热更 `MonoBehaviour` 会变成 **missing script**。

### macOS

**未实现**：`default: throw new Exception("未实现打包平台:" + buildTarget)`。

## `BuildPlayerOptions` 构造

实际调用的是：

```csharp
BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget, BuildOptions)
```

| 场景 | BuildOptions |
|------|-------------|
| `Debug` | `CompressWithLz4HC \| Development \| AllowDebugging`（若 `ShouldEnableProfilerForPackageBuild` 再叠加 profiler 选项） |
| `DebugForProfiler` | 上述 + `ConnectWithProfiler \| EnableDeepProfilingSupport` |
| `Release` / `ReleaseForTest` | `CompressWithLz4HC` |
| iOS 追加 | `AcceptExternalModificationsToPlayer`（条件见上） |
| Windows | `ResolveWindowsBuildOptions(mode)`（**显式移除** profiler / deep profiling） |
| scenes | `string[] scenes = { SCENE_PATH }`（单场景） |

`BuildPlayerSettingsScope`（`IDisposable`）暂存/覆盖：`development`、`allowDebugging`、`connectProfiler`、`deepProfilingSupport`。

!!! note "窗口分辨率 / 全屏不再由代码覆盖"
    由 `ProjectSettings.asset` 决定。

## 版本注入与产物命名

`OnBeginBuildPackage(BuildTarget, outputpath, clientVersion)`（`ABDFrameworkPublishPipelineBehaviour` 默认实现）：

```text
BasePckScriptSVCVersion = GitProcessor.GetVersion(6);    // git log -1 的 hash 前 6 位
config.ClientVersionNum = clientVersion;
Android: bundleVersionVersionCode++
iOS:     buildNumber++
Windows: bundleVersion = clientVersion
```

**产物命名后缀**：非 `Release` 模式下

```csharp
PlayerSettings.productName        += "." + buildMode.ToString().ToLower();
PlayerSettings.applicationIdentifier += 同后缀;
```

构造前会先做 4 种后缀（`.Debug` / `.DebugForProfiler` / `.ReleaseForTest` / `.Profiler`）的**容错剥离**，`finally` 中还原。

## 版本号回退顺序

`-clientVersion` 缺失时：

```text
① EditorSetting.BuildClientPackage.ClientVersion
② GameBaseConfigProcessor.Config.ClientVersionNum
③ "0.1.0"
```

CI 传入的 `clientVersion` = `compose_client_version(prefix, build_number)` = `major.minor.buildNumber`。

## 相关页面

- [热更代码 HybridCLR](build-hotfix-dll.md) —— `PreBuild` 与测试程序集
- [资源总管道](index.md) —— `BuildAll` 与 `BuildPackageOption`
- [管线回调钩子](../editor/publish-hooks.md) —— `OnBegin/EndBuildPackage`
- [DevOps 与 CI](devops-ci.md) —— BatchMode 入口与 Python 脚本
- [资源发布](publish-assets.md) —— 母包发布与资源发布的区别
