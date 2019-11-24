using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Build.Content;
using BDFramework.Editor.Asset;
using BDFramework.Editor.BuildPackage;
using Code.BDFramework.Editor;

namespace BDFramework.Editor
{
    static public class BuildPipeline_CI
    {

        [MenuItem("BDFrameWork工具箱/打包/导出APK(已有资源)")]
        public static void GenEmptyApk()
        {
            BuildPipeline_CI.BuildPackage_APK();
        }
        [MenuItem("BDFrameWork工具箱/打包/导出APK(RebuildAsset)")]
        public static void GenApk_RebuildAsset()
        {

            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildPipeline_CI.AndBuildPackage_APK_RebuildAssets();
            }
        }
        
        [MenuItem("BDFrameWork工具箱/打包/导出XCode工程(ipa暂未实现)")]
        public static void GenIpa()
        {
            BuildPipeline_CI.BuildALLAssetsAndBuildInPackage_iOS_Editor();
        }
        
        
        
        #region Android
        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void AndBuildPackage_APK_RebuildAssets()
        {
            //开始项目一键打包
            EditorWindow_OnekeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.Android,Application.streamingAssetsPath);
            BuildPackage_APK();
        }
        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildPackage_APK()
        {
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

            var outpath =  EditorUtility.OpenFolderPanel("选择导出文件夹", Application.dataPath, "");
            if (string.IsNullOrEmpty(outpath))
            {
                return;
            }
            var outputPath = IPath.Combine( outpath ,  Application.productName+".apk");
            File.Delete(outputPath);
            Debug.Log(outputPath);
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
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.Android,BuildOptions.None);
            //
            EditorUtility.DisplayDialog("提示", "打包完成", "OK");
        }
        #endregion
        
        #region iOS
        

        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildALLAssetsAndBuildInPackage_iOS_Editor()
        {
            var outputPath = Application.streamingAssetsPath + "/" + DateTime.Now.ToShortTimeString() + ".apk";
            //清空StreamingAsset
            var and = IPath.Combine(Application.streamingAssetsPath, "Android");
            if (Directory.Exists(and))
            {
                Directory.Delete(and, true);
            }

            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win, true);
            }

            //开始项目一键打包
            EditorWindow_OnekeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.IPhonePlayer,
                Application.streamingAssetsPath);
            UnityEditor.BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.iOS,
                BuildOptions.None);

            //TODO  Build 完之后，需要调用shell 编译xcode
        }

        #endregion
    }
}