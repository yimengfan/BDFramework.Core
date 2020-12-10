using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.TableData;
using BDFramework.Editor.Asset;
using Code.BDFramework.Core.Tools;
using Code.BDFramework.Editor;
using Sirenix.Utilities.Editor;

namespace BDFramework.Editor.BuildPackage
{
    public class EditorWindow_OnekeyBuildAsset : EditorWindow
    {
        [MenuItem("BDFrameWork工具箱/资源一键打包", false, (int) BDEditorMenuEnum.OnekeyBuildAsset)]
        public static void Open()
        {
            var window = (EditorWindow_OnekeyBuildAsset) EditorWindow.GetWindow(typeof(EditorWindow_OnekeyBuildAsset), false, "一键打包");
            window.Show();
        }


        private EditorWindow_Table editorTable;

        private EditorWindow_ScriptBuildDll editorScript;
        private EditorWindow_GenAssetBundle editorAsset;

        public void Show()
        {
            this.editorTable  = new EditorWindow_Table();
            this.editorAsset  = new EditorWindow_GenAssetBundle();
            this.editorScript = new EditorWindow_ScriptBuildDll();

            this.minSize = this.maxSize = new Vector2(1100, 800);
            base.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                if (editorScript != null)
                {
                    //GUILayout.BeginVertical();
                    SirenixEditorGUI.BeginBox("脚本", true, GUILayout.Width(220), GUILayout.Height(450));
                    editorScript.OnGUI();
                    SirenixEditorGUI.EndBox();
                    //GUILayout.EndVertical();
                }

                // Layout_DrawLineV(Color.white);

                if (editorAsset != null)
                {
                    SirenixEditorGUI.BeginBox("资源", true, GUILayout.Width(220), GUILayout.Height(450));
                    editorAsset.OnGUI();
                    SirenixEditorGUI.EndBox();
                }

                // Layout_DrawLineV(Color.white);
                if (editorTable != null)
                {
                    SirenixEditorGUI.BeginBox("表格", true, GUILayout.Width(200), GUILayout.Height(450));
                    editorTable.OnGUI();
                    SirenixEditorGUI.EndBox();
                    //Layout_DrawLineV(Color.white);
                }
            }
            GUILayout.EndHorizontal();

            Layout_DrawLineH(Color.white);
            
            GUILayout.BeginHorizontal();
            OnGUI_OneKeyExprot();
            OnGUI_BuildpipelineCI();
            GUILayout.EndHorizontal();
        }


        private void OnDisable()
        {
            //保存
            BDFrameEditorConfigHelper.EditorConfig.Save();
        }

        public  string exportPath         = "";
        private bool   isGenIosAssets     = false;
        private bool   isGenAndroidAssets = true;

        private bool isBuilding = false;

        /// <summary>
        /// 一键导出
        /// </summary>
        public void OnGUI_OneKeyExprot()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("注:上面按钮操作,会默认生成到StreamingAssets", GUILayout.Width(500), GUILayout.Height(30));
                //isGenWindowsAssets=GUILayout.Toggle(isGenWindowsAssets, "生成Windows资源");
                isGenAndroidAssets = GUILayout.Toggle(isGenAndroidAssets, "生成Android资源(Windows共用)");
                isGenIosAssets     = GUILayout.Toggle(isGenIosAssets, "生成Ios资源");

                //
                GUILayout.Label("导出地址:" + exportPath, GUILayout.Width(500));
                //
                if (GUILayout.Button("一键导出", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    if (isBuilding) return;
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
            }
            GUILayout.EndVertical();
        }


        /// <summary>
        /// CI 相关
        /// </summary>
        public void OnGUI_BuildpipelineCI()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("CI相关测试");
                
                BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl = EditorGUILayout.TextField("文件服务器", BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig.AssetBundleFileServerUrl, GUILayout.Width(350));
        
                //构建资源
                int Width = 100;
                GUILayout.Label("[构建资源]");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("IOS资源", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildAssetBundle_iOS();
                    }

                    if (GUILayout.Button("Android资源", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildAssetBundle_Android();
                    }
                }
                GUILayout.EndHorizontal();
                //构建dll
                GUILayout.Label("[代码检查]");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("DLL", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildDLL();
                    }
                }
                GUILayout.EndHorizontal();
                //构建包体
                GUILayout.Label("[构建包体]");
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("IOS-Release", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildIOSRelease();
                    }

                    if (GUILayout.Button("IOS-Debug", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildIOSDebug();
                    }

                    if (GUILayout.Button("Android-Release", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildAndroidRelease();
                    }

                    if (GUILayout.Button("Android-Debug", GUILayout.Width(Width)))
                    {
                        BuildPipeLine_CI.BuildAndroidDebug();
                    }
                }
            }
            GUILayout.EndVertical();
        }


        /// <summary>
        /// 生成所有资源
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="platform"></param>
        /// <param name="target"></param>
        static public void GenAllAssets(string outputPath, RuntimePlatform platform, BuildTarget target)
        {
            var _outputPath = Path.Combine(outputPath, BDApplication.GetPlatformPath(platform)); //  + "/" + ;
            if (Directory.Exists(_outputPath))
            {
                Directory.Delete(_outputPath, true);
            }

            //1.打包资源
            try
            {
                //1.搜集keywork
                ShaderCollection.GenShaderVariant();
                //2.打包模式
                var config = BDFrameEditorConfigHelper.EditorConfig.BuildAssetConfig;
                AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform, target, BuildAssetBundleOptions.ChunkBasedCompression, config.IsUseHashName, config.AESCode);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return;
            }

            //2.编译脚本
            try
            {
                EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, platform, ScriptBuildTools.BuildMode.Release);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return;
            }

            //3.打包表格
            try
            {
                Excel2SQLiteTools.ALLExcel2SQLite(outputPath, platform);
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