# Assets (BResources)

`BDFramework.ResourceMgr.BResources` — a static façade (partial) that holds an `IResMgr` instance and picks a backend according to `AssetLoadPathType`.

## Initialisation

```csharp
static public IResMgr ResLoader { get; private set; }

static public void Init(AssetLoadPathType loadPathType, string firstDir = "", string secondDir = "");
static public void InitLoadAssetBundleEnv(string path, RuntimePlatform platform);   // 测试用
```

What `Init` does:

| `AssetLoadPathType` | Backend | Notes |
|--------------------|---------|-------|
| `Editor` | `DevResourceMgr` (`AssetDatabase`) | **The whole block sits inside `#if UNITY_EDITOR`**; on a non-Editor platform it silently leaves `ResLoader == null` |
| `Hotfix` | `AssetBundleMgrV2` | `Init(firstDir, secondDir)` |

Both paths end with `InitObjectPools()` (when `Application.isPlaying && !isInitedPools` it creates a `GameobjectPools` GameObject and attaches `GameObjectPoolManager`).

!!! danger "Passing `Editor` on a non-Editor platform leaves `ResLoader` as `null`"
    Every subsequent `BResources.Load` then throws a `NullReferenceException` (**the source has no null check**).

    `UnloadAll()` / `FindShader()`, on the other hand, use the null-safe `ResLoader?.` form and **do not throw**. The behaviour is asymmetric.

## Two enums you must not confuse

```csharp
// BDFramework，Runtime/GameConfig/Config.cs
public enum AssetLoadPathType { Editor = 0, Hotfix = 1 }      // 资源根来源

// BDFramework.ResourceMgr，Runtime/AssetsManager/ArtAsset/IResMgr.cs
public enum LoadPathType { RuntimePath, GUID, AssetsPath }     // 单次加载的路径语义
```

→ See [Asset Load Paths](../guide/asset-load-path.md).

## Loading API

```csharp
public static T Load<T>(string assetLoadPath, LoadPathType pathType = LoadPathType.RuntimePath, string groupName = null)
    where T : UnityEngine.Object;

public static LoadTaskGroup AsyncLoad<T>(string assetLoadPath, LoadPathType loadPathType = LoadPathType.RuntimePath)
    where T : UnityEngine.Object;

public static int AsyncLoad<T>(string assetLoadPath, Action<T> action,
                               LoadPathType loadPathType = LoadPathType.RuntimePath, string groupName = null)
    where T : UnityEngine.Object;

public static List<int> AsyncLoad(List<string> assetlist,
                                  Action<int,int> onProcess = null,
                                  Action<IDictionary<string,Object>> onLoadEnd = null,
                                  LoadPathType loadPathType = LoadPathType.RuntimePath, string groupName = null);

[Obsolete("已废弃,不建议项目使用!")]
public static T[] LoadALL<T>(string assetLoadPath) where T : UnityEngine.Object;

static public string[] GetAssets(string floder, string searchPattern = null);
```

**When the path is `null` / an empty string, `Load` / `AsyncLoad` / `Unload` all return `null` straight away (without reporting an error).**

```csharp
// 推荐写法：Runtime 相对路径，不带扩展名
var prefab = BResources.Load<GameObject>("Test/Cube");
var sprite = BResources.Load<Sprite>("UI/Icon/coin");

// 异步 + 取消
int id = BResources.AsyncLoad<GameObject>("Test/Cube", go => { /* 主线程 */ });
BResources.LoadCancel(id);

// 批量异步 + 进度
var ids = BResources.AsyncLoad(pathList,
    onProcess: (total, cur) => { /* 进度 */ },
    onLoadEnd: dict => { /* 全部完成 */ });

// 协程等待
LoadTaskGroup task = BResources.AsyncLoad<GameObject>("Test/Cube");
yield return task;                       // LoadTaskGroup : CustomYieldInstruction
var go = task.GetResult<GameObject>();
```

!!! warning "`LoadALL<T>` is deprecated"
    - `DevResourceMgr`: a **real implementation** using `AssetDatabase.LoadAllAssetsAtPath(rets[0])`
    - `AssetBundleMgrV2`: marked `[Obsolete]`, and the implementation simply `return null`

    When you need to "load every asset under a folder", use `GetAssets(folder, pattern)` to get the paths and `Load` them one at a time.

## Cancellation

