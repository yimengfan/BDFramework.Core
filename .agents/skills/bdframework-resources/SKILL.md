---
name: bdframework-resources
description: 'BDFramework 资源加载与热更下载技能。使用场景：用 BResources.Load/AsyncLoad/LoadFormPool 加载资源、排查"加载不到资源"/"art_assets.info不存在资产配置"/资源在 Editor 正常真机失败、理解 AssetLoadPathType 与 LoadPathType 的区别、双寻址 FIRST/SECOND 目录、AssetsVersionController 下载与校验流程、对象池 WarmPool/ReleaseToPool、客户端资源热更与版本目录降级、ClientAssetsUtils、DevResourceMgr 与 AssetBundleMgrV2 差异。关键字：BResources、IResMgr、AssetLoadPathType、LoadPathType、RuntimePath、AssetsVersionController、ClientAssetsUtils、FIRST_LOAD_DIR、SECOND_LOAD_DIR、art_assets.info、assets.info、GameObjectPoolManager、WarmPool、DevResourceMgr、AssetBundleMgrV2、LoadTaskGroup、persistentDataPath、streamingAssetsPath。'
---

# BDFramework 资源加载技能

## 1. 何时使用

命中以下任一情况时加载本技能：

- 用 `BResources` 加载/卸载资源、用对象池
- 排查"加载不到资源"、"Editor 正常真机失败"
- 实现/调试客户端资源热更（下载、校验、版本对比）
- 需要理解资源寻址规则

不适用：**打包**（用 `bdframework-build-pipeline`）、**表格**（用 `bdframework-sqlite`）。

## 2. 铁律（先读这 8 条）

| # | 铁律 | 违反后果 |
|---|------|---------|
| 1 | **两个枚举别混**：`AssetLoadPathType` 决定"资源根来源"，`LoadPathType` 决定"单次加载路径语义" | 混用导致真机加载失败 |
| 2 | **非 Editor 平台传 `AssetLoadPathType.Editor` 会让 `ResLoader == null`** | 后续 `BResources.Load` 抛 `NullReferenceException`（源码无 null 检查） |
| 3 | **路径不带扩展名**：`"Test/Cube"` → `Assets/*/Runtime/Test/Cube.prefab` | 带扩展名加载不到 |
| 4 | **路径大小写**：AB 后端不敏感（`OrdinalIgnoreCase`），Editor 后端敏感（真实文件名） | "Editor 能跑真机挂" |
| 5 | **AB 异步加载依赖 `IEnumeratorTool` 组件** | 缺组件时**静默不推进**，不报错 |
| 6 | **`art_assets.info` 缺失只报错不抛异常** | 故障表现为"加载不到资源"而非崩溃 |
| 7 | **版本目录被降级为 `x.y.0`** | `1.4.7` 与 `1.4.0` 共用 `1.4.0` 目录 |
| 8 | **重复 `WarmPool` 同一 prefab 会抛异常** | `Pool for prefab ... has already been created` |

## 3. 两个枚举

```csharp
// BDFramework，Runtime/GameConfig/Config.cs
public enum AssetLoadPathType { Editor = 0, Hotfix = 1 }     // 资源根从哪里来

// BDFramework.ResourceMgr，Runtime/AssetsManager/ArtAsset/IResMgr.cs
public enum LoadPathType { RuntimePath, GUID, AssetsPath }    // 这次加载用什么路径语义
```

`AssetLoadPathType` 在 `GameBaseConfigProcessor.Config` 上**分三处独立配置**：`CodeRoot` / `SQLRoot` / `ArtRoot`。

| `AssetLoadPathType` | 解析结果 |
|--------------------|---------|
| `Editor` | `BApplication.DevOpsPublishAssetsPath` → `<ProjectRoot>/DevOps/PublishAssets` |
| `Hotfix` | `BApplication.persistentDataPath` → Editor 下为 `<ProjectRoot>/.AppData` |

`LoadPathType`：

| 值 | 含义 | 示例 |
|----|------|------|
| `RuntimePath`（默认） | `Runtime` 目录之后的相对路径，**不带扩展名** | `"Test/Cube"` |
| `GUID` | Unity GUID | `art_assets.info` 的 `GUID` 列 |
| `AssetsPath` | 从 `Assets/` 起的完整路径 | `"Assets/Resource/Runtime/Test/Cube.prefab"` |

## 4. 加载 API

```csharp
public static T Load<T>(string assetLoadPath, LoadPathType pathType = LoadPathType.RuntimePath,
                        string groupName = null) where T : UnityEngine.Object;

public static LoadTaskGroup AsyncLoad<T>(string assetLoadPath, LoadPathType loadPathType = LoadPathType.RuntimePath)
    where T : UnityEngine.Object;

public static int AsyncLoad<T>(string assetLoadPath, Action<T> action,
                               LoadPathType loadPathType = LoadPathType.RuntimePath,
                               string groupName = null) where T : UnityEngine.Object;

public static List<int> AsyncLoad(List<string> assetlist, Action<int,int> onProcess = null,
                                  Action<IDictionary<string,Object>> onLoadEnd = null,
                                  LoadPathType loadPathType = LoadPathType.RuntimePath,
                                  string groupName = null);

[Obsolete("已废弃,不建议项目使用!")]
public static T[] LoadALL<T>(string assetLoadPath) where T : UnityEngine.Object;

static public string[] GetAssets(string floder, string searchPattern = null);
```

**路径为 `null` / 空时，`Load` / `AsyncLoad` / `Unload` 都直接返回 `null`（不报错）。**

```csharp
// 同步
var prefab = BResources.Load<GameObject>("Test/Cube");
var sprite = BResources.Load<Sprite>("UI/Icon/coin");

// 异步 + 取消
int id = BResources.AsyncLoad<GameObject>("Test/Cube", go => { /* 主线程 */ });
BResources.LoadCancel(id);

// 协程等待
LoadTaskGroup task = BResources.AsyncLoad<GameObject>("Test/Cube");
yield return task;                          // LoadTaskGroup : CustomYieldInstruction
var go = task.GetResult<GameObject>();

// 批量 + 进度
var ids = BResources.AsyncLoad(pathList,
    onProcess: (total, cur) => { },
    onLoadEnd: dict => { });
```

!!! danger "`LoadALL<T>` 已废弃"
    - `DevResourceMgr`：用 `AssetDatabase.LoadAllAssetsAtPath` **真实实现**
    - `AssetBundleMgrV2`：`[Obsolete]`，**直接 `return null`**

    需要"加载文件夹下全部资源"时：`GetAssets(folder, pattern)` 拿路径再逐个 `Load`。

## 5. 卸载与对象池

```csharp
public static void UnloadAsset(string assetPath, bool isForceUnload = false, Type type = null);
public static void UnloadAsset(UnityEngine.Object obj);
public static void UnloadAssets(params string[] assetPaths);
public static void UnloadAll();
public static void Destroy(Transform transform);
public static void Destroy(GameObject go);
```

!!! danger "`isForceUnload` 与 `type` 参数被忽略"
    都没有透传到 `AssetBundleMgrV2.UnloadAsset`（实现只调 `UnUseAssetBundle(loadPath)`）。

`UnloadAsset(Object)` 分派：`GameObject` → `Destroy` + `UnloadAsset`；`Sprite` → 额外卸载 texture；其他 → `UnloadAsset`。

```csharp
// 对象池
BResources.WarmPool("Bullet/Normal", 20);
var bullet = BResources.LoadFormPool("Bullet/Normal");
BResources.ReleaseToPool(bullet);
BResources.DestroyPool("Bullet/Normal");
```

!!! danger "重复 `WarmPool` 同一 prefab 抛异常"
    `GameObjectPoolManager.WarmPool` 对已存在的池 `throw new Exception("Pool for prefab ... has already been created")`。预热前先 `DestroyPool` 或加判断。

