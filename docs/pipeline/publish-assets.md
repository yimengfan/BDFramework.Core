# 资源发布

把构建产物转成"服务器 hash 布局"并上传。这是热更链路的最后一环。

## 公开 API

```csharp
namespace BDFramework.Editor.PublishPipeline

public static class PublishPipelineTools
{
    static public void PublishAssetsToServer(string path);
    static public List<AssetItem> GetGameAssetItemList(string assetsRootPath, RuntimePlatform platform);
    static public string GenServerHashAssets(string source, string outputRootPath,
                                            RuntimePlatform platform, string version);
    static public string UPLOAD_FOLDER_SUFFIX = "_ReadyToUpload";
}
```

`PublishAssetsToServer` 流程：

```text
遍历 BApplication.SupportPlatform
 → 比对源/目标 assets.info 的 MurmurHash3
     一致 → 【PublishPipeline】资源无改动，无需重新生成服务器文件.
 → GenServerHashAssets(source, outputRoot, platform, version)
 → OnBegin/EndPublishAssets
```

## 上传目录布局

```csharp
var outputPath = IPath.Combine(path, UPLOAD_FOLDER_SUFFIX, baseconfig.ClientVersionNum,
                               BApplication.GetPlatformLoadPath(platform));
```

实际布局：

```text
<path>/_ReadyToUpload/<ClientVersionNum>/<platform>/
├── <hash>                     ← 文件名即内容 hash（MurmurHash3）
├── assets.info                ← List<AssetItem> CSV
├── server_assets_version.info ← AssetsVersionInfo JSON
└── ...
```

!!! warning "源码注释与实现不一致"
    代码注释写的是 `{UPLOAD_FOLDER_SUFFIX}/{platform}/{version}`，**实现顺序是 `<version>/<platform>`**。以代码为准。

!!! warning "分包路径有双层 `_ReadyToUpload`"
    ```csharp
    BResources.GetAssetsSubPackageInfoPath(
        IPath.Combine(outputRootPath, UPLOAD_FOLDER_SUFFIX), platform, packageName)
    ```
    会产生 `<...>/_ReadyToUpload/<version>/<platform>/_ReadyToUpload/<platform>/server_assets_subpack_<name>.info`。这是已知的路径拼接问题。

## 产出的清单文件

| 文件 | 格式 | 位置 |
|------|------|------|
| `assets.info` | CSV `List<AssetItem>`（`Id` / `HashName` / `LocalPath` / `FileSize`） | 本地 `<platform>/`；服务器 `<上传根>/` |
| `server_assets_version.info` | JSON `AssetsVersionInfo{Platfrom, Version, SubPckMap}` | `<上传根>/` |
| `server_assets_subpack_<PackageName>.info` | CSV `List<AssetItem>` | 见上方警告 |
| `global_version.info` | JSON 数组，按平台 `version_num` | 远端共享版控入口 |
| `clientRes_{platform}/version.info` | 文本 `code.assetbundle.table` | 远端 |

### 黑名单（不写入 `assets.info`）

`IsExcludedServerAssetLocalPath` 会剔除：

```text
art_assets/EditorBuild.Info
assets.info
assets_subpack.info
build_result.info
package_build.info
art_assets/art_assets
文件名含 buildlogtep.json
文件名含 build_result.info
```

另有 `GetMixAssets()` 的混淆黑名单。

!!! danger "hash 冲突会抛异常"
    `throw new Exception("【ServerAssetsItem.Info】错误! hash重复! ...")`

    hash 是内容的 MurmurHash3，理论上不会冲突；出现即意味着文件内容被篡改或 hash 计算异常。

## 资源发布 vs 母包发布

| 维度 | 资源发布 | 母包发布 |
|------|---------|---------|
| 入口 | `PublishPipelineTools.PublishAssetsToServer` | `BuildTools_ClientPackage.Build` |
| 菜单 | `BDFrameWork工具箱/PublishPipeline/1.发布资源` | `BDFrameWork工具箱/5.构建包体` |
| 本地输出 | `DevOps/PublishAssets/_ReadyToUpload/<version>/<platform>/<hash>` | `DevOps/PublishPackages/<platform>/<identifier>/…` |
| 远端目录 | `ClientRes_Code_*` / `ClientRes_Assetbundle_*` / `ClientRes_Table/*` | `ClientPackage_{platform}/{buildnum}/` |
| 生命周期钩子 | `OnBegin/EndPublishAssets` | `OnBegin/EndBuildPackage` |
| 频率 | **每次资源改动** | 发版/测试包 |

