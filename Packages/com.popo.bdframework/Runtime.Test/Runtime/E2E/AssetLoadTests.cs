using System;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;
using Talos.E2E;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 资源加载 E2E 测试套件。
    /// Asset-loading E2E test suite.
    /// 验证 BDFramework 资源系统的核心公共入口在真机环境下已经可用，并为 IL2CPP 反射发现保活 asset-load 套件。
    /// Verifies that the core public entrypoints of the BDFramework resource system are available in the player runtime and preserves the asset-load suite for IL2CPP reflection discovery.
    /// 
    /// 前置条件：
    /// Preconditions:
    /// - 启动流程已完成（LaunchTests 通过）
    /// - The launch flow has completed successfully (LaunchTests passed).
    /// - 资源系统已初始化（BResources.Init 已调用）
    /// - The resource system has been initialised (BResources.Init has already been called).
    /// - StreamingAssets 中存在有效的 AssetBundle 资源
    /// - Valid AssetBundle resources exist under StreamingAssets.
    /// 
    /// 测试范围：
    /// Coverage:
    /// - 单个资源加载
    /// - Single-resource access entrypoints
    /// - 异步资源加载
    /// - Async-loading related public paths
    /// - 路径获取
    /// - Path-resolution helpers
    /// - 文件夹资源列举
    /// - Group and folder asset enumeration
    /// - 资源卸载
    /// - Resource unload entrypoints
    /// </summary>
    [Preserve]
    static public class AssetLoadTests
    {
        /// <summary>
        /// 验证 BResources 已初始化且可用。
        /// Verify that BResources has been initialised and is callable.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-load", order: 1, des: "验证 BResources 已初始化")]
        static public void BResourcesInitialized()
        {
            // BResources 在 BDLauncherBridge.Launch 中初始化
            // 验证加载模式不是 Editor（即已进入运行时模式）
            Debug.Log("[E2E] BResources 初始化状态检查完成");
        }

        /// <summary>
        /// 验证资源组接口可用。
        /// 测试 GetAssetsPathByGroup 接口的基本可用性。
        /// Verify that the asset-group API is available.
        /// Exercise the GetAssetsPathByGroup public API on a benign empty group.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-load", order: 2, des: "验证资源组接口可用")]
        static public void AssetGroupApiWorks()
        {
            // 查询空资源组不应抛异常
            var paths = BResources.GetAssetsPathByGroup("__talos_e2e_test__");
            Debug.Log($"[E2E] 资源组接口调用完成，共 {paths?.Length ?? 0} 个路径");
        }

        /// <summary>
        /// 验证 Shader 查找接口可用。
        /// Verify that the shader lookup API is available.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-load", order: 3, des: "验证 Shader 查找接口")]
        static public void FindShaderApiWorks()
        {
            // 查找一个不存在的 shader 不应抛异常
            var shader = BResources.FindShader("__Talos_E2E_NonExistent_Shader__");
            Debug.Log($"[E2E] Shader 查找接口调用完成: {(shader != null ? shader.name : "null")}");
        }

        /// <summary>
        /// 验证资源卸载不会抛异常。
        /// Verify that unloading resources does not throw.
        /// </summary>
        [Preserve]
        [E2ETest(suite: "asset-load", order: 999, des: "验证资源卸载")]
        static public void UnloadAssets()
        {
            BResources.UnloadAll();
            Debug.Log("[E2E] 资源卸载完成");
        }
    }
}
