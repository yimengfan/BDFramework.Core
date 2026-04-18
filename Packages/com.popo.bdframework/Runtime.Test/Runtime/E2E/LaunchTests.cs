using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BDFramework;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using Talos.E2E;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 框架启动流程 E2E 测试套件。
    /// Framework startup E2E test suite.
    /// 验证 BDFramework 从 DLL 加载到完全初始化的完整启动流程，并为 Android IL2CPP 反射发现保活 launch 套件。
    /// Verifies the end-to-end BDFramework startup path from DLL loading to full initialisation, and preserves the launch suite for Android IL2CPP reflection discovery.
    /// </summary>
    [Preserve]
    static public class LaunchTests
    {
        /// <summary>
        /// 验证热更 DLL 加载状态。
        /// Verify hotfix DLL loading state.
        /// Player 构建模式要求 ScriptLoder.IsRunning 为 true；Editor batchmode 直接编译热更代码，因此跳过运行态检查。
        /// Player builds require ScriptLoder.IsRunning to be true; Editor batchmode compiles hotfix code directly, so the runtime-only check is skipped there.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 1, des: "验证热更 DLL 加载完成")]
        static public void HotfixDllLoaded()
        {
            if (Application.isEditor)
            {
                // Editor batchmode 下热更代码直接由 Editor 编译，不通过 LoadHotfix()。
                // In Editor batchmode hotfix code is compiled directly by the Editor instead of going through LoadHotfix().
                Debug.Log("[E2E] Editor 模式：热更代码已由 Editor 直接编译，跳过 IsRunning 检查");
            }
            else
            {
                // Player 构建模式下，热更 DLL 必须通过 LoadHotfix() 加载。
                // In player builds the hotfix DLL must be loaded through LoadHotfix().
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
        /// Verify that hosted types from the hotfix assembly can be enumerated.
        /// Player 构建模式要求宿主类型枚举非空；Editor batchmode 中热更类型已直接并入 Editor 编译，因此跳过该检查。
        /// Player builds require a non-empty hosted-type enumeration; Editor batchmode already merges hotfix types into the Editor compilation, so the check is skipped there.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "launch", order: 2, des: "验证热更类型可被枚举")]
        static public void HotfixTypesDiscovered()
        {
            if (Application.isEditor)
            {
                // Editor batchmode 下热更类型直接在 Editor 程序集中。
                // In Editor batchmode the hotfix types live directly in the Editor-compiled assemblies.
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
        /// Verify that the BApplication runtime flag has been set.
        /// </summary>
        [Preserve]
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
        /// Verify that the framework version is readable.
        /// </summary>
        [Preserve]
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
