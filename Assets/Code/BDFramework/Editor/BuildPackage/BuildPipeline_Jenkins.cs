using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Build.Content;

namespace BDFramework.Editor
{
    static public class BuildPipeline_Jenkins
    {
        private static string[] _args;

        static void _setPass()
        {
            var pass = _getArg(0);
            PlayerSettings.keyaliasPass = pass;
            PlayerSettings.keystorePass = pass;
        }

        static string _getOutPath()
        {
            return  _getArg(_args.Length - 1);
        }

        static void _initArgs()
        {
            _args = Environment.GetCommandLineArgs();
        }
        
        static string _getArg(int index)
        {
            return _args[index];
        }

        #region Android

         /// <summary>
        /// build apk成为一个空包.
        ///  需要cmd传参
        /// </summary>
        static public void BuildEmpty_APK()
        {
            _initArgs();
            _setPass();
            var outputPath = _getOutPath();
            UnityEditor.BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.Android, BuildOptions.None);
        }

        /// <summary>
        /// 只生成资源,
        /// 需要cmd传参
        /// </summary>
        static public void BuildALLAssets_APK()
        {
            _initArgs();
            _setPass();
            var outputPath = _getOutPath();
            EditorWindow_OnkeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.Android, outputPath);
        }
        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildALLAssetsAndBuildInPackage_APK()
        {
            _initArgs();

            _setPass();
            
            var outputPath = _getOutPath();
            
            //清空StreamingAsset
            var ios = IPath.Combine(Application.streamingAssetsPath, "iOS");
            if (Directory.Exists(ios))
            {
                Directory.Delete(ios,true);
            }
            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win,true);
            }
            //开始项目一键打包
            EditorWindow_OnkeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.Android, Application.streamingAssetsPath);
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.Android, BuildOptions.None);
        }

        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildALLAssetsAndBuildInPackage_APK_Editor()
        {

            PlayerSettings.keyaliasPass = "123456";
            PlayerSettings.keystorePass = "123456";
            var outputPath = Application.streamingAssetsPath +"/"+ DateTime.Now.ToShortTimeString() + ".apk";
            //清空StreamingAsset
            var ios = IPath.Combine(Application.streamingAssetsPath, "iOS");
            if (Directory.Exists(ios))
            {
                Directory.Delete(ios,true);
            }
            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win,true);
            }
            //开始项目一键打包
            EditorWindow_OnkeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.Android, Application.streamingAssetsPath);
            UnityEditor.BuildPipeline.BuildPlayer( EditorBuildSettings.scenes, outputPath, BuildTarget.Android, BuildOptions.None);
        }

        #endregion



        #region iOS

        
         /// <summary>
        /// build apk成为一个空包.
        ///  需要cmd传参
        /// </summary>
        static public void BuildEmpty_iOS()
        {
            _initArgs();
            _setPass();
            var outputPath = _getOutPath();
            UnityEditor.BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.iOS, BuildOptions.None);
        }

        /// <summary>
        /// 只生成资源,
        /// 需要cmd传参
        /// </summary>
        static public void BuildALLAssets_iOS()
        {
            _initArgs();
            var outputPath = _getOutPath();
            EditorWindow_OnkeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.IPhonePlayer, outputPath);
        }
        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildALLAssetsAndBuildInPackage_iOS()
        {
            _initArgs();
            var outputPath = _getOutPath();
            
            //清空StreamingAsset
            var ios = IPath.Combine(Application.streamingAssetsPath, "Android");
            if (Directory.Exists(ios))
            {
                Directory.Delete(ios,true);
            }
            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win,true);
            }
            //开始项目一键打包
            EditorWindow_OnkeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.IPhonePlayer, Application.streamingAssetsPath);
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.iOS, BuildOptions.None);
        }

        /// <summary>
        /// build apk,Assetbunld 位于Streaming下~
        /// </summary>
        static public void BuildALLAssetsAndBuildInPackage_iOS_Editor()
        {
            
            var outputPath = Application.streamingAssetsPath +"/"+ DateTime.Now.ToShortTimeString() + ".apk";
            //清空StreamingAsset
            var and = IPath.Combine(Application.streamingAssetsPath, "Android");
            if (Directory.Exists(and))
            {
                Directory.Delete(and,true);
            }
            var win = IPath.Combine(Application.streamingAssetsPath, "Windows");
            if (Directory.Exists(win))
            {
                Directory.Delete(win,true);
            }
            //开始项目一键打包
            EditorWindow_OnkeyBuildAsset.OneKeyBuildALLAssets_ForBuildPackage(RuntimePlatform.IPhonePlayer, Application.streamingAssetsPath);
            UnityEditor.BuildPipeline.BuildPlayer( EditorBuildSettings.scenes, outputPath, BuildTarget.iOS, BuildOptions.None);
            
            //TODO  Build 完之后，需要调用shell 编译xcode
        }

        #endregion



    }
}