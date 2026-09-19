# 版本控制 API 参考

> 来源：`Runtime/AssetsManager/VersionController/**`

## 命名空间

```csharp
namespace BDFramework.ResourceMgr       // BResources、AssetsVersionController
namespace BDFramework.Asset             // ClientAssetsUtils
```

## `AssetsVersionController`

```csharp
public partial class AssetsVersionController
{
    public enum RetStatus { Checkassets = 0, DeleteOldAssets, Error, Success, SuccessNeedRestart }

    public AssetsVersionController();                       // 构造里 BetterStreamingAssets.Initialize()
    public static void SetRetryCount(uint count);            // 默认 RETRY_COUNT = 5

    public void UpdateAssets(UpdateMode updateMode, string serverConfigUrl, string assetsPackageName = "",
                             Action<AssetItem,List<AssetItem>> onDownloadProccess = null,
                             Action<RetStatus,string> onTaskEndCallback = null);      // UniTask.RunOnThreadPool

    public void GetServerSubPackageInfos(string serverUrl, Action<Dictionary<string,string>> callback);

    public (string,string,List<AssetItem>,List<AssetItem>,string)
        GetDownloadAssetsData(string serverUrl, RuntimePlatform, AssetsVersionInfo, AssetsVersionInfo);

    public (string,string,List<AssetItem>,List<AssetItem>,string)
        GetDownloadSubPackageData(string serverUrl, string subPackageName, RuntimePlatform,
                                  AssetsVersionInfo, AssetsVersionInfo);

    public IEnumerator IE_DownloadAssets(string serverUrl, string localSaveAssetsPath,
                                         Queue<AssetItem> downloadQueue,
                                         Action<AssetItem,List<AssetItem>> onDownloadProccess);
}
```

### `BResources` 静态门面

```csharp
static public AssetsVersionController AssetsVersionController { get; private set; }

static public void StartAssetsVersionControl(UpdateMode updateMode, string serverUrl,
    string assetsPackageName = "",
    Action<AssetItem, List<AssetItem>> onDownloadProccess = null,
    Action<AssetsVersionController.RetStatus, string> onTaskEndCallback = null);

static public void StartAssetsVersionControlWithDevOps(…);     // 文件服务器协议
static public void GetServerSubPacks(string serverUrl, Action<Dictionary<string,string>> callback);
static public void GetServerSubPacksWithDevOps(string serverUrl,
    Action<Dictionary<string,string>> callback, Action<string> onError = null);

static public string GetServerAssetsVersionInfoPath(string rootPath, RuntimePlatform platform);
static public string GetAssetsInfoPath(string rootPath);
static public string GetAssetsInfoPath(string rootPath, RuntimePlatform platform);
static public string GetAssetsSubPackageInfoPath(string rootPath, RuntimePlatform platform, string subPackageName);
```

`GetAssetsSubPackageInfoPath`：`subPackageName` 以 `ServerAssetsSubPackage_` 开头时走旧版兼容。

## 数据类型

```csharp
public class AssetsVersionInfo
{
    string Platfrom;                                    // ★ 源码拼写是 Platfrom
    string Version;
    Dictionary<string,string> SubPckMap { get; }
}

public class AssetItem
{
    public enum AssetType { AssetBundle, DLL, Sqlite }

    int    Id;
    string HashName;
    string LocalPath;
    float  FileSize;

    // Equals      = HashName + LocalPath
    // GetHashCode = "LocalPath|HashName"
}

public class SubPackageConfigItem
{
    public string       PackageName;
    public List<int>    ArtAssetsIdList;
    public List<string> HotfixCodePathList;
    public List<string> TablePathList;
    public List<string> ConfAndInfoList;
}
```

## 下载流程

见 [SKILL.md 第 10 节](../SKILL.md#10-热更下载流程)。

### `UpdateMode` 对比

| 模式 | 行为 |
|------|------|
| `CompareSimple` | 本地 >= 服务器 → `"本地版本相同或更新,无需下载!"` 直接结束 |
| `CompareWithRepairCoreAssets` | 对比同 Compare，差异走 Repair |
| `RepairFull` | `LoadServerAssetInfo(serverUrl/<platform>/assets.info)` 全量 hash 校验 |

## `ClientAssetsUtils`

```csharp
namespace BDFramework.Asset

public class ClientPackageBuildInfo
{
    long   BuildTime;
    string Version = "0.0.0";
    string BasePckScriptSVCVersion;
    string HotfixScriptSVCVersion;
    string AssetBundleSVCVersion;
    string TableSVCVersion;
}

static public class ClientAssetsUtils
{
    readonly static public string PACKAGE_BUILD_INFO_PATH = "package_build.info";

    public static string FIRST_LOAD_DIR  = null;    // 可写：persistentDataPath/<major.minor.0>/<platform>
    public static string SECOND_LOAD_DIR = null;    // 只读母包

    static public (string,string) GetMultiAssetsLoadPath(RuntimePlatform platform, string version);

    static public string GetPersistentAssetPath(string fileName);   // FIRST_LOAD_DIR/fileName
    static public string GetStreamingAssetPath(string fileName);    // SECOND_LOAD_DIR/fileName

    static public void CheckBaseClientAssets(string firstPath, string secondPath);

    static public ClientPackageBuildInfo GetPackageBuildInfo(string ouptputPath, RuntimePlatform platform);
    static public ClientPackageBuildInfo GetBasePackBuildInfo();

    static public byte[] ReadAllBytes(string firstPath, string secondPath, string fileName);
    static public string ReadAllText(string firstPath, string secondPath, string fileName);

    static public bool IsExsitAsset(RuntimePlatform platform, string assetName, string assetHashName);
    static public bool IsExsitAssetWithCheckHash(RuntimePlatform platform, string assetName, string assetHash);

#if UNITY_EDITOR
    static public void GenBasePackageBuildInfo(string outputPath, RuntimePlatform platform,
        string bundleVersion = "", string basePckScriptSVC = "", string artAssetsSVC = "",
        string hotfixScriptSVC = "", string tableSVC = "");
    static public void SaveBasePackageBuildInfo(string ouptputPath, RuntimePlatform platform,
                                                ClientPackageBuildInfo info);
#endif
}
```

### 关键语义

| 项 | 说明 |
|----|------|
| 版本目录降级 | `VersionNumHelper.ParseVersion` 清零小版本与增量位，`1.4.7` → `1.4.0` |
| `PersistentOnlyFiles` | 当前**只有** `SqliteLoder.LOCAL_DB_PATH = "local.db"` |
| `IsExsitAssetWithCheckHash` 判定顺序 | persistent 的 `<hash>` 文件 → persistent 实体文件 → streaming 实体文件；hash 不匹配的 `<hash>` 文件会被 `File.Delete` |
| `ReadAllBytes` 找不到 | `Debug.LogError($"不存在:{path} & {path2}")` 并返回 `null` |
| `CheckBaseClientAssets` | `firstPath == secondPath` 直接跳过；否则检查母包 `package_build.info`，缺失时非 Editor **抛异常** |

## `VersionNumHelper`

```csharp
static public class VersionNumHelper
{
    public struct VersionNum { public string ToString(); … }

    static public string AddVersionNum(string lastVersionNum, string newVersionNum, int add = 1);
    static public string AddVersionNum(string lastVersionNum, int bigNum = 0, int smallNum = 0,
                                       int additiveNum = 0, int add = 1);
    // ParseVersion / GT 同文件成员
}
```

## 文件服务器协议（DevOps）

```csharp
public sealed class FileServerVersionInfo
{
    string CodeVersion, AssetBundleVersion, TableVersion;
    string RawValue { get; }
    bool   HasAnyVersion { get; }
}

public sealed class FileServerBatchVerificationRequest
{
    RuntimePlatform TargetPlatform = RuntimePlatform.WindowsEditor;   // ★ 默认值需显式覆盖
    string ServerUrl;
    FileServerVersionInfo ExpectedVersionInfo;
    bool ResetLocalStateBeforeVerify = true;
}

public sealed class FileServerAssetBundleValidationEntry
{
    int    AssetId;
    string AssetDisplayPath, AssetLoadPath, AssetGuid,
           AssetBundleRelativePath, AssetBundleLocalPath;
}

public sealed class FileServerBatchVerificationResult
{
    RuntimePlatform Platform;
    string PlatformPath, FirstLoadDir, ExpectedVersion, ActualVersion;
    bool   UsedLocalFallbackVersion;
    string CodeAssetLocalPath, AssetBundleAssetLocalPath;
    string AssetBundleValidationFirstTarget;
    List<string> AssetBundleAssetLocalPaths;
    List<FileServerAssetBundleValidationEntry> AssetBundleValidationEntries;
    string TableAssetLocalPath;
    ClientPackageBuildInfo PackageBuildInfo;
    string Error;
    bool   IsSuccess => string.IsNullOrEmpty(Error);
}
```

### 公开 API

```csharp
public void UpdateAssetsWithDevOps(UpdateMode, string serverConfigUrl, string assetsPackageName = "",
        Action<AssetItem,List<AssetItem>> onDownloadProccess = null,
        Action<RetStatus,string> onTaskEndCallback = null);

public void GetServerSubPackageInfosWithDevOps(string serverUrl,
        Action<Dictionary<string,string>> callback, Action<string> onError = null);

public FileServerBatchVerificationResult VerifyFileServerAssetsForBatchModeWithDevOps(
        FileServerBatchVerificationRequest request);

public static FileServerBatchVerificationRequest CreateFileServerBatchVerificationRequest(
        string serverUrl, string expectedCodeVersion, string expectedAssetbundleVersion,
        string expectedTableVersion, bool resetLocalStateBeforeVerify = true);

public static FileServerBatchVerificationRequest CreateFileServerBatchVerificationRequestFromCommandLine(
        bool resetLocalStateBeforeVerify = true);

public static void VerifyFileServerAssetsForBatchModeFromCommandLine(RuntimePlatform targetPlatform);
```

`BatchModeArgs` partial：

```csharp
public static FileServerBatchVerificationRequest CreateFileServerBatchVerificationRequestFromArgs(
        IReadOnlyList<string> args, bool resetLocalStateBeforeVerify = true);

public static FileServerBatchVerificationResult VerifyFileServerAssetsForBatchMode(
        RuntimePlatform targetPlatform, IReadOnlyList<string> args, bool resetLocalStateBeforeVerify = true);

public static FileServerBatchVerificationResult VerifyFileServerAssetsForBatchModeWithRequest(
        FileServerBatchVerificationRequest request);
```

### 关键文件与参数

| 项 | 值 |
|----|-----|
| `global_version.info` | JSON 数组，`version_num` 格式 `code.assetbundle.table` |
| `client_res_state.json` | 本地状态 |
| `version_cache/` | 元数据缓存 |
| `file_server_verify/` | CI 独立下载根 |
| `{Code\|AssetBundle\|Table}_package_build.info` | 各组件构建信息 |

命令行参数：`-fileServerUrl`、`-expectedCodeVersion`、`-expectedAssetbundleVersion`、`-expectedTableVersion`。

### 验证实现细节

```text
VerifyFileServerAssetsForBatchModeWithDevOps
 ① Task.Run(...).GetAwaiter().GetResult()          后台下载
 ② 回主线程 ValidateFileServerRepresentativeLocalLoads
 ③ ValidateFileServerPackageBuildInfo 终检
 ④ 失败统一 throw new Exception("[CI][VerifyClientRes] 文件服务器 BatchMode 验证失败! ...")
```

日志前缀 `[CI][VerifyClientRes]`，含中文 `测试目的=` / `实现手段=`。

### 内部类型（不可访问）

`FileServerComponentKind`、`FileServerVersionControllerState`、`FileServerComponentContext`、`FileServerDownloadItem`、`FileServerResolveResult` 都是 **`internal`**。

## 本地隔离输出根（CI）

```text
<projectDir>/Library/CIOutputs/<build_kind>/<build_name>/<build_number>[/<platform>]
```

`build_kind` ∈ `clientres_code` / `clientres_assetbundle` / `clientres_table`。

## 陷阱清单

| # | 陷阱 |
|---|------|
| 1 | 版本目录降级为 `x.y.0`，同大版本共用目录 |
| 2 | `SuccessNeedRestart` 必须重启客户端 |
| 3 | `FileServerBatchVerificationRequest.TargetPlatform` 默认 `WindowsEditor`，必须显式覆盖 |
| 4 | `AssetsVersionInfo.Platfrom` 拼写错误（`Platfrom`） |
| 5 | `ClientAssetsUtils` 的 `FIRST_LOAD_DIR` / `SECOND_LOAD_DIR` 是 **public static 可变字段** |
| 6 | `CheckBaseClientAssets` 在非 Editor 下母包信息缺失会**抛异常** |
| 7 | `onTaskEndCallback` 会被多次调用（`Checkassets` / `DeleteOldAssets` 是中间态） |
| 8 | `GetDownloadAssetsData` 返回 5 元组，`err` 非空表示失败 |
