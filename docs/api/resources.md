# 资源加载 BResources

`BDFramework.ResourceMgr.BResources` —— 静态门面（partial），持有 `IResMgr` 实例，按 `AssetLoadPathType` 选择后端。

## 初始化

```csharp
static public IResMgr ResLoader { get; private set; }

static public void Init(AssetLoadPathType loadPathType, string firstDir = "", string secondDir = "");
static public void InitLoadAssetBundleEnv(string path, RuntimePlatform platform);   // 测试用
```

`Init` 的行为：

| `AssetLoadPathType` | 后端 | 说明 |
|--------------------|------|------|
| `Editor` | `DevResourceMgr`（`AssetDatabase`） | **整段在 `#if UNITY_EDITOR` 内**；非 Editor 平台会静默留下 `ResLoader == null` |
| `Hotfix` | `AssetBundleMgrV2` | `Init(firstDir, secondDir)` |

末尾统一 `InitObjectPools()`（`Application.isPlaying && !isInitedPools` 时创建 `GameobjectPools` GameObject 并挂 `GameObjectPoolManager`）。

!!! danger "非 Editor 平台传 `Editor` 会让 `ResLoader` 为 `null`"
    随后 `BResources.Load` 会抛 `NullReferenceException`（**源码没有 null 检查**）。

    而 `UnloadAll()` / `FindShader()` 用的是 `ResLoader?.` 安全写法，**不会抛**。行为不对称。

## 两个易混枚举

```csharp
// BDFramework，Runtime/GameConfig/Config.cs
public enum AssetLoadPathType { Editor = 0, Hotfix = 1 }      // 资源根来源

// BDFramework.ResourceMgr，Runtime/AssetsManager/ArtAsset/IResMgr.cs
public enum LoadPathType { RuntimePath, GUID, AssetsPath }     // 单次加载的路径语义
```

→ 详见[资源加载寻址](../guide/asset-load-path.md)。

## 加载 API

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

**路径为 `null` / 空字符串时，`Load` / `AsyncLoad` / `Unload` 都直接返回 `null`（不报错）。**

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

!!! warning "`LoadALL<T>` 已废弃"
    - `DevResourceMgr`：用 `AssetDatabase.LoadAllAssetsAtPath(rets[0])` **真实实现**
    - `AssetBundleMgrV2`：标了 `[Obsolete]`，实现直接 `return null`

    需要"加载文件夹下全部资源"时，用 `GetAssets(folder, pattern)` 拿路径再逐个 `Load`。

## 取消

```csharp
public static bool LoadCancel(int id);
public static void LoadCancel(params int[] ids);
public static void LoadCancel();          // 内部走 ResLoader.LoadAllCancel()
```

## 卸载

```csharp
public static void UnloadAsset(string assetPath, bool isForceUnload = false, Type type = null);
public static void UnloadAsset(UnityEngine.Object obj);
public static void UnloadAssets(params string[] assetPaths);
public static void UnloadAll();

public static void Destroy(Transform transform);
public static void Destroy(GameObject go);
```

`UnloadAsset(Object)` 的分派逻辑：

| 类型 | 行为 |
|------|------|
| `GameObject` | `Destroy` + `Resources.UnloadAsset` |
| `Sprite` | 额外卸载其 `texture` |
| 其他 | `Resources.UnloadAsset` |

