# Pipeline Hooks

Derive from `ABDFrameworkPublishPipelineBehaviour` and you are attached to the build/publish pipelines.

```csharp
namespace BDFramework.Editor

abstract public class ABDFrameworkPublishPipelineBehaviour
{
    // 全部 virtual，默认空实现（OnBeginBuildAllAssets / OnBeginBuildPackage 除外）
}
```

!!! note "The namespace is `BDFramework.Editor`"
    Not `BDFramework.Editor.BuildPipeline`.

## Complete virtual method table

### Compiling the DLL

```csharp
virtual public void OnBeginBuildDLL();
virtual public void OnEndBuildDLL(string outputPath);      // outputPath = dll 输出路径
```

### Packing SQLite

```csharp
virtual public void OnBeginBuildSqlite();
virtual public void OnEndBuildSqlite(string outputPath);
```

### Table export

```csharp
virtual public void OnExportExcel(Type type);              // 每导出一个表类调用一次
```

### Building the AssetBundle

```csharp
virtual public void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx);
virtual public void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx);
```

`AssetBundleBuildingContext` gives you access to: `BuildParams` / `BuildAssetInfos` / `AssetBundleItemList` / `RuntimeAssetsList` / `DependAssetList`.

### Building all assets (aggregate asset pipeline)

```csharp
virtual public void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath,
                                          string lastVersionNum, out string newVersionNum);
virtual public void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath,
                                        string newVersionNum);
```

!!! note "`OnBeginBuildAllAssets` is the **only hook with default semantics**"
    ```csharp
    virtual public void OnBeginBuildAllAssets(..., out string newVersionNum)
    {
        newVersionNum = lastVersionNum;      // 默认沿用上一个版本号
    }
    ```
    This is where the **version number is resolved**. When overriding you must assign `newVersionNum`, otherwise it will be `null`.

### SVC version numbers

```csharp
virtual public string GetArtSVCNum(RuntimePlatform platform, string outputPath);      // 默认 "0"
virtual public string GetTableSVCNum(RuntimePlatform platform, string outputPath);    // 默认 "0"
virtual public string GetScriptSVCNum(RuntimePlatform platform, string outputPath);   // 默认 "0"
```

SVC = Source Version Control. It records "the source version this build corresponds to" (git hash / svn revision / p4 changelist).

### Building the client package

```csharp
virtual public void OnBeginBuildPackage(BuildTarget buildTarget, string outputpath, string clientVersion);
virtual public void OnEndBuildPackage(BuildTarget buildTarget, string outputpath);
```

!!! danger "The default `OnBeginBuildPackage` implementation mutates global state"
    The base implementation (**not empty**) does this:

    ```csharp
    var githash = GitProcessor.GetVersion(6);
    ClientAssetsUtils.GenBasePackageBuildInfo(BApplication.DevOpsPublishAssetsPath,
        BApplication.GetRuntimePlatform(buildTarget), basePckScriptSVC: githash);

    var config = ConfigEditorUtil.GetEditorConfig<GameBaseConfigProcessor.Config>();
    config.ClientVersionNum = clientVersion;

    switch (buildTarget)
    {
        case BuildTarget.Android:
            PlayerSettings.Android.bundleVersionCode++;
            PlayerSettings.bundleVersion = clientVersion;
            break;
        case BuildTarget.iOS:
            buildNumber++;
            PlayerSettings.iOS.buildNumber = buildNumber.ToString();
            PlayerSettings.bundleVersion = config.GetClientVersionNumForIOS();
            break;
        case StandaloneWindows / StandaloneWindows64 / StandaloneOSX:
            PlayerSettings.bundleVersion = clientVersion;
            break;
    }
    ```

    **Always call `base.OnBeginBuildPackage(...)` when overriding**, otherwise the version number will not increment and `package_build.info` will not be generated.

### Publishing assets

```csharp
virtual public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum);
virtual public void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum);
```

## Example in this repository

