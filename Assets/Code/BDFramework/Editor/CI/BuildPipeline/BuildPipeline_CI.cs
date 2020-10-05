using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using BDFramework.Editor.BuildPackage;
using BDFramework.Editor.EditorLife;
using Code.BDFramework.Core.Tools;
using Code.BDFramework.Editor;
using UnityEditor.SceneManagement;

namespace BDFramework.Editor
{
    static public class BuildPipeline_CI
    {

        public enum BuildMode
        {
            Debug = 0,
            Release,
        }
        static string SCENEPATH="Assets/Scenes/BDFrame.unity";
        static string[] SceneConfigs =
        {
            "Assets/Scenes/Config/Debug.json",
            "Assets/Scenes/Config/Release.json",
        };

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(使用当前配置 )")]
        public static void GenAPKEmpty()
        {
            LoadConfig();
            BuildAPK_Empty();
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(Debug-StreamingAsset)")]
        public static void GenAPKDebug()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildAPK_Debug();
            }
          
        }

        [MenuItem("BDFrameWork工具箱/打包/BuildAPK(Release-Persistent)")]
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
            BuildIpa();
        }


        
       
        static void LoadConfig(BuildMode? mode=null)
        {
            
            var  scene=  EditorSceneManager.OpenScene(SCENEPATH);
            TextAsset textContent = null;
            if (mode != null)
            {
                string path = SceneConfigs[(int)mode];
                textContent  = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var config = GameObject.Find("BDFrame").GetComponent<BDLauncher>();
                config.ConfigContent = textContent;
            }
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// 初始化BDFrame
        /// </summary>
        public static void InitBDFrame()
        {
            BDFrameEditorLife.InitBDEditorLife();
        }


        #region Android

        static public void BuildAPK_Empty()
        {
            LoadConfig();
            BuildAPK();
        }

        
        static public void BuildAPK_Debug()
        {
            LoadConfig(BuildMode.Debug);
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath,RuntimePlatform.Android, BuildTarget.Android);
            BuildAPK();
        }
        
        static public void BuildAPK_Release()
        {
            LoadConfig(BuildMode.Release);
            EditorWindow_OnekeyBuildAsset.GenAllAssets(Application.streamingAssetsPath,RuntimePlatform.Android, BuildTarget.Android);
            BuildAPK();
        }

        
        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildAPK()
        {

            InitBDFrame();
            
            if (!BDFrameEditorConfigHelper.EditorConfig.IsSetConfig())
            {
                BDebug.LogError("请注意设置apk keystore账号密码");
                return;
            }

            var absroot = Application.dataPath.Replace("Assets", "");
            PlayerSettings.Android.keystoreName =absroot  + BDFrameEditorConfigHelper.EditorConfig.Android.keystoreName;
            PlayerSettings.keystorePass =  BDFrameEditorConfigHelper.EditorConfig.Android.keystorePass;
            PlayerSettings.Android.keyaliasName= BDFrameEditorConfigHelper.EditorConfig.Android.keyaliasName;
            PlayerSettings.keyaliasPass =  BDFrameEditorConfigHelper.EditorConfig.Android.keyaliasPass;
            //
            var outdir = BDApplication.ProjectRoot + "/Build";
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
            string[] scenes = { SCENEPATH };
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