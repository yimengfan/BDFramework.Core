using System;
using System.Collections;
using System.Net;
using BDFramework.Core.Tools;
using LitJson;
using marijnz.EditorCoroutines;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace BDFramework.Editor
{
    /// <summary>
    /// 框架启动引导
    /// </summary>
    public class EditorWindow_BDFrameworkStart : EditorWindow
    {
        private static string WIKI_URL = "https://www.yuque.com/naipaopao/eg6gik";

        private static string GITHUB_URL = "https://github.com/yimengfan/BDFramework.Core";

        //更新日志
        private static string CHANGEDLOG_URL =
            "https://gitee.com/yimengfan/BDFramework.Core/blob/master/Packages/com.bdframework.core/CHANGELOG.md";

        //版本号
        private static string VERSION_URL =
            "https://gitee.com/yimengfan/BDFramework.Core/blob/master/Packages/com.bdframework.core/Runtime/Resources/BDFrameConfig.Json";

        private static Texture webIcon; //= EditorGUIUtility.IconContent( "BuildSettings.Web.Small" ).image;

        //btn样式
        private GUIContent wikiBtnContent; // = new GUIContent( " 中文Wiki", webIcon );

        private GUIContent gitBtnContent; // = new GUIContent( " Github", webIcon );

        //label样式
        private GUIStyle titleStyle;
        private GUIStyle errorStyle;

        [MenuItem("Assets/测试111")]
        static public void Open()
        {
            var win = GetWindow<EditorWindow_BDFrameworkStart>("BDFramework使用引导");
            win.minSize = win.maxSize = new Vector2(400, 500);
            win.Show();
        }

        /// <summary>
        /// 打开窗口
        /// 覆盖show接口
        /// </summary>
        public void Show()
        {
            //初始化各种样式
            webIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
            wikiBtnContent = new GUIContent(" 中文Wiki", webIcon);
            gitBtnContent = new GUIContent(" Github", webIcon);
            titleStyle = new GUIStyle("BoldLabel")
            {
                margin = new RectOffset(4, 4, 4, 4), padding = new RectOffset(2, 2, 2, 2), fontSize = 13
            };

            base.Show();
            //执行各种任务
            this.GetNewChangeLog();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(wikiBtnContent))
                {
                    Application.OpenURL(WIKI_URL);
                }

                if (GUILayout.Button(gitBtnContent))
                {
                    Application.OpenURL(GITHUB_URL);
                }
            }
            GUILayout.EndHorizontal();

            OnGUI_DependLib();
            OnGUI_UpdateNewVersion();
            OnGUI_DrawUpdateNote();
        }

        /// <summary>
        /// 画线
        /// </summary>
        public void DrawLine()
        {
            GUILayout.Box(string.Empty, GUILayout.Width(position.width - 24), GUILayout.Height(2));
        }


        #region 依赖信息

        /// <summary>
        /// 依赖库相关
        /// </summary>
        public void OnGUI_DependLib()
        {
            GUILayout.Space(10);
            GUILayout.Label("依赖库", titleStyle);
            DrawLine();
            //1.判断Odin
            if (!IsExsitOdin())
            {
                GUILayout.BeginHorizontal();

                GUI.color = Color.red;
                GUILayout.Label("缺少Odin");
                GUI.color = GUI.contentColor;
                if (GUILayout.Button("Download", GUILayout.Width(80)))
                {
                    var packagePath = AssetDatabase.GUIDToAssetPath("69227cf6ea5304641ae95ffb93874014");
                    AssetDatabase.ImportPackage(packagePath, true);
                }

                GUILayout.EndHorizontal();
            }

            //2.判断是否缺少资源
            if (!IsImportedAsset())
            {
                GUILayout.BeginHorizontal();
                GUI.color = Color.red;
                GUILayout.Label("导入Asset.package");
                GUI.color = GUI.contentColor;
                if (GUILayout.Button("Import", GUILayout.Width(80)))
                {
                    var packagePath = AssetDatabase.GUIDToAssetPath("69227cf6ea5304641ae95ffb93874014");
                    AssetDatabase.ImportPackage(packagePath, true);
                }

                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 检测odin
        /// </summary>
        /// <returns></returns>
        static public bool IsExsitOdin()
        {
            bool isHaveOdin = false;
#if ODIN_INSPECTOR
           isHaveOdin = true;
#endif
            return isHaveOdin;
        }

        /// <summary>
        /// 是否导入过
        /// </summary>
        /// <returns></returns>
        static public bool IsImportedAsset()
        {
            return false;
        }

        #endregion


        #region 版本信息

        /// <summary>
        /// 新版本号
        /// </summary>
        private string NewVersionNum = "获取中...";

        /// <summary>
        /// 更新 新版本
        /// </summary>
        public void OnGUI_UpdateNewVersion()
        {
            GUILayout.Space(10);
            GUILayout.Label("版本信息", titleStyle);
            DrawLine();
            GUILayout.Label("当前版本:" + BDEditorApplication.BDFrameConfig.Version);
            GUILayout.Label("最新版本:" + NewVersionNum);
            if (!CheckExsitNewVersion() && GUILayout.Button("更新"))
            {
                Application.OpenURL(GITHUB_URL);
            }
        }

        // <summary>
        /// 检测odin
        /// </summary>
        /// <returns></returns>
        public bool CheckExsitNewVersion()
        {
            return BDEditorApplication.BDFrameConfig.Version == NewVersionNum;
        }

        /// <summary>
        /// 更新服务器版本号好
        /// </summary>
        public void GetNewVersionNum()
        {
            WebClient wc = new WebClient();
            var ret = wc.DownloadString(VERSION_URL);
            var config = JsonMapper.ToObject<BDFrameConfig>(ret); // ret;
            if (config != null)
            {
                this.NewVersionNum = config.Version;
            }
        }

        #endregion


        #region 更新日志

        //
        Vector2 scrollPosition = Vector2.zero;
        private string FrameUpdateNote = "正在获取...";

        /// <summary>
        /// 更新日志
        /// </summary>
        public void OnGUI_DrawUpdateNote()
        {
            GUILayout.Space(10);
            GUILayout.Label("更新日志", titleStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "ProgressBarBack",
                GUILayout.Height(200), GUILayout.ExpandWidth(true));
            GUILayout.Label(FrameUpdateNote, "WordWrappedMiniLabel", GUILayout.Height(200));
            GUILayout.EndScrollView();
        }


        /// <summary>
        /// 更新日志
        /// </summary>
        /// <returns></returns>
        void GetNewChangeLog()
        {
            WebClient wc = new WebClient();
            var ret = wc.DownloadString(CHANGEDLOG_URL);
            this.FrameUpdateNote = ret;
        }

        #endregion
    }
}