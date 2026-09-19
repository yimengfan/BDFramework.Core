# 管线回调钩子

继承 `ABDFrameworkPublishPipelineBehaviour` 即可挂到构建/发布管线上。

```csharp
namespace BDFramework.Editor

abstract public class ABDFrameworkPublishPipelineBehaviour
{
    // 全部 virtual，默认空实现（OnBeginBuildAllAssets / OnBeginBuildPackage 除外）
}
```

!!! note "命名空间是 `BDFramework.Editor`"
    不是 `BDFramework.Editor.BuildPipeline`。

## 完整虚方法表

### 编译 DLL

```csharp
virtual public void OnBeginBuildDLL();
virtual public void OnEndBuildDLL(string outputPath);      // outputPath = dll 输出路径
```

### 打包 SQLite

```csharp
virtual public void OnBeginBuildSqlite();
virtual public void OnEndBuildSqlite(string outputPath);
```

### 导表

```csharp
virtual public void OnExportExcel(Type type);              // 每导出一个表类调用一次
```

### 打包 AssetBundle

```csharp
virtual public void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx);
virtual public void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx);
```

`AssetBundleBuildingContext` 可访问：`BuildParams` / `BuildAssetInfos` / `AssetBundleItemList` / `RuntimeAssetsList` / `DependAssetList`。

### 构建所有 Assets（资源总管道）

```csharp
virtual public void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath,
                                          string lastVersionNum, out string newVersionNum);
virtual public void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath,
                                        string newVersionNum);
```

!!! note "`OnBeginBuildAllAssets` 是**唯一有默认语义**的钩子"
    ```csharp
    virtual public void OnBeginBuildAllAssets(..., out string newVersionNum)
    {
        newVersionNum = lastVersionNum;      // 默认沿用上一个版本号
    }
    ```
    这是**取版本号**的地方。覆写时必须给 `newVersionNum` 赋值，否则会是 `null`。

### SVC 版本号

```csharp
virtual public string GetArtSVCNum(RuntimePlatform platform, string outputPath);      // 默认 "0"
virtual public string GetTableSVCNum(RuntimePlatform platform, string outputPath);    // 默认 "0"
virtual public string GetScriptSVCNum(RuntimePlatform platform, string outputPath);   // 默认 "0"
```

SVC = Source Version Control。用于记录"本次构建对应的源码版本"（git hash / svn revision / p4 changelist）。

### 构建母包

```csharp
virtual public void OnBeginBuildPackage(BuildTarget buildTarget, string outputpath, string clientVersion);
virtual public void OnEndBuildPackage(BuildTarget buildTarget, string outputpath);
```

!!! danger "`OnBeginBuildPackage` 的默认实现会改全局状态"
    基类实现（**非空**）做这些事：

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

    **覆写时务必 `base.OnBeginBuildPackage(...)`**，否则版本号不会递增、`package_build.info` 不会生成。

### 发布资源

```csharp
virtual public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum);
virtual public void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum);
```

## 仓库中的示例

`Assets/Code/BDFramework.Game/Editor/BDFrameworkPublishPipelineBehaviourTest.cs` 是官方示例，展示了如何挂载钩子。

## 钩子触发时机

| 钩子 | 触发点 |
|------|--------|
| `OnBeginBuildDLL` / `OnEndBuildDLL` | `BuildTools_HotfixScript.BuildDLL` |
| `OnBeginBuildSqlite` / `OnEndBuildSqlite` | `BuildTools_Excel2SQLite.BuildSqlite` |
| `OnExportExcel` | 每导出一个表类 |
| `OnBeginBuildAssetBundle` / `OnEndBuildAssetBundle` | `AssetBundleBuildingContext.StartBuildAssetBundle` |
| `OnBeginBuildAllAssets` / `OnEndBuildAllAssets` | `BuildTools_Assets.BuildAll` 首尾 |
| `Get*SVCNum` | 写 `package_build.info` 时查询 |
| `OnBeginBuildPackage` / `OnEndBuildPackage` | `BuildTools_ClientPackage.Build` 首尾 |
| `OnBeginPublishAssets` / `OnEndPublishAssets` | `PublishPipelineTools.PublishAssetsToServer` |

## 典型用法

### 自动递增版本号

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

### 记录 git 版本

```csharp
public override string GetArtSVCNum(RuntimePlatform platform, string outputPath)
    => GitProcessor.GetVersion(8);

public override string GetScriptSVCNum(RuntimePlatform platform, string outputPath)
    => GitProcessor.GetVersion(8);

public override string GetTableSVCNum(RuntimePlatform platform, string outputPath)
    => GitProcessor.GetVersion(8);
```

### AB 构建后做产物校验

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

### 母包构建后归档产物

```csharp
public override void OnEndBuildPackage(BuildTarget buildTarget, string outputpath)
{
    base.OnEndBuildPackage(buildTarget, outputpath);

    // 拷贝到归档目录
    var archive = IPath.Combine(outputpath, "archive");
    FileHelper.CopyFolderTo(outputpath, archive);
}
```

## 常见故障

| 现象 | 根因 |
|------|------|
| 版本号不递增 | 覆写 `OnBeginBuildPackage` 时漏了 `base.` 调用 |
| `newVersionNum` 为 `null` | 覆写 `OnBeginBuildAllAssets` 时没给 out 参数赋值 |
| `package_build.info` 未生成 | 同上，基类实现未执行 |
| 钩子没被调用 | 类未继承 `ABDFrameworkPublishPipelineBehaviour`；或不在 Editor 程序集 |
| SVC 版本号恒为 `"0"` | 未覆写 `Get*SVCNum`（默认返回 `"0"`） |

## 相关页面

- [构建与发布](../pipeline/index.md)
- [AssetBundle 打包](../pipeline/build-assetbundle.md)
- [母包构建](../pipeline/build-package.md)
- [资源发布](../pipeline/publish-assets.md)
