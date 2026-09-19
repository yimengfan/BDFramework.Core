# 热更代码 HybridCLR

热更方案为 **HybridCLR**（v3.0.0 起已移除 ILRuntime）。

!!! danger "ILRuntime 相关文档已全部失效"
    旧文档中的 CLRBinding、Adaptor 生成、`ILRuntimeCLRBinding` 报错排查等内容**在本分支完全不适用**。相关实现整段注释在 `Unity3dRoslynBuildTools.cs` 中，不生效。

## 入口

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

## 程序集集合 { #hotfix-assembly-set }

`HyCLREditorTools.SetBDFramework2HCLRConfig()` 写入的约定：

| HybridCLR 配置项 | 值 |
|-----------------|-----|
| `preserveHotUpdateAssemblies`（追加） | `Assembly-CSharp`、`Assembly-CSharp-firstpass`、`BDFramework.Core` |
| `hotUpdateAssemblies`（移除） | 原列表全部清空 |
| `patchAOTAssemblies`（追加） | `mscorlib`、`System`、`System.Core` |

!!! note "`preserveHotUpdateAssemblies` 的含义"
    表示"这些程序集在母包中**已经以 DLL 形式存在**，热更时替换而非新增"。

    由于 `Assembly.Load(byte[])` 重复装载同名程序集会**产生两个类型副本**，`ScriptLoderAOT.ShouldSkipAlreadyLoadedHotfixAssembly` 会跳过已由 Player 预加载的同名程序集。

`SetHyCLRConfig()` 还会调 `Unity3dEditorEx.AddSymbols("ENABLE_HYCLR")`。

热更程序集名列表来自 `SettingsUtil.HotUpdateAssemblyNamesIncludePreserved`。

## 输出布局与常量

| 常量 | 值 | 定义位置 |
|------|-----|---------|
| `ScriptLoder.HOTFIX_DLL_PATH` | `script/hotfix` | `Runtime/HotfixScript/ScriptLoder.cs` |
| `ScriptLoder.HYCLR_AOT_PATCH_PATH` | `script/aot_patch` | 同上 |
| `ScriptLoder.HOT_DLL_EXTENSION` | `.zlua.bytes` | 同上 |

```text
DevOps/PublishAssets/<platform>/script/hotfix/<assembly>.zlua.bytes
DevOps/PublishAssets/<platform>/script/aot_patch/<assembly>.zlua.bytes
```

!!! note "扩展名 `.zlua.bytes` 只是伪装"
    **没有额外的 DLL 加密步骤**，扩展名仅用于规避某些平台/工具的自动识别。（AB 有独立混淆：`BuildTools_AssetBundleV2.MixAssetBundle`。）

!!! warning "常量在两个程序集里各有一份"
    `HYCLR_AOT_PATCH_PATH` / `HOTFIX_DLL_PATH` / `HOT_DLL_EXTENSION` 在 `Runtime/HotfixScript/ScriptLoder.cs` 与 `Runtime.AOT/ScriptLoderAOT.cs` 各定义一次。

    这是**刻意的程序集隔离**（AOT 不能引用 Core），但**改一处必须改另一处**。

## `PreBuild(BuildTarget target)` 步骤

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

!!! danger "`PreBuild` 会把 AOT patch 写入两个位置"
    `Application.streamingAssetsPath`（母包内置）与 `BApplication.DevOpsPublishAssetsPath`（构建产物）。

    Editor 下 `streamingAssetsPath` 被重写为 `DevOps/PublishAssets`，所以两者实际是同一处；但真机/CI 场景下需要两个都写，否则母包缺少 AOT 元数据。

## `BuildHotfixDLL` 步骤

```text
① 临时目录：HybridCLRData/out_hotfixdlls_temp（先删后建）

② CompileDllCommand.CompileDll(tmpOutputPath, target, false)
     挂 Application.logMessageReceived 检测含 "Fail" 的 Error
     失败 → throw "build hotfix_dll failed"

③ CopyHotfixDLLs(tmpOutputPath, outputDir, target)

④ HotfixTestAssemblyInjector.ValidateNoTestAssembliesInOutput(destDLLRootDir, isReleaseBuild)
```

## 测试程序集隔离

`HotfixTestAssemblyInjector` 负责把测试程序集从 Release 产物中排除。

| 菜单 | 作用 |
|------|------|
| `BDFrameWork工具箱/Hotfix/查看热更程序集配置` | 打印当前配置 |
| `BDFrameWork工具箱/Hotfix/注入测试程序集 (Debug)` | 手动注入 |
| `BDFrameWork工具箱/Hotfix/移除测试程序集 (Release)` | 手动移除 |
| `BDFrameWork工具箱/Hotfix/自动配置测试程序集` | 按构建模式自动配置 |

