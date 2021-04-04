using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BDFramework.Core.Tools
{
    /// <summary>
    /// 职责类似Unity的Application
    /// </summary>
    static public class BDApplication
    {
        #region 路径相关

        static  BDApplication()
        {

            if (Application.isEditor && Application.isPlaying)
            {
                Init();
            }
        }

        /// <summary>
        /// 项目根目录
        /// </summary>
        static public string ProjectRoot { get; private set; }
        /// <summary>
        /// Library
        /// </summary>
        static public string Library { get; private set; }
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
        /// 编辑器缓存目录
        /// </summary>
        public static string BDEditorCachePath { get; private set; }
                
        static public void Init()
        {
            ProjectRoot = Application.dataPath.Replace("/Assets", "");
            Library = ProjectRoot + "/Library";
            RuntimeResourceLoadPath = "Assets/Resource/Runtime";
            EditorResourcePath = "Assets/Resource_SVN";
            EditorResourceRuntimePath = EditorResourcePath + "/Runtime";

            BDEditorCachePath = Library + "/BDFrameCache";
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
            var directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly).ToList();
            for (int i = directories.Count - 1; i >= 0; i--)
            {
                var dir = directories[i].Replace(BDApplication.ProjectRoot + "/", "").Replace("\\", "/") + "/Runtime";
                if (!Directory.Exists(dir))
                {
                    directories.RemoveAt(i);
                }
                else
                {
                    directories[i] = dir;
                }
            }

            return directories;
        }
        
        /// <summary>
        /// 获取所有资源
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllAssetsPath()
        {
            List<string> allAssetsList = new List<string>();
            var          directories   = GetAllRuntimeDirects();
            //所有资源列表
            foreach (var dir in directories)
            {
                var rets = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where((s) => !s.EndsWith(".meta"));
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
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                   // return "Windows";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
            }

            return "";
        }
        #endregion
    }
}