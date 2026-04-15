using System.Collections.Generic;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using UnityEditor;

namespace BDFramework.Editor.DevOps
{
    /// <summary>
    /// PublishPipeLineCI 的 BatchMode 请求桥接扩展。
    /// BatchMode request bridge extensions for PublishPipeLineCI.
    /// 该 partial 负责把 BuildTarget 与显式参数列表收敛成运行时文件服务器验证请求。
    /// This partial converts BuildTarget and explicit argument lists into runtime file-server verification requests.
    /// </summary>
    public static partial class PublishPipeLineCI
    {
        /// <summary>
        /// 为指定 BuildTarget 构造文件服务器 BatchMode 验证请求。
        /// Create a file-server BatchMode verification request for a specific BuildTarget.
        /// 该 helper 只负责平台映射与参数桥接，不执行真实下载，方便单元测试锁定 TeamCity 到 Unity 的路由契约。
        /// This helper only maps platform and arguments and does not execute downloads, which keeps the TeamCity-to-Unity routing contract unit-testable.
        /// </summary>
        public static AssetsVersionController.FileServerBatchVerificationRequest CreateVerifyClientResRequestForBatchMode(
            BuildTarget buildTarget,
            IReadOnlyList<string> args,
            bool resetLocalStateBeforeVerify = true)
        {
            var request = AssetsVersionController.CreateFileServerBatchVerificationRequestFromArgs(args,
                resetLocalStateBeforeVerify);
            request.TargetPlatform = BApplication.GetRuntimePlatform(buildTarget);
            return request;
        }
    }
}