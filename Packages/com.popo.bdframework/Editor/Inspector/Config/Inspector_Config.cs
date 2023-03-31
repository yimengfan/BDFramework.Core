#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Configure;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BDFramework.Editor.Inspector.Config
{
    [CustomEditor(typeof(BDFramework.Config))]
    public class Inspector_Config : UnityEditor.Editor
    {
        /// <summary>
        /// config实例map缓存
        /// </summary>
        private Dictionary<Type, Tuple<ConfigDataBase, PropertyTree>> configInstanceMap = new Dictionary<Type, Tuple<ConfigDataBase, PropertyTree>>();

        public override void OnInspectorGUI()
        {
            var config = target as BDFramework.Config;

            //渲染Select config
            ONGUI_SelcectConfig();
            //渲染具体配置
            ONGUI_ConfigProperty();
            //
            ONGUI_Bottom();
        }


        //上次打开scene名
        private static string lastSceneName = "";

        /// <summary>
        /// 当前选择的idx
        /// </summary>
        private int curSelectConfigIdx = -1;

        /// <summary>
        /// 当前选择的config
        /// </summary>
        private string curSelectConfigPath = "";

        /// <summary>
        /// 当前选择配置类型
        /// </summary>
        private Type curSelectConfigType = typeof(GameBaseConfigProcessor);


        /// <summary>
        /// 配置列表
        /// </summary>
        private Dictionary<string, string> configMap = new Dictionary<string, string>();

        /// <summary>
        /// 选择配置
        /// </summary>
        public void ONGUI_SelcectConfig()
        {
            var launcher = GameObject.FindObjectOfType<BDLauncher>();
            if (launcher == null)
            {
                BDebug.LogError("场景上没找到BDLauncher");
                return;
            }

            //判断是否切换场景，防止数据污染！
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!lastSceneName.Equals(activeScene.name))
            {
                curSelectConfigIdx = -1;
                lastSceneName = activeScene.name;
            }

            //开始赋值
            var configPathList = ConfigEditorUtil.GetConfigPaths().ToList();
            //创建必须存在的配置
            if (!configPathList.Exists(c => c.EndsWith("debug" + ConfigEditorUtil.FILE_SUFFIX, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigEditorUtil.CreateConfig("Debug");
            }

            if (!configPathList.Exists(c => c.EndsWith("release" + ConfigEditorUtil.FILE_SUFFIX, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigEditorUtil.CreateConfig("Release");
            }

            if (!configPathList.Exists(c => c.EndsWith("editor" + ConfigEditorUtil.FILE_SUFFIX, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigEditorUtil.CreateConfig("Editor");
            }

            var newlist = ConfigEditorUtil.GetConfigPaths().ToList();
            if (newlist.Count != configPathList.Count)
            {
                configPathList = newlist;
                AssetDatabase.Refresh();
            }


            //容错,各种异常情况判断
            if (launcher.ConfigText != null)
            {
                var curConfigPath = AssetDatabase.GetAssetPath(launcher.ConfigText.GetInstanceID());

                var idx = configPathList.FindIndex(c => c == curConfigPath);
                if (idx != curSelectConfigIdx)
                {
                    curSelectConfigIdx = idx;
                    //
                    SelectNewConfig(configPathList[curSelectConfigIdx], false);
                }
            }

            if (launcher.ConfigText == null || curSelectConfigIdx == -1 || curSelectConfigIdx > configPathList.Count)
            {
                curSelectConfigIdx = 0;
                SelectNewConfig(configPathList[curSelectConfigIdx], false);
            }


            //渲染下拉
            var configNames = configPathList.Select((s) => Path.GetFileName(s)).ToArray();
            var newSelectIdx = EditorGUILayout.Popup("选择配置:", curSelectConfigIdx, configNames);
            if (configPathList.Count < newSelectIdx)
            {
                newSelectIdx = 0;
            }

            GUILayout.Label($"Tips: Editor{ConfigEditorUtil.FILE_SUFFIX} 会作为editor下框架工具默认获取的配置!");

            //保存configMap
            for (int i = 0; i < configNames.Length; i++)
            {
                configMap[configNames[i]] = configPathList[i];
            }

            //选择新Config
            if (newSelectIdx != curSelectConfigIdx)
            {
                curSelectConfigIdx = newSelectIdx;
                //
                SelectNewConfig(configPathList[curSelectConfigIdx], true);
            }

            if (this.configInstanceMap.Count == 0)
            {
                SelectNewConfig(configPathList[curSelectConfigIdx], false);
            }

            GUILayout.Space(10);
        }

        private string curSelectConfigButonName = "框架";

        /// <summary>
        /// 渲染config属性
        /// </summary>
        public void ONGUI_ConfigProperty()
        {
            var keys = this.configInstanceMap.Keys.ToArray();
            for (int i = 1; i <= keys.Length; i++)
            {
                var key = keys[i - 1];
                if (i % 3 == 1)
                {
                    GUILayout.BeginHorizontal();
                }

                //渲染按钮
                var attr = key.GetCustomAttribute<GameConfigAttribute>();
                if (key == this.curSelectConfigType)
                {
                    GUI.color = Color.red;
                }

                GUIContent content = new GUIContent(attr.Title, key.Name + ".cs");
                if (GUILayout.Button(content))
                {
                    this.curSelectConfigType = key;
                    curSelectConfigButonName = attr.Title;
                }

                GUI.color = GUI.backgroundColor;
                //
                if (i % 3 == 0 || i == keys.Length)
                {
                    GUILayout.EndHorizontal();
                }
            }

            SirenixEditorGUI.Title("配置属性", "", TextAlignment.Left, true);
            this.configInstanceMap.TryGetValue(this.curSelectConfigType, out var inst);
            if (inst != null)
            {
                inst.Item2.Draw(false);
            }
            else
            {
                EditorGUILayout.HelpBox("等待GameSettingManager初始化!", MessageType.Error);
            }
        }


        private string configName = "newConfig";

        /// <summary>
        /// bottom渲染
        /// </summary>
        public void ONGUI_Bottom()
        {
            GUILayout.Space(20);
            SirenixEditorGUI.Title("操作", "", TextAlignment.Left, true);
            GUILayout.BeginHorizontal();
            {
                //同步当前到其他
                GUI.color = Color.yellow;
                if (GUILayout.Button($"同步[{curSelectConfigButonName}]配置", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("提示", $"只会同步当前[{curSelectConfigButonName}]配置至当其他Config", "OK", "Cancel"))
                    {
                        this.configInstanceMap.TryGetValue(this.curSelectConfigType, out var inst);
                        //更新当前数据到所有
                        ConfigEditorUtil.UpdateConfigDataToAll(inst.Item1);
                    }
                }

                GUI.color = GUI.backgroundColor;

                GUILayout.Space(10);
                //保存
                if (GUILayout.Button("保存", GUILayout.Height(30)))
                {
                    SaveCurrentConfig(true);
                }

                GUILayout.Space(10);
                //创建
                configName = EditorGUILayout.TextArea(configName, GUILayout.Width(150), GUILayout.Height(25));

                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(25)))
                {
                    if (configName.Contains(".") || configName.Contains("/"))
                    {
                        EditorUtility.DisplayDialog("提示", "配置名非法", "OK");
                        return;
                    }

                    ConfigEditorUtil.CreateConfig(configName);
                }
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Clear Persistent", GUILayout.Height(20)))
                {
                    Debug.Log(Application.persistentDataPath);
                    var files = Directory.GetFiles(Application.persistentDataPath, "*");
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 选择新Config
        /// </summary>
        private void SelectNewConfig(string configPath, bool isSaveCurrentConfig)
        {
            //保存last配置
            if (isSaveCurrentConfig)
            {
                SaveCurrentConfig();
            }

            //赋值
            var assetText = File.ReadAllText(configPath);
            var map = GameConfigManager.Inst.ReadConfig(assetText);
            //赋值新的
            configInstanceMap = new Dictionary<Type, Tuple<ConfigDataBase, PropertyTree>>();

            foreach (var item in map)
            {
                configInstanceMap[item.Key] = new Tuple<ConfigDataBase, PropertyTree>(item.Value, PropertyTree.Create(item.Value));
            }

            curSelectConfigPath = configPath;
            //设置到面板
            var go = GameObject.FindObjectOfType<BDLauncher>();
            go.ConfigText = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
            //
            EditorUtility.SetDirty(go);
        }

        /// <summary>
        /// 保存当前Config
        /// </summary>
        private void SaveCurrentConfig(bool isNeedTips = false)
        {
            if (isNeedTips && !EditorUtility.DisplayDialog("提示", $"是否保存到：{curSelectConfigPath}", "OK", "Cancel"))
            {
                return;
            }

            if (!string.IsNullOrEmpty(curSelectConfigPath))
            {
                var configlist = this.configInstanceMap.Select((i) => i.Value.Item1).ToList();
                var ret = ConfigEditorUtil.SaveConfig(curSelectConfigPath, configlist);
                if (ret)
                {
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
#endif