# 资源加载寻址

框架有三类资源需要寻址：**代码（DLL）**、**表格（SQLite）**、**美术资源（AssetBundle）**。它们各自有独立的根来源与寻址规则，但共享同一套"双寻址"目录结构。

## 两个必须区分开的枚举

这是最容易被混用的两个类型，名字相似但语义完全不同：

| 枚举 | 命名空间 | 取值 | 回答的问题 |
|------|---------|------|-----------|
| `AssetLoadPathType` | `BDFramework` | `Editor = 0`、`Hotfix = 1` | **资源根从哪里来？**（发布目录 or 设备持久化目录） |
| `LoadPathType` | `BDFramework.ResourceMgr` | `RuntimePath`、`GUID`、`AssetsPath` | **这一次加载用什么路径语义？** |

`AssetLoadPathType` 在 `GameBaseConfigProcessor.Config` 上分三处独立配置——`CodeRoot`、`SQLRoot`、`ArtRoot`，可以分别取不同值：

```csharp
var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
// config.CodeRoot / config.SQLRoot / config.ArtRoot : AssetLoadPathType
// config.CodeRunMode : HotfixCodeRunMode { HyCLR = 1, Mono64 }
```

解析实现（`GameBaseConfigProcessor.GetLoadPath`）：

| `AssetLoadPathType` | 解析结果 |
|--------------------|---------|
| `Editor` | `BApplication.DevOpsPublishAssetsPath` → `<ProjectRoot>/DevOps/PublishAssets` |
| `Hotfix` | `BApplication.persistentDataPath` → Editor 下为 `<ProjectRoot>/.AppData` |

## 路径重写矩阵 { #path-rewrite-matrix }

`BApplication`（`Runtime/Utils/Extensions/Unity3d/BApplication.cs`）在 Editor 下**重写了 Unity 的原生路径**，这是"Editor 能跑通但真机挂"的头号原因：

| `BApplication` 属性 | Editor | 真机 |
|---------------------|--------|------|
| `persistentDataPath` | `<ProjectRoot>/.AppData` | Windows/macOS: `<dataPath>/.AppData`；其他: Unity 原生 |
| `streamingAssetsPath` | `<ProjectRoot>/DevOps/PublishAssets` | Unity 原生 |
| `ProjectRoot` | 工程根 | — |
| `BDEditorCachePath` | `Library/BDFrameCache` | — |
| `DevOpsPath` | `DevOps` | — |
| `DevOpsPublishAssetsPath` | `DevOps/PublishAssets` | — |
| `DevOpsPublishClientPackagePath` | `DevOps/PublishPackages` | — |
| `EditorResourcePath` | `Assets/Resource_SVN` | — |
| `EditorResourceRuntimePath` | `Assets/Resource_SVN/Runtime` | — |
| `RuntimePlatform` | 由宏判定，**绝不返回 `Editor` 枚举** | 当前平台 |

平台目录名由 `BApplication.GetPlatformLoadPath(platform)` 决定：

```text
RuntimePlatform.WindowsPlayer → "windows"
RuntimePlatform.Android       → "android"
RuntimePlatform.OSXPlayer     → "osx"
RuntimePlatform.IPhonePlayer  → "ios"
```

## 双寻址：FIRST / SECOND

真机运行时，所有资源都按"先查可写目录，再查母包只读目录"的两级顺序查找。

```csharp
// BDLauncherBridge.Launch() 内部
var (firstLoadDir, secondLoadDir) =
    ClientAssetsUtils.GetMultiAssetsLoadPath(BApplication.RuntimePlatform, Config.ClientVersionNum);

if (Application.isEditor)
{
    firstLoadDir = secondLoadDir;      // Editor 下强制合并为同一个目录
}

ClientAssetsUtils.CheckBaseClientAssets(firstLoadDir, secondLoadDir);
BResources.Init(Config.ArtRoot, firstLoadDir, secondLoadDir);
SqliteLoder.Init(Config.SQLRoot, firstLoadDir, secondLoadDir);
```

| 目录 | 语义 | 组成 |
|------|------|------|
| `FIRST_LOAD_DIR` | **可写**，热更下载落点 | `persistentDataPath/<major.minor.0>/<platform>` |
| `SECOND_LOAD_DIR` | **只读**，母包内置 | Android: `"<platform>"`（经 `BetterStreamingAssets` 读 APK 内资源）<br/>其他平台: `streamingAssetsPath/<platform>` |

!!! note "版本目录被降级为 `x.y.0`"
    `VersionNumHelper.ParseVersion` 会**清零小版本号与增量位**，即 `1.4.7` 与 `1.4.0` 共用 `1.4.0` 目录。目的是让同一大版本复用同一份资源目录，避免每次发版都全量重新下载。

    **副作用**：同大版本内做灰度/回滚要格外小心，两个构建会写同一个目录。

### `CheckBaseClientAssets` 做的事

1. 若 `firstPath == secondPath`（Editor 场景）→ 直接返回，不做任何检查。
2. 读 `secondPath/package_build.info`；**缺失时非 Editor 直接抛异常**：
   `【母包资源检测】严重错误！母包配置不存在：...`
3. `firstPath` 不存在时执行 `ClearOldPersistentAssets()` —— 删除所有匹配 `^\d+\.\d+\.\d+$` 的旧版本目录。
4. 把 `ClientAssetsUtils.PersistentOnlyFiles` 中列出的文件从母包复制到可写目录（仅在目标不存在时）。当前清单**只有一项**：`SqliteLoder.LOCAL_DB_PATH = "local.db"`。

   也就是说 `local.db` 必须从母包复制到可写目录才能被打开——这就是 Editor 下要手动 `SqliteLoder.LoadLocalDBOnEditor()` 的原因。

## 单次加载的路径语义（`LoadPathType`）

| 值 | 含义 | 示例 |
|----|------|------|
| `RuntimePath`（默认） | **`Runtime` 目录之后的相对路径，不带扩展名** | `"Test/Cube"` → `Assets/*/Runtime/Test/Cube.prefab` |
| `GUID` | Unity GUID | `art_assets.info` 中的 `GUID` 列 |
| `AssetsPath` | 从 `Assets/` 起的完整路径 | `"Assets/Resource/Runtime/Test/Cube.prefab"` |

```csharp
// 推荐写法：不带扩展名
var go = BResources.Load<GameObject>("AssetTest/Cube");

// 异步
int taskId = BResources.AsyncLoad<GameObject>("Test/Cube", o => { /* main thread */ });
BResources.LoadCancel(taskId);
```

!!! danger "路径大小写"
    - `AssetBundleMgrV2` 的索引 `LoadPathIdxMap` 使用 `StringComparer.OrdinalIgnoreCase`，**大小写不敏感**。
    - `DevResourceMgr`（Editor）用 `Directory.GetFiles` 做真实文件名匹配，**大小写敏感**（受文件系统影响，macOS 默认不敏感、Linux 敏感）。

    → **Editor 里能加载不代表真机能加载**。统一按资源真实文件名大小写书写。

## 各资源的磁盘布局

以 `DevOps/PublishAssets/` 为构建根（`BuildParams.OutputPath` 默认值）为例：

```text
DevOps/PublishAssets/
├── <platform>/
│   ├── art_assets/
│   │   ├── <ABName 或 GUID>            # AB 本体（无扩展名）
│   │   ├── art_assets.info             # List<AssetBundleItem> CSV —— 加载索引
│   │   ├── art_asset_type.info         # AssetTypeConfig CSV —— 资源类型表
│   │   └── EditorBuild.Info            # BuildAssetInfos JSON（仅 Editor 用）
│   ├── script/
│   │   ├── hotfix/<asm>.zlua.bytes     # 热更 DLL
│   │   └── aot_patch/<asm>.zlua.bytes  # AOT 补充元数据
│   ├── local.db                        # 主库（SqlCipher 加密）
│   ├── assets.info                     # 资源清单
│   ├── assets_subpack.info             # 分包配置
│   ├── package_build.info              # 母包构建信息（含三段 SVC 版本号）
│   └── server_assets_version.info      # 版本信息
├── server_data/server.db               # 服务器库（不加密）
└── _ReadyToUpload/<version>/<platform> # 待上传（发布管线产出）
```

必备文件的常量定义在 `BResources` 上，不要硬编码字符串：

```csharp
BResources.ART_ASSET_ROOT_PATH            // "art_assets"
BResources.ART_ASSET_INFO_PATH            // "art_assets/art_assets.info"
BResources.ART_ASSET_TYPES_PATH           // "art_assets/art_asset_type.info"
BResources.EDITOR_ART_ASSET_BUILD_INFO_PATH // "art_assets/EditorBuild.Info"
BResources.ASSETS_INFO_PATH               // "assets.info"
BResources.ASSETS_SUB_PACKAGE_CONFIG_PATH // "assets_subpack.info"
BResources.SERVER_ASSETS_VERSION_INFO_PATH // "server_assets_version.info"
```

## 常见故障对照

| 现象 | 根因 | 处理 |
|------|------|------|
| `加载不到资源`，无异常 | `art_assets.info` 缺失或路径不在索引里 | 检查 `DevResourceMgr` 是否能找到；`Runtime` 目录约定是否满足 |
| `DB不存在:<path>` | `local.db` 不在 `FIRST_LOAD_DIR` | Editor 下调用 `SqliteLoder.LoadLocalDBOnEditor()`；真机检查母包复制流程 |
| `【母包资源检测】严重错误！` | 母包 `package_build.info` 缺失 | 重新构建母包 |
| AB 异步加载卡住不回调 | `IEnumeratorTool` 未挂载 | 确认 `BDLauncherBridge.Launch()` 已执行 |
| Editor 正常、真机白屏 | 走了 `AssetLoadPathType.Editor` 分支 | 真机上 `ResLoader` 会是 `null`（见下） |

!!! danger "`BResources.Load` 在未初始化时会抛 NRE"
    `BResources.Init` 的 `AssetLoadPathType.Editor` 分支整体被 `#if UNITY_EDITOR` 包裹。**非 Editor 平台传 `Editor` 会静默留下 `ResLoader == null`**，随后 `BResources.Load` 抛 `NullReferenceException`（源码没有 null 检查）。

    `BResources.UnloadAll()` / `FindShader()` 是 `ResLoader?.` 安全写法，不会抛。行为不对称，需注意。

## 相关页面

- [资源加载 BResources](../api/resources.md) —— 完整 API 与加载/卸载/对象池语义
- [目录结构与约定](project-structure.md) —— `Runtime` / `Table` / `@hotfix` 约定
- [启动链路](../architecture/bootstrap.md) —— 初始化顺序
- [资源发布](../pipeline/publish-assets.md) —— `_ReadyToUpload` 与上传协议
