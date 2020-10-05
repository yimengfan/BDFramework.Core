using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Tools;
using Code.BDFramework.Core.Tools;

namespace BDFramework.Editor.Asset
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

        //private bool isSelectWindows = false;
        private bool isSelectAndroid = true;

        //
        void DrawToolsBar()
        {
            GUILayout.Label("平台选择:");
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(30);
                isSelectAndroid = GUILayout.Toggle(isSelectAndroid, "生成Android资源(Windows共用)");
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

        private BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        private string AES = "";

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
            options = (BuildAssetBundleOptions) EditorGUILayout.EnumPopup("压缩格式:", options);
            AES = EditorGUILayout.TextField("AES密钥(V2 only):", AES);
        }


        private string exportPath = "";

        /// <summary>
        /// 最新包
        /// </summary>
        void LastestGUI()
        {
            GUILayout.BeginVertical();

            // if (GUILayout.Button("检测资源", GUILayout.Width(100)))
            // {
            //     exportPath = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath, "");
            //
            //
            //     if (string.IsNullOrEmpty(exportPath))
            //     {
            //         return;
            //     }
            //
            //     RuntimePlatform rp = RuntimePlatform.Lumin;
            //     BuildTarget bt = BuildTarget.Lumin;
            //
            //
            //     if (isSelectAndroid)
            //     {
            //         AssetBundleEditorTools.CheckAssets(rootResourceDir,
            //             exportPath + "/" +
            //             BApplication.GetPlatformPath(RuntimePlatform.Android),
            //             BuildTarget.Android);
            //     }
            //
            //     if (isSelectIOS)
            //     {
            //         AssetBundleEditorTools.CheckAssets(rootResourceDir,
            //             exportPath + "/" +
            //             BApplication.GetPlatformPath(RuntimePlatform.IPhonePlayer),
            //             BuildTarget.iOS);
            //     }
            //
            //
            //     AssetDatabase.Refresh();
            //     Debug.Log("资源打包完毕");
            // }

            if (GUILayout.Button("收集Shader keyword", GUILayout.Width(200)))
            {
                ShaderCollection.GenShaderVariant();
            }

            if (GUILayout.Button("一键打包[美术资源]", GUILayout.Width(380), GUILayout.Height(30)))
            {
                exportPath = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath, "");
                if (string.IsNullOrEmpty(exportPath))
                {
                    return;
                }

                //搜集keyword
                ShaderCollection.GenShaderVariant();
                //开始打包
                BuildAsset();
            }

            if (GUILayout.Button("AssetBundle还原目录", GUILayout.Width(380), GUILayout.Height(30)))
            {
                exportPath = EditorUtility.OpenFolderPanel("选择资源目录", Application.dataPath, "");
                if (string.IsNullOrEmpty(exportPath))
                {
                    return;
                }

                AssetBundleEditorTools.HashName2AssetName(exportPath);
            }

            GUILayout.EndVertical();
        }


        /// <summary>
        /// 打包资源
        /// </summary>
        public void BuildAsset()
        {
            RuntimePlatform platform = RuntimePlatform.Android;
            BuildTarget buildTarget = BuildTarget.Android;
 
            if (isSelectAndroid)
            {
                platform = RuntimePlatform.Android;
                buildTarget = BuildTarget.Android;
            }
            else if (isSelectIOS)
            {
                platform = RuntimePlatform.IPhonePlayer;
                buildTarget = BuildTarget.iOS;
            }


            var outPath = exportPath + "/" + BDApplication.GetPlatformPath(platform);
            var config = GameObject.Find("BDFrame").GetComponent<Config>();
            //根据版本进入不同打包模式
            if (config.Data.AssetBundleManagerVersion == AssetBundleManagerVersion.V1)
            {
                AssetBundleEditorTools.GenAssetBundle(outPath, buildTarget, options);
            }
            else if (config.Data.AssetBundleManagerVersion == AssetBundleManagerVersion.V2_experiment)
            {
                AssetBundleEditorToolsV2.GenAssetBundle(outPath, buildTarget, options,AES:AES);
            }


            AssetDatabase.Refresh();
            Debug.Log("资源打包完毕");
        }
    }
}