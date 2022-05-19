using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Table;
using BDFramework.Editor.AssetBundle;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.Tools;
using BDFramework.Editor.Tools.EditorHttpServer;
using BDFramework.Editor.Unity3dEx;
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

        /// <summary>
        /// 默认导出地址
        /// </summary>
        static private string EXPORT_PATH
        {
            get
            {
                return BApplication.DevOpsPublishAssetsPath;
            }
        }

        private EditorWindow_Table editorTable;
        private EditorWindow_ScriptBuildDll editorScript;
        private EditorWindow_BuildAssetBundle editorAsset;

        public void Show()
        {
            this.editorTable = new EditorWindow_Table();
            this.editorAsset = new EditorWindow_BuildAssetBundle();
            this.editorScript = new EditorWindow_ScriptBuildDll();

            this.minSize = this.maxSize = new Vector2(1000, 800);
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
                // EXPORT_PATH = BApplication.DevOpsPublishAssetsPath;
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
            BDEditorApplication.BDFrameworkEditorSetting.Save();
        }


        //Runtimeform不支持flag
        private List<RuntimePlatform> selectPlatforms = new List<RuntimePlatform>() {RuntimePlatform.Android};

        private Dictionary<RuntimePlatform, string> platformVersionMap = new Dictionary<RuntimePlatform, string>();

        //状态
        private bool isBuilding = false;

        /// <summary>
        /// 一键导出
        /// </summary>
        public void OnGUI_OneKeyExprot()
        {
            GUILayout.BeginVertical(GUILayout.Width(this.maxSize.x / 2), GUILayout.Height(350));
            {
                GUILayout.Label("资源发布:", EditorGUIHelper.GetFontStyle(Color.red, 15));

                EditorGUILayout.HelpBox("版本号采用三段式:0.0.1,前两位可以自定义,最后一位默认自增！\n默认导出地址:Devops/PublishAssets", MessageType.Info);
                GUILayout.Space(5);
                //
                foreach (var sp in BApplication.SupportPlatform)
                {
                    GUILayout.BeginHorizontal();
                    {
                        var isHas = selectPlatforms.Contains(sp);
                        //选择
                        var isSelcet = GUILayout.Toggle(isHas, $"生成{BApplication.GetPlatformPath(sp)}资产", GUILayout.Width(150));
                        //
                        if (isHas != isSelcet)
                        {
                            if (isSelcet)
                            {
                                selectPlatforms.Add(sp);
                            }
                            else
                            {
                                selectPlatforms.Remove(sp);
                            }
                        }

                        var basePackageBuildInfo = BasePackageAssetsHelper.GetPacakgeBuildInfo(EXPORT_PATH, sp);
                        string setVersionNum = "";
                        var ret = platformVersionMap.TryGetValue(sp, out setVersionNum);
                        if (!ret)
                        {
                            platformVersionMap[sp] = basePackageBuildInfo.Version;
                        }
                        //根据即将设置信息开始解析
                        var vs =  platformVersionMap[sp] .Split('.');
                        int bigNum = 0;
                        int smallNum = 0;
                        int additiveNum = 0;
                        bigNum = int.Parse(vs[0]);
                        smallNum = int.Parse(vs[1]);
                        additiveNum = int.Parse(vs[2]);
                        //version.info信息 渲染
                        GUILayout.Label("Ver:", GUILayout.Width(30));
                        bigNum = EditorGUILayout.IntField(bigNum, GUILayout.Width(20));
                        GUILayout.Label(".", GUILayout.Width(5));
                        smallNum = EditorGUILayout.IntField(smallNum, GUILayout.Width(20));
                        GUILayout.Label(".", GUILayout.Width(5));
                        GUILayout.Label(additiveNum.ToString(),GUILayout.Width(40));
                        //保存 设置信息
                        setVersionNum= string.Format("{0}.{1}.{2}", bigNum, smallNum, additiveNum);
                        //渲染预览信息
                        GUILayout.Space(10);
                        var newVersion=  VersionNumHelper.AddVersionNum(basePackageBuildInfo.Version, setVersionNum);
                        GUILayout.Label($"预览: {basePackageBuildInfo.Version}  =>  {newVersion}");
                        platformVersionMap[sp] = setVersionNum;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }


                //
                GUILayout.Space(5);
                if (GUILayout.Button("一键导出所选平台资产(脚本、美术、表格)", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    if (isBuilding)
                    {
                        return;
                    }

                    isBuilding = true;


                    //开始 生成资源
                    foreach (var sp in selectPlatforms)
                    {
                        BuildAssetsTools.BuildAllAssets(sp, EXPORT_PATH,platformVersionMap[sp]);
                        platformVersionMap.Remove(sp);
                    }

                    isBuilding = false;
                }

                //
                if (GUILayout.Button("热更资源转hash(生成服务器配置)", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    //自动转hash
                    PublishPipelineTools.PublishAssetsToServer(EXPORT_PATH);
                }

                GUILayout.Space(20);
                GUILayout.Label("调试功能:", EditorGUIHelper.LabelH4);
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("拷贝资源到Streaming", GUILayout.Width(175), GUILayout.Height(30)))
                    {
                        //路径
                        var source = IPath.Combine(EXPORT_PATH, BApplication.GetRuntimePlatformPath());
                        var target = IPath.Combine(Application.streamingAssetsPath, BApplication.GetRuntimePlatformPath());
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
                        var target = IPath.Combine(Application.streamingAssetsPath, BApplication.GetRuntimePlatformPath());
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

            GUILayout.BeginVertical(GUILayout.Width(this.maxSize.x / 2));
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
                        GUIUtility.systemCopyBuffer = IPHelper.GetLocalIP() + ":" + EditorHttpListener.port+"/Assetbundle";
                        EditorUtility.DisplayDialog("提示", "复制成功!", "OK");
                    }
                }
                GUILayout.EndHorizontal();
                //
                GUILayout.Label("资源地址: ");
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("项目根目录/DevOps/PublishAssets/" + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX + "/*");
                    if (GUILayout.Button("打开", GUILayout.Width(40)))
                    {
                        var dir = BApplication.DevOpsPublishPackagePath + "/" + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX;
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
                PublishPipelineTools.PublishAssetsToServer(EXPORT_PATH);
                //开启文件服务器
                EditorHttpListener = new EditorHttpListener();
                //添加AB文件服务器处理器
                EditorHttpListener.AddWebAPIProccesor<WP_LocalABFileServer>();
                var webdir = IPath.Combine(EXPORT_PATH, PublishPipelineTools.UPLOAD_FOLDER_SUFFIX);
                EditorHttpListener.Start("+", "10086");
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


        private void OnDestroy()
        {
            this.EditorHttpListener?.Stop();
            EditorHttpListener = null;
        }
    }
}
