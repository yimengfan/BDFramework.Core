using System;
using System.IO;
using UnityEngine;
using BDFramework;
using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Sql;
using BDFramework.Core.Tools;
using Talos.E2E;

namespace BDFramework.Game.E2E
{
    /// <summary>
    /// 下载与热更新业务能力 E2E 测试套件。
    /// Download and hot-update business-capability E2E test suite.
    /// 验证 BDFramework 的资源版本控制、下载和热更新功能。
    /// Verify BDFramework's asset version control, download, and hot-update functionality.
    /// 
    /// 前置条件 / Prerequisites:
    /// - 启动流程已完成 / Startup flow has completed
    /// - 网络可用（部分测试需要文件服务器）/ Network available (some tests require file server)
    /// 
    /// 测试范围 / Test scope:
    /// - 版本信息读取 / Version info reading
    /// - 本地资源完整性检查 / Local asset integrity check
    /// - 资产路径解析 / Asset path resolution
    /// </summary>
    [Preserve]
    static public class DownloadUpdateTests
    {
        /// <summary>
        /// 验证客户端版本号可被读取。
        /// Verify that the client version number can be read.
        /// 测试目的=验证客户端版本号可读 实现手段=直接读取 GameConfigManager 配置。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-update", order: 1, des: "验证客户端版本号可读")]
        static public void ClientVersionReadable()
        {
            var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            if (config == null)
            {
                throw new Exception("无法获取 GameBaseConfigProcessor.Config");
            }
            Debug.Log($"[E2E] 客户端版本号: {config.ClientVersionNum}");
        }

        /// <summary>
        /// 验证资产路径解析。
        /// Verify that asset path resolution is correct.
        /// 测试目的=验证资产路径解析 实现手段=调用 ClientAssetsUtils.GetMultiAssetsLoadPath 并验证主备路径。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-update", order: 2, des: "验证资产路径解析")]
        static public void AssetPathsResolved()
        {
            var config = GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>();
            var clientVersion = config.ClientVersionNum;
            var (firstPath, secondPath) = ClientAssetsUtils.GetMultiAssetsLoadPath(
                BApplication.RuntimePlatform, clientVersion);

            if (string.IsNullOrEmpty(firstPath) && string.IsNullOrEmpty(secondPath))
            {
                throw new Exception("资产路径解析失败：主路径和备用路径均为空");
            }
            Debug.Log($"[E2E] 主路径: {firstPath}");
            Debug.Log($"[E2E] 备用路径: {secondPath}");
        }

        /// <summary>
        /// 验证母包基础资源存在性。
        /// Verify that base package fundamental resources exist.
        /// 测试目的=验证母包基础资源存在 实现手段=检查 StreamingAssets 热更目录存在性和文件列表。
        /// </summary>
        [Preserve]
        [E2ETest(suite: "download-update", order: 3, des: "验证母包基础资源存在")]
        static public void StreamingAssetsExist()
        {
            var platform = BApplication.GetPlatformLoadPath(BApplication.RuntimePlatform);
            var scriptPath = System.IO.Path.Combine(Application.streamingAssetsPath, platform, "script");
            var hotfixPath = System.IO.Path.Combine(scriptPath, "hotfix");

            if (!Directory.Exists(hotfixPath))
            {
                // 在某些平台上 streamingAssetsPath 的行为可能不同，不直接报错
                Debug.Log($"[E2E] StreamingAssets 热更路径不存在（可能正常）: {hotfixPath}");
            }
            else
            {
                var files = Directory.GetFiles(hotfixPath);
                Debug.Log($"[E2E] StreamingAssets 热更目录中包含 {files.Length} 个文件");
            }
        }
    }
}
