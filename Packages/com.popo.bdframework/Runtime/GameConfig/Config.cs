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
        /// 热更
        /// </summary>
        Hotfix = 1,
    }

    /// <summary>
    /// 热更代码执行模式
    /// </summary>
    public enum HotfixCodeRunMode
    {
        /// <summary>
        /// 华佗执行
        /// </summary>
        HyCLR=1,
        Mono64,
    }

    /// <summary>
    /// 游戏进入的config
    /// </summary>
    public class Config : MonoBehaviour
    {



        
    }
}