```csharp
public static bool LoadCancel(int id);
public static void LoadCancel(params int[] ids);
public static void LoadCancel();          // 内部走 ResLoader.LoadAllCancel()
```

## Unloading

```csharp
public static void UnloadAsset(string assetPath, bool isForceUnload = false, Type type = null);
public static void UnloadAsset(UnityEngine.Object obj);
public static void UnloadAssets(params string[] assetPaths);
public static void UnloadAll();

public static void Destroy(Transform transform);
public static void Destroy(GameObject go);
```

The dispatch logic of `UnloadAsset(Object)`:

| Type | Behaviour |
|------|-----------|
| `GameObject` | `Destroy` + `Resources.UnloadAsset` |
| `Sprite` | Additionally unloads its `texture` |
| Anything else | `Resources.UnloadAsset` |

!!! danger "The `isForceUnload` and `type` parameters are ignored"
    Neither parameter is forwarded to `AssetBundleMgrV2.UnloadAsset` (the implementation only calls `UnUseAssetBundle(loadPath)`). See [Refactor Backlog](../architecture/refactor-backlog.md#ref-1-unloadasset).

## Asset groups

```csharp
public static void AddAssetsPathToGroup(string groupName, params string[] assetPath);
public static string[] GetAssetsPathByGroup(string groupName);
public static void ClearAssetGroup(string groupName);
static public void UnloadAssetByGouroup(string groupName);      // 注意源码拼写是 Gouroup
```

```csharp
// 把一组资源标记为一个逻辑组，便于批量卸载
BResources.AddAssetsPathToGroup("Battle", "Battle/Map1", "Battle/Map2");
// ...
BResources.UnloadAssetByGouroup("Battle");
```

## Object pool { #object-pool }

```csharp
static public void WarmPool(string assetPath, int size = 5);
static public void AsyncWarmPool(string assetPath, int size = 5);
static public void DestroyPool(string assetPath);

static public GameObject LoadFormPool(string assetPath, LoadPathType pathType = LoadPathType.RuntimePath);
static public GameObject LoadFormPool(string assetPath, Vector3 position, Quaternion rotation);
static public void ReleaseToPool(GameObject gobjClone);
```

```csharp
BResources.WarmPool("Bullet/Normal", 20);
var bullet = BResources.LoadFormPool("Bullet/Normal");
// ...
BResources.ReleaseToPool(bullet);
```

!!! danger "Warming the same prefab twice throws"
    `GameObjectPoolManager.WarmPool` throws `new Exception("Pool for prefab ... has already been created")` for a pool that already exists. Call `DestroyPool` first, or guard against it.

Underneath sits `GameObjectPoolManager : Singleton<GameObjectPoolManager>` (namespace `BDFramework.ResourceMgr`), created automatically by `BResources.Init`.

## Shaders and load configuration

```csharp
public static void WarmUpShaders();
public static Shader FindShader(string shaderName);      // ResLoader?. 安全写法

public enum AUPLevel { LowRender, Height, Normal, Low }
static public void SetAUPLEvel(AUPLevel level);
static public void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1);
```

`SetAUPLEvel` sets `asyncUploadBufferSize` / `asyncUploadTimeSlice` / `backgroundLoadingPriority`.

`DevResourceMgr`'s `WarmUpShaders` / `SetLoadConfig` are **empty implementations** (only the AB backend honours them).

## The `IResMgr` interface

```csharp
void Init(string firstDir, string secondDir = null);
T Load<T>(string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;
UnityEngine.Object Load(Type type, string loadPath, LoadPathType loadPathType = LoadPathType.RuntimePath);
T[] LoadAll<T>(string path) where T : UnityEngine.Object;
LoadTaskGroup AsyncLoad<T>(string loadPath, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;
int AsyncLoad<T>(string loadPath, Action<T> callback, LoadPathType pathType = LoadPathType.RuntimePath) where T : UnityEngine.Object;
List<int> AsyncLoad(List<string> loadPathList, Action<int,int> onLoadProcess,
                    Action<IDictionary<string,Object>> onLoadEnd, LoadPathType pathType = LoadPathType.RuntimePath);
bool LoadCancel(int taskid);
void LoadAllCancel();
string[] GetAssets(string floder, string searchPattern = null);
void WarmUpShaders();
Shader FindShader(string shaderName);
void UnloadAsset(string assetLoadPath, Type type = null);
void UnloadAllAsset();
void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1);
```

## Differences between the two backends

| Dimension | `DevResourceMgr` (Editor) | `AssetBundleMgrV2` (device) |
|-----------|--------------------------|--------------------------|
| Compile condition | The whole file is `#if UNITY_EDITOR` | All platforms |
| Lookup | `FindAssets`: walks `Assets/*/Runtime`, `Directory.GetFiles(dir, filename + ".*")`, and on a name clash re-matches against the requested `Type` | `AssetBundleConfig.LoadPathIdxMap` (`StringComparer.OrdinalIgnoreCase`) |
| Loading | `AssetDatabase.LoadAssetAtPath` + `objsCacheMap` | AB loading → `AssetLoder.LoadAsset(type, guid)` |
| Sync/async | `AsyncLoad<T>(...)` returns `null`; the callback version **runs synchronously**; the List version does 5 per frame | Genuinely async (`LoadTaskGroup`), `MAX_LOAD_TASK_NUM = 10` |
| `LoadAll<T>` | A real implementation | `[Obsolete]`, returns `null` |
| `UnloadAsset` | Only removes from `objsCacheMap` | `UnUseAssetBundle(loadPath)` (reference counting) |
| `UnloadAllAsset` | `Clear` + `UnloadUnusedAssets` + `GC.Collect` | Clears every map + `AssetBundle.UnloadAllAssetBundles(true)` |
| `FindShader` | `Shader.Find(name)` | `ShaderLoder.FindShader` |
| Casing | **Sensitive** (matches real file names) | **Insensitive** (`OrdinalIgnoreCase`) |

!!! danger "Casing differences cause 'works in the Editor, fails to load on device'"
    Always write paths using the exact casing of the real asset file names.

## Additional `AssetBundleMgrV2` APIs

```csharp
public AssetBundleConfigLoader AssetBundleConfig { get; private set; }
static public AssetBundleConfigLoader LoadAssetbundleConfig(string rootPath);

public string FindMultiAddressAsset(string assetbundleFileName);   // firstDir/art_assets → 不存在则 secondDir/art_assets
public void AddAsyncTaskGroup(LoadTaskGroup taskGroup);
static public void AddGlobalLoadTask(LoadTask loadTask);
static public void RemoveGlobalLoadTask(LoadTask loadTask);
static public LoadTask GetExsitLoadTask(string abPath);
public void AddUnloadTask(string assetbundleFileName, UnLoadTask unloadTask);
public void CancelUnloadTask(params string[] paths);

static public int MAX_LOAD_TASK_NUM = 10;
```

**The loading chain** (`Load(Type, loadPath, pathType)`):

```text
AssetBundleConfig.GetDependAssets(loadPath, type)     // 或 GetDependAssetsByGUID
  → 逐个 LoadAssetBundle(item)（主 AB + 依赖 AB）
  → UseAssetBundle(loadPath, type)                    // 引用计数 +1
  → GetAssetObjectFromCache，未命中则 mainAssetLoder.LoadAsset(type, mainItem.GUID)
  → AddAssetObjectToCache
```

!!! warning "An empty dependency list only logs an error, it does not throw"
    `BDebug.LogError("依赖获取失败,art_assets.info不存在资产配置,传入路径:" + loadPath)` followed by `return null`. The symptom is "the asset will not load" rather than a crash.

### `AssetBundleItem`

| Field | Meaning |
|-------|---------|
| `Id` | Asset ID |
| `AssetType` | Asset type (int, mapping to `AssetTypeConfig`) |
| `LoadPath` | Runtime relative path |
| `GUID` | Unity GUID |
| `AssetBundleLoadType` | How the AB is loaded |
| `AssetBundlePath` | AB name |
| `Hash` | Content hash |
| `AssetsPackSourceHash` | Sub-package hash |
| `Mix` | Whether it is obfuscated |
| `DependAssetIds` | List of dependent asset IDs |

`AssetBundleConfigLoader.Load(rootDir)` reads two CSVs (`ServiceStack.Text.CsvSerializer`): `art_asset_type.info` → `List<AssetTypeConfig>`; `art_assets.info` → `List<AssetBundleItem>`.

**If either is `null` → `BDebug.LogError("assets配置文件不存在!!!")` and return (no throw).**

### `LoadTaskGroup`

```csharp
public class LoadTaskGroup : CustomYieldInstruction, IDisposable
{
    public readonly static string LogTag = "LoadTask";
    public delegate void OnTaskComplete();

    public int  Id { get; set; }
    public bool IsComplete { get; }      // isCancel || IsFail || resultObject != null
    public bool IsFail { get; private set; }
    public bool IsCancel { get; }

    public T GetResult<T>() where T : Object;
    public LoadTaskGroup(AssetBundleMgrV2 loader, Type loadType, string loadPath, string guid, …);
    public LoadTaskGroup(Object exsitObject);    // 已加载对象直接包装
}
```

## `BResourcesV2` / `GameObjectWrapper`

```csharp
namespace BDFramework.ResourceMgrV2

public partial class BResourcesV2
{
    public static int GetGlobalInstId();
    public static GameObjectWrapper Instantiate<T>(string assetLoadPath,
        LoadPathType pathType = LoadPathType.RuntimePath, string groupName = null) where T : UnityEngine.Object;
}

public class GameObjectWrapper
{
    public int InstId { get; private set; }
    public int SourceGameObjectId { get; private set; } = -1;    // -1 表示自身即源
    public GameObjectWrapper(int instid, int sourceid, GameObject gameObject);
    public GameObjectWrapper Clone();
}
```

!!! warning "`GameObjectWrapper` does not expose `gameObject`"
    The backing field is `private GameObject gameObject { get; set; }`, so **outside code cannot get hold of the GameObject**. This API is currently half-finished.

On failure: `BDebug.LogError("BResource", $"无该资源:{assetLoadPath}")` and it returns `new GameObjectWrapper(-1, -1, null)`.

## Version control

```csharp
static public AssetsVersionController AssetsVersionController { get; private set; }

static public void StartAssetsVersionControl(UpdateMode updateMode, string serverUrl, string assetsPackageName = "",
    Action<AssetItem, List<AssetItem>> onDownloadProccess = null,
    Action<AssetsVersionController.RetStatus, string> onTaskEndCallback = null);

static public void StartAssetsVersionControlWithDevOps(…);      // 文件服务器协议
static public void GetServerSubPacks(string serverUrl, Action<Dictionary<string,string>> callback);
static public void GetServerSubPacksWithDevOps(…);
```

→ See [Publishing Assets](../pipeline/publish-assets.md).

## Path constants

```csharp
static public string LogTag = "BResourcesV2";

ALL_SHADER_VARAINT_RUNTIME_PATH  = "Shader"
ALL_SHADER_VARAINT_ASSET_PATH    = "Assets/Resource/Runtime/Shader"
DUMMY_SHADER_PATH                = "Shader/Dummy"
MIX_SOURCE_FOLDER                = "Assets/Resource/Runtime/MIX_AB_SOURCE"
SOUND_ASSET_PATH                 = "sound"
ART_ASSET_ROOT_PATH              = "art_assets"
ART_ASSET_INFO_PATH              = "art_assets/art_assets.info"
ART_ASSET_TYPES_PATH             = "art_assets/art_asset_type.info"
EDITOR_ART_ASSET_BUILD_INFO_PATH = "art_assets/EditorBuild.Info"
ASSETS_INFO_PATH                 = "assets.info"
ASSETS_SUB_PACKAGE_CONFIG_PATH   = "assets_subpack.info"
SERVER_ASSETS_VERSION_INFO_PATH  = "server_assets_version.info"
SERVER_ASSETS_SUB_PACKAGE_INFO_PATH = "server_assets_subpack_{0}.info"
SBPBuildLog                      = "buildlogtep.json"
SBPBuildLog2                     = "build_result.info"
```

## Common failures

| Symptom | Root cause |
|---------|------------|
| `NullReferenceException` on `BResources.Load` | `ResLoader == null` (a non-Editor platform misconfigured with `AssetLoadPathType.Editor`) |
| `依赖获取失败,art_assets.info不存在资产配置` | The asset is not in the index; or `art_assets.info` is missing |
| `assets配置文件不存在!!!` | The asset root directory structure is wrong |
| Async loads never call back | `IEnumeratorTool` is not attached |
| `Pool for prefab ... has already been created` | `WarmPool` called twice for the same prefab |
| Fine in the Editor, fails to load on device | Path casing mismatch |

## Related pages

- [Asset Load Paths](../guide/asset-load-path.md)
- [AssetBundle Packing](../pipeline/build-assetbundle.md)
- [Publishing Assets](../pipeline/publish-assets.md)
- [Runtime Module Map](../architecture/runtime-modules.md)
