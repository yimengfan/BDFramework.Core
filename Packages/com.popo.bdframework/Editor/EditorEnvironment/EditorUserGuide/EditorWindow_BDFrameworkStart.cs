using System;
using System.Collections;
using System.IO;
using System.Net;
using BDFramework.Assets.VersionContrller;
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
        //版本号标记
        //URL 相关
        private static string WIKI_URL = "https://www.wolai.com/hPVgjgyR4ZVGgAruqvoTL2";
        private static string GITHUB_URL = "https://github.com/yimengfan/BDFramework.Core";
        private static string Odin_URL = "https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041";
        private static string QQGroup_URL = "https://jq.qq.com/?_wv=1027&k=OSxzhgK4";

        //更新日志
        private static string CHANGEDLOG_URL = "https://github.com/yimengfan/BDFramework.Core/blob/master/Packages/com.popo.bdframework/CHANGELOG.md";

        //版本号
        private static string PCKAGE_URL = "https://raw.githubusercontent.com/yimengfan/BDFramework.Core/master/Packages/com.popo.bdframework/package.json";

        private static Texture webIcon; //= EditorGUIUtility.IconContent( "BuildSettings.Web.Small" ).image;

        //btn样式
        private static GUIContent wikiBtnContent; // = new GUIContent( " 中文Wiki", webIcon );
        private static GUIContent gitBtnContent; // = new GUIContent( " Github", webIcon );

        //label样式
        private static GUIStyle titleStyle;
        private static GUIStyle errorStyle;


        [MenuItem(("BDFrameWork工具箱/框架引导 " + BDLauncher.Version), false, (int)BDEditorGlobalMenuItemOrderEnum.BDFrameworkGuid)]
        static public void Open()
        {
            var win = GetWindow<EditorWindow_BDFrameworkStart>("BDFramework使用引导");
            win.minSize = win.maxSize = new Vector2(400, 500);
            win.Show();
        }

        /// <summary>
        /// 自动打开
        /// </summary>
        static public void AutoOpen()
        {
            if (!IsOpenedSomeDay(10) //7天内不重复
                && (IsHaveNewVerison() //新版本
                    || !IsExsitOdin() //缺少odin
                    || !IsImportedAsset() //缺少文件
                ))
            {
                Open();
            }
        }


        /// <summary>
        /// 检测7天内是否打开过
        /// </summary>
        /// <returns></returns>
        static public bool IsOpenedSomeDay(int day = 10)
        {
            for (int i = 0; i < day; i++)
            {
                var dayStr = DateTime.Today.AddDays(-1 * i).ToLongDateString();
                //Debug.Log(dayStr);
                var path = BApplication.BDEditorCachePath + "/OpenGuideLog_" + dayStr;
                if (File.Exists(path))
                {
                    // BDebug.Log($"open in {dayStr}!");
                    return true;
                }
            }

            //没打开过，写入打开日志
            {
                var log = $"EditorWindow_BDFrameworkStart  is open in {DateTime.Today.ToLongDateString()}!";
                var path = BApplication.BDEditorCachePath + "/OpenGuideLog_" + DateTime.Today.ToLongDateString();
                FileHelper.WriteAllText(path, log);
            }
            return false;
        }

        /// <summary>
        /// 打开窗口
        /// 覆盖show接口
        /// </summary>
        public void Show()
        {
            base.Show();
            //执行各种任务
            this.GetNewChangeLog();
            this.Focus();
        }

        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitStyle()
        {
            if (webIcon)
            {
                return;
            }

            //初始化各种样式
            webIcon = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
            wikiBtnContent = new GUIContent(" 中文Wiki", webIcon);
            gitBtnContent = new GUIContent(" Github", webIcon);
            titleStyle = new GUIStyle("BoldLabel") { margin = new RectOffset(4, 4, 4, 4), padding = new RectOffset(2, 2, 2, 2), fontSize = 13 };
        }

        /// <summary>
        /// 渲染当前页面
        /// </summary>
        private void OnGUI()
        {
            InitStyle();
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

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            {
                GUI.color = Color.green;
                GUILayout.Label("官方1群:184111890", GUILayout.Width(120));
                GUI.color = GUI.contentColor;

                if (GUILayout.Button("加入", GUILayout.Width(35)))
                {
                    Application.OpenURL(QQGroup_URL);
                }
            }
            GUILayout.EndHorizontal();
            //this.Focus();
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
            {
                GUILayout.BeginHorizontal();
                if (!IsExsitOdin())
                {
                    GUI.color = Color.red;
                    GUILayout.Label("1.缺少Odin");
                    GUI.color = GUI.contentColor;
                    if (GUILayout.Button("Download", GUILayout.Width(80)))
                    {
                        Application.OpenURL(Odin_URL);
                    }
                }
                else
                {
                    GUI.color = Color.green;
                    GUILayout.Label("1.已存在Odin");
                    GUI.color = GUI.contentColor;
                }

                GUILayout.EndHorizontal();
            }

            //2.判断是否缺少资源
            {
                GUILayout.BeginHorizontal();
                if (!IsImportedAsset())
                {
                    GUI.color = Color.red;
                    GUILayout.Label("2.导入Asset.package");
                    GUI.color = GUI.contentColor;
                    if (GUILayout.Button("Import", GUILayout.Width(80)))
                    {
                        var packagePath = AssetDatabase.GUIDToAssetPath("69227cf6ea5304641ae95ffb93874014");
                        AssetDatabase.ImportPackage(packagePath, true);
                    }
                }
                else
                {
                    GUI.color = Color.green;
                    GUILayout.Label("2.已导入Asset.package");
                    GUI.color = GUI.contentColor;
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
            var path = AssetDatabase.GUIDToAssetPath("924d970067c935c4f8b818e6b4ab9e07");
            if (File.Exists(path))
            {
                var version = File.ReadAllText(path);
                if (version == BDLauncher.Version)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region 版本信息

        /// <summary>
        /// 新版本号
        /// </summary>
        static private string NewVersionNum = null;

        /// <summary>
        /// 更新 新版本
        /// </summary>
        public void OnGUI_UpdateNewVersion()
        {
            GUILayout.Space(10);
            GUILayout.Label("版本信息", titleStyle);
            DrawLine();
            GUI.color = Color.green;

            GUILayout.Label("当前版本:" + BDLauncher.Version);
            //
            if (IsHaveNewVerison())
            {
                GUI.color = Color.red;
                GUILayout.Label("最新版本:" + NewVersionNum);
                if (GUILayout.Button("更新"))
                {
                    Application.OpenURL(GITHUB_URL);
                }
            }
            else
            {
                GUILayout.Label("最新版本:" + NewVersionNum);
            }

            GUI.color = GUI.contentColor;
        }

        public class PackageData
        {
            public string version = "null";
        }


        // <summary>
        /// 是否有新版本
        /// </summary>
        /// <returns></returns>
        static public bool IsHaveNewVerison()
        {
            if (NewVersionNum == null)
            {
                WebClient wc = new WebClient();
                //BDebug.Log($"download:{PCKAGE_URL}");
                try
                {
                    var ret = wc.DownloadString(PCKAGE_URL);
                    var config = JsonMapper.ToObject<PackageData>(ret);
                    if (config != null)
                    {
                        NewVersionNum = config.version;
                    }
                }
                catch
                {
                    NewVersionNum = "0.0.1";
                    BDebug.LogError($"访问github失败:{PCKAGE_URL}");
                }
            }

            var isHaveNewVersion = !VersionNumHelper.GT(BDLauncher.Version, NewVersionNum);
            return isHaveNewVersion;
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
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "ProgressBarBack", GUILayout.Height(200), GUILayout.ExpandWidth(true));
            GUILayout.Label(FrameUpdateNote, "WordWrappedMiniLabel", GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }


        /// <summary>
        /// 更新日志
        /// </summary>
        /// <returns></returns>
        void GetNewChangeLog()
        {
            //有新版本则拉取服务器上的
            if (IsHaveNewVerison())
            {
                var newLogPath = Path.Combine(BApplication.BDEditorCachePath, "VersionLog_" + NewVersionNum);
                //本地不存在就缓存到本地
                if (!File.Exists(newLogPath))
                {
                    try
                    {
                        WebClient wc = new WebClient();
                        var ret = wc.DownloadString(CHANGEDLOG_URL);
                        FileHelper.WriteAllText(newLogPath, ret);
                    }
                    catch (Exception e)
                    {
                        BDebug.LogError($"访问github失败:{CHANGEDLOG_URL}");
                    }
                }

                this.FrameUpdateNote = File.ReadAllText(newLogPath);
            }
            else
            {
                var path = AssetDatabase.GUIDToAssetPath("20c952b57a090a14f86ceff9cc824d05");
                var localNote = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                this.FrameUpdateNote = localNote.text;
            }
        }

        #endregion
    }
}