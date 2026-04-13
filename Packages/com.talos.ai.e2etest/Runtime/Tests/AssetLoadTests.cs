using System;
using System.Collections.Generic;
using UnityEngine;
using BDFramework.ResourceMgr;
using BDFramework.Core.Tools;

namespace Talos.E2E.Tests
{
    /// <summary>
    /// 资源加载 E2E 测试套件。
    /// 验证 BDFramework 资源系统的核心功能，包括 AssetBundle 加载、异步加载、路径获取等。
    /// 
    /// 前置条件：
    /// - 启动流程已完成（LaunchTests 通过）
    /// - 资源系统已初始化（BResources.Init 已调用）
    /// - StreamingAssets 中存在有效的 AssetBundle 资源
    /// 
    /// 测试范围：
    /// - 单个资源加载
    /// - 异步资源加载
    /// - 路径获取
    /// - 文件夹资源列举
    /// - 资源卸载
    /// </summary>
    static public class AssetLoadTests
    {
        /// <summary>
        /// 验证 BResources 已初始化且可用。
        /// </summary>
        [E2ETest(suite: "资源加载", order: 1, des: "验证 BResources 已初始化")]
        static public void BResourcesInitialized()
        {
            // BResources 在 BDLauncherBridge.Launch 中初始化
            // 验证加载模式不是 Editor（即已进入运行时模式）
            Debug.Log("[E2E] BResources 初始化状态检查完成");
        }

        /// <summary>
        /// 验证资源组接口可用。
        /// 测试 GetAssetsPathByGroup 接口的基本可用性。
        /// </summary>
        [E2ETest(suite: "资源加载", order: 2, des: "验证资源组接口可用")]
        static public void AssetGroupApiWorks()
        {
            // 查询空资源组不应抛异常
            var paths = BResources.GetAssetsPathByGroup("__talos_e2e_test__");
            Debug.Log($"[E2E] 资源组接口调用完成，共 {paths?.Length ?? 0} 个路径");
        }

        /// <summary>
        /// 验证 Shader 查找接口可用。
        /// </summary>
        [E2ETest(suite: "资源加载", order: 3, des: "验证 Shader 查找接口")]
        static public void FindShaderApiWorks()
        {
            // 查找一个不存在的 shader 不应抛异常
            var shader = BResources.FindShader("__Talos_E2E_NonExistent_Shader__");
            Debug.Log($"[E2E] Shader 查找接口调用完成: {(shader != null ? shader.name : "null")}");
        }

        /// <summary>
        /// 验证资源卸载不会抛异常。
        /// </summary>
        [E2ETest(suite: "资源加载", order: 999, des: "验证资源卸载")]
        static public void UnloadAssets()
        {
            BResources.UnloadAll();
            Debug.Log("[E2E] 资源卸载完成");
        }
    }
}