!!! danger "`isForceUnload` 与 `type` 参数被忽略"
    两个参数都没有透传到 `AssetBundleMgrV2.UnloadAsset`（实现只调 `UnUseAssetBundle(loadPath)`）。见[重构清单](../architecture/refactor-backlog.md#ref-1-unloadasset)。

## 资源组

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

## 对象池 { #object-pool }

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

!!! danger "重复 `WarmPool` 同一 prefab 会抛异常"
    `GameObjectPoolManager.WarmPool` 对已存在的池 `throw new Exception("Pool for prefab ... has already been created")`。预热前先 `DestroyPool` 或加判断。

底层是 `GameObjectPoolManager : Singleton<GameObjectPoolManager>`（命名空间 `BDFramework.ResourceMgr`），由 `BResources.Init` 自动创建。

## Shader 与加载配置

```csharp
public static void WarmUpShaders();
public static Shader FindShader(string shaderName);      // ResLoader?. 安全写法

public enum AUPLevel { LowRender, Height, Normal, Low }
static public void SetAUPLEvel(AUPLevel level);
static public void SetLoadConfig(int maxLoadTaskNum = -1, int maxUnloadTaskNum = -1);
```

`SetAUPLEvel` 设置 `asyncUploadBufferSize` / `asyncUploadTimeSlice` / `backgroundLoadingPriority`。

`DevResourceMgr` 的 `WarmUpShaders` / `SetLoadConfig` 是**空实现**（仅 AB 后端有效）。

## `IResMgr` 接口

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

## 两个后端的差异

| 维度 | `DevResourceMgr`（Editor） | `AssetBundleMgrV2`（真机） |
|------|--------------------------|--------------------------|
| 编译条件 | 整个文件 `#if UNITY_EDITOR` | 全平台 |
| 检索 | `FindAssets`：遍历 `Assets/*/Runtime`，`Directory.GetFiles(dir, filename + ".*")`，同名时按目标 `Type` 二次匹配 | `AssetBundleConfig.LoadPathIdxMap`（`StringComparer.OrdinalIgnoreCase`） |
| 加载 | `AssetDatabase.LoadAssetAtPath` + `objsCacheMap` | 依赖 AB 加载 → `AssetLoder.LoadAsset(type, guid)` |
| 同步/异步 | `AsyncLoad<T>(...)` 返回 `null`；带回调版本**同步执行**；List 版本每帧 5 个 | 真异步（`LoadTaskGroup`），`MAX_LOAD_TASK_NUM = 10` |
| `LoadAll<T>` | 真实实现 | `[Obsolete]`，返回 `null` |
| `UnloadAsset` | 只从 `objsCacheMap` 移除 | `UnUseAssetBundle(loadPath)`（引用计数） |
| `UnloadAllAsset` | `Clear` + `UnloadUnusedAssets` + `GC.Collect` | 清各 map + `AssetBundle.UnloadAllAssetBundles(true)` |
| `FindShader` | `Shader.Find(name)` | `ShaderLoder.FindShader` |
| 大小写 | **敏感**（真实文件名匹配） | **不敏感**（`OrdinalIgnoreCase`） |

!!! danger "大小写差异会导致"Editor 能跑、真机加载失败""
    统一按资源真实文件名大小写书写路径。

## `AssetBundleMgrV2` 补充 API

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

**加载链路**（`Load(Type, loadPath, pathType)`）：

```text
AssetBundleConfig.GetDependAssets(loadPath, type)     // 或 GetDependAssetsByGUID
  → 逐个 LoadAssetBundle(item)（主 AB + 依赖 AB）
  → UseAssetBundle(loadPath, type)                    // 引用计数 +1
  → GetAssetObjectFromCache，未命中则 mainAssetLoder.LoadAsset(type, mainItem.GUID)
  → AddAssetObjectToCache
```

!!! warning "依赖为空时只报错不抛异常"
    `BDebug.LogError("依赖获取失败,art_assets.info不存在资产配置,传入路径:" + loadPath)` 后返回 `null`。故障表现是"加载不到资源"而不是崩溃。

### `AssetBundleItem`

| 字段 | 说明 |
|------|------|
| `Id` | 资源 ID |
| `AssetType` | 资源类型（int，对应 `AssetTypeConfig`） |
| `LoadPath` | Runtime 相对路径 |
| `GUID` | Unity GUID |
| `AssetBundleLoadType` | AB 加载方式 |
| `AssetBundlePath` | AB 名 |
| `Hash` | 内容 hash |
| `AssetsPackSourceHash` | 分包 hash |
| `Mix` | 是否混淆 |
| `DependAssetIds` | 依赖资源 ID 列表 |

`AssetBundleConfigLoader.Load(rootDir)` 读两份 CSV（`ServiceStack.Text.CsvSerializer`）：`art_asset_type.info` → `List<AssetTypeConfig>`；`art_assets.info` → `List<AssetBundleItem>`。

**任一为 `null` → `BDebug.LogError("assets配置文件不存在!!!")` 并 return（不抛异常）。**

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

!!! warning "`GameObjectWrapper` 不暴露 `gameObject`"
    内部字段是 `private GameObject gameObject { get; set; }`，**外部无法直接取到 GameObject**。这个 API 目前是半成品状态。

失败时：`BDebug.LogError("BResource", $"无该资源:{assetLoadPath}")` 并返回 `new GameObjectWrapper(-1, -1, null)`。

## 版本控制

```csharp
static public AssetsVersionController AssetsVersionController { get; private set; }

static public void StartAssetsVersionControl(UpdateMode updateMode, string serverUrl, string assetsPackageName = "",
    Action<AssetItem, List<AssetItem>> onDownloadProccess = null,
    Action<AssetsVersionController.RetStatus, string> onTaskEndCallback = null);

static public void StartAssetsVersionControlWithDevOps(…);      // 文件服务器协议
static public void GetServerSubPacks(string serverUrl, Action<Dictionary<string,string>> callback);
static public void GetServerSubPacksWithDevOps(…);
```

→ 详见[资源发布](../pipeline/publish-assets.md)。

## 路径常量

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

## 常见故障

| 现象 | 根因 |
|------|------|
| `NullReferenceException` on `BResources.Load` | `ResLoader == null`（非 Editor 平台误配 `AssetLoadPathType.Editor`） |
| `依赖获取失败,art_assets.info不存在资产配置` | 资源不在索引里；或 `art_assets.info` 缺失 |
| `assets配置文件不存在!!!` | 资源根目录结构不对 |
| 异步加载无回调 | `IEnumeratorTool` 未挂载 |
| `Pool for prefab ... has already been created` | 重复 `WarmPool` |
| Editor 正常、真机加载失败 | 路径大小写不匹配 |

## 相关页面

- [资源加载寻址](../guide/asset-load-path.md)
- [AssetBundle 打包](../pipeline/build-assetbundle.md)
- [资源发布](../pipeline/publish-assets.md)
- [Runtime 模块地图](../architecture/runtime-modules.md)
