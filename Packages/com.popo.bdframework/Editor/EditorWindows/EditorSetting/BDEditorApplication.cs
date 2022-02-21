using System.IO;
using System.Net.Mime;
using BDFramework.Core.Tools;
using JetBrains.Annotations;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 编辑器下application的帮助
    /// </summary>
    static public class BDEditorApplication
    {
        /// <summary>
        /// 编辑器设置
        /// </summary>
        static public BDFrameWorkEditorSetting BDFrameWorkFrameEditorSetting { get; private set; }
        

        /// <summary>
        /// Editor工作状态
        /// </summary>
        static public BDFrameworkEditorStatus EditorStatus { get; set; }
        
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            BDFrameWorkFrameEditorSetting = BDFrameWorkEditorSetting.Load();
        }


       
        /// <summary>
        /// 获取最近修改的热更代码
        /// </summary>
        static public string[] GetLeastHotfixCodes()
        {
            return BDFrameworkAssetImporter.CacheData?.HotfixList.ToArray();
        }
        
        /// <summary>
        /// 平台资源的父路径
        /// </summary>
        public static string GetPlatformPath(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Android";
                case BuildTarget.iOS:
                case BuildTarget.StandaloneOSX:
                    return "iOS";
            }

            return "";
        }
    }
}