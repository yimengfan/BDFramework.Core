using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.TableData;
using BDFramework.Editor.Asset;
using Code.BDFramework.Core.Tools;

namespace BDFramework.Editor.BuildPackage
{
    public class EditorWindow_OnekeyBuildAsset : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/资源一键打包", false, (int) BDEditorMenuEnum.OnekeyBuildAsset)]
        public static void Open()
        {
            var window =
                (EditorWindow_OnekeyBuildAsset) EditorWindow.GetWindow(typeof(EditorWindow_OnekeyBuildAsset), false,
                    "一键打包");
            window.Show();
        }


        private EditorWindow_Table editorTable;

        private EditorWindow_ScriptBuildDll editorScript;
        private EditorWindow_GenAssetBundle editorAsset;

        public void Show()
        {
            this.editorTable = new EditorWindow_Table();
            this.editorAsset = new EditorWindow_GenAssetBundle();
            this.editorScript = new EditorWindow_ScriptBuildDll();

            this.minSize = this.maxSize = new Vector2(1050, 600);
            base.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                if (editorScript != null)
                {
                    GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                    editorScript.OnGUI();
                    GUILayout.EndVertical();
                }

                Layout_DrawLineV(Color.white);

                if (editorAsset != null)
                {
                    GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                    editorAsset.OnGUI();
                    GUILayout.EndVertical();
                }

                Layout_DrawLineV(Color.white);
                if (editorTable != null)
                {
                    GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                    editorTable.OnGUI();
                    GUILayout.EndVertical();
                    Layout_DrawLineV(Color.white);
                }
            }
            GUILayout.EndHorizontal();

            Layout_DrawLineH(Color.white);
            OnGUI_OneKeyExprot();
        }


        public string exportPath = "";
        private bool isGenIosAssets = false;
        private bool isGenAndroidAssets = true;

        private bool isBuilding = false;

        public void OnGUI_OneKeyExprot()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("注:上面按钮操作,会默认生成到StreamingAssets", GUILayout.Width(500), GUILayout.Height(30));
                //isGenWindowsAssets=GUILayout.Toggle(isGenWindowsAssets, "生成Windows资源");
                isGenAndroidAssets = GUILayout.Toggle(isGenAndroidAssets, "生成Android资源(Windows共用)");
                isGenIosAssets = GUILayout.Toggle(isGenIosAssets, "生成Ios资源");

                //
                GUILayout.Label("导出地址:" + exportPath, GUILayout.Width(500));
                //
                if (GUILayout.Button("一键导出", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    if (isBuilding)
                        return;
                    isBuilding = true;

                    //选择目录
                    exportPath = EditorUtility.OpenFolderPanel("选择导出目录", exportPath, "");
                    if (string.IsNullOrEmpty(exportPath))
                    {
                        return;
                    }


                    //生成android资源
                    if (isGenAndroidAssets)
                    {
                        GenAllAssets(exportPath, RuntimePlatform.Android, BuildTarget.Android);
                    }

                    //生成ios资源
                    if (isGenIosAssets)
                    {
                        GenAllAssets(exportPath, RuntimePlatform.IPhonePlayer, BuildTarget.iOS);
                    }

                    EditorUtility.DisplayDialog("提示", "资源导出完成", "OK");

                    isBuilding = false;
                }

                //
                if (GUILayout.Button("热更资源转hash", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    //选择目录
                    exportPath = EditorUtility.OpenFolderPanel("选择导出目录", exportPath, "");
                    if (string.IsNullOrEmpty(exportPath))
                    {
                        return;
                    }

                    //自动转hash
                    AssetUploadToServer.Assets2Hash(exportPath, "");
                }

                if (GUILayout.Button("上传到文件服务器[内网测试]", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    //先不实现,等待使用者实现
                }
            }
            GUILayout.EndVertical();
        }


        /// <summary>
        /// 生成所有资源
        /// </summary>
        /// <param name="exportPath"></param>
        /// <param name="platform"></param>
        /// <param name="target"></param>
        static public void GenAllAssets(string exportPath, RuntimePlatform platform, BuildTarget target)
        {
            var outPath = exportPath + "/" + BApplication.GetPlatformPath(platform);
            if (Directory.Exists(outPath))
            {
                Directory.Delete(outPath, true);
            }

            //1.打包资源
            try
            {
                //搜集keywork
                ShaderCollection.GenShaderVariant();


                var config = GameObject.Find("BDFrame").GetComponent<Config>();
                //根据版本进入不同打包模式
                if (config.Data.AssetBundleManagerVersion == AssetBundleManagerVersion.V1)
                {
                    AssetBundleEditorTools.GenAssetBundle(outPath, target);
                }
                else if (config.Data.AssetBundleManagerVersion == AssetBundleManagerVersion.V2_experiment)
                {
                    AssetBundleEditorToolsV2.GenAssetBundle(outPath, target);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return;
            }

            //2.编译脚本
            try
            {
                EditorWindow_ScriptBuildDll.RoslynBuild(ScriptBuildTools.BuildMode.Release, outPath);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return;
            }

            //3.打包表格
            try
            {
                Excel2SQLiteTools.GenExcel2SQLite(exportPath, platform);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }


        public static void Layout_DrawLineH(Color color, float height = 4f)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(height);
        }

        public static void Layout_DrawLineV(Color color, float width = 4f)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, width, rect.height), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(width);
        }
    }
}