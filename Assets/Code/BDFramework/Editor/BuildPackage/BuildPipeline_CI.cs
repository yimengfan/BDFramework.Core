using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Build.Content;
using BDFramework.Editor.Asset;
using BDFramework.Editor.BuildPackage;
using BDFramework.Editor.EditorLife;
using Code.BDFramework.Core.Tools;
using Code.BDFramework.Editor;

namespace BDFramework.Editor
{
    static public class BuildPipeline_CI
    {

        public enum BuildMode
        {
            AndroidDebug = 0,
            AndroidRelease,
            IosDebug,
            IosRelease,
        }

        static private string[] Scenes =
        {
            "Assets/Scenes/BuildAndroidDebug.unity",
            "Assets/Scenes/BuildAndroidRelease.unity",
            "Assets/Scenes/BuildIosDebug.unity",
            "Assets/Scenes/BuildIosRelease.unity",
        };

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(空)")]
        public static void GenAPKEmpty()
        {
            BuildAPK_Empty();
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(Debug)")]
        public static void GenAPKDebug()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildAPK_Debug();
            }
          
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(Release)")]
        public static void GenPAK()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildAPK_Release();
            }
        }



        [MenuItem("BDFrameWork工具箱/打包/导出XCode工程(ipa暂未实现)")]
        public static void GenIpa()
        {
            BuildPipeline_CI.BuildIpa();
        }



        /// <summary>
        /// 初始化BDFrame
        /// </summary>
        public static void InitBDFrame()
        {
            BDEditorLife.BDEditorInit();
        }


        #region Android

        static public void BuildAPK_Empty()
        {
            BuildAPK(Scenes[(int) BuildMode.AndroidDebug]);
        }

        
        static public void BuildAPK_Debug()
        {
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath,RuntimePlatform.Android, BuildTarget.Android);
            BuildAPK(Scenes[(int) BuildMode.AndroidDebug]);
        }
        
        static public void BuildAPK_Release()
        {
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath,RuntimePlatform.Android, BuildTarget.Android);
            BuildAPK(Scenes[(int) BuildMode.AndroidRelease]);
        }

        
        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildAPK(string scene)
        {

            InitBDFrame();
            
            if (!BDEditorHelper.EditorConfig.IsSetConfig())
            {
                BDebug.LogError("请注意设置apk keystore账号密码");
                return;
            }

            var absroot = Application.dataPath.Replace("Assets", "");
            PlayerSettings.Android.keystoreName =absroot  + BDEditorHelper.EditorConfig.Android.keystoreName;
            PlayerSettings.keystorePass =  BDEditorHelper.EditorConfig.Android.keystorePass;
            PlayerSettings.Android.keyaliasName= BDEditorHelper.EditorConfig.Android.keyaliasName;
            PlayerSettings.keyaliasPass =  BDEditorHelper.EditorConfig.Android.keyaliasPass;
            //
            var outdir = BApplication.projroot + "/Build";
            var outputPath = IPath.Combine(  outdir,  Application.productName+".apk");
            //文件夹处理
            if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);
            if(File.Exists(outputPath)) File.Delete(outputPath);
            //清空StreamingAsset
            var ios = IPath.Combine(Application.streamingAssetsPath, "iOS");
            if (Directory.Exists(ios))
            {
                Directory.Delete(ios, true);
            }
            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win)) 
            {
                Directory.Delete(win, true);
            }
            //开始项目一键打包
            string[] scenes = { scene };
            UnityEditor.BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android,BuildOptions.None);
            if (File.Exists(outputPath))
            {
                Debug.Log( "Build Success :" + outputPath);
            }
            else
            {

                Debug.LogException(new Exception("Build Fail! Please Check the log! "));
            }
        }
        #endregion
        
        #region iOS
        

        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildIpa()
        {
           
        }

        #endregion
    }
}