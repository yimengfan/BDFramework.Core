# Hotfix Code (HybridCLR)

The hotfix solution is **HybridCLR** (ILRuntime was removed as of v3.0.0).

!!! danger "All ILRuntime-related documentation is now obsolete"
    Content in older docs such as CLRBinding, Adaptor generation and troubleshooting `ILRuntimeCLRBinding` errors **does not apply to this branch at all**. The related implementation is commented out in its entirety in `Unity3dRoslynBuildTools.cs` and has no effect.

## Entry points

```csharp
namespace BDFramework.Editor.HotfixScript

public static class BuildTools_HotfixScript
{
    static public void BuildDLL(string outpath, RuntimePlatform platform);   // 薄封装
}

public static class HyCLREditorTools                                          // 实际实现
{
    static public void SetHyCLRConfig();
    static public bool CheckEditorCode();
    static public void PreBuild(BuildTarget target);
    static public void BuildHotfixDLL(string outputDir, BuildTarget target);
    static public void CopyAOTMetadataDLL(string sourceDir, string outputRoot, BuildTarget target);
}
```

## Assembly set { #hotfix-assembly-set }

Conventions written by `HyCLREditorTools.SetBDFramework2HCLRConfig()`:

| HybridCLR setting | Value |
|-----------------|-----|
| `preserveHotUpdateAssemblies` (appended) | `Assembly-CSharp`, `Assembly-CSharp-firstpass`, `BDFramework.Core` |
| `hotUpdateAssemblies` (removed) | The original list is cleared entirely |
| `patchAOTAssemblies` (appended) | `mscorlib`, `System`, `System.Core` |

!!! note "What `preserveHotUpdateAssemblies` means"
    It means these assemblies **already exist as DLLs inside the client package**; a hotfix replaces them instead of adding new ones.

    Because loading an assembly with the same name twice through `Assembly.Load(byte[])` **produces two copies of every type**, `ScriptLoderAOT.ShouldSkipAlreadyLoadedHotfixAssembly` skips assemblies with the same name that the Player has already preloaded.

`SetHyCLRConfig()` also calls `Unity3dEditorEx.AddSymbols("ENABLE_HYCLR")`.

The hotfix assembly name list comes from `SettingsUtil.HotUpdateAssemblyNamesIncludePreserved`.

## Output layout and constants

| Constant | Value | Defined in |
|------|-----|---------|
| `ScriptLoder.HOTFIX_DLL_PATH` | `script/hotfix` | `Runtime/HotfixScript/ScriptLoder.cs` |
| `ScriptLoder.HYCLR_AOT_PATCH_PATH` | `script/aot_patch` | Same as above |
| `ScriptLoder.HOT_DLL_EXTENSION` | `.zlua.bytes` | Same as above |

```text
DevOps/PublishAssets/<platform>/script/hotfix/<assembly>.zlua.bytes
DevOps/PublishAssets/<platform>/script/aot_patch/<assembly>.zlua.bytes
```

!!! note "The `.zlua.bytes` extension is only a disguise"
    **There is no additional DLL encryption step**; the extension exists only to dodge automatic recognition by certain platforms or tools. (AssetBundles have separate obfuscation: `BuildTools_AssetBundleV2.MixAssetBundle`.)

!!! warning "The constants exist once in each of two assemblies"
    `HYCLR_AOT_PATCH_PATH` / `HOTFIX_DLL_PATH` / `HOT_DLL_EXTENSION` are each defined once in `Runtime/HotfixScript/ScriptLoder.cs` and once in `Runtime.AOT/ScriptLoderAOT.cs`.

    This is **deliberate assembly isolation** (AOT cannot reference Core), but **changing one means changing the other**.

## `PreBuild(BuildTarget target)` steps

```text
① 校验 HybridCLRSettings.Instance != null
     否则 throw new Exception("请先生成HCLR Setting!")

② 平台校验
     不一致 → BDEditorApplication.SwitchToBuildTarget(target) 并复检，失败抛异常

③ SetBDFramework2HCLRConfig()

④ CleanupLegacyGeneratedOutputs()
     删除 Assets/HybridCLRGenerate/link.xml、AOTGenericReferences.cs（+ .meta）
     仅当已迁移到新输出路径时执行

⑤ EnsureHybridClrInstalled(new InstallerController())
     优先 InstallFromLocal(HybridCLRData/il2cpp_plus_repo/libil2cpp)
     失败回退 InstallDefaultHybridCLR()

⑥ PrebuildCommand.GenerateAll()

⑦ CopyAOTMetadataDLL(sourceDir, outputRoot, target)
     sourceDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target)
     输出根 = GetAotMetadataOutputRoots(Application.streamingAssetsPath, BApplication.DevOpsPublishAssetsPath)
     ★ 两个根都要写
```

!!! danger "`PreBuild` writes the AOT patch to two locations"
    `Application.streamingAssetsPath` (embedded in the client package) and `BApplication.DevOpsPublishAssetsPath` (build artifacts).

    In the Editor `streamingAssetsPath` is rewritten to `DevOps/PublishAssets`, so the two are effectively the same place; but on device and in CI both must be written, otherwise the client package is missing its AOT metadata.

## `BuildHotfixDLL` steps

