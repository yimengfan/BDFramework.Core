using System;
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

        private HotfixFileConfigLogic.HotfixFileConfigItem curSlectConfigItem = null;

        private string addTag = "none";
        private string addFolder = "none";
        private string addExtension = "";

        private void ONGUI_DrawAllConfig()
        {
            GUILayout.BeginVertical();
            //渲染所有的配置
            EditorGUILayout.LabelField("热更文件配置:");

            GUILayout.BeginHorizontal(GUILayout.Width(w));
            {
                EditorGUILayout.LabelField("Tag", GUILayout.Width(100));
                EditorGUILayout.LabelField("路径", GUILayout.Width(300));
                EditorGUILayout.LabelField("后缀", GUILayout.Width(100));
            }
            GUILayout.EndHorizontal();

            //添加配置
            GUILayout.BeginHorizontal(GUILayout.Width(w));
            {
                // Tag
                addTag = EditorGUILayout.TextField(addTag, GUILayout.Width(100));

                //添加目录
                EditorGUILayout.LabelField(addFolder, GUILayout.Width(280));
                if (GUILayout.Button("...", GUILayout.Width(20)))
                {
                    var folder = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");

                    addFolder = folder.Replace(BDApplication.ProjectRoot + "/", "");
                }

                //添加的后缀名
                addExtension = EditorGUILayout.TextField(addExtension, GUILayout.Width(80));
                if (!addExtension.StartsWith("."))
                {
                    addExtension = ("." + addExtension);
                }

                GUILayout.Space(20);
                //添加
                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    var ret = HotfixPipelineTools.HotfixFileConfig.AddConfigItem(addTag, addFolder, addExtension);

                    EditorUtility.DisplayDialog("提示", "添加:" + (ret ? "成功" : "失败"), "OK");
                }
            }
            GUILayout.EndHorizontal();

            //渲染所有的配置
            EditorGUILayoutEx.Layout_DrawLineH(Color.white, 2f);
            GUILayout.Label("已存在配置:");
            var configs = HotfixPipelineTools.HotfixFileConfig.GetAllConfig();
            for (int i = 0; i < configs.Length; i++)
            {
                var item = configs[i];
                if (item.Equals(curSlectConfigItem))
                {
                    GUI.color = Color.red;
                }

                GUILayout.BeginHorizontal(GUILayout.Width(w));
                {
                    item.Tag = EditorGUILayout.TextField(item.Tag, GUILayout.Width(100));

                    GUILayout.Label(item.FloderPath, GUILayout.Width(300));

                    GUILayout.Label(item.FileExtensionName, GUILayout.Width(100));

                    if (GUILayout.Button("选择", GUILayout.Width(40)))
                    {
                        curSlectConfigItem = item;
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(40)))
                    {
                        HotfixPipelineTools.HotfixFileConfig.RemoveConfigItem(item.Tag);
                        break;
                    }
                }
                GUILayout.EndHorizontal();
                GUI.color = GUI.backgroundColor;
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
            GUILayout.Label("热更文件:",GUILayout.Width(395));
            EditorGUILayoutEx.Layout_DrawLineV(Color.white, 2f);
            GUILayout.Label("非热更文件:",GUILayout.Width(400));
            GUILayout.EndHorizontal();
            
            EditorGUILayoutEx.Layout_DrawLineH(Color.white, 2f);
            GUILayout.BeginHorizontal();
            //左边
            pos1 = EditorGUILayout.BeginScrollView(pos1, false, false, GUILayout.Width(398),GUILayout.Height(700));
            {
                foreach (var file in curSlectConfigItem.GetHotfixFiles())
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(398));
                    {
                        GUILayout.Label(file, GUILayout.Width(370));
                        if (GUILayout.Button(">"))
                        {
                            var ret = EditorUtility.DisplayDialog("提示", "是否添加到非热更列表？", "OK", "Cancel");
                            if (ret)
                            {
                                curSlectConfigItem.AddNotHotfixFile(file);
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
            pos2 = EditorGUILayout.BeginScrollView(pos2, false, false, GUILayout.Width(398),GUILayout.Height(700));
            {
                for (int i = 0; i < curSlectConfigItem.NotHotFixFileList.Count; i++)
                {
                    var file = curSlectConfigItem.NotHotFixFileList[i];
                    
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(file, GUILayout.Width(370));
                        if (GUILayout.Button("X"))
                        {
                            var ret = EditorUtility.DisplayDialog("提示", "是否从非热更列表移除？", "OK", "Cancel");
                            if (ret)
                            {
                                curSlectConfigItem.RemoveNotHotfixFile(file);
                                break;
                            }
                        }
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