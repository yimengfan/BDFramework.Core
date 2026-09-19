# Client Package Build

The entry point for building the client package (the installable client build), covering Android / iOS / Windows.

## Entry points

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

Scene constants:

```csharp
SCENE_PATH    = "Assets/Scenes/BDFrame.unity"
QA_SCENE_PATH = "Assets/Scenes/BDFrameForQA.unity"
```

## The four `BuildMode` levels

| Mode | BuildOptions | Test assemblies |
|------|-------------|-----------|
| `Debug` | `CompressWithLz4HC \| Development \| AllowDebugging` | **Injected** |
| `DebugForProfiler` | The above + `ConnectWithProfiler \| EnableDeepProfilingSupport` | **Not injected** |
| `Release` | `CompressWithLz4HC` | Not injected |
| `ReleaseForTest` | `CompressWithLz4HC` | **Injected** |

Helper predicates: `IsDebugBuildMode`, `IsReleaseBuildMode`, `ShouldInjectTestAssemblies`.

!!! danger "`DebugForProfiler` does not inject test assemblies"
    This is deliberate — a profiler build should be close to real performance behaviour.

## Build flow

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

## Platform differences

### Android

```csharp
BuildAPK(BuildMode, string outdir)
```

| Item | Value |
|----|-----|
| Artifact | `DevOps/PublishPackages/android/<Application.identifier>.apk` |
| Symbols | `androidCreateSymbolsZip = true` |
| keystore | **Required**, otherwise `throw "请注意设置apk keystore账号密码"` |
| Version number | `PlayerSettings.Android.bundleVersionCode++` |
| HybridCLR specialisation | `gcIncremental = false` + `ApiCompatibilityLevel.NET_4_6` |

### iOS

```csharp
BuildIpa(BuildMode, string outdir)
```

| Item | Value |
|----|-----|
| Artifact (intermediate) | `DevOps/PublishPackages/ios/<identifier>/` (Xcode project) |
| Artifact (final) | `DevOps/PublishPackages/ios/<identifier>.ipa` (produced by the post-build script) |
| Incremental mode | When `Info.plist` already exists and `Application.platform == OSXEditor` → append `BuildOptions.AcceptExternalModificationsToPlayer` |
| Xcode build | Calls `Packages/com.popo.bdframework/Editor.DevOps~/BuildTools/BuildClientPackage/build_xcode.shell <xcode_dir> <mode...>`; `exitCode != 0` throws |
| Version number | `PlayerSettings.iOS.buildNumber++`, `bundleVersion = config.GetClientVersionNumForIOS()` |

### Windows

```csharp
BuildExe(BuildMode, string outdir)
```

| Item | Value |
|----|-----|
| Artifact | `DevOps/PublishPackages/windows/<identifier>/Launcher.exe` |
| Cleanup | `Directory.Delete(outdir, true)` first |
| Post-build | `EnsureHybridClrHotUpdateAssembliesCopiedToManaged(outputPath)` |

!!! danger "A Windows build must copy the hotfix assemblies afterwards"
    Otherwise hotfix `MonoBehaviour`s in the first scene become **missing script**.

### macOS

**Not implemented**: `default: throw new Exception("未实现打包平台:" + buildTarget)`.

## `BuildPlayerOptions` construction

What is actually called is:

```csharp
BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget, BuildOptions)
```

| Scenario | BuildOptions |
|------|-------------|
| `Debug` | `CompressWithLz4HC \| Development \| AllowDebugging` (plus the profiler options again when `ShouldEnableProfilerForPackageBuild`) |
| `DebugForProfiler` | The above + `ConnectWithProfiler \| EnableDeepProfilingSupport` |
| `Release` / `ReleaseForTest` | `CompressWithLz4HC` |
| iOS extra | `AcceptExternalModificationsToPlayer` (condition above) |
| Windows | `ResolveWindowsBuildOptions(mode)` (**explicitly removes** profiler / deep profiling) |
| scenes | `string[] scenes = { SCENE_PATH }` (single scene) |

`BuildPlayerSettingsScope` (`IDisposable`) stashes and overrides: `development`, `allowDebugging`, `connectProfiler`, `deepProfilingSupport`.

!!! note "Screen resolution / fullscreen are no longer overridden by code"
    They come from `ProjectSettings.asset`.

## Version injection and artifact naming

`OnBeginBuildPackage(BuildTarget, outputpath, clientVersion)` (the default implementation on `ABDFrameworkPublishPipelineBehaviour`):

```text
BasePckScriptSVCVersion = GitProcessor.GetVersion(6);    // git log -1 的 hash 前 6 位
config.ClientVersionNum = clientVersion;
Android: bundleVersionVersionCode++
iOS:     buildNumber++
Windows: bundleVersion = clientVersion
```

**Artifact naming suffix**: in every mode other than `Release`

```csharp
PlayerSettings.productName        += "." + buildMode.ToString().ToLower();
PlayerSettings.applicationIdentifier += 同后缀;
```

Before building, the four possible suffixes (`.Debug` / `.DebugForProfiler` / `.ReleaseForTest` / `.Profiler`) are **tolerantly stripped**, and restored in `finally`.

## Version fallback order

When `-clientVersion` is missing:

```text
① EditorSetting.BuildClientPackage.ClientVersion
② GameBaseConfigProcessor.Config.ClientVersionNum
③ "0.1.0"
```

The `clientVersion` passed in by CI = `compose_client_version(prefix, build_number)` = `major.minor.buildNumber`.

## Related pages

- [Hotfix Code (HybridCLR)](build-hotfix-dll.md) —— `PreBuild` and test assemblies
- [Aggregate asset pipeline](index.md) —— `BuildAll` and `BuildPackageOption`
- [Pipeline Hooks](../editor/publish-hooks.md) —— `OnBegin/EndBuildPackage`
- [DevOps & CI](devops-ci.md) —— BatchMode entry points and Python scripts
- [Asset Publishing](publish-assets.md) —— how client package publishing differs from asset publishing
