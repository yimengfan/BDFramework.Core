using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// 配置path
        /// </summary>
        static public string CONFIG_PATH = "Assets/Scenes/Config";

        /// <summary>
        /// config实例map缓存
        /// </summary>
        private Dictionary<Type, Tuple<object, PropertyTree>> configInstanceMap = new Dictionary<Type, Tuple<object, PropertyTree>>();

        public override void OnInspectorGUI()
        {
            var config = target as BDFramework.Config;

            //渲染Select config
            ONGUI_SelcectConfig();

            //渲染具体配置
            ONGUI_ConfigProperty();
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

            configPathList = GetConfigPaths().ToList();

            //容错,各种异常情况判断
            if (launcher.ConfigText != null)
            {
                var curConfigPath = AssetDatabase.GetAssetPath(launcher.ConfigText.GetInstanceID());

                var idx = configPathList.FindIndex(c => c == curConfigPath);
                if (idx != curSelectConfigIdx)
                {
                    curSelectConfigIdx = idx;
                    //
                    SelectNewConfig(configPathList[curSelectConfigIdx]);
                }
            }
            if (launcher.ConfigText == null ||  curSelectConfigIdx == -1 || curSelectConfigIdx > configPathList.Count)
            {
                curSelectConfigIdx = 0;
                SelectNewConfig(configPathList[curSelectConfigIdx]);
            }


            //渲染下拉
            var configNames = configPathList.Select((s) => Path.GetFileName(s)).ToArray();
            var newSelectIdx = EditorGUILayout.Popup("选择配置:", curSelectConfigIdx, configNames);
            if (configPathList.Count < newSelectIdx)
            {
                newSelectIdx = 0;
            }


            //选择新Config
            if (configPathList.Count > 0 && newSelectIdx != curSelectConfigIdx)
            {
                curSelectConfigIdx = newSelectIdx;
                //
                SelectNewConfig(configPathList[curSelectConfigIdx]);
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
                if (key == this.curSelectConfigType)
                    GUI.color = Color.green;
                if (GUILayout.Button(attr.Title))
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

            SirenixEditorGUI.Title("配置属性","", TextAlignment.Left,true);
            this.configInstanceMap[this.curSelectConfigType].Item2.Draw(false);
        }


        /// <summary>
        /// 获取所有配置path
        /// </summary>
        /// <returns></returns>
        static public string[] GetConfigPaths()
        {
            var configList = Directory.GetFiles(CONFIG_PATH, "*.bytes", SearchOption.AllDirectories).Select((s) => s.Replace("\\", "/"))
                .Where((s) => Path.GetExtension(s) != ".meta").ToList();

            return configList.ToArray();
        }

        /// <summary>
        /// 创建配置
        /// </summary>
        /// <param name="configName"></param>
        private void CreateConfig(string configName)
        {
            List<ConfigDataBase> datalist = new List<ConfigDataBase>();
            //type
            var allconfigtype = GameConfigManager.Inst.GetAllClassDatas();
            foreach (var cd in allconfigtype)
            {
                var configType = cd.Type.GetNestedType("Config");
                if (configType != null)
                {
                    var configInstance = Activator.CreateInstance(configType) as ConfigDataBase;
                    datalist.Add(configInstance);
                }
            }

            //保存默认的
            SaveConfig(IPath.Combine(CONFIG_PATH, configName + ".bytes"), datalist);
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

            var jsonConfig = JsonMapper.ToJson(configList, true);
            FileHelper.WriteAllText(filePath, jsonConfig);
            //
            Debug.Log("保存成功!");
        }

        /// <summary>
        /// 选择新Config
        /// </summary>
        private void SelectNewConfig(string configPath)
        {
            //赋值
            var assetText = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
            var (datalist, processorlist) = GameConfigManager.Inst.LoadConfig(assetText.text);
            //赋值新的
            configInstanceMap = new Dictionary<Type, Tuple<object, PropertyTree>>();
            var allconfigtype = GameConfigManager.Inst.GetAllClassDatas();
            foreach (var cd in allconfigtype)
            {
                var nestedType = cd.Type.GetNestedType("Config");
                if (nestedType != null)
                {
                    //寻找本地配置
                    var configData = datalist.FirstOrDefault((c) => c.ClassType == nestedType.FullName);
                    //不存在则创建新的
                    if (configData == null)
                    {
                        configData = Activator.CreateInstance(nestedType) as ConfigDataBase;
                    }

                    configInstanceMap[cd.Type] = new Tuple<object, PropertyTree>(configData, PropertyTree.Create(configData));
                }
            }
        }
    }
}
