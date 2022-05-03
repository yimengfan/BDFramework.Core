using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Table;
using BDFramework.Editor.AssetBundle;
using BDFramework.Core.Tools;
using BDFramework.Editor.EditorPipeline.PublishPipeline;
using BDFramework.Editor.Tools;
using BDFramework.Editor.Unity3dEx;
using BDFramework.ResourceMgr;
using ServiceStack.Text;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif

namespace BDFramework.Editor.PublishPipeline
{
    /// <summary>
    ///  发布资源页面
    /// </summary>
    public class EditorWindow_PublishAssets : EditorWindow

    {
        [MenuItem("BDFrameWork工具箱/PublishPipeline/1.发布资源", false, (int) BDEditorGlobalMenuItemOrderEnum.PublishPipeline_BuildAsset)]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_PublishAssets>(false, "发布资源");
            window.Show();
            window.Focus();
        }

        private EditorWindow_Table editorTable;
        private EditorWindow_ScriptBuildDll editorScript;
        private EditorWindow_GenAssetBundle editorAsset;

        public void Show()
        {
            this.editorTable = new EditorWindow_Table();
            this.editorAsset = new EditorWindow_GenAssetBundle();
            this.editorScript = new EditorWindow_ScriptBuildDll();

            this.minSize = this.maxSize = new Vector2(1200, 800);
            base.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
#if !ODIN_INSPECTOR
                GUILayout.Label("缺少Odin!");
#endif

#if ODIN_INSPECTOR
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
#endif
            }
            GUILayout.EndHorizontal();


