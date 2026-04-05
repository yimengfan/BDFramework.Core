using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Asset;
using BDFramework.Assets.VersionContrller;
using UnityEditor;
using UnityEngine;
using BDFramework.Editor.Table;
using BDFramework.Editor.AssetBundle;
using BDFramework.Core.Tools;
using BDFramework.Editor.BuildPipeline;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.Editor.HotfixScript;
using BDFramework.Editor.Tools;
using BDFramework.Editor.Tools.EditorHttpServer;
using BDFramework.Editor.Unity3dEx;
using UnityEngine.AssetGraph;
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
        private enum BuildState
        {
            Idle,
            Queued,
            Running,
            Succeeded,
            Failed,
        }

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
            get { return BApplication.DevOpsPublishAssetsPath; }
        }

        private EditorWindow_Table editorTable;
        private EditorWindow_BuildHotfixDll _editor;
        private EditorWindow_BuildAssetBundle editorAsset;

        public void Show()
        {
            this.editorTable = new EditorWindow_Table();
            this.editorAsset = new EditorWindow_BuildAssetBundle();
            this._editor = new EditorWindow_BuildHotfixDll();
            this.minSize = this.maxSize = new Vector2(1000, 800);
            //
            selectPlatforms.Add(BApplication.RuntimePlatform);
            base.Show();
        }

        //状态
        private bool isBuilding = false;
        private bool isBuildQueued = false;
        private BuildState buildState = BuildState.Idle;
        private string buildStatusMessage = "未开始构建。";
        private float buildProgress = 0f;
        private List<RuntimePlatform> queuedBuildPlatforms = new List<RuntimePlatform>();
        private Dictionary<RuntimePlatform, string> queuedBuildVersionMap = new Dictionary<RuntimePlatform, string>();

        private void OnGUI()
        {
            if (BDEditorApplication.EditorSetting == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
#if !ODIN_INSPECTOR
                GUILayout.Label("缺少Odin! 编辑器绘制Error!!!!", EditorGUIHelper.LabelH1);
#endif

#if ODIN_INSPECTOR
                if (_editor != null)
                {
                    SirenixEditorGUI.BeginBox("脚本", true, GUILayout.Width(220), GUILayout.Height(450));
                    try
                    {
                        _editor.OnGUI();
                    }
                    finally
                    {
                        SirenixEditorGUI.EndBox();
                    }
                }

                if (editorAsset != null)
                {
                    SirenixEditorGUI.BeginBox("资源", true, GUILayout.Width(220), GUILayout.Height(450));
                    try
                    {
                        editorAsset.OnGUI();
                    }
                    finally
                    {
                        SirenixEditorGUI.EndBox();
                    }
                }

                if (editorTable != null)
                {
                    SirenixEditorGUI.BeginBox("表格", true, GUILayout.Width(200), GUILayout.Height(450));
                    try
                    {
                        editorTable.OnGUI();
                    }
                    finally
                    {
                        SirenixEditorGUI.EndBox();
                    }
                }
#endif
            }

            EditorGUILayoutEx.Layout_DrawLineH(Color.white);

            using (new EditorGUILayout.HorizontalScope())
            {
                OnGUI_OneKeyExprot();
                EditorGUILayoutEx.Layout_DrawLineV(Color.white);
                OnGUI_PublishEditorService();
            }
        }


        private void OnDisable()
        {
            ClearQueuedBuildCallbacks();
            //保存
            BDEditorApplication.EditorSetting.Save();
        }


        //Runtimeform不支持flag
        private List<RuntimePlatform> selectPlatforms = new List<RuntimePlatform>() { };

        private Dictionary<RuntimePlatform, string> platformVersionMap = new Dictionary<RuntimePlatform, string>();

        /// <summary>
        /// 一键导出
        /// </summary>
        public void OnGUI_OneKeyExprot()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(this.maxSize.x / 2), GUILayout.Height(350)))
            {
                GUILayout.Label("资源发布:", EditorGUIHelper.GetFontStyle(Color.red, 15));

                EditorGUILayout.HelpBox("版本号采用三段式:0.0.1,前两位可以自定义,最后一位默认自增！\n 默认导出地址:Devops/PublishAssets", MessageType.Info);
                GUILayout.Space(5);

                foreach (var sp in BApplication.SupportPlatform)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var isHas = selectPlatforms.Contains(sp);
                        var isSelcet = GUILayout.Toggle(isHas, $"生成{BApplication.GetPlatformLoadPath(sp)}资产", GUILayout.Width(150));
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

                        var basePackageBuildInfo = ClientAssetsUtils.GetPackageBuildInfo(EXPORT_PATH, sp);
                        var ret = platformVersionMap.TryGetValue(sp, out var setVersionNum);
                        if (!ret)
                        {
                            setVersionNum = basePackageBuildInfo.Version;
                            platformVersionMap[sp] = setVersionNum;
                        }

                        var versionNum = VersionNumHelper.ParseVersion(platformVersionMap[sp]);
                        var bigNum = versionNum.bigNum;
                        var smallNum = versionNum.smallNum;
                        var additiveNum = versionNum.additiveNum;

                        GUILayout.Label("Ver:", GUILayout.Width(30));
                        bigNum = EditorGUILayout.IntField(bigNum, GUILayout.Width(20));
                        GUILayout.Label(".", GUILayout.Width(5));
                        smallNum = EditorGUILayout.IntField(smallNum, GUILayout.Width(20));
                        GUILayout.Label(".", GUILayout.Width(5));
                        GUILayout.Label(additiveNum.ToString(), GUILayout.Width(40));

                        setVersionNum = string.Format("{0}.{1}.{2}", bigNum, smallNum, additiveNum);
                        GUILayout.Space(10);
                        var newVersion = VersionNumHelper.AddVersionNum(basePackageBuildInfo.Version, setVersionNum);
                        GUILayout.Label($"预览: {basePackageBuildInfo.Version}  =>  {newVersion}");
                        platformVersionMap[sp] = setVersionNum;
                    }

                    GUILayout.Space(2);
                }

                GUILayout.Space(5);
                EditorGUILayout.HelpBox(buildStatusMessage, GetBuildStatusMessageType());
                if (isBuildQueued || isBuilding)
                {
                    var progressRect = GUILayoutUtility.GetRect(350, 18);
                    EditorGUI.ProgressBar(progressRect, buildProgress, $"{Mathf.RoundToInt(buildProgress * 100)}%");
                    GUILayout.Space(4);
                }

                using (new EditorGUI.DisabledScope(isBuildQueued || isBuilding))
                {
                    if (GUILayout.Button(GetBuildButtonText(), GUILayout.Width(350), GUILayout.Height(30)))
                    {
                        if (QueueBuildSelectedPlatforms())
                        {
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                GUILayout.Label("上传:", EditorGUIHelper.GetFontStyle(Color.red, 15));
                if (GUILayout.Button("热更资源转hash(生成服务器配置)", GUILayout.Width(350), GUILayout.Height(30)))
                {
                    PublishPipelineTools.PublishAssetsToServer(EXPORT_PATH);
                    EditorUtility.DisplayDialog("提示", $"生成到 {EXPORT_PATH}/{PublishPipelineTools.UPLOAD_FOLDER_SUFFIX} 完成,等待提交到服务器! \n也可以用于本机服务器!", "确定");
                }

                // if (GUILayout.Button("上传到阿里云OSS", GUILayout.Width(350), GUILayout.Height(30)))
                // {
                //     //自动转hash
                //     PublishPipelineTools.PublishAssetsToServer(EXPORT_PATH);
                // }

                // GUILayout.Space(5);
                // GUILayout.Label("调试功能:", EditorGUIHelper.LabelH4);
                // GUILayout.BeginHorizontal();
                // {
                //     if (GUILayout.Button("拷贝资源到Streaming", GUILayout.Width(175), GUILayout.Height(30)))
                //     {
                //         //拷贝
                //         BuildPackageTools.CopyPublishAssetsTo(Application.streamingAssetsPath, BApplication.RuntimePlatform);
                //         AssetDatabase.Refresh();
                //     }
                //
                //     if (GUILayout.Button("删除Streaming资源", GUILayout.Width(175), GUILayout.Height(30)))
                //     {
                //         var target = IPath.Combine(Application.streamingAssetsPath, BApplication.GetRuntimePlatformPath());
                //         Directory.Delete(target, true);
                //     }
                // }
                // GUILayout.EndHorizontal();
            }
        }

        private string GetBuildButtonText()
        {
            if (isBuildQueued)
            {
                return "构建任务已排队...";
            }

            if (isBuilding)
            {
                return "正在构建，请稍候...";
            }

            return "一键生成所选平台资产(脚本、美术、表格)";
        }


        private MessageType GetBuildStatusMessageType()
        {
            switch (buildState)
            {
                case BuildState.Failed:
                    return MessageType.Error;
                case BuildState.Queued:
                case BuildState.Running:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }


        private bool QueueBuildSelectedPlatforms()
        {
            if (isBuilding || isBuildQueued)
            {
                buildStatusMessage = "已有构建任务正在执行，请等待当前任务完成。";
                buildState = BuildState.Running;
                Repaint();
                return false;
            }

            var buildPlatforms = this.selectPlatforms.Distinct().ToList();
            if (buildPlatforms.Count == 0)
            {
                buildState = BuildState.Failed;
                buildStatusMessage = "请至少选择一个平台后再执行构建。";
                EditorUtility.DisplayDialog("提示", "请至少选择一个平台后再执行构建。", "确定");
                Repaint();
                return false;
            }

            isBuilding = true;
            isBuildQueued = true;
            buildState = BuildState.Queued;
            buildProgress = 0f;
            buildStatusMessage = $"已加入构建队列，等待启动... 平台数: {buildPlatforms.Count}";

            queuedBuildPlatforms = buildPlatforms;
            queuedBuildVersionMap = buildPlatforms.ToDictionary(
                sp => sp,
                sp => this.platformVersionMap.TryGetValue(sp, out var version)
                    ? version
                    : ClientAssetsUtils.GetPackageBuildInfo(EXPORT_PATH, sp).Version);

            EditorApplication.delayCall -= BeginQueuedBuildOnNextUpdate;
            EditorApplication.update -= ExecuteQueuedBuildFromUpdate;
            EditorApplication.delayCall += BeginQueuedBuildOnNextUpdate;
            Repaint();
            return true;
        }


        private void BeginQueuedBuildOnNextUpdate()
        {
            EditorApplication.delayCall -= BeginQueuedBuildOnNextUpdate;
            EditorApplication.update -= ExecuteQueuedBuildFromUpdate;
            EditorApplication.update += ExecuteQueuedBuildFromUpdate;
            buildStatusMessage = "构建任务准备开始...";
            Repaint();
        }


        private void ExecuteQueuedBuildFromUpdate()
        {
            EditorApplication.update -= ExecuteQueuedBuildFromUpdate;
            ExecuteBuildSelectedPlatforms(this.queuedBuildPlatforms, this.queuedBuildVersionMap);
        }


        private void ExecuteBuildSelectedPlatforms(List<RuntimePlatform> buildPlatforms, Dictionary<RuntimePlatform, string> buildVersionMap)
        {
            try
            {
                buildState = BuildState.Running;

                for (var i = 0; i < buildPlatforms.Count; i++)
                {
                    var sp = buildPlatforms[i];
                    buildProgress = (float) i / buildPlatforms.Count;
                    buildStatusMessage = $"正在构建 {BApplication.GetPlatformLoadPath(sp)} 资源... ({i + 1}/{buildPlatforms.Count})";
                    EditorUtility.DisplayProgressBar("资源发布", buildStatusMessage, buildProgress);
                    Repaint();

                    Debug.Log($"==============>:{sp}");
                    BuildTools_Assets.BuildAll(sp, EXPORT_PATH, buildVersionMap[sp]);
                }

                buildProgress = 1f;
                buildState = BuildState.Succeeded;
                buildStatusMessage = $"构建完成，共处理 {buildPlatforms.Count} 个平台。";
            }
            catch (Exception e)
            {
                buildState = BuildState.Failed;
                buildStatusMessage = $"构建失败: {e.Message}";
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                foreach (var sp in buildPlatforms)
                {
                    this.platformVersionMap.Remove(sp);
                }

                this.queuedBuildPlatforms.Clear();
                this.queuedBuildVersionMap.Clear();
                isBuilding = false;
                isBuildQueued = false;
                Repaint();
            }
        }


        private void ClearQueuedBuildCallbacks()
        {
            EditorApplication.delayCall -= BeginQueuedBuildOnNextUpdate;
            EditorApplication.update -= ExecuteQueuedBuildFromUpdate;
            EditorUtility.ClearProgressBar();
        }

        private EditorHttpListener EditorHttpListener;

        /// <summary>
        /// 文件服务器
        /// </summary>
        public void OnGUI_PublishEditorService()
        {
            //playmode时候启动
            this.StartAssetsServerOnPlayMode();

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(this.maxSize.x / 2)))
            {
                GUILayout.Label("AB文件服务器:", EditorGUIHelper.GetFontStyle(Color.red, 15));
                EditorGUILayout.HelpBox("在本机Devops搭建文件服务器，提供测试下载功能", MessageType.Info);
                GUILayout.Space(10);

                var ret = BDEditorApplication.EditorSetting.ABFileEditorServerSetting.IsAutoStartLocalABServer;
                ret = EditorGUILayout.Toggle("PlayMode自动开启", ret);
                BDEditorApplication.EditorSetting.ABFileEditorServerSetting.IsAutoStartLocalABServer = ret;

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

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("weburl:  " + weburl);

                    if (GUILayout.Button("复制", GUILayout.Width(40)))
                    {
                        GUIUtility.systemCopyBuffer = IPHelper.GetLocalIP() + ":" + EditorHttpListener.port + "/Assetbundle";
                        EditorUtility.DisplayDialog("提示", "复制成功!", "OK");
                    }
                }

                GUILayout.Label("资源地址: ");
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("项目根目录/DevOps/PublishAssets/" + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX + "/*");
                    if (GUILayout.Button("打开", GUILayout.Width(40)))
                    {
                        var dir = BApplication.DevOpsPublishClientPackagePath + "/" + PublishPipelineTools.UPLOAD_FOLDER_SUFFIX;
                        EditorUtility.RevealInFinder(dir);
                    }
                }
            }
        }


        /// <summary>
        /// playmode 启动
        /// </summary>
        private void StartAssetsServerOnPlayMode()
        {
            if (EditorApplication.isPlaying && EditorHttpListener == null)
            {
                AssetGraphEditorWindow.Window?.Close();

                if (BDEditorApplication.EditorSetting.ABFileEditorServerSetting.IsAutoStartLocalABServer)
                {
                    this.StartLocalAssetsFileServer();
                }
            }
        }

        /// <summary>
        /// 启动本地的资源服务器
        /// </summary>
        private void StartLocalAssetsFileServer()
        {
            if (EditorHttpListener == null)
            {
                //开启文件服务器
                EditorHttpListener = new EditorHttpListener();
                //添加AB文件服务器处理器
                EditorHttpListener.AddWebAPIProccesor<WP_LocalABFileServer>();
                var webdir = IPath.Combine(EXPORT_PATH, PublishPipelineTools.UPLOAD_FOLDER_SUFFIX);
                EditorHttpListener.Start("+", "10086");

                //发布资源
                PublishPipelineTools.PublishAssetsToServer(EXPORT_PATH);
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
            ClearQueuedBuildCallbacks();
            this.EditorHttpListener?.Stop();
            EditorHttpListener = null;
        }
    }
}