```text
① 临时目录：HybridCLRData/out_hotfixdlls_temp（先删后建）

② CompileDllCommand.CompileDll(tmpOutputPath, target, false)
     挂 Application.logMessageReceived 检测含 "Fail" 的 Error
     失败 → throw "build hotfix_dll failed"

③ CopyHotfixDLLs(tmpOutputPath, outputDir, target)

④ HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(destDLLRootDir, isReleaseBuild)
```

## Test assembly isolation

`HotfixTestAssemblyInjector` is responsible for keeping test assemblies out of Release artifacts.

| Menu | Purpose |
|------|------|
| `BDFrameWork工具箱/Hotfix/查看热更程序集配置` | Print the current configuration |
| `BDFrameWork工具箱/Hotfix/注入测试程序集 (Debug)` | Inject manually |
| `BDFrameWork工具箱/Hotfix/移除测试程序集 (Release)` | Remove manually |
| `BDFrameWork工具箱/Hotfix/自动配置测试程序集` | Configure automatically from the build mode |

`ValidateNoTestAssembliesInOutput` checks both the `.dll.bytes` **and** `.zlua.bytes` extensions, and **throws** when a test assembly is found in a Release build.

`IsCurrentBuildDebug()` resolves in this priority order: `-buildMode` → `-buildDebug` → `EditorUserBuildSettings.development`.

## Pre-compile check

```csharp
HyCLREditorTools.CheckEditorCode()
```

Internally it uses `PlayerBuildInterface.CompilePlayerScripts` writing to `Library/BuildTest` and checks whether `Assembly-CSharp.dll` was produced. This is a fast way to verify that the code compiles without doing a package build.

Corresponding menu: "Code Check" inside `BDFrameWork工具箱/DevOps/CI - API预览`, plus the BatchMode entry point `PublishPipeLineCI.CheckEditorCode`.

## Runtime loading

→ See [Startup Sequence](../architecture/bootstrap.md#hotfix-dll-loading) for details.

Key points at a glance:

| Item | Description |
|----|------|
| AOT metadata source | **Always** the client package at `StreamingAssets/<platform>/script/aot_patch/`, **it does not follow the version directory** |
| Loading API | `RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)` |
| Hotfix DLL source | Prefers `FIRST_LOAD_DIR/script/hotfix/`, falls back to the client package |
| Load order | `bdframework.core*` → `assembly-csharp-firstpass*` → `assembly-csharp*` → others |
| Missing entirely | `throw new Exception("【AOT.Load】HyCLR热更DLL不存在! 路径:...")` |
| Single file missing | `FileNotFoundException` / `DirectoryNotFoundException` is downgraded to `Debug.LogWarning` and processing continues |

## HybridCLR prerequisites

HybridCLR must be initialised the first time it is brought in:

1. Install via the `HybridCLR/Installer...` menu (or let `EnsureHybridClrInstalled` do it automatically, preferring a local install from `HybridCLRData/il2cpp_plus_repo/libil2cpp`).
2. Generate `HybridCLRSettings` via the `HybridCLR/Settings...` menu (when it is missing, `PreBuild` throws `请先生成HCLR Setting!`).
3. From then on BDFramework's `PreBuild` takes over writing the configuration.

!!! warning "You must switch to the target platform"
    `PreBuild` verifies that the current `EditorUserBuildSettings.activeBuildTarget` matches the target, and attempts `SwitchToBuildTarget` when it does not. **It fails if the target platform module is not installed**.

## `HotfixCodeRunMode`

```csharp
public enum HotfixCodeRunMode { HyCLR = 1, Mono64 }
```

`GameBaseConfigProcessor.Config.CodeRunMode` defaults to `HyCLR`. `BuildTools_ClientPackage` branches on `CodeRunMode == HotfixCodeRunMode.HyCLR` in several places.

## Common failures

| Symptom | Root cause |
|------|------|
| `请先生成HCLR Setting!` | HybridCLR was never initialised |
| `build hotfix_dll failed` | Compilation errors; inspect the Error captured by `Application.logMessageReceived` |
| `【AOT.Load】HyCLR热更DLL不存在!` | The client package / version directory is missing `script/hotfix/*.zlua.bytes` |
| Duplicate types or odd behaviour after a hotfix | An assembly with the same name was loaded twice (check `preserveHotUpdateAssemblies` and the preload logic) |
| Test assemblies inside a Release build | `HotfixTestAssemblyInjector` did not remove them correctly; check the `-buildMode` argument |
| AOT metadata version mismatch | `script/aot_patch` does not come from the same source as the client package's IL2CPP output (it **does not follow the version directory**) |
| Android build time explodes | deep profiling was not cleared before `HyCLR PreBuild` |

!!! danger "deep profiling must be cleared before `HyCLR PreBuild`"
    Otherwise `GenerateStripedAOTDlls` inherits `EnableDeepProfilingSupport`, the Tundra cache is polluted, and an Android IL2CPP build balloons from ~10 minutes to ~34 minutes.

!!! danger "A Windows package build must copy the assemblies afterwards"
    `EnsureHybridClrHotUpdateAssembliesCopiedToManaged(outputPath)` —— otherwise hotfix MonoBehaviours in the first scene become **missing script**.

## Related pages

- [Startup Sequence](../architecture/bootstrap.md) —— runtime loading details
- [Assemblies & Dependencies](../architecture/assemblies.md) —— collection whitelist
- [Client Package Build](build-package.md) —— `BuildMode` and test assemblies
- [DevOps & CI](devops-ci.md) —— BatchMode entry points
