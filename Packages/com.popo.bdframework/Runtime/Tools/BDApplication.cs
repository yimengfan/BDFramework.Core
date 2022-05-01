using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BDFramework.Core.Tools
{
    /// <summary>
    /// 职责类似Unity的Application
    /// </summary>
    static public class BDApplication
    {
        #region 路径相关

        static BDApplication()
        {
            if (Application.isEditor && Application.isPlaying)
            {
                Init();
            }
        }

        #region Unity路径缓存防止各种异步访问

        /// <summary>
        /// Persistent
        /// </summary>
       static public string persistentDataPath { get;private set; }
       /// <summary>
       /// Streaming
       /// </summary>
       static public string streamingAssetsPath { get;private set; }

        #endregion
        
        
        #region 自定义路径

        /// <summary>
        /// 项目根目录
        /// </summary>
        static public string ProjectRoot { get; private set; }

        /// <summary>
        /// BD Assets目录以外的workspace目录
        /// </summary>
        static public string BDWorkSpace { get; private set; }

        /// <summary>
        /// Library
        /// </summary>
        static public string Library { get; private set; }

        /// <summary>
        /// Package目录
        /// </summary>
        static public string Package { get; private set; }

        /// <summary>
        /// 资源的根目录
        /// </summary>
        static public string RuntimeResourceLoadPath { get; private set; }

        /// <summary>
        /// Editor的资源路径
        /// </summary>
        public static string EditorResourcePath { get; private set; }

        /// <summary>
        /// Editor的资源路径
        /// </summary>
        public static string EditorResourceRuntimePath { get; private set; }

        /// <summary>
        /// Devops 路径
        /// </summary>
        public static string DevOpsPath { get; private set; }

        /// <summary>
        /// Devops 代码路径
        /// </summary>
        public static string DevOpsCodePath { get; private set; }
        /// <summary>
        /// Devops 资源路径
        /// </summary>
        public static string DevOpsPublishAssetsPath { get; private set; }
        /// <summary>
        /// 发布包体路径
        /// </summary>
        public static string DevOpsPublishPackagePath { get; private set; }
        /// <summary>
        /// Devops 配置文件路径
        /// </summary>
        public static string DevOpsConfigPath { get; private set; }

        /// <summary>
        /// Devops CI路径
        /// </summary>
        public static string DevOpsCIPath { get; private set; }
        
        /// <summary>
        /// 编辑器缓存目录
        /// </summary>
        public static string BDEditorCachePath { get; private set; }
#endregion
        static public void Init()
        {
            //Unity路径
            persistentDataPath = Application.persistentDataPath;
            streamingAssetsPath = Application.streamingAssetsPath;
            //自定义路径
            ProjectRoot             = Application.dataPath.Replace("/Assets", "");
            Library                 = ProjectRoot + "/Library";
            Package                 = ProjectRoot + "/Package";
            BDWorkSpace             = ProjectRoot + "/BDWorkSpace";
            RuntimeResourceLoadPath = "Assets/Resource/Runtime";
            //Editor相关目录
            EditorResourcePath        = "Assets/Resource_SVN";
            EditorResourceRuntimePath = EditorResourcePath + "/Runtime";
            BDEditorCachePath         = Library            + "/BDFrameCache";
            //DevOps路径
            DevOpsPath = ProjectRoot + "/DevOps";
            DevOpsPublishAssetsPath  = DevOpsPath + "/PublishAssets";
            DevOpsPublishPackagePath =  DevOpsPath + "/PublishPackages";
            DevOpsConfigPath = DevOpsPath + "/Config";
            DevOpsCIPath = DevOpsPath + "/CI";
        }

        /// <summary>
        /// 获取所有runtime的目录
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllRuntimeDirects()
        {
            //搜索所有资源
            var root = Application.dataPath;
            //获取根路径所有runtime
            var          directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly).ToList();
            
            //ret
            List<string> retList     = new List<string>();
            foreach (var dirt in directories)
            {
                //
                var _dirt = dirt + "/Runtime";
                if (Directory.Exists(_dirt))
                {
                    _dirt = _dirt.Replace("\\","/").Replace(Application.dataPath,"Assets");
                    retList.Add(_dirt);
                }
            }
            return retList;
        }

        /// <summary>
        /// 获取所有资源
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllRuntimeAssetsPath()
        {
            List<string> allAssetsList = new List<string>();
            var          directories   = GetAllRuntimeDirects();
            //所有资源列表
            foreach (var dir in directories)
            {
                var rets = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Where((s) => !s.EndsWith(".meta"));
                allAssetsList.AddRange(rets);
            }


            for (int i = 0; i < allAssetsList.Count; i++)
            {
                var res = allAssetsList[i];
                allAssetsList[i] = res.Replace("\\", "/");
            }

            return allAssetsList;
        }


        /// <summary>
        /// 平台资源的父路径
        /// </summary>
        public static string GetPlatformPath(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                // return "Windows";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
            }

            return "";
        }

        /// <summary>
        /// 获取支持的平台
        /// </summary>
        /// <returns></returns>
        public static RuntimePlatform[] GetSupportPlatform()
        {

            return new RuntimePlatform[] {RuntimePlatform.Android, RuntimePlatform.IPhonePlayer};
        }


#if  UNITY_EDITOR

        #region BuildTarget 和RuntimePlatform互转

        /// <summary>
        /// 通过buildTarget获取platform
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static string GetPlatformPath(BuildTarget buildTarget)
        {
            var platform = GetRuntimePlatform(buildTarget);
            return GetPlatformPath(platform);
        }
        
        
        /// <summary>
        /// 获取AB构建平台
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static BuildTarget GetBuildTarget(RuntimePlatform platform)
        {
            //构建平台
            BuildTarget target = BuildTarget.Android;
            switch (platform)
            {
                case RuntimePlatform.Android:
                    target = BuildTarget.Android;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    target = BuildTarget.iOS;
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                {
                    target = BuildTarget.StandaloneWindows64;
                }
                    break;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                {
                    target = BuildTarget.StandaloneOSX;
                }
                    break;
            }

            return target;
        }

        /// <summary>
        /// 获取runtimeplatform
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static RuntimePlatform GetRuntimePlatform(BuildTarget buildTarget)
        {
            var platform = RuntimePlatform.Android;
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    platform = RuntimePlatform.Android;
                }
                    break;
                case BuildTarget.iOS:
                {
                    platform = RuntimePlatform.IPhonePlayer;
                }
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                {
                    platform = RuntimePlatform.WindowsPlayer;
                }
                    break;
                case BuildTarget.StandaloneOSX:
                {
                    platform = RuntimePlatform.OSXPlayer;
                }
                    break;
            }

            return platform;
        }

        #endregion
#endif

        #endregion
    }
}