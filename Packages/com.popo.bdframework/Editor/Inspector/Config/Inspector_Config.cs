using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BDFramework.Configure;
using LitJson;
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
      static  private string FILE_SUFFIX = ".bytes";
        /// <summary>
        /// 配置path
        /// </summary>
        static public string CONFIG_PATH = "Assets/Scenes/Config";

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
            var configPathList = GetConfigPaths().ToList();
            //创建必须存在的配置
            if (!configPathList.Exists(c => c.EndsWith("debug.bytes", StringComparison.OrdinalIgnoreCase)))
            {
                this.CreateConfig("Debug");
            }

            if (!configPathList.Exists(c => c.EndsWith("release.bytes", StringComparison.OrdinalIgnoreCase)))
            {
                this.CreateConfig("Release");
            }

            if (!configPathList.Exists(c => c.EndsWith("editor.bytes", StringComparison.OrdinalIgnoreCase)))
            {
                this.CreateConfig("Editor");
            }
            var newlist = GetConfigPaths().ToList();
            if (newlist.Count != configPathList.Count)
            {
                configPathList =newlist;
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

            //保存config
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
                if (key == this.curSelectConfigType) GUI.color = Color.yellow;

                GUIContent content = new GUIContent(attr.Title, key.Name + ".cs");
                if (GUILayout.Button(content))
                {
                    this.curSelectConfigType = key;
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
            inst?.Item2.Draw(false);
        }


        private string configName = "newConfig";
        /// <summary>
        /// bottom渲染
        /// </summary>
        public void ONGUI_Bottom()
        {
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            {
                configName = EditorGUILayout.TextArea(configName ,GUILayout.Width(200));
                GUILayout.Space(20);
                if(GUILayout.Button("创建",GUILayout.Width(60)))
                {
                    if (configName.Contains(".") || configName.Contains("/"))
                    {
                        EditorUtility.DisplayDialog("提示", "配置名非法", "OK");
                        return;
                    }
                    CreateConfig(configName);
                }
            }
            GUILayout.EndHorizontal();


            if (GUILayout.Button("保存 (修改后点击!)", GUILayout.Height(30)))
            {
                SaveCurrentConfig(true);
            }
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
        /// 获取所有配置path
        /// </summary>
        /// <returns></returns>
        static public string[] GetConfigPaths()
        {
            var configList = Directory.GetFiles(CONFIG_PATH, "*"+ FILE_SUFFIX, SearchOption.AllDirectories).Select((s) => s.Replace("\\", "/"))
                .Where((s) => Path.GetExtension(s) != ".meta").ToList();

            return configList.ToArray();
        }

        /// <summary>
        /// 创建配置
        /// </summary>
        /// <param name="configName"></param>
        private void CreateConfig(string configName)
        {
           var datalist = GameConfigManager.Inst.CreateNewConfig();
            //保存默认的
            SaveConfig(IPath.Combine(CONFIG_PATH, configName + FILE_SUFFIX), datalist);
            
            AssetDatabase.Refresh();
        }


        /// <summary>
        /// 保存Config
        /// </summary>
        /// <param name="configMap"></param>
        public void SaveConfig(string filePath, List<ConfigDataBase> configList)
        {
            foreach (var config in configList)
            {
                config.ClassType = config.GetType().FullName;
            }

            var json = JsonMapper.ToJson(configList, true);
            if (File.Exists(filePath))
            {
                var exsitFile = File.ReadAllText(filePath);
                if (exsitFile != json)
                {
                    FileHelper.WriteAllText(filePath, json);
                    //
                    Debug.Log($"覆盖成功:{filePath} \n {json}");
                }
            }
            else
            {
                FileHelper.WriteAllText(filePath, json);
                Debug.Log($"保存成功:{filePath} \n {json}");
            }

;
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
                SaveConfig(curSelectConfigPath, configlist);
            }
        }
    }
}