## 6. 资源组

```csharp
BResources.AddAssetsPathToGroup("Battle", "Battle/Map1", "Battle/Map2");
BResources.UnloadAssetByGouroup("Battle");      // ★ 源码拼写是 Gouroup
BResources.ClearAssetGroup("Battle");
var paths = BResources.GetAssetsPathByGroup("Battle");
```

## 7. 双寻址

```csharp
// BDLauncherBridge.Launch() 内部
var (firstLoadDir, secondLoadDir) =
    ClientAssetsUtils.GetMultiAssetsLoadPath(BApplication.RuntimePlatform, Config.ClientVersionNum);

if (Application.isEditor) firstLoadDir = secondLoadDir;      // Editor 下强制合并

ClientAssetsUtils.CheckBaseClientAssets(firstLoadDir, secondLoadDir);
BResources.Init(Config.ArtRoot, firstLoadDir, secondLoadDir);
```

| 目录 | 语义 | 组成 |
|------|------|------|
| `FIRST_LOAD_DIR` | **可写**，热更下载落点 | `persistentDataPath/<major.minor.0>/<platform>` |
| `SECOND_LOAD_DIR` | **只读**，母包内置 | Android: `"<platform>"`（经 `BetterStreamingAssets`）<br/>其他: `streamingAssetsPath/<platform>` |

!!! danger "版本目录被降级为 `x.y.0`"
    `VersionNumHelper.ParseVersion` **清零小版本号与增量位**。`1.4.7` 与 `1.4.0` 共用 `1.4.0` 目录。

    **副作用**：同大版本内做灰度/回滚要格外小心。

### `CheckBaseClientAssets` 做的事

1. `firstPath == secondPath`（Editor）→ 直接返回
2. 读 `secondPath/package_build.info`；缺失时非 Editor **抛异常**：`【母包资源检测】严重错误！母包配置不存在：...`
3. `firstPath` 不存在 → `ClearOldPersistentAssets()`（删除匹配 `^\d+\.\d+\.\d+$` 的旧版本目录）
4. 把 `ClientAssetsUtils.PersistentOnlyFiles` 从母包复制到可写目录（仅在目标不存在时）。**当前清单只有 `local.db`**

### Editor 路径重写

| `BApplication` 属性 | Editor | 真机 |
|---------------------|--------|------|
| `persistentDataPath` | `<ProjectRoot>/.AppData` | Windows/macOS: `<dataPath>/.AppData`；其他: Unity 原生 |
| `streamingAssetsPath` | `<ProjectRoot>/DevOps/PublishAssets` | Unity 原生 |
| `BDEditorCachePath` | `Library/BDFrameCache` | — |

平台目录名：`windows` / `android` / `osx` / `ios`。

## 8. 两个后端差异

| 维度 | `DevResourceMgr`（Editor） | `AssetBundleMgrV2`（真机） |
|------|--------------------------|--------------------------|
| 编译条件 | 整文件 `#if UNITY_EDITOR` | 全平台 |
| 检索 | `Assets/*/Runtime` 遍历 + `Directory.GetFiles(dir, filename + ".*")`，同名按目标 `Type` 二次匹配 | `AssetBundleConfig.LoadPathIdxMap`（`StringComparer.OrdinalIgnoreCase`） |
| 加载 | `AssetDatabase.LoadAssetAtPath` + `objsCacheMap` | AB 加载 → `AssetLoder.LoadAsset(type, guid)` |
| 同步/异步 | `AsyncLoad<T>(...)` 返回 `null`；带回调版本**同步执行**；List 版本每帧 5 个 | 真异步（`LoadTaskGroup`），`MAX_LOAD_TASK_NUM = 10` |
| `LoadAll<T>` | 真实实现 | `[Obsolete]`，返回 `null` |
| `UnloadAsset` | 只从 `objsCacheMap` 移除 | `UnUseAssetBundle(loadPath)`（引用计数） |
| `UnloadAllAsset` | `Clear` + `UnloadUnusedAssets` + `GC.Collect` | 清各 map + `AssetBundle.UnloadAllAssetBundles(true)` |
| `FindShader` | `Shader.Find(name)` | `ShaderLoder.FindShader` |
| 大小写 | **敏感** | **不敏感** |

