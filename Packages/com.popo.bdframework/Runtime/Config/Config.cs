using System;
using System.IO;
using System.Linq;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using DotNetExtension;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BDFramework
{
    /// <summary>
    /// 资产加载路径
    /// </summary>
    public enum AssetLoadPathType
    {
        Editor = 0,

        /// <summary>
        /// 用户可读写沙盒
        /// </summary>
        Persistent,

        /// <summary>
        /// Streaming
        /// </summary>
        StreamingAsset,

        /// <summary>
        /// devop的发布目录
        /// </summary>
        DevOpsPublish
    }

    /// <summary>
    /// 热更代码执行模式
    /// </summary>
    public enum HotfixCodeRunMode
    {
        /// <summary>
        /// ILRuntime解释执行
        /// </summary>
        ILRuntime = 0,

        /// <summary>
        /// 华佗执行
        /// </summary>
        HCLR,

        /// <summary>
        /// 这里只做预留,因为OSX只支持mono方式
        /// </summary>
        Mono
    }

    /// <summary>
    /// 游戏进入的config
    /// </summary>
    public class Config : MonoBehaviour
    {



        
    }
}