            EditorGUILayoutEx.Layout_DrawLineH(Color.white);
            //绘制一键导出和构建Editor WebServer
            GUILayout.BeginHorizontal();
            OnGUI_OneKeyExprot();
            EditorGUILayoutEx.Layout_DrawLineV(Color.white);
            OnGUI_PublishEditorService();
            GUILayout.EndHorizontal();
        }


        private void OnDisable()
        {
            //保存
            BDEditorApplication.BDFrameWorkFrameEditorSetting.Save();
        }

        public string exportPath = "";
        private bool isGenIOSAssets = false;
        private bool isGenAndroidAssets = true;
        private bool isBuilding = false;

        /// <summary>
        /// 一键导出
        /// </summary>
        public void OnGUI_OneKeyExprot()
        {
            GUILayout.BeginVertical(GUILayout.Width(550), GUILayout.Height(350));
            {
                GUILayout.Label("资源发布:", EditorGUIHelper.GetFontStyle(Color.red, 15));
                GUILayout.Label("注:上面按钮操作,会默认生成到DevOps", GUILayout.Height(30));
                //isGenWindowsAssets=GUILayout.Toggle(isGenWindowsAssets, "生成Windows资源");
                isGenAndroidAssets = GUILayout.Toggle(isGenAndroidAssets, "生成Android资源(Windows共用)");
                isGenIOSAssets = GUILayout.Toggle(isGenIOSAssets, "生成Ios资源");

                //
                GUILayout.Label("导出地址:" + exportPath);
                //
                if (GUILayout.Button("一键导出所有资源", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    if (isBuilding)
                    {
                        return;
                    }

                    isBuilding = true;

                    //选择目录
                    exportPath = BDApplication.DevOpsPublishAssetsPath;

                    //生成android资源
                    if (isGenAndroidAssets)
                    {
                        GenAllAssets(exportPath, RuntimePlatform.Android);
                    }

                    //生成ios资源
                    if (isGenIOSAssets)
                    {
                        GenAllAssets(exportPath, RuntimePlatform.IPhonePlayer);
                    }

                    //EditorUtility.DisplayDialog("提示", "资源导出完成", "OK");

                    isBuilding = false;
                }

                //
                if (GUILayout.Button("热更资源转hash(生成服务器配置)", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    //自动转hash
                    PublishPipelineTools.PublishAssetsToServer(BDApplication.DevOpsPublishAssetsPath);
                }

                GUILayout.Space(20);
                GUILayout.Label("调试功能:", EditorGUIHelper.LabelH4);
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("拷贝资源到Streaming", GUILayout.Width(175), GUILayout.Height(30)))
                    {
                        RuntimePlatform platform = RuntimePlatform.Android;
                        if (isGenAndroidAssets)
                        {
                            platform = RuntimePlatform.Android;
                        }
                        else if (isGenIOSAssets)
                        {
                            platform = RuntimePlatform.IPhonePlayer;
                        }

                        //路径
                        var source = IPath.Combine(BDApplication.DevOpsPublishAssetsPath, BDApplication.GetPlatformPath(platform));
                        var target = IPath.Combine(Application.streamingAssetsPath, BDApplication.GetPlatformPath(platform));
                        if (Directory.Exists(target))
                        {
                            Directory.Delete(target, true);
                        }

                        //拷贝
                        FileHelper.CopyFolderTo(source, target);
                        AssetDatabase.Refresh();
                    }

                    if (GUILayout.Button("删除Streaming资源", GUILayout.Width(175), GUILayout.Height(30)))
                    {
                        RuntimePlatform platform = RuntimePlatform.Android;
                        if (isGenAndroidAssets)
                        {
                            platform = RuntimePlatform.Android;
                        }
                        else if (isGenIOSAssets)
                        {
                            platform = RuntimePlatform.IPhonePlayer;
                        }

                        var target = IPath.Combine(Application.streamingAssetsPath, BDApplication.GetPlatformPath(platform));
                        Directory.Delete(target, true);
                    }
                }
            }
            GUILayout.EndHorizontal();
            
            
            GUILayout.EndVertical();
        }


        private EditorHttpListener EditorHttpListener;

        /// <summary>
        /// 文件服务器
        /// </summary>
        public void OnGUI_PublishEditorService()
        {
            //playmode时候启动
            this.StartAssetsServerOnPlayMode();
            
            GUILayout.BeginVertical();
            {
                GUILayout.Label("AB文件服务器:", EditorGUIHelper.GetFontStyle(Color.red, 15));
                EditorGUILayout.HelpBox("在本机Devops搭建文件服务器，提供测试下载功能", MessageType.Info);

                if (EditorHttpListener == null)
                {
                 
                    if (GUILayout.Button("启动本机文件服务器"))
                    {
                        StartLocalAssetsFileServer();
                    }
                }
                else
                {
                    GUI.color = Color.green;

                    if (GUILayout.Button("[已启动]关闭本机文件服务器"))
                    {
                        StopLocalAssetsFileServer();
                    }

                    GUI.color = GUI.backgroundColor;
                }

                GUILayout.Space(10);
                string weburl = "";
                if (EditorHttpListener != null)
                {
                    var ip = IPHelper.GetLocalIP();
                    weburl = "http://" + ip + ":" + EditorHttpListener.port;
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("weburl:  " + weburl);

                    if (GUILayout.Button("复制", GUILayout.Width(40)))
                    {
                        GUIUtility.systemCopyBuffer = weburl;
                        EditorUtility.DisplayDialog("提示", "复制成功!", "OK");
                    }
                }
                GUILayout.EndHorizontal();
                //
                GUILayout.Label("资源地址: ");
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(BDApplication.DevOpsPublishPackagePath + "/" + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX + "/*");
                    if (GUILayout.Button("打开", GUILayout.Width(40)))
                    {
                        var dir = BDApplication.DevOpsPublishPackagePath + "/" + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX;
                        EditorUtility.RevealInFinder(dir);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        /// <summary>
        /// playmode 启动
        /// </summary>
        private void StartAssetsServerOnPlayMode()
        {
            if (EditorApplication.isPlaying && EditorHttpListener == null)
            {
                this.StartLocalAssetsFileServer();
            }
        }

        /// <summary>
        /// 启动本地的资源服务器
        /// </summary>
        private void StartLocalAssetsFileServer()
        {
            if (EditorHttpListener == null)
            {
                //自动转hash
                PublishPipelineTools.PublishAssetsToServer(BDApplication.DevOpsPublishAssetsPath);
                //开启文件服务器
                EditorHttpListener = new EditorHttpListener();
                var webdir = IPath.Combine(BDApplication.DevOpsPublishAssetsPath, PublishPipelineTools.UPLOAD_FOLDER_SUFFIX);
                EditorHttpListener.Start("*", "8081", webdir);
            }
        }

        private void StopLocalAssetsFileServer()
        {
            if (EditorHttpListener != null)
            {
                EditorHttpListener.Stop();
                EditorHttpListener = null;
            }
        }

        /// <summary>
        /// 生成所有资源
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="platform"></param>
        /// <param name="target"></param>
        static public void GenAllAssets(string outputPath, RuntimePlatform platform)
        {
            var _outputPath = Path.Combine(outputPath, BDApplication.GetPlatformPath(platform)); //  + "/" + ;
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            //1.编译脚本
            try
            {
                EditorWindow_ScriptBuildDll.RoslynBuild(outputPath, platform, ScriptBuildTools.BuildMode.Release);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            //2.打包表格
            try
            {
                Excel2SQLiteTools.AllExcel2SQLite(outputPath, platform);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            //3.打包资源
            try
            {
                //var config = BDEditorApplication.BDFrameWorkFrameEditorSetting.BuildAssetBundle;
                AssetBundleEditorToolsV2.GenAssetBundle(outputPath, platform);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            //4.生成本地assetinfo配置
            var allServerAssetItemList = PublishPipelineTools.GetAssetItemList(outputPath, platform);
            var csv = CsvSerializer.SerializeToString(allServerAssetItemList);
            var assetsInfoPath = IPath.Combine(outputPath, BDApplication.GetPlatformPath(platform), BResources.ART_ASSETS_INFO_PATH);
            File.WriteAllText(assetsInfoPath, csv);
        }


        private void OnDestroy()
        {
            this.EditorHttpListener?.Stop();
            EditorHttpListener = null;
        }
    }
}
