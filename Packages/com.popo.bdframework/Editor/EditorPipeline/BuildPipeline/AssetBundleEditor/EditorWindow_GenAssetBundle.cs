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


        public void OnGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(200));
            {
                TipsGUI();
                OnGUI_BuildParams();
                GUILayout.Space(10);
                OnGUI_TestAssetBundle();
                GUILayout.Space(75);
            }
            GUILayout.EndVertical();
        }


        /// <summary>
        /// 提示UI
        /// </summary>
        void TipsGUI()
        {
            GUILayout.Label("2.资源打包", EditorGUIHelper.LabelH2);
            GUILayout.Space(5);
            GUILayout.Label("Runtime目录:");
            foreach (var root in BApplication.GetAllRuntimeDirects())
            {
                GUILayout.Label(root);
            }

            GUILayout.Space(3);
            GUILayout.Label("AssetBundle输出目录:DevOps");
            //
            //assetConfig.AESCode = EditorGUILayout.TextField("AES密钥(V2 only):", assetConfig.AESCode);
            //assetConfig.IsUseHashName = EditorGUILayout.Toggle("hash命名:", assetConfig.IsUseHashName);
        }


        public static GUIContent buildParamsDisableBuildTitle = EditorGUIUtility.TrTextContent("打包参数", "修改打包参数后，需要重新构建，不能增量!");
        public static GUIContent buildParamsDisableTypeTreeLabel = EditorGUIUtility.TrTextContent("关闭TypeTree:", "能减少内存、磁盘占用,部分unity版本加载会有bug!");
        public static GUIContent buildParamsEnableObfuscationLabel = EditorGUIUtility.TrTextContent("是否混淆:", "用小颗粒AB进行混淆,以降低破解概率!");

        /// <summary>
        /// 绘制打包参数
        /// </summary>
        void OnGUI_BuildParams()
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

            //打包参数
            GUILayout.Space(5);
            GUILayout.Label(buildParamsDisableBuildTitle, EditorGUIHelper.LabelH4);
            var buildAssetConf = BDEditorApplication.BDFrameWorkFrameEditorSetting?.BuildAssetBundle;
            if (buildAssetConf != null)
            {
                buildAssetConf.IsDisableTypeTree = EditorGUILayout.Toggle(buildParamsDisableTypeTreeLabel, buildAssetConf.IsDisableTypeTree);
                buildAssetConf.IsEnableObfuscation = EditorGUILayout.Toggle(buildParamsEnableObfuscationLabel, buildAssetConf.IsEnableObfuscation);
            }
        }

        /// <summary>
        /// 最新包
        /// </summary>
        void OnGUI_TestAssetBundle()
        {
            GUILayout.BeginVertical();
            {
                if (GUILayout.Button("收集Keyword[Shader Feature]", GUILayout.Width(200)))
                {
                    ShaderCollection.CollectShaderVariant();
                }

                if (GUILayout.Button("一键打包[美术资源]", GUILayout.Width(380), GUILayout.Height(30)))
                {
                    //开始打包
                    BuildAssetBundle(BApplication.DevOpsPublishAssetsPath);
                }

                GUILayout.Space(10); //();
                GUILayout.Label("资源验证:", EditorGUIHelper.LabelH4);
                //加载ab
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("AssetBundle验证: 加载所有 (DevOps目录)", EditorGUIHelper.GetFontStyle(Color.white, 12));
                    if (GUILayout.Button("Play", GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        AssetBundleEditorToolsV2CheckAssetbundle.TestLoadAssetbundleRuntime();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5); //();
                //加载ab异步
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("AssetBundle验证: 加载所有-Async (DevOps目录)", EditorGUIHelper.GetFontStyle(Color.white, 12));
                    if (GUILayout.Button("Play", GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        AssetBundleEditorToolsV2CheckAssetbundle.TestLoadAssetbundleRuntimeAsync();
                    }
                }
                GUILayout.EndHorizontal();
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

            //生成Assetbundlebunle
            AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform);
            AssetDatabase.Refresh();
            Debug.Log("资源打包完毕");
        }

        private void OnDestroy()
        {
            //保存
            BDEditorApplication.BDFrameWorkFrameEditorSetting?.Save();
        }
    }
}
