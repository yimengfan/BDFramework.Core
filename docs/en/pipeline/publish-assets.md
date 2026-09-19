# Asset Publishing

Turns build artifacts into the "server hash layout" and uploads them. This is the final link of the hotfix chain.

## Public API

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

The `PublishAssetsToServer` flow:

```text
遍历 BApplication.SupportPlatform
 → 比对源/目标 assets.info 的 MurmurHash3
     一致 → 【PublishPipeline】资源无改动，无需重新生成服务器文件.
 → GenServerHashAssets(source, outputRoot, platform, version)
 → OnBegin/EndPublishAssets
```

## Upload directory layout

```csharp
var outputPath = IPath.Combine(path, UPLOAD_FOLDER_SUFFIX, baseconfig.ClientVersionNum,
                               BApplication.GetPlatformLoadPath(platform));
```

The actual layout:

```text
<path>/_ReadyToUpload/<ClientVersionNum>/<platform>/
├── <hash>                     ← 文件名即内容 hash（MurmurHash3）
├── assets.info                ← List<AssetItem> CSV
├── server_assets_version.info ← AssetsVersionInfo JSON
└── ...
```

!!! warning "The source comment does not match the implementation"
    The code comment says `{UPLOAD_FOLDER_SUFFIX}/{platform}/{version}`, but **the implementation orders it `<version>/<platform>`**. The code is authoritative.

!!! warning "Split package paths have a double `_ReadyToUpload`"
    ```csharp
    BResources.GetAssetsSubPackageInfoPath(
        IPath.Combine(outputRootPath, UPLOAD_FOLDER_SUFFIX), platform, packageName)
    ```
    This produces `<...>/_ReadyToUpload/<version>/<platform>/_ReadyToUpload/<platform>/server_assets_subpack_<name>.info`. It is a known path concatenation issue.

## Manifest files produced

| File | Format | Location |
|------|------|------|
| `assets.info` | CSV `List<AssetItem>` (`Id` / `HashName` / `LocalPath` / `FileSize`) | Local `<platform>/`; server `<upload root>/` |
| `server_assets_version.info` | JSON `AssetsVersionInfo{Platfrom, Version, SubPckMap}` | `<upload root>/` |
| `server_assets_subpack_<PackageName>.info` | CSV `List<AssetItem>` | See the warning above |
| `global_version.info` | JSON array, per-platform `version_num` | Remote shared version-control entry point |
| `clientRes_{platform}/version.info` | Text `code.assetbundle.table` | Remote |

### Blacklist (never written into `assets.info`)

`IsExcludedServerAssetLocalPath` removes:

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

There is also the obfuscation blacklist in `GetMixAssets()`.

!!! danger "A hash collision throws"
    `throw new Exception("【ServerAssetsItem.Info】错误! hash重复! ...")`

    The hash is a MurmurHash3 of the content and should not collide in theory; if it happens, it means the file content was tampered with or the hash computation is faulty.

## Asset publishing vs client package publishing

| Dimension | Asset publishing | Client package publishing |
|------|---------|---------|
| Entry point | `PublishPipelineTools.PublishAssetsToServer` | `BuildTools_ClientPackage.Build` |
| Menu | `BDFrameWork工具箱/PublishPipeline/1.发布资源` | `BDFrameWork工具箱/5.构建包体` |
| Local output | `DevOps/PublishAssets/_ReadyToUpload/<version>/<platform>/<hash>` | `DevOps/PublishPackages/<platform>/<identifier>/…` |
| Remote directory | `ClientRes_Code_*` / `ClientRes_Assetbundle_*` / `ClientRes_Table/*` | `ClientPackage_{platform}/{buildnum}/` |
| Lifecycle hooks | `OnBegin/EndPublishAssets` | `OnBegin/EndBuildPackage` |
| Frequency | **Every asset change** | Release builds / test builds |

## Remote directory rules (CI upload)

```text
ClientPackage_{platform}/{build_label}/...
ClientRes_Code_{platform}/{build_label}/...
ClientRes_Assetbundle_{platform}/{build_label}/...
ClientRes_Table/{build_label}/...
clientRes_{platform}/version.info          # {code}.{assetbundle}.{table}
global_version.info                        # JSON，按平台 version_num
```

`{build_label}` is built on the Python side by `build_artifact_remote_root(artifact_type, build_number, platform)`.

## Upload protocol

Python helper (`Editor.DevOps~/BuildTools/Common/artifact_uploader.py`):

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

Key APIs:

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

## Runtime download flow

The complete steps of `AssetsVersionController.UpdateAssets` (`UniTask.RunOnThreadPool`):

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

## Key types

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

## File server protocol (DevOps)

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

Version file format: `global_version.info` (JSON array, `version_num` in the form `code.assetbundle.table`).

Other key files: `client_res_state.json` (local state), `version_cache/` (metadata cache), `file_server_verify/` (an isolated download root for CI), `{Code|AssetBundle|Table}_package_build.info`.

BatchMode command line arguments: `-fileServerUrl`, `-expectedCodeVersion`, `-expectedAssetbundleVersion`, `-expectedTableVersion`.

Verification failures are uniform:

```csharp
throw new Exception("[CI][VerifyClientRes] 文件服务器 BatchMode 验证失败! ...");
```

The log prefix is `[CI][VerifyClientRes]`, and it includes the Chinese `测试目的=` / `实现手段=` fields.

## Local isolated output root

```text
<projectDir>/Library/CIOutputs/<build_kind>/<build_name>/<build_number>[/<platform>]
```

`build_kind` ∈ `clientres_code` / `clientres_assetbundle` / `clientres_table`; logs live in `Library/CIOutputs/logs`.

- `build` phase: cleared first (`prepare_clean_ci_output_root`)
- `upload` phase: **not** cleared

## Editor service

`EditorWindow_PublishAssets` also provides `OnGUI_PublishEditorService()` —— publishing to the Editor's built-in http service, so that a local file server can serve a device for download verification.

→ See [Editor HTTP Service](../editor/http-server.md) for details.

## Common failures

| Symptom | Root cause |
|------|------|
| `资源无改动，无需重新生成服务器文件.` | The MurmurHash3 of the source and target `assets.info` match (expected behaviour) |
| `hash重复!` | Abnormal file content or a hash computation problem |
| `服务器不存在子包:<name>` | The sub-package was never published |
| `SuccessNeedRestart` | A DLL was downloaded; the **client must restart** before it takes effect |
| Assets are still old after downloading on device | The version directory falls back to `x.y.0`, reusing the directory of the same major version (see [Asset Load Paths](../guide/asset-load-path.md)) |
| `DeleteOldAssets` keeps firing | The server `assets.info` and the local one disagree (version comparison logic) |
| Upload returns something other than 201 | Check `X-Checksum-Sha256` and `Authorization` |

## Related pages

- [Assets (BResources)](../api/resources.md) —— the `StartAssetsVersionControl` API
- [Asset Load Paths](../guide/asset-load-path.md) —— dual addressing and version directories
- [AssetBundle Building](build-assetbundle.md) —— where the artifacts come from
- [DevOps & CI](devops-ci.md) —— upload helpers and verification flow
