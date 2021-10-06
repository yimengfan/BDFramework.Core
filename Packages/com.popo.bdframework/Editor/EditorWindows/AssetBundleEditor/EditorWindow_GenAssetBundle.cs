using System;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Tools;
using BDFramework.Core.Tools;
using BDFramework.Editor;

namespace BDFramework.Editor.AssetBundle
{
    public class EditorWindow_GenAssetBundle : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/2.AssetBundle打包", false, (int) BDEditorMenuEnum.BuildPackage_Assetbundle)]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_GenAssetBundle>(false, "AB打包工具");
            window.Show();
        }

        /// <summary>
        /// 资源下面根节点
        /// </summary>
        public string rootResourceDir = "Resource/Runtime/";

        private bool isSelectIOS = false;

        private bool isSelectAndroid = true;


        //
        void DrawToolsBar()
        {
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
            TipsGUI();
            DrawToolsBar();
            GUILayout.Space(10);
            LastestGUI();
            GUILayout.Space(75);
            GUILayout.EndVertical();
        }


        /// <summary>
        /// 提示UI
        /// </summary>
        void TipsGUI()
        {
            GUILayout.Label("2.资源打包", EditorGUIHelper.TitleStyle);
            GUILayout.Space(5);
            GUILayout.Label("资源根目录:");
            foreach (var root in BDApplication.GetAllRuntimeDirects())
            {
                GUILayout.Label(root);
            }

            GUILayout.Label(string.Format("AB输出目录:{0}", exportPath));

            var assetConfig = BDEditorApplication.BdFrameEditorSetting.BuildAssetBundle;
            //assetConfig.AESCode = EditorGUILayout.TextField("AES密钥(V2 only):", assetConfig.AESCode);
            assetConfig.IsUseHashName = EditorGUILayout.Toggle("hash命名:", assetConfig.IsUseHashName);
        }


        private void OnDestroy()
        {
            //保存
            BDEditorApplication.BdFrameEditorSetting?.Save();
        }


        private string exportPath = "";

        /// <summary>
        /// 最新包
        /// </summary>
        void LastestGUI()
        {
            GUILayout.BeginVertical();


            if (GUILayout.Button("简单收集Shader keyword[无光照]", GUILayout.Width(200)))
            {
                ShaderCollection.SimpleGenShaderVariant();
            }

            if (GUILayout.Button("一键打包[美术资源]", GUILayout.Width(380), GUILayout.Height(30)))
            {
                exportPath = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath, "");
                if (string.IsNullOrEmpty(exportPath))
                {
                    return;
                }

                //搜集keyword
                ShaderCollection.SimpleGenShaderVariant();
                //开始打包
                BuildAsset();
            }

            if (GUILayout.Button("AssetBundle SG打包测试", GUILayout.Width(380), GUILayout.Height(30)))
            {
                var outputpath = BDApplication.ProjectRoot + "/CI_TEMP";
                AssetBundleEditorToolsV2ForAssetGraph.Build(BuildTarget.Android, outputpath, true);
            }

            if (GUILayout.Button("AssetBundle 加载测试", GUILayout.Width(380), GUILayout.Height(30)))
            {
                var outputpath = BDApplication.ProjectRoot + "/CI_TEMP";
                var outputpath2 = Application.streamingAssetsPath;
                AssetBundleEditorToolsV2.TestLoadAllAssetbundle(outputpath2);
            }

            GUILayout.EndVertical();
        }


        /// <summary>
        /// 打包资源
        /// </summary>
        public void BuildAsset()
        {
            RuntimePlatform platform = RuntimePlatform.Android;
            if (isSelectAndroid)
            {
                platform = RuntimePlatform.Android;
            }
            else if (isSelectIOS)
            {
                platform = RuntimePlatform.IPhonePlayer;
            }

            var assetConfig = BDEditorApplication.BdFrameEditorSetting.BuildAssetBundle;
            //生成Assetbundlebunle
            AssetBundleEditorToolsV2.GenAssetBundle(exportPath, platform, assetConfig.IsUseHashName);
            AssetDatabase.Refresh();
            Debug.Log("资源打包完毕");
        }
    }
}