!!! danger "大小写差异是"Editor 能跑、真机加载失败"的头号原因"
    统一按资源真实文件名大小写书写路径。

### `AssetBundleMgrV2` 加载链路

```text
Load(Type, loadPath, pathType)
 → AssetBundleConfig.GetDependAssets(loadPath, type)      // 或 GetDependAssetsByGUID
 → 逐个 LoadAssetBundle(item)（主 AB + 依赖 AB）
 → UseAssetBundle(loadPath, type)                          // 引用计数 +1
 → GetAssetObjectFromCache，未命中则 mainAssetLoder.LoadAsset(type, mainItem.GUID)
 → AddAssetObjectToCache
```

!!! warning "依赖为空时只报错不抛异常"
    `BDebug.LogError("依赖获取失败,art_assets.info不存在资产配置,传入路径:" + loadPath)` 后返回 `null`。

## 9. 磁盘布局

```text
DevOps/PublishAssets/                     ← 构建根（Editor 下 streamingAssetsPath 指向这里）
├── <platform>/
│   ├── art_assets/
│   │   ├── <ABName 或 GUID>            AB 本体（无扩展名）
│   │   ├── art_assets.info             List<AssetBundleItem> CSV ← 加载索引
│   │   ├── art_asset_type.info         AssetTypeConfig CSV
│   │   └── EditorBuild.Info            构建信息（仅 Editor）
│   ├── script/hotfix/                  热更 DLL
│   ├── script/aot_patch/               AOT 补充元数据
│   ├── local.db                        客户端表（加密）
│   ├── assets.info                     资源清单
│   ├── assets_subpack.info             分包配置
│   ├── package_build.info              母包构建信息
│   └── server_assets_version.info      版本信息
├── server_data/server.db               服务器表（不加密）
└── _ReadyToUpload/<version>/<platform> 待上传
```

用常量而非硬编码：

```csharp
BResources.ART_ASSET_ROOT_PATH              // "art_assets"
BResources.ART_ASSET_INFO_PATH              // "art_assets/art_assets.info"
BResources.ART_ASSET_TYPES_PATH             // "art_assets/art_asset_type.info"
BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH // "art_assets/EditorBuild.Info"
BResources.ASSETS_INFO_PATH                 // "assets.info"
BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH   // "assets_subpack.info"
BResources.SERVER_ASSETS_VERSION_INFO_PATH  // "server_assets_version.info"
BResources.SOUND_ASSET_PATH                 // "sound"
BResources.MIX_SOURCE_FOLDER                // "Assets/Resource/Runtime/MIX_AB_SOURCE"
BResources.SBPBuildLog / SBPBuildLog2       // "buildlogtep.json" / "build_result.info"
```

## 10. 热更下载流程

```csharp
BResources.StartAssetsVersionControl(
    UpdateMode.CompareSimple,
    serverUrl,
    assetsPackageName: "",
    onDownloadProccess: (item, all) => { },
    onTaskEndCallback: (status, err) => { });
```

`AssetsVersionController.UpdateAssets` 步骤（`UniTask.RunOnThreadPool`）：

```text
① 目录准备 persistentDataPath/<platform>
② 下载 server_assets_version.info（JsonMapper，重试 RETRY_COUNT = 5）
     本地优先读 persistent 同名文件，回退 ClientAssetsUtils.GetBasePackBuildInfo().Version
③ 按 UpdateMode 对比：
     CompareSimple               → 本地 >= 服务器 → "本地版本相同或更新,无需下载!" 结束
     CompareWithRepairCoreAssets → 差异走 Repair
     RepairFull                  → LoadServerAssetInfo
     分包模式 → GetDownloadSubPackageData（子包不存在 → "【版本控制】服务器不存在子包:"）
④ 生成差异队列（Compare 用 Equals+IsExsitAsset；Repair 用 IsExsitAssetWithCheckHash）
⑤ 下载（线程池）逐个 webClient.DownloadDataTaskAsync(serverUrl/<platform>/<HashName>)
     每文件最多 RETRY_COUNT 次；校验 FileHelper.GetMurmurHash3(data) == HashName
     成功后 SwitchToMainThread 触发 onDownloadProccess，再 SwitchToThreadPool 落盘
     全部完成后 FileHelper.Move(hashFile → LocalPath)
     .dll / .dll.bytes / .zlua.bytes → isNeedRestart = true
⑥ 写回 assets.info / server_assets_version.info
⑦ 删除过期资源（仅非分包模式）→ RetStatus.DeleteOldAssets
⑧ 逐项校验 → RetStatus.Checkassets
⑨ RetStatus.Success / SuccessNeedRestart / Error
```

```csharp
public enum RetStatus { Checkassets = 0, DeleteOldAssets, Error, Success, SuccessNeedRestart }
public enum UpdateMode { CompareSimple, CompareWithRepairCoreAssets, RepairFull }
```

!!! danger "`SuccessNeedRestart` 必须重启客户端"
    下载了 DLL 时返回此状态，DLL 需要重启才能装载。

## 11. 故障对照

| 现象 | 根因 | 处理 |
|------|------|------|
| `NullReferenceException` on `BResources.Load` | `ResLoader == null`（非 Editor 平台误配 `AssetLoadPathType.Editor`） | 检查 `Config.ArtRoot` |
| `依赖获取失败,art_assets.info不存在资产配置` | 资源不在索引里；或 `art_assets.info` 缺失 | 检查资源是否在 `Assets/*/Runtime/**` |
| `assets配置文件不存在!!!` | 资源根目录结构不对 | 检查 `DevOps/PublishAssets/<platform>/art_assets/` |
| 异步加载无回调 | `IEnumeratorTool` 未挂载 | 确认 `BDLauncherBridge.Launch()` 已执行 |
| `Pool for prefab ... has already been created` | 重复 `WarmPool` | 先 `DestroyPool` |
| Editor 正常、真机加载失败 | 路径大小写不匹配 | 按真实文件名大小写书写 |
| `DB不存在:<path>` | `local.db` 不在 `FIRST_LOAD_DIR` | Editor 用 `SqliteLoder.LoadLocalDBOnEditor()` |
| `【母包资源检测】严重错误！` | 母包 `package_build.info` 缺失 | 重新构建母包 |
| `服务器不存在子包:<name>` | 子包未发布 | 检查发布流程 |
| 同大版本灰度/回滚异常 | 版本目录降级为 `x.y.0`，两构建共用目录 | 设计如此，需在流程上规避 |

## 12. 详细参考

| 文件 | 内容 |
|------|------|
| [references/version-controller.md](./references/version-controller.md) | 版本控制完整 API、文件服务器协议、CI BatchMode 验证 |

在线文档：

- [资源加载 BResources](https://yimengfan.github.io/BDFramework.Core/api/resources.md)
- [资源加载寻址](https://yimengfan.github.io/BDFramework.Core/guide/asset-load-path.md)
- [资源发布](https://yimengfan.github.io/BDFramework.Core/pipeline/publish-assets.md)

## 13. 改动前检查清单

- [ ] 路径**不带扩展名**，且大小写与真实文件名一致
- [ ] 资源位于 `Assets/*/Runtime/**` 下
- [ ] 真机路径用 `AssetLoadPathType.Hotfix`，不是 `Editor`
- [ ] 异步加载前确认 `IEnumeratorTool` 已挂载
- [ ] `WarmPool` 前检查池是否已存在
- [ ] 卸载时注意 `isForceUnload` / `type` 参数**无效**
- [ ] `SuccessNeedRestart` 时提示用户重启