## 远端目录规则（CI 上传）

```text
ClientPackage_{platform}/{build_label}/...
ClientRes_Code_{platform}/{build_label}/...
ClientRes_Assetbundle_{platform}/{build_label}/...
ClientRes_Table/{build_label}/...
clientRes_{platform}/version.info          # {code}.{assetbundle}.{table}
global_version.info                        # JSON，按平台 version_num
```

`{build_label}` 由 Python 侧 `build_artifact_remote_root(artifact_type, build_number, platform)` 构造。

## 上传协议

Python helper（`Editor.DevOps~/BuildTools/Common/artifact_uploader.py`）：

```text
PUT /api/files/{remote_path}?overwrite=true|false
Headers:
    Content-Length
    Content-Type
    X-Checksum-Sha256
    Authorization
期望响应: 201
服务端 5xx → try_recover_failed_upload()

下载: GET /files/{remote_path}
      （Python 侧 base_url 不含 /files，传给 Unity 时追加 /files）
```

关键 API：

```python
resolve_file_server_settings(server_url, config_path)
upload_single_file(...)
upload_to_remote_root(source_path, remote_root=..., ...)
upload_client_package(...)
build_artifact_remote_root(artifact_type, build_number, platform)
fetch_remote_metadata / listing / download_sha256

ArtifactType{CLIENT_PACKAGE, CODE, ASSET_BUNDLE, TABLE}
FileServerClientSettings
UploadedArtifact
compute_file_sha256
```

## 运行时下载流程

`AssetsVersionController.UpdateAssets` 的完整步骤（`UniTask.RunOnThreadPool`）：

```text
① 目录准备
     localSavePlatformPath = persistentDataPath/<platform>
     不存在则 Directory.CreateDirectory

② 下载服务器版本信息
     DownloadAssetVersionInfo → 拉 server_assets_version.info（JsonMapper）
     重试 RETRY_COUNT = 5 次
     本地侧优先读 persistent 的同名文件，不存在则回退 ClientAssetsUtils.GetBasePackBuildInfo().Version
     失败 → onTaskEndCallback(RetStatus.Error, err)

③ 对比版本 / 取数据（按 UpdateMode）
     CompareSimple              → GetDownloadAssetsData
                                  本地 >= 服务器 → "本地版本相同或更新,无需下载!" 直接结束
     CompareWithRepairCoreAssets → 对比同上，差异走 Repair
     RepairFull                 → LoadServerAssetInfo(serverUrl/<platform>/assets.info)
     分包模式（assetsPackageName 非空）→ GetDownloadSubPackageData
       子包不存在 → err = "【版本控制】服务器不存在子包:" + subPackageName

④ 生成差异队列
     Compare → AssetItem.Equals 匹配本地 + ClientAssetsUtils.IsExsitAsset 双校验
     Repair  → ClientAssetsUtils.IsExsitAssetWithCheckHash 全量 hash 校验

⑤ 下载（Task 线程池）
     逐个 webClient.DownloadDataTaskAsync(serverUrl/<platform>/<HashName>)
     每文件最多 RETRY_COUNT 次
     校验 FileHelper.GetMurmurHash3(taskData) == HashName
     成功后 await UniTask.SwitchToMainThread() 触发 onDownloadProccess
     再 SwitchToThreadPool 落盘到 <local>/<platform>/<HashName>
     全部完成后统一 FileHelper.Move(hashFile → LocalPath)
     .dll / .dll.bytes / .zlua.bytes 会置 isNeedRestart = true

⑥ 写回本地
     <persistent>/<platform>/assets.info（或子包文件）← serverAssetsContent
     server_assets_version.info ← 更新后的本地 AssetsVersionInfo

⑦ 删除过期资源（仅非分包模式）
     扫 persistent/<platform>/art_assets/**
     服务器列表里找不到的文件 → onTaskEndCallback(RetStatus.DeleteOldAssets, localPath) 后 File.Delete

⑧ 逐项校验
     对每条 server item 调 ClientAssetsUtils.IsExsitAssetWithCheckHash
     每次触发 onTaskEndCallback(RetStatus.Checkassets, HashName)
     有失败项 → err = "资源不存在:" + 换行列表

⑨ 收尾
     err == null → RetStatus.Success 或 RetStatus.SuccessNeedRestart
     否则       → RetStatus.Error
```

