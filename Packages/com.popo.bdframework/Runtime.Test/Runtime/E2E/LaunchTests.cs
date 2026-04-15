using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BDFramework;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架启动流程 E2E 测试套件。
    /// 验证 BDFramework 从 DLL 加载到完全初始化的完整启动流程。
    /// 
    /// 测试范围：
    /// - 热更 DLL 加载状态验证
    /// - 框架基础配置读取
    /// - 资源系统初始化
    /// - SQLite 数据库初始化
    /// - 管理器系统启动
    /// 
    /// 注意：
    /// - Editor batchmode 下热更 DLL 不通过 HotfixAssembliesHelper.LoadHotfix() 加载
    ///   而是直接由 Editor 编译，因此 IsRunning=false、GetHotfixTypes() 返回空。
    /// - 这两个测试在 Editor 下验证"跳过热更模式可正常运行"。
    /// </summary>
    static public class LaunchTests
    {
        /// <summary>
        /// 验证热更 DLL 加载状态。
        /// Player 构建模式：HotfixAssembliesHelper.IsRunning 必须为 true。
        /// Editor batchmode 模式：热更代码已由 Editor 直接编译，跳过此检查。
        /// </summary>
        [E2ETest(suite: "launch", order: 1, des: "验证热更 DLL 加载完成")]
        static public void HotfixDllLoaded()
        {
            if (Application.isEditor)
            {
                // Editor batchmode 下热更代码直接由 Editor 编译，不通过 LoadHotfix()
                Debug.Log("[E2E] Editor 模式：热更代码已由 Editor 直接编译，跳过 IsRunning 检查");
            }
            else
            {
                // Player 构建模式下，热更 DLL 必须通过 LoadHotfix() 加载
                var isRunning = ScriptLoder.IsRunning;
                if (!isRunning)
                {
                    throw new Exception("热更 DLL 未加载: HotfixAssembliesHelper.IsRunning = false");
                }
                Debug.Log("[E2E] 热更 DLL 已成功加载（Player 模式）");
            }
        }

        /// <summary>
        /// 验证热更程序集中的类型可被枚举。
        /// Player 构建模式：GetHotfixTypes() 应返回非空列表。
        /// Editor batchmode 模式：跳过此检查，热更类型已包含在 Editor 编译中。
        /// </summary>
        [E2ETest(suite: "launch", order: 2, des: "验证热更类型可被枚举")]
        static public void HotfixTypesDiscovered()
        {
            if (Application.isEditor)
            {
                // Editor batchmode 下热更类型直接在 Editor 程序集中
                Debug.Log("[E2E] Editor 模式：热更类型已包含在 Editor 编译中，跳过 GetHotfixTypes 检查");
            }
            else
            {
                var types = ScriptLoder.GetAppDomainHostingTypes();
                
                if (types == null || types.Count() == 0)
                {
                    throw new Exception($"热更类型列表为空: count={types?.Count() ?? 0}");
                }
                Debug.Log($"[E2E] 发现 {types.Count()} 个热更类型（Player 模式）");
            }
        }

        /// <summary>
        /// 验证 BApplication 运行时标记已设置。
        /// </summary>
        [E2ETest(suite: "launch", order: 3, des: "验证运行时标记已设置")]
        static public void BApplicationIsPlaying()
        {
            if (!BApplication.IsPlaying)
            {
                throw new Exception("BApplication.IsPlaying 未设置为 true");
            }
            Debug.Log("[E2E] BApplication.IsPlaying = true");
        }

        /// <summary>
        /// 验证框架版本号可被读取。
        /// </summary>
        [E2ETest(suite: "launch", order: 4, des: "验证框架版本号可读")]
        static public void FrameworkVersionReadable()
        {
            var version = BDLauncher.FrameworkVersion;
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception("框架版本号为空");
            }
            Debug.Log($"[E2E] 框架版本: {version}");
        }
    }
}