`Assets/Code/BDFramework.Game/Editor/BDFrameworkPublishPipelineBehaviourTest.cs` is the official example showing how to attach hooks.

## When hooks fire

| Hook | Trigger point |
|------|---------------|
| `OnBeginBuildDLL` / `OnEndBuildDLL` | `BuildTools_HotfixScript.BuildDLL` |
| `OnBeginBuildSqlite` / `OnEndBuildSqlite` | `BuildTools_Excel2SQLite.BuildSqlite` |
| `OnExportExcel` | Once per exported table class |
| `OnBeginBuildAssetBundle` / `OnEndBuildAssetBundle` | `AssetBundleBuildingContext.StartBuildAssetBundle` |
| `OnBeginBuildAllAssets` / `OnEndBuildAllAssets` | Start and end of `BuildTools_Assets.BuildAll` |
| `Get*SVCNum` | Queried when writing `package_build.info` |
| `OnBeginBuildPackage` / `OnEndBuildPackage` | Start and end of `BuildTools_ClientPackage.Build` |
| `OnBeginPublishAssets` / `OnEndPublishAssets` | `PublishPipelineTools.PublishAssetsToServer` |

## Typical usage

### Auto-incrementing the version number

```csharp
public class MyPublishBehaviour : ABDFrameworkPublishPipelineBehaviour
{
    public override void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath,
                                               string lastVersionNum, out string newVersionNum)
    {
        // 从 CI 环境变量取，或按规则递增
        var buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "0";
        var parts = (lastVersionNum ?? "0.0.0").Split('.');
        newVersionNum = $"{parts[0]}.{parts[1]}.{buildNumber}";
    }
}
```

### Recording the git version

```csharp
public override string GetArtSVCNum(RuntimePlatform platform, string outputPath)
    => GitProcessor.GetVersion(8);

public override string GetScriptSVCNum(RuntimePlatform platform, string outputPath)
    => GitProcessor.GetVersion(8);

public override string GetTableSVCNum(RuntimePlatform platform, string outputPath)
    => GitProcessor.GetVersion(8);
```

### Validating artifacts after an AB build

```csharp
public override void OnEndBuildAssetBundle(AssetBundleBuildingContext ctx)
{
    base.OnEndBuildAssetBundle(ctx);

    var abCount = ctx.AssetBundleItemList.Count;
    var runtimeCount = ctx.RuntimeAssetsList.Count;
    BDebug.Log($"[MyBehaviour] AB 数量={abCount} Runtime 资源数={runtimeCount}");

    // 例如：断言没有超过阈值的 AB 数量
    if (abCount > 5000)
        BDebug.LogError($"[MyBehaviour] AB 数量异常: {abCount}");
}
```

### Archiving artifacts after a client package build

```csharp
public override void OnEndBuildPackage(BuildTarget buildTarget, string outputpath)
{
    base.OnEndBuildPackage(buildTarget, outputpath);

    // 拷贝到归档目录
    var archive = IPath.Combine(outputpath, "archive");
    FileHelper.CopyFolderTo(outputpath, archive);
}
```

## Common failures

| Symptom | Root cause |
|---------|------------|
| The version number does not increment | A missing `base.` call when overriding `OnBeginBuildPackage` |
| `newVersionNum` is `null` | The `out` parameter was not assigned when overriding `OnBeginBuildAllAssets` |
| `package_build.info` is not generated | Same as above — the base implementation did not run |
| The hook is never called | The class does not derive from `ABDFrameworkPublishPipelineBehaviour`; or it is not in an Editor assembly |
| The SVC version number is always `"0"` | `Get*SVCNum` was not overridden (the default returns `"0"`) |

## Related pages

- [Build & Release](../pipeline/index.md)
- [AssetBundle Building](../pipeline/build-assetbundle.md)
- [Client Package Build](../pipeline/build-package.md)
- [Asset Publishing](../pipeline/publish-assets.md)