### `RetStatus`

```csharp
public enum RetStatus
{
    Checkassets = 0,        // 逐项校验中
    DeleteOldAssets,        // 删除过期资源
    Error,
    Success,
    SuccessNeedRestart,     // 下载了 DLL，需要重启
}
```

## 关键类型

```csharp
public class AssetsVersionInfo
{
    string Platfrom;                          // ★ 源码拼写是 Platfrom
    string Version;
    Dictionary<string,string> SubPckMap { get; }
}

public class AssetItem
{
    public enum AssetType { AssetBundle, DLL, Sqlite }
    int   Id;
    string HashName;
    string LocalPath;
    float FileSize;
    // Equals = HashName + LocalPath
    // GetHashCode = "LocalPath|HashName"
}

public enum UpdateMode { CompareSimple, CompareWithRepairCoreAssets, RepairFull }

public class SubPackageConfigItem
{
    public string PackageName;
    public List<int>    ArtAssetsIdList;
    public List<string> HotfixCodePathList;
    public List<string> TablePathList;
    public List<string> ConfAndInfoList;
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

public sealed class FileServerBatchVerificationResult
{
    RuntimePlatform Platform;
    string PlatformPath, FirstLoadDir, ExpectedVersion, ActualVersion;
    bool UsedLocalFallbackVersion;
    string CodeAssetLocalPath, AssetBundleAssetLocalPath;
    string AssetBundleValidationFirstTarget;
    List<string> AssetBundleAssetLocalPaths;
    List<FileServerAssetBundleValidationEntry> AssetBundleValidationEntries;
    string TableAssetLocalPath;
    ClientPackageBuildInfo PackageBuildInfo;
    string Error;
    bool IsSuccess => string.IsNullOrEmpty(Error);
}
```

版本文件格式：`global_version.info`（JSON 数组，`version_num` 格式 `code.assetbundle.table`）。

其他关键文件：`client_res_state.json`（本地状态）、`version_cache/`（元数据缓存）、`file_server_verify/`（CI 独立下载根）、`{Code|AssetBundle|Table}_package_build.info`。

BatchMode 命令行参数：`-fileServerUrl`、`-expectedCodeVersion`、`-expectedAssetbundleVersion`、`-expectedTableVersion`。

验证失败统一：

```csharp
throw new Exception("[CI][VerifyClientRes] 文件服务器 BatchMode 验证失败! ...");
```

日志前缀 `[CI][VerifyClientRes]`，含中文 `测试目的=` / `实现手段=`。

## 本地隔离输出根

```text
<projectDir>/Library/CIOutputs/<build_kind>/<build_name>/<build_number>[/<platform>]
```

`build_kind` ∈ `clientres_code` / `clientres_assetbundle` / `clientres_table`；日志在 `Library/CIOutputs/logs`。

- `build` 阶段：先清空（`prepare_clean_ci_output_root`）
- `upload` 阶段：**不清空**

## 编辑器服务

`EditorWindow_PublishAssets` 还提供 `OnGUI_PublishEditorService()` —— 发布到编辑器内置 http 服务，配合本机文件服务器供真机下载验证。

→ 详见 [Editor Http 服务](../editor/http-server.md)。

## 常见故障

| 现象 | 根因 |
|------|------|
| `资源无改动，无需重新生成服务器文件.` | 源/目标 `assets.info` 的 MurmurHash3 一致（预期行为） |
| `hash重复!` | 文件内容异常或 hash 计算问题 |
| `服务器不存在子包:<name>` | 子包未发布 |
| `SuccessNeedRestart` | 下载了 DLL，**必须重启客户端**才生效 |
| 真机下载后资源还是旧的 | 版本目录降级为 `x.y.0`，同大版本复用目录（见[资源加载寻址](../guide/asset-load-path.md)） |
| `DeleteOldAssets` 反复触发 | 服务器 `assets.info` 与本地不一致（版本对比逻辑） |
| 上传返回非 201 | 检查 `X-Checksum-Sha256` 与 `Authorization` |

## 相关页面

- [资源加载 BResources](../api/resources.md) —— `StartAssetsVersionControl` API
- [资源加载寻址](../guide/asset-load-path.md) —— 双寻址与版本目录
- [AssetBundle 打包](build-assetbundle.md) —— 产物来源
- [DevOps 与 CI](devops-ci.md) —— 上传 helper 与验证流程
