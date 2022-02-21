using System;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.Unity3dEx;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.HotfixPipeline
{
    /// <summary>
    /// 一键构建资源
    /// </summary>
    public class EditorWindow_HotfixFileSetting : EditorWindow
    {
        private static int w = 800;
        private static int h = 900;

        [MenuItem("BDFrameWork工具箱/HotfixPipeline/1.配置热更文件", false, (int) BDEditorGlobalMenuItemOrderEnum.HotfixPipeline)]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<EditorWindow_HotfixFileSetting>(false, "热更配置");
            window.Show();
            window.minSize = window.maxSize = new Vector2(w, h);
            window.Focus();
        }

        private void OnGUI()
        {
            var allconfigs = HotfixPipelineTools.HotfixFileConfig.GetAllConfig();
            if (curSlectConfigItem == null && allconfigs.Length > 0)
            {
                curSlectConfigItem = allconfigs[0];
            }

            //绘制config
            ONGUI_DrawAllConfig();
            //排版
            EditorGUILayoutEx.Layout_DrawLineH(new Color(1, 1, 1, 0.5f), 2);
            GUILayout.BeginVertical();
            ONGUI_DrawFileList();
            GUILayout.EndVertical();
        }

        private int curSelectTagIdx = 0;
        private HotfixFileConfigLogic.HotfixFileConfigItem curSlectConfigItem = null;

        private string addTag = "none";

        private void ONGUI_DrawAllConfig()
        {
            GUILayout.BeginVertical();
            //渲染所有的配置
            EditorGUILayout.LabelField("热更文件配置:");


            //添加配置
            GUILayout.BeginHorizontal(GUILayout.Width(w));
            {
                // Tag
                addTag = EditorGUILayout.TextField(addTag, GUILayout.Width(100));
                GUILayout.Space(20);
                //添加
                if (GUILayout.Button("添加", GUILayout.Width(40)))
                {
                    var ret = HotfixPipelineTools.HotfixFileConfig.AddConfigItem(addTag);
                    if (ret)
                    {
                        var config = HotfixPipelineTools.HotfixFileConfig.GetConfig(addTag);
                        config.AddFolderFilter("Assets", ".xxx");
                    }

                    EditorUtility.DisplayDialog("提示", "添加:" + (ret ? "成功" : "失败"), "OK");
                }
            }
            GUILayout.EndHorizontal();

            //渲染所有的配置
            EditorGUILayoutEx.Layout_DrawLineH(Color.white, 2f);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("已存在配置", GUILayout.Width(200));
                GUILayout.Label("默认配置类型", GUILayout.Width(200));
            }
            GUILayout.EndHorizontal();
            var configs = HotfixPipelineTools.HotfixFileConfig.GetAllConfig();
            GUILayout.BeginHorizontal();
            {
                var tags = configs.Select((con) => con.Tag).ToArray();
                //
                if (tags.Length > 0)
                {
                    curSelectTagIdx = EditorGUILayout.Popup(curSelectTagIdx, tags, GUILayout.Width(200));
                }
                else
                {
                    GUILayout.Label("无配置", GUILayout.Width(200));
                }

                this.curSlectConfigItem = configs.FirstOrDefault((c) => c.Tag == tags[curSelectTagIdx]);
                if (curSlectConfigItem == null)
                {
                    this.curSlectConfigItem = new HotfixFileConfigLogic.HotfixFileConfigItem();
                }

                this.curSlectConfigItem.DefeaultConfigType = (HotfixFileConfigLogic.HotfixFileConfigItem.DefeaultConfigTypeEnum) EditorGUILayout.EnumPopup(this.curSlectConfigItem.DefeaultConfigType, GUILayout.Width(200));
                //添加按钮
                GUILayout.Space(20);

                GUI.color = Color.green;
                if (GUILayout.Button("添加目录", GUILayout.Width(75)))
                {
                    curSlectConfigItem.AddFolderFilter("null", "xx");
                }

                GUI.color = GUI.backgroundColor;

                GUILayout.Space(20);
                GUI.color = Color.red;
                if (GUILayout.Button("删除", GUILayout.Width(75)))
                {
                    var ret = EditorUtility.DisplayDialog("提示", "是否删除该配置？", "OK", "Cancel");
                    if (ret)
                    {
                        HotfixPipelineTools.HotfixFileConfig.RemoveConfigItem(this.curSlectConfigItem.Tag);
                    }
                }

                GUI.color = GUI.backgroundColor;
            }
            GUILayout.EndHorizontal();
            //开始排版
            GUILayout.BeginHorizontal(GUILayout.Width(w));
            {
                GUILayout.Label("路径", GUILayout.Width(400));
                GUILayout.Space(10);
                GUILayout.Label("后缀", GUILayout.Width(100));
            }
            GUILayout.EndHorizontal();


            //显示所有的floder
            foreach (var floderFilter in this.curSlectConfigItem.GetFloderFilters())
            {
                GUILayout.BeginHorizontal(GUILayout.Width(w));
                {
                    //添加目录
                    GUILayout.Label(floderFilter.FloderPath, GUILayout.Width(380));
                    if (GUILayout.Button("...", GUILayout.Width(20)))
                    {
                        var folder = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "");
                        floderFilter.FloderPath = folder.Replace(BDApplication.ProjectRoot + "/", "");

                        var files = Directory.GetFiles(floderFilter.FloderPath, "*", SearchOption.AllDirectories);

                        if (files.Length > 0)
                        {
                            floderFilter.FileExtensionName = Path.GetExtension(files[0]);
                        }
                    }

                    GUILayout.Space(10);
                    //添加的后缀名
                    floderFilter.FileExtensionName = EditorGUILayout.TextField(floderFilter.FileExtensionName, GUILayout.Width(80));
                    if (!floderFilter.FileExtensionName.StartsWith("."))
                    {
                        floderFilter.FileExtensionName = ("." + floderFilter.FileExtensionName);
                    }

                    GUILayout.Space(20);

                    GUI.color = Color.red;
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        var ret = EditorUtility.DisplayDialog("提示", "是否删除该目录？", "OK", "Cancel");
                        if (ret)
                        {
                            this.curSlectConfigItem.RemoveFloderFilter(floderFilter.FloderPath);
                            return;
                        }
                    }

                    GUI.color = GUI.backgroundColor;
                }
                GUILayout.EndHorizontal();
            }


            if (configs.Length < 10)
            {
                GUILayout.Space(20 * (10 - configs.Length));
            }

            GUILayout.EndVertical();
        }

        Vector2 pos1 = Vector2.zero;
        Vector2 pos2 = Vector2.zero;

        /// <summary>
        /// 绘制文件列表
        /// </summary>
        private void ONGUI_DrawFileList()
        {
            if (curSlectConfigItem == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("非热更文件:", GUILayout.Width(395));
            EditorGUILayoutEx.Layout_DrawLineV(Color.white, 2f);
            GUILayout.Label("热更文件:", GUILayout.Width(400));
            GUILayout.EndHorizontal();

            EditorGUILayoutEx.Layout_DrawLineH(Color.white, 2f);
            GUILayout.BeginHorizontal();
            //左边
            pos1 = EditorGUILayout.BeginScrollView(pos1, false, false, GUILayout.Width(398), GUILayout.Height(700));
            {
                var hotfixfiles = curSlectConfigItem.GetNotHotfixFiles();
                foreach (var file in hotfixfiles)
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(398));
                    {
                        GUILayout.Label(file, GUILayout.Width(370));
                        if (GUILayout.Button(">"))
                        {
                            var ret = EditorUtility.DisplayDialog("提示", "是否添加热更文件？", "OK", "Cancel");
                            if (ret)
                            {
                                curSlectConfigItem.AddHotfixFile(file);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                    EditorGUILayoutEx.Layout_DrawLineH(Color.grey, 1f);
                }
            }
            EditorGUILayout.EndScrollView();


            EditorGUILayoutEx.Layout_DrawLineV(Color.white, 2f);
            //右边
            pos2 = EditorGUILayout.BeginScrollView(pos2, false, false, GUILayout.Width(398), GUILayout.Height(700));
            {
                var nothotfixFiles = curSlectConfigItem.GetHotfixFiles();
                for (int i = 0; i < nothotfixFiles.Length; i++)
                {
                    var file = nothotfixFiles[i];

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("<"))
                        {
                            var ret = EditorUtility.DisplayDialog("提示", "是否移除热更列表？", "OK", "Cancel");
                            if (ret)
                            {
                                curSlectConfigItem.RemoveHotfixFile(file);
                                break;
                            }
                        }

                        GUILayout.Label(file, GUILayout.Width(370));
                    }
                    GUILayout.EndHorizontal();
                    EditorGUILayoutEx.Layout_DrawLineH(Color.grey, 1f);
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.EndHorizontal();
        }

        private void OnDisable()
        {
            //保存
            HotfixPipelineTools.Save();
        }
    }
}