`ValidateNoTestAssembliesInOutput` 检查 `.dll.bytes` **和** `.zlua.bytes` 两种扩展名，Release 下发现测试程序集**抛异常**。

`IsCurrentBuildDebug()` 的判定优先级：`-buildMode` → `-buildDebug` → `EditorUserBuildSettings.development`。

## 编译前检查

```csharp
HyCLREditorTools.CheckEditorCode()
```

内部用 `PlayerBuildInterface.CompilePlayerScripts` 输出到 `Library/BuildTest`，检查 `Assembly-CSharp.dll` 是否生成。用于在不打包的情况下快速验证代码能否编译。

对应菜单：`BDFrameWork工具箱/DevOps/CI - API预览` 中的「代码检查」，以及 BatchMode 入口 `PublishPipeLineCI.CheckEditorCode`。

## 运行时装载

→ 详见 [启动链路](../architecture/bootstrap.md#hotfix-dll-loading)。

要点回顾：

| 项 | 说明 |
|----|------|
| AOT 元数据来源 | **始终**母包 `StreamingAssets/<platform>/script/aot_patch/`，**不跟随版本目录** |
| 加载 API | `RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)` |
| 热更 DLL 来源 | 优先 `FIRST_LOAD_DIR/script/hotfix/`，回退母包 |
| 装载顺序 | `bdframework.core*` → `assembly-csharp-firstpass*` → `assembly-csharp*` → 其他 |
| 缺失处理 | 全部缺失时 `throw new Exception("【AOT.Load】HyCLR热更DLL不存在! 路径:...")` |
| 单文件缺失 | `FileNotFoundException` / `DirectoryNotFoundException` 降级为 `Debug.LogWarning` 并继续 |

## HybridCLR 前置操作

首次接入时需要初始化 HybridCLR：

1. 菜单 `HybridCLR/Installer...` 安装（或由 `EnsureHybridClrInstalled` 自动完成，优先从本地 `HybridCLRData/il2cpp_plus_repo/libil2cpp` 安装）。
2. 菜单 `HybridCLR/Settings...` 生成 `HybridCLRSettings`（缺失时 `PreBuild` 会抛 `请先生成HCLR Setting!`）。
3. 之后由 BDFramework 的 `PreBuild` 接管配置写入。

!!! warning "必须切换到目标平台"
    `PreBuild` 会校验当前 `EditorUserBuildSettings.activeBuildTarget` 与目标一致，不一致时尝试 `SwitchToBuildTarget`。**目标平台模块未安装会失败**。

## `HotfixCodeRunMode`

```csharp
public enum HotfixCodeRunMode { HyCLR = 1, Mono64 }
```

`GameBaseConfigProcessor.Config.CodeRunMode` 默认 `HyCLR`。`BuildTools_ClientPackage` 在多处以 `CodeRunMode == HotfixCodeRunMode.HyCLR` 分支。

## 常见故障

| 现象 | 根因 |
|------|------|
| `请先生成HCLR Setting!` | 未执行 HybridCLR 初始化 |
| `build hotfix_dll failed` | 编译错误；查看 `Application.logMessageReceived` 捕获的 Error |
| `【AOT.Load】HyCLR热更DLL不存在!` | 母包/版本目录缺少 `script/hotfix/*.zlua.bytes` |
| 热更后类型重复 / 行为异常 | 同名程序集被装载两次（检查 `preserveHotUpdateAssemblies` 与预加载逻辑） |
| Release 包里有测试程序集 | `HotfixTestAssemblyInjector` 未正确移除；检查 `-buildMode` 参数 |
| AOT 元数据版本不匹配 | `script/aot_patch` 与母包 IL2CPP 产物不同源（它**不跟随版本目录**） |
| Android 构建耗时异常膨胀 | deep profiling 未在 `HyCLR PreBuild` 之前清除 |

!!! danger "deep profiling 必须在 `HyCLR PreBuild` 之前清除"
    否则 `GenerateStripedAOTDlls` 会继承 `EnableDeepProfilingSupport`，Tundra 缓存被污染，Android IL2CPP 构建从 ~10 分钟膨胀到 ~34 分钟。

!!! danger "Windows 母包构建后必须补拷贝"
    `EnsureHybridClrHotUpdateAssembliesCopiedToManaged(outputPath)` —— 否则首场景的热更 MonoBehaviour 会变成 **missing script**。

## 相关页面

- [启动链路](../architecture/bootstrap.md) —— 运行时装载细节
- [程序集与依赖](../architecture/assemblies.md) —— 收集白名单
- [母包构建](build-package.md) —— `BuildMode` 与测试程序集
- [DevOps 与 CI](devops-ci.md) —— BatchMode 入口
