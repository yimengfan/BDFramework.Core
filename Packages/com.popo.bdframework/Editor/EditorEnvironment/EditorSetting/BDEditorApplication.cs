using System;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using BDFramework.Core.Tools;
using BDFramework.Editor.Environment;
using BDFramework.Editor.Unity3dEx;
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
        /// 这个只用于编辑器本身的设置，不要做任何业务
        /// </summary>
        static public BDFrameworkEditorSetting EditorSetting { get; private set; }
        
        /// <summary>
        /// Editor工作状态
        /// </summary>
        static public BDFrameworkEditorStatus EditorStatus { get; set; }
        
        
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            EditorSetting = BDFrameworkEditorSetting.Load();
        }


        /// <summary>
        /// 获取最近修改的热更代码
        /// </summary>
        // static public string[] GetLeastHotfixCodes()
        // {
        //     return BDFrameworkAssetImporter.CacheData?.HotfixList.ToArray();
        // }
        //

        #region 平台切换

        /// <summary>
        /// 切换到指定目标平台
        /// </summary>
        static public bool SwitchToBuildTarget(BuildTarget buildTarget)
        {
            var buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (buildTargetGroup == BuildTargetGroup.Unknown)
            {
                throw new Exception("未知的构建目标组:" + buildTarget);
            }

            return EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        }

        /// <summary>
        /// 切换到安卓
        /// </summary>
        static public void SwitchToAndroid()
        {
            SwitchToBuildTarget(BuildTarget.Android);
        }
        
        /// <summary>
        /// 切换到iOS
        /// </summary>
        static public void SwitchToiOS()
        {
            SwitchToBuildTarget(BuildTarget.iOS);
        }

        
        /// <summary>
        /// 切换到安卓
        /// </summary>
        static public void SwitchToWindows()
        {
            SwitchToBuildTarget(BuildTarget.StandaloneWindows64);
        }
        
        /// <summary>
        /// 切换到iOS
        /// </summary>
        static public void SwitchToMacOSX()
        {
            SwitchToBuildTarget(BuildTarget.StandaloneOSX);
        }
        
        delegate bool IsModuleNotInstalled_type(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget);
        static private IsModuleNotInstalled_type IsModuleNotInstalledType_Impl;
        /// <summary>
        /// 是否安装平台
        /// </summary>
        /// <param name="buildTargetGroup"></param>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        static  public bool IsPlatformModuleInstalled(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            if (IsModuleNotInstalledType_Impl == null)
            {
                var getwindows = EditorWindow.GetWindow<BuildPlayerWindow>();
                if (getwindows != null)
                {
                    try
                    {
                        getwindows.Close();
                    }
                    catch (Exception e)
                    {
                    }

                    var method = typeof(BuildPlayerWindow).GetMethod("IsModuleNotInstalled" , BindingFlags.NonPublic | BindingFlags.Instance);
                    IsModuleNotInstalledType_Impl = Delegate.CreateDelegate(typeof(IsModuleNotInstalled_type), getwindows,method) as IsModuleNotInstalled_type;
                }

            }

            return !IsModuleNotInstalledType_Impl(buildTargetGroup, buildTarget);
        }
        #endregion
    }
}
