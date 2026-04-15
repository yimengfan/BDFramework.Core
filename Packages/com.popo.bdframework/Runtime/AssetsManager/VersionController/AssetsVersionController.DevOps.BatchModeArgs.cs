using System;
using System.Collections.Generic;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 文件服务器 BatchMode 参数桥接扩展。
    /// File-server BatchMode argument bridge extensions.
    /// 该 partial 负责把显式参数列表收敛成验证请求，并复用统一的日志与失败契约。
    /// This partial collects explicit argument lists into verification requests and reuses the unified logging and failure contract.
    /// </summary>
    public partial class AssetsVersionController
    {
        /// <summary>
        /// 按显式参数列表构造文件服务器 BatchMode 验证请求。
        /// Create a file-server BatchMode verification request from an explicit argument list.
        /// 该重载用于测试和外层桥接，避免调用方只能依赖当前进程的全局命令行。
        /// This overload exists for tests and outer bridges so callers do not need to rely on the current process-wide command line.
        /// </summary>
        public static FileServerBatchVerificationRequest CreateFileServerBatchVerificationRequestFromArgs(
            IReadOnlyList<string> args,
            bool resetLocalStateBeforeVerify = true)
        {
            return CreateFileServerBatchVerificationRequest(
                GetBatchModeCommandLineArg(args, FileServerUrlBatchArgName),
                GetBatchModeCommandLineArg(args, ExpectedCodeVersionBatchArgName),
                GetBatchModeCommandLineArg(args, ExpectedAssetbundleVersionBatchArgName),
                GetBatchModeCommandLineArg(args, ExpectedTableVersionBatchArgName),
                resetLocalStateBeforeVerify);
        }

        /// <summary>
        /// 按显式参数列表和目标平台执行文件服务器 BatchMode 验证。
        /// Execute file-server BatchMode verification from an explicit argument list and target platform.
        /// 该入口把请求构造、阶段日志与最终失败契约收敛在同一条正式代码路径里。
        /// This entry keeps request creation, phase logging, and the final failure contract on one production code path.
        /// </summary>
        public static FileServerBatchVerificationResult VerifyFileServerAssetsForBatchMode(
            RuntimePlatform targetPlatform,
            IReadOnlyList<string> args,
            bool resetLocalStateBeforeVerify = true)
        {
            var request = CreateFileServerBatchVerificationRequestFromArgs(args, resetLocalStateBeforeVerify);
            request.TargetPlatform = targetPlatform;
            return VerifyFileServerAssetsForBatchModeWithRequest(request);
        }

        /// <summary>
        /// 按显式请求对象执行文件服务器 BatchMode 验证。
        /// Execute file-server BatchMode verification from an explicit request object.
        /// 该重载供 Editor/CI wrapper 先补齐平台与参数，再复用运行时 owner 的统一下载验证与日志契约。
        /// This overload lets Editor/CI wrappers fill in platform and arguments first, then reuse the runtime owner's unified download verification and logging contract.
        /// </summary>
        public static FileServerBatchVerificationResult VerifyFileServerAssetsForBatchModeWithRequest(
            FileServerBatchVerificationRequest request)
        {
            Debug.Log(
                "[CI][VerifyClientRes] 测试目的=验证文件服务器共享版控会触发真实下载，并按 art_assets.info 资产列表逐项执行本地加载校验。 实现手段=解析远端三段版控、下载差异资源、重建本地元数据，并在主线程逐条校验 Code/AssetBundle/Table 样本。");
            Debug.Log(
                $"[CI][VerifyClientRes] platform={request.TargetPlatform} serverUrl={request.ServerUrl} expectedVersion={request.ExpectedVersionInfo.RawValue}");

            var result = BResources.VerifyFileServerAssetsForBatchModeWithDevOps(request);

            Debug.Log(
                $"[CI][VerifyClientRes] actualVersion={result.ActualVersion} codeAsset={result.CodeAssetLocalPath} assetBundleFirstTarget={result.AssetBundleValidationFirstTarget} assetBundleAssetCount={result.AssetBundleValidationEntries.Count} assetBundleBundleCount={result.AssetBundleAssetLocalPaths.Count} tableAsset={result.TableAssetLocalPath}");
            if (!result.IsSuccess)
            {
                throw new Exception(
                    $"[CI][VerifyClientRes] 文件服务器 BatchMode 验证失败! platform={request.TargetPlatform} expected={result.ExpectedVersion} actual={result.ActualVersion} error={result.Error}");
            }

            return result;
        }

        /// <summary>
        /// 从显式参数列表读取文件服务器 BatchMode 参数。
        /// Read a file-server BatchMode argument from an explicit argument list.
        /// 该 helper 与现有命令行读取语义保持一致：忽略参数名大小写，值缺失时返回空。
        /// This helper preserves the existing command-line semantics: case-insensitive argument matching and null when a value is missing.
        /// </summary>
        internal static string GetBatchModeCommandLineArg(IReadOnlyList<string> args, string argName)
        {
            if (args == null || string.IsNullOrEmpty(argName))
            {
                return null;
            }

            for (var index = 0; index < args.Count - 1; index++)
            {
                if (string.Equals(args[index], argName, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}