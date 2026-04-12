using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Asset;
using BDFramework.ResourceMgr.V2;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 文件服务器版控协议的纯逻辑工具集。
    /// 这个类型专门承载不会触发下载器、StreamingAssets 或资源控制器初始化的 helper，
    /// 让 Runtime.Test 和 batchmode 验证可以直接复用生产逻辑而不拉起重量环境。
    /// </summary>
    /// <remarks>
    /// 运行时主流程仍通过 <see cref="AssetsVersionController"/> 协调；这里只放可独立复用的解析、合并、筛选和一致性校验逻辑。
    /// </remarks>
    /// <example>
    /// 纯逻辑批验证入口可以直接调用：
    /// <code>
    /// AssetsVersionControllerDevOpsPureLogic.TryParseFileServerVersionInfo("101.202.303", out var versionInfo);
    /// </code>
    /// </example>
    internal static class AssetsVersionControllerDevOpsPureLogic
    {
        private const string ArtAssetRootPath = "art_assets";
        private const string LocalDbPath = "local.db";

        /// <summary>
        /// 解析共享三段版控文件 <c>code.assetbundle.table</c>。
        /// </summary>
        internal static bool TryParseFileServerVersionInfo(
            string content,
            out AssetsVersionController.FileServerVersionInfo versionInfo)
        {
            versionInfo = null;
            var normalized = (content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            var segments = normalized.Split('.');
            if (segments.Length != 3)
            {
                return false;
            }

            segments = segments.Select(item => item.Trim()).ToArray();
            if (segments.Any(string.IsNullOrEmpty) || segments.Any(item => item.Contains(".")))
            {
                return false;
            }

            versionInfo = new AssetsVersionController.FileServerVersionInfo()
            {
                CodeVersion = segments[0],
                AssetBundleVersion = segments[1],
                TableVersion = segments[2],
            };
            return true;
        }

        /// <summary>
        /// 把三段版控对象序列化回共享文本格式。
        /// </summary>
        internal static string FormatFileServerVersionInfo(AssetsVersionController.FileServerVersionInfo versionInfo)
        {
            if (versionInfo == null)
            {
                return string.Empty;
            }

            return $"{versionInfo.CodeVersion}.{versionInfo.AssetBundleVersion}.{versionInfo.TableVersion}";
        }

        /// <summary>
        /// 从 <c>global_version.info</c> JSON 数组中提取指定平台的 <c>version_num</c>，
        /// 再解析为三段版控 <c>FileServerVersionInfo</c>。
        /// JSON 格式：<c>[{"key":"default","platform":"android","version_num":"20.22.17","game_server_ip":"127.0.0.1"}]</c>。
        /// 平台匹配忽略大小写；找不到或格式非法返回 false。
        /// </summary>
        internal static bool TryParseGlobalVersionInfoJson(
            string content,
            string platform,
            out AssetsVersionController.FileServerVersionInfo versionInfo)
        {
            versionInfo = null;
            var normalizedContent = (content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalizedContent) || string.IsNullOrEmpty(platform))
            {
                return false;
            }

            // MiniJSON does not ship in this assembly; use UnityEngine.JsonUtility via a wrapper array.
            // The root is a JSON array, so wrap it for JsonUtility which expects a top-level object.
            var wrappedJson = "{\"entries\":" + normalizedContent + "}";
            GlobalVersionInfoWrapper wrapper;
            try
            {
                wrapper = JsonUtility.FromJson<GlobalVersionInfoWrapper>(wrappedJson);
            }
            catch (Exception)
            {
                return false;
            }

            if (wrapper?.entries == null)
            {
                return false;
            }

            var normalizedPlatform = platform.Trim().ToLowerInvariant();
            GlobalVersionInfoEntry matchedEntry = null;
            foreach (var entry in wrapper.entries)
            {
                if ((entry.platform ?? "").Trim().ToLowerInvariant() == normalizedPlatform)
                {
                    matchedEntry = entry;
                    break;
                }
            }

            if (matchedEntry == null || string.IsNullOrEmpty(matchedEntry.version_num))
            {
                return false;
            }

            return TryParseFileServerVersionInfo(matchedEntry.version_num, out versionInfo);
        }

        /// <summary>
        /// <c>global_version.info</c> JSON 数组的 Unity 序列化包装。
        /// </summary>
        [Serializable]
        internal sealed class GlobalVersionInfoWrapper
        {
            public GlobalVersionInfoEntry[] entries;
        }

        /// <summary>
        /// <c>global_version.info</c> JSON 数组中单条记录的 Unity 序列化结构。
        /// </summary>
        [Serializable]
        internal sealed class GlobalVersionInfoEntry
        {
            public string key;
            public string platform;
            public string version_num;
            public string game_server_ip;
        }

        /// <summary>
        /// 合并三个组件各自携带的 package_build.info，避免新协议落地时互相覆盖。
        /// </summary>
        internal static ClientPackageBuildInfo MergeFileServerPackageBuildInfo(
            ClientPackageBuildInfo baseInfo,
            ClientPackageBuildInfo codeInfo,
            ClientPackageBuildInfo assetBundleInfo,
            ClientPackageBuildInfo tableInfo)
        {
            var mergedInfo = new ClientPackageBuildInfo();
            if (baseInfo != null)
            {
                mergedInfo.BuildTime = baseInfo.BuildTime;
                mergedInfo.Version = baseInfo.Version;
                mergedInfo.BasePckScriptSVCVersion = baseInfo.BasePckScriptSVCVersion;
                mergedInfo.HotfixScriptSVCVersion = baseInfo.HotfixScriptSVCVersion;
                mergedInfo.AssetBundleSVCVersion = baseInfo.AssetBundleSVCVersion;
                mergedInfo.TableSVCVersion = baseInfo.TableSVCVersion;
            }

            ApplyFileServerPackageBuildInfo(mergedInfo, codeInfo);
            ApplyFileServerPackageBuildInfo(mergedInfo, assetBundleInfo);
            ApplyFileServerPackageBuildInfo(mergedInfo, tableInfo);

            return mergedInfo;
        }

        /// <summary>
        /// 规范化单个组件携带的 <c>package_build.info</c>。
        /// 当组件根目录里的显式元数据仍保留旧默认值 <c>none</c> 时，这里用当前组件目录版本号兜底，
        /// 避免文件服务器协议明明已经通过 <c>global_version.info</c> 和目录名确定了构建号，最终却把本地三段版本重新写回成 <c>none</c>。
        /// </summary>
        internal static ClientPackageBuildInfo NormalizeFileServerComponentPackageBuildInfo(
            AssetsVersionController.FileServerComponentKind componentKind,
            string componentVersion,
            ClientPackageBuildInfo sourceInfo)
        {
            var normalizedInfo = CloneFileServerPackageBuildInfo(sourceInfo);
            if (string.IsNullOrEmpty(componentVersion))
            {
                return normalizedInfo;
            }

            switch (componentKind)
            {
                case AssetsVersionController.FileServerComponentKind.Code:
                    if (IsMissingFileServerComponentVersion(normalizedInfo.HotfixScriptSVCVersion))
                    {
                        normalizedInfo.HotfixScriptSVCVersion = componentVersion;
                    }

                    break;
                case AssetsVersionController.FileServerComponentKind.AssetBundle:
                    if (IsMissingFileServerComponentVersion(normalizedInfo.AssetBundleSVCVersion))
                    {
                        normalizedInfo.AssetBundleSVCVersion = componentVersion;
                    }

                    break;
                case AssetsVersionController.FileServerComponentKind.Table:
                    if (IsMissingFileServerComponentVersion(normalizedInfo.TableSVCVersion))
                    {
                        normalizedInfo.TableSVCVersion = componentVersion;
                    }

                    break;
            }

            return normalizedInfo;
        }

        /// <summary>
        /// 根据分包配置，从 Code / AssetBundle / Table 三类资源中筛出本次子包真正需要下载的资源。
        /// </summary>
        internal static List<AssetItem> BuildFileServerSubPackageAssetItems(
            SubPackageConfigItem subPackageConfig,
            List<AssetItem> assetBundleAssets,
            List<AssetItem> codeAssets,
            List<AssetItem> tableAssets)
        {
            var selectedAssets = new List<AssetItem>();
            if (subPackageConfig == null)
            {
                return selectedAssets;
            }

            if (assetBundleAssets != null)
            {
                foreach (var artAssetId in subPackageConfig.ArtAssetsIdList)
                {
                    var found = assetBundleAssets.FirstOrDefault(item => item.Id == artAssetId);
                    if (found != null)
                    {
                        selectedAssets.Add(found);
                    }
                }
            }

            if (codeAssets != null)
            {
                foreach (var hotfixPath in subPackageConfig.HotfixCodePathList)
                {
                    var found = codeAssets.FirstOrDefault(item => item.LocalPath == hotfixPath);
                    if (found != null)
                    {
                        selectedAssets.Add(found);
                    }
                }
            }

            if (tableAssets != null)
            {
                foreach (var tablePath in subPackageConfig.TablePathList)
                {
                    var found = tableAssets.FirstOrDefault(item => item.LocalPath == tablePath);
                    if (found != null)
                    {
                        selectedAssets.Add(found);
                    }
                }
            }

            foreach (var confName in subPackageConfig.ConfAndInfoList)
            {
                var found = assetBundleAssets?.FirstOrDefault(item => item.LocalPath == confName)
                            ?? codeAssets?.FirstOrDefault(item => item.LocalPath == confName)
                            ?? tableAssets?.FirstOrDefault(item => item.LocalPath == confName);
                if (found != null)
                {
                    selectedAssets.Add(found);
                }
            }

            return selectedAssets.Distinct().OrderBy(item => item.Id).ToList();
        }

        /// <summary>
        /// 校验 TeamCity 当前链路期望的共享三段版本号是否与远端 <c>clientRes_{platform}/version.info</c> 一致。
        /// </summary>
        internal static string ValidateExpectedFileServerVersionInfo(
            AssetsVersionController.FileServerVersionInfo expectedVersionInfo,
            AssetsVersionController.FileServerVersionInfo actualVersionInfo)
        {
            if (expectedVersionInfo == null || !expectedVersionInfo.HasAnyVersion)
            {
                return null;
            }

            if (actualVersionInfo == null || !actualVersionInfo.HasAnyVersion)
            {
                return "文件服务器共享版控为空，无法校验 TeamCity 期望版本号。";
            }

            if (!string.IsNullOrEmpty(expectedVersionInfo.CodeVersion)
                && !string.Equals(expectedVersionInfo.CodeVersion, actualVersionInfo.CodeVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"文件服务器热更代码版控不匹配 expected={expectedVersionInfo.CodeVersion} actual={actualVersionInfo.CodeVersion}";
            }

            if (!string.IsNullOrEmpty(expectedVersionInfo.AssetBundleVersion)
                && !string.Equals(expectedVersionInfo.AssetBundleVersion, actualVersionInfo.AssetBundleVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"文件服务器 AssetBundle 版控不匹配 expected={expectedVersionInfo.AssetBundleVersion} actual={actualVersionInfo.AssetBundleVersion}";
            }

            if (!string.IsNullOrEmpty(expectedVersionInfo.TableVersion)
                && !string.Equals(expectedVersionInfo.TableVersion, actualVersionInfo.TableVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"文件服务器表格版控不匹配 expected={expectedVersionInfo.TableVersion} actual={actualVersionInfo.TableVersion}";
            }

            return null;
        }

        /// <summary>
        /// 校验下载完成后本地重建的 <c>package_build.info</c> 是否已经回写为当前 TeamCity 链路期望的三段版本号。
        /// </summary>
        internal static string ValidateFileServerPackageBuildInfo(
            ClientPackageBuildInfo packageBuildInfo,
            AssetsVersionController.FileServerVersionInfo expectedVersionInfo)
        {
            if (expectedVersionInfo == null || !expectedVersionInfo.HasAnyVersion)
            {
                return null;
            }

            if (packageBuildInfo == null)
            {
                return "本地 package_build.info 为空，无法校验 TeamCity 期望版本号。";
            }

            if (!string.IsNullOrEmpty(expectedVersionInfo.CodeVersion)
                && !string.Equals(packageBuildInfo.HotfixScriptSVCVersion, expectedVersionInfo.CodeVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"本地 package_build.info 热更代码版本不匹配 expected={expectedVersionInfo.CodeVersion} actual={packageBuildInfo.HotfixScriptSVCVersion}";
            }

            if (!string.IsNullOrEmpty(expectedVersionInfo.AssetBundleVersion)
                && !string.Equals(packageBuildInfo.AssetBundleSVCVersion, expectedVersionInfo.AssetBundleVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"本地 package_build.info AssetBundle 版本不匹配 expected={expectedVersionInfo.AssetBundleVersion} actual={packageBuildInfo.AssetBundleSVCVersion}";
            }

            if (!string.IsNullOrEmpty(expectedVersionInfo.TableVersion)
                && !string.Equals(packageBuildInfo.TableSVCVersion, expectedVersionInfo.TableVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"本地 package_build.info 表格版本不匹配 expected={expectedVersionInfo.TableVersion} actual={packageBuildInfo.TableSVCVersion}";
            }

            return null;
        }

        /// <summary>
        /// 从单个组件的资源清单里挑出最能代表“这类资源真的可用”的验证样本。
        /// Code 优先挑脚本热更 payload，AssetBundle 优先挑真实 art_assets bundle，Table 优先挑 local.db。
        /// </summary>
        internal static AssetItem FindFileServerRepresentativeAsset(
            AssetsVersionController.FileServerComponentKind componentKind,
            List<AssetItem> assetItems)
        {
            var candidates = assetItems ?? new List<AssetItem>();
            switch (componentKind)
            {
                case AssetsVersionController.FileServerComponentKind.Code:
                    return candidates.FirstOrDefault(item =>
                               item != null
                               && item.LocalPath.StartsWith("script/", StringComparison.OrdinalIgnoreCase))
                           ?? candidates.FirstOrDefault(item =>
                               item != null
                               && item.LocalPath.EndsWith(".dll.bytes", StringComparison.OrdinalIgnoreCase))
                           ?? candidates.FirstOrDefault(item => item != null);
                case AssetsVersionController.FileServerComponentKind.AssetBundle:
                    return candidates.FirstOrDefault(item =>
                               item != null
                               && item.LocalPath.StartsWith(ArtAssetRootPath + "/",
                                   StringComparison.OrdinalIgnoreCase)
                               && !item.LocalPath.EndsWith(".info", StringComparison.OrdinalIgnoreCase)
                               && !item.LocalPath.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
                           ?? candidates.FirstOrDefault(item =>
                               item != null
                               && !item.LocalPath.EndsWith(".info", StringComparison.OrdinalIgnoreCase)
                               && !item.LocalPath.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
                           ?? candidates.FirstOrDefault(item => item != null);
                case AssetsVersionController.FileServerComponentKind.Table:
                    return candidates.FirstOrDefault(item =>
                               item != null
                               && string.Equals(item.LocalPath, LocalDbPath,
                                   StringComparison.OrdinalIgnoreCase))
                           ?? candidates.FirstOrDefault(item => item != null);
                default:
                    return candidates.FirstOrDefault(item => item != null);
            }
        }

        /// <summary>
        /// 从 <c>art_assets.info</c> 的解析结果里提取需要做本地打开校验的 AssetBundle 相对路径。
        /// 这里按非空 <c>AssetBundlePath</c> 去重，避免同一个 bundle 因多个资源记录被重复 <c>LoadFromFile</c>。
        /// </summary>
        internal static List<string> CollectFileServerAssetBundleValidationRelativePaths(
            IEnumerable<AssetBundleItem> assetBundleItems)
        {
            var validationRelativePaths = new List<string>();
            var seenRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assetBundleItem in assetBundleItems ?? Enumerable.Empty<AssetBundleItem>())
            {
                if (assetBundleItem == null || string.IsNullOrWhiteSpace(assetBundleItem.AssetBundlePath))
                {
                    continue;
                }

                var relativePath = (ArtAssetRootPath + "/" + assetBundleItem.AssetBundlePath.Trim())
                    .Replace("\\", "/");
                if (seenRelativePaths.Add(relativePath))
                {
                    validationRelativePaths.Add(relativePath);
                }
            }

            return validationRelativePaths;
        }

        /// <summary>
        /// 把单个组件提供的 package_build.info 字段合并到共享结构。
        /// </summary>
        private static void ApplyFileServerPackageBuildInfo(
            ClientPackageBuildInfo mergedInfo,
            ClientPackageBuildInfo sourceInfo)
        {
            if (mergedInfo == null || sourceInfo == null)
            {
                return;
            }

            if (sourceInfo.BuildTime > mergedInfo.BuildTime)
            {
                mergedInfo.BuildTime = sourceInfo.BuildTime;
            }

            if (string.IsNullOrEmpty(mergedInfo.Version) && !string.IsNullOrEmpty(sourceInfo.Version))
            {
                mergedInfo.Version = sourceInfo.Version;
            }

            if (!string.IsNullOrEmpty(sourceInfo.BasePckScriptSVCVersion)
                && !string.Equals(sourceInfo.BasePckScriptSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.BasePckScriptSVCVersion = sourceInfo.BasePckScriptSVCVersion;
            }

            if (!string.IsNullOrEmpty(sourceInfo.HotfixScriptSVCVersion)
                && !string.Equals(sourceInfo.HotfixScriptSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.HotfixScriptSVCVersion = sourceInfo.HotfixScriptSVCVersion;
            }

            if (!string.IsNullOrEmpty(sourceInfo.AssetBundleSVCVersion)
                && !string.Equals(sourceInfo.AssetBundleSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.AssetBundleSVCVersion = sourceInfo.AssetBundleSVCVersion;
            }

            if (!string.IsNullOrEmpty(sourceInfo.TableSVCVersion)
                && !string.Equals(sourceInfo.TableSVCVersion, "none", StringComparison.OrdinalIgnoreCase))
            {
                mergedInfo.TableSVCVersion = sourceInfo.TableSVCVersion;
            }
        }

        /// <summary>
        /// 复制一份可安全修改的 <see cref="ClientPackageBuildInfo"/>，避免兜底逻辑直接污染调用方保存的原始对象。
        /// </summary>
        private static ClientPackageBuildInfo CloneFileServerPackageBuildInfo(ClientPackageBuildInfo sourceInfo)
        {
            if (sourceInfo == null)
            {
                return new ClientPackageBuildInfo();
            }

            return new ClientPackageBuildInfo()
            {
                BuildTime = sourceInfo.BuildTime,
                Version = sourceInfo.Version,
                BasePckScriptSVCVersion = sourceInfo.BasePckScriptSVCVersion,
                HotfixScriptSVCVersion = sourceInfo.HotfixScriptSVCVersion,
                AssetBundleSVCVersion = sourceInfo.AssetBundleSVCVersion,
                TableSVCVersion = sourceInfo.TableSVCVersion,
            };
        }

        /// <summary>
        /// 判断组件版本字段是否仍然是空值或旧默认值 <c>none</c>。
        /// </summary>
        private static bool IsMissingFileServerComponentVersion(string version)
        {
            return string.IsNullOrEmpty(version)
                   || string.Equals(version, "none", StringComparison.OrdinalIgnoreCase);
        }
    }
}