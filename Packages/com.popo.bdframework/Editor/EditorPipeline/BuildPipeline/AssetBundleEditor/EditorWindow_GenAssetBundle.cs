using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Tools;
using BDFramework.Core.Tools;
using BDFramework.Editor;
using BDFramework.Editor.PublishPipeline;

namespace BDFramework.Editor.AssetBundle
{
    public class EditorWindow_GenAssetBundle : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/2.AssetBundle打包", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Assetbundle)]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_PublishAssets>(false, "发布资源");
            window.Show();
            window.Focus();
        }
        
        private bool isSelectIOS = false;

        private bool isSelectAndroid = true;


        //
        void DrawToolsBar()
        {
            GUILayout.Space(5);
            GUILayout.Label("平台选择:");
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(30);
                isSelectAndroid = GUILayout.Toggle(isSelectAndroid, "生成Android资源(Windows资源 Editor环境下共用)");
            }
            GUILayout.EndHorizontal();
            //
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(30);
                isSelectIOS = GUILayout.Toggle(isSelectIOS, "生成iOS资源");
            }
            GUILayout.EndHorizontal();
            //
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(220));
            {
                TipsGUI();
                DrawToolsBar();
                GUILayout.Space(10);
                LastestGUI();
                GUILayout.Space(75);
            }
            GUILayout.EndVertical();
        }


        /// <summary>
        /// 提示UI
        /// </summary>
        void TipsGUI()
        {
            GUILayout.Label("2.资源打包", EditorGUIHelper.LabelH3);
            GUILayout.Space(5);
            GUILayout.Label("Runtime目录:");
            foreach (var root in BDApplication.GetAllRuntimeDirects())
            {
                GUILayout.Label(root);
            }

            GUILayout.Space(3);
            GUILayout.Label("AssetBundle输出目录:");
            GUILayout.Label(BDApplication.DevOpsPublishAssetsPath);
            //var assetConfig = BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildAssetBundle;
            //assetConfig.AESCode = EditorGUILayout.TextField("AES密钥(V2 only):", assetConfig.AESCode);
            //assetConfig.IsUseHashName = EditorGUILayout.Toggle("hash命名:", assetConfig.IsUseHashName);
        }


        private void OnDestroy()
        {
            //保存
            BDEditorApplication.BDFrameWorkFrameEditorSetting?.Save();
        }




        /// <summary>
        /// 最新包
        /// </summary>
        void LastestGUI()
        {
            GUILayout.BeginVertical();


            if (GUILayout.Button("收集Keyword[Shader Feature]", GUILayout.Width(200)))
            {
                ShaderCollection.CollectShaderVariant();
            }

            if (GUILayout.Button("一键打包[美术资源]", GUILayout.Width(380), GUILayout.Height(30)))
            {
                //开始打包
                BuildAssetBundle(BDApplication.DevOpsPublishAssetsPath);
            }
            
            GUILayout.Label("测试:");
            if (GUILayout.Button("AssetBundle SG打包(DevOps)", GUILayout.Width(380), GUILayout.Height(30)))
            {
                var outputpath = BDApplication.DevOpsPublishAssetsPath;
                // outputpath2 = Application.streamingAssetsPath;
                //删除目录里面资源
                // if (Directory.Exists(outputpath))
                // {
                //     Directory.Delete(outputpath,true);
                // }
                //打包AB
                AssetBundleEditorToolsV2ForAssetGraph.Build(BuildTarget.Android, outputpath);
            }

            // if (GUILayout.Button("AssetBundle 加载测试Editor(DevOps)", GUILayout.Width(380), GUILayout.Height(30)))
            // {
            //     var outputpath = BDApplication.DevOpsPublishAssetsPath;
            //     //outputpath = Application.streamingAssetsPath;
            //     AssetBundleEditorToolsV2CheckAssetbundle.TestLoadAssetbundleOnEditor(outputpath);
            // }
            if (GUILayout.Button("AssetBundle 加载测试Scene(DevOps)", GUILayout.Width(380), GUILayout.Height(30)))
            {
                AssetBundleEditorToolsV2CheckAssetbundle.TestLoadAssetbundleRuntime();
            }

            GUILayout.EndVertical();
        }


        /// <summary>
        /// 打包资源
        /// </summary>
        public void BuildAssetBundle(string outputPath)
        {
            //搜集keyword
            ShaderCollection.CollectShaderVariant();
            //打包
            RuntimePlatform platform = RuntimePlatform.Android;
            if (isSelectAndroid)
            {
                platform = RuntimePlatform.Android;
            }
            else if (isSelectIOS)
            {
                platform = RuntimePlatform.IPhonePlayer;
            }

            var assetConfig = BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildAssetBundle;
            //生成Assetbundlebunle
            AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform);
            AssetDatabase.Refresh();
            Debug.Log("资源打包完毕");
        }
    }
}
