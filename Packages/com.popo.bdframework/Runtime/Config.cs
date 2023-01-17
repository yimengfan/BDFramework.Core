using System;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using DotNetExtension;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BDFramework
{
    public enum AssetLoadPathType
    {
        Editor = 0,

        /// <summary>
        /// 用户可读写沙盒
        /// </summary>
        Persistent,

        /// <summary>
        /// Streaming
        /// </summary>
        StreamingAsset,

        /// <summary>
        /// devop的发布目录
        /// </summary>
        DevOpsPublish
    }

    /// <summary>
    /// 热更代码执行模式
    /// </summary>
    public enum HotfixCodeRunMode
    {
        /// <summary>
        /// ILRuntime解释执行
        /// </summary>
        ILRuntime = 0,

        /// <summary>
        /// 华佗执行
        /// </summary>
        HCLR,

        /// <summary>
        /// 这里只做预留,因为OSX只支持mono方式
        /// </summary>
        Mono
    }


    [Serializable]
    public class GameConfig
    {
        [VerticalGroup("a")]
        [HorizontalGroup("a/a1")]
        [LabelText("代码路径")]
        public AssetLoadPathType CodeRoot = AssetLoadPathType.Editor;

        [LabelText("SQLite路径")]
        [HorizontalGroup("a/a2")]
        public AssetLoadPathType SQLRoot = AssetLoadPathType.Editor;

        [LabelText("资源路径")]
        [HorizontalGroup("a/a3")]
        public AssetLoadPathType ArtRoot = AssetLoadPathType.Editor;


        [LabelText("热更代码执行模式")]
        [HorizontalGroup("a/a4")]
        public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ILRuntime;

        [LabelText("是否开启ILRuntime调试")]
        [HorizontalGroup("a/a5")]
        public bool IsDebuggerILRuntime = false;

        [LabelText("是否打印日志")]
        [HorizontalGroup("a/a6")]
        public bool IsDebugLog = true;


        [LabelText("文件服务器")]
        [HorizontalGroup("a/a7")]
        public string FileServerUrl = "192.168.8.68";

        [LabelText("Gate服务器")]
        [HorizontalGroup("a/a8")]
        public string GateServerIp = "";

        [HorizontalGroup("a/a9")]
        public int Port;

        [LabelText("是否热更")]
        [HorizontalGroup("a/a10")]
        public bool IsHotfix = false;

        [LabelText("是否联网")]
        [HorizontalGroup("a/a11")]
        public bool IsNeedNet = false;

        [Space(5)]
        [LabelText("客户端版本")]
        [HorizontalGroup("a/a12")]
        public string ClientVersionNum = "0.0.0";

#if UNITY_EDITOR
        [HorizontalGroup("a/a12", width: 150)]
        [LabelText("更新至所有配置")]
        [Button]
        [GUIColor(0, 1, 0)]
        public void UpdateClientToAllConfig()
        {
            Config.UpdateAllCofnigClientVersion(ClientVersionNum);
        }
#endif
        /// <summary>
        /// 获取加载路径
        /// </summary>
        /// <param name="assetLoadPathType"></param>
        static public string GetLoadPath(AssetLoadPathType assetLoadPathType)
        {
            var path = "";
            //Editor下按照加载路径区分
            if (Application.isEditor)
            {
                switch (assetLoadPathType)
                {
                    case AssetLoadPathType.Persistent:
                        path = Application.persistentDataPath;
                        break;
                    case AssetLoadPathType.Editor:
                    case AssetLoadPathType.StreamingAsset:
                    {
                        path = Application.streamingAssetsPath;
                    }
                        break;
                    case AssetLoadPathType.DevOpsPublish:
                    {
                        path = BApplication.DevOpsPublishAssetsPath;
                    }
                        break;
                }
            }
            else
            {
                //真机环境默认都在persistent下，
                //因为需要io.不在的各个模块会自行拷贝
                path = Application.persistentDataPath;
            }

            return path;
        }
    }

    /// <summary>
    /// 游戏进入的config
    /// </summary>
    public class Config : MonoBehaviour
    {
        /// <summary>
        /// 配置path
        /// </summary>
        static public string CONFIG_PATH = "Assets/Scenes/Config";

        [HideLabel]
        [InlinePropertyAttribute]
#if UNITY_EDITOR
        [OnInspectorGUI("_ONGUI")]
#endif
        public GameConfig Data;

        /// <summary>
        /// 设置新配置
        /// </summary>
        /// <param name="gameConfig"></param>
        public void SetNewConfig(string gameConfig)
        {
            this.Data = JsonMapper.ToObject<GameConfig>(gameConfig);
        }

        #region 编辑器

#if UNITY_EDITOR
        static bool isGuiChanged = false;

        private void OnValidate()
        {
            isGuiChanged = true;
        }

        //TODO 触发这个函数的时候，有些值没被修改，暂时这样让他刷新


        private static float lastSaveTime = -1;

        public void _ONGUI()
        {
            if (!Application.isPlaying && isGuiChanged)
            {
                var asset = this.gameObject.GetComponent<BDLauncher>().ConfigText;
                var path = AssetDatabase.GetAssetPath(asset.GetInstanceID());

                //防止一些情况下不同的config覆盖
                if (path == curSelectConfigPath)
                {
                    //10s保存一次
                    if (Time.realtimeSinceStartup - lastSaveTime > 5)
                    {
                        lastSaveTime = Time.realtimeSinceStartup;
                        isGuiChanged = false;
                        //保存
                        SaveGameConfig(path, this.gameObject.GetComponent<Config>().Data);
                    }
                }
                //重新select
                else
                {
                    SelectNewConfig(path);
                }
            }
            else
            {
                lastSaveTime = Time.realtimeSinceStartup;
            }
        }


        [PropertySpace(20)]
        [InfoBox("真机环境下,BDLauncher的Config不能为null！", InfoMessageType.Info)]
        //[ ("", alignment: TitleAlignments.Centered)]
        [LabelText("生成配置名")]
        [OnInspectorGUI("ONGUI_SelcectConfig")]
        public string ConfigFileName = "Default";

        /// <summary>
        /// 当前选择的idx
        /// </summary>
        private static int curSelectConfigIdx = -1;

        private static string curSelectConfigPath = "";

        //上次打开scene名
        private static string lastSceneName = "";


        //选择配置
        static public void ONGUI_SelcectConfig()
        {
            if (Application.isPlaying)
            {
                return;
            }

            GUI.color = Color.green;
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

            var configPathList = GetConfigPaths().ToList();
            //开始赋值
            var curConfigPath = AssetDatabase.GetAssetPath(launcher.ConfigText.GetInstanceID());
            if (curSelectConfigIdx == -1)
            {
                curSelectConfigIdx = configPathList.FindIndex((s) => s == curConfigPath);
            }

            //容错
            if (curSelectConfigIdx == -1)
            {
                curSelectConfigIdx = 0;
            }

            //渲染下拉
            var configNames = configPathList.Select((s) => Path.GetFileName(s)).ToArray();
            var newSelectIdx = EditorGUILayout.Popup("选择配置:", curSelectConfigIdx, configNames);
            //选择新Config
            if (configPathList[newSelectIdx] != curConfigPath)
            {
                curSelectConfigIdx = newSelectIdx;
                //保存当前
                if (isGuiChanged)
                {
                    SaveGameConfig(curConfigPath, GameObject.FindObjectOfType<Config>().Data);
                }

                //选择新配置
                SelectNewConfig(configPathList[curSelectConfigIdx]);
            }

            GUI.color = GUI.backgroundColor;
        }


        /// <summary>
        /// 选择新Config
        /// </summary>
        private static void SelectNewConfig(string configPath)
        {
            var launcher = GameObject.FindObjectOfType<BDLauncher>();
            //赋值
            var assetText = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
            if (assetText)
            {
                launcher.ConfigText = assetText;
                curSelectConfigPath = configPath;
                //设置新的配置内容
                var config = GameObject.FindObjectOfType<Config>();
                config.SetNewConfig(assetText.text);
                //设置脏数据
                EditorUtility.SetDirty(config.gameObject);
            }
            else
            {
                Debug.LogError($"不存在Config:{configPath}");
            }
        }


        [ButtonGroup("1")]
        [Button("清空Persistent", ButtonSizes.Small)]
        public static void DeletePersistent()
        {
            Directory.Delete(Application.persistentDataPath, true);
        }

        [ButtonGroup("1")]
        [Button("生成Config", ButtonSizes.Small)]
        public static void GenConfig()
        {
            var config = GameObject.FindObjectOfType<Config>();
            var savePath = $"{CONFIG_PATH}/{config.ConfigFileName}.bytes";
            SaveGameConfig(savePath, config.Data);
            EditorUtility.DisplayDialog("提示:", "生成:Assets/Scenes/Config/" + config.ConfigFileName, "OK");
        }


        /// <summary>
        /// 生成GameConfig
        /// </summary>
        /// <param name="dirt"></param>
        /// <param name="platform"></param>
        static public void SaveGameConfig(string configPath, GameConfig conf)
        {
            if (Application.isPlaying)
            {
                return;
            }

            //写入本地
            FileHelper.WriteAllText(configPath, JsonMapper.ToJson(conf));
            //
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
            var bdconfig = GameObject.FindObjectOfType<BDLauncher>();
            bdconfig.ConfigText = textAsset;
            AssetDatabase.Refresh();
            Debug.LogFormat("[{0}] 修改配置保存成功: {1} - {2}", EditorSceneManager.GetActiveScene().name, configPath, Time.frameCount);


            // 
        }


        /// <summary>
        /// 值变动
        /// </summary>
        /// <param name="text"></param>
        static public void OnValueChanged_GameConfig(TextAsset text)
        {
            if (text)
            {
                var configData = JsonMapper.ToObject<GameConfig>(text.text);
                var config = GameObject.FindObjectOfType<Config>();
                config.Data = configData;
            }

            BDebug.Log("配置改变,刷新!");
        }


        /// <summary>
        /// 更新所有配置client——version
        /// </summary>
        /// <param name="clientversion"></param>
        static public void UpdateAllCofnigClientVersion(string clientversion)
        {
            var configPaths = GetConfigPaths();

            foreach (var cp in configPaths)
            {
               Debug.Log($"更新:{Path.GetFileName(cp)} ClientVersion至{clientversion}！");

                var json = File.ReadAllText(cp);
                var config = JsonMapper.ToObject<GameConfig>(json);
                config.ClientVersionNum = clientversion;
                var newdata = JsonMapper.ToJson(config);
                FileHelper.WriteAllText(cp, newdata);
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 获取所有配置path
        /// </summary>
        /// <returns></returns>
        static public string[] GetConfigPaths()
        {

            var configList = Directory.GetFiles(CONFIG_PATH, "*", SearchOption.AllDirectories).Select((s) => s.Replace("\\", "/"))
                .Where((s)=>Path.GetExtension(s)!= ".meta").ToList();

            return configList.ToArray();
        }
#endif

        #endregion
    }
}
