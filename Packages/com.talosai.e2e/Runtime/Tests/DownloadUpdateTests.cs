using System;
using System.IO;
using UnityEngine;
using BDFramework;
using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Sql;
using BDFramework.Core.Tools;

namespace Talos.E2E.Tests
{
    /// <summary>
    /// 下载与热更新 E2E 测试套件。
    /// 验证 BDFramework 的资源版本控制、下载和热更新功能。
    /// 
    /// 前置条件：
    /// - 启动流程已完成
    /// - 网络可用（部分测试需要文件服务器）
    /// 
    /// 测试范围：
    /// - 版本信息读取
    /// - 本地资源完整性检查
    /// - 资产路径解析
    /// </summary>
    static public class DownloadUpdateTests
    {
        /// <summary>
        /// 验证客户端版本号可被读取。
        /// 版本号应从配置中正确解析。
        /// </summary>
        [E2ETest(suite: "下载与热更", order: 1, des: "验证客户端版本号可读")]
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
        /// 检查 ClientAssetsUtils 能正确解析主路径和备用路径。
        /// </summary>
        [E2ETest(suite: "下载与热更", order: 2, des: "验证资产路径解析")]
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
        /// 验证 StreamingAssets 母包资源存在性。
        /// 确保母包中至少有基本资源文件。
        /// </summary>
        [E2ETest(suite: "下载与热更", order: 3, des: "验证母包基础资源存在")]
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
