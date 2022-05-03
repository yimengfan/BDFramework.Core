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
        Huatuo
    }


    [Serializable]
    public class GameConfig
    {
        [LabelText("代码路径")]
        public AssetLoadPathType CodeRoot = AssetLoadPathType.Editor;

        [LabelText("SQLite路径")]
        public AssetLoadPathType SQLRoot = AssetLoadPathType.Editor;

        [LabelText("资源路径")]
        public AssetLoadPathType ArtRoot = AssetLoadPathType.Editor;


        [LabelText("热更代码执行模式")]
        public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ILRuntime;

        [LabelText("是否开启ILRuntime调试")]
        public bool IsDebuggerILRuntime = false;

        [LabelText("是否打印日志")]
        public bool IsDebugLog = true;


        [LabelText("文件服务器")]
        public string FileServerUrl = "192.168.8.68";

        [LabelText("Gate服务器")]
        public string GateServerIp = "";

        public int Port;

        [LabelText("是否热更")]
        public bool IsHotfix = false;

        [LabelText("是否联网")]
        public bool IsNeedNet = false;

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
                //真机环境默认都在persistent下，因为需要io.不在的各个模块会自行拷贝
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
        static bool isChangedData = false;

        private void OnValidate()
        {
            isChangedData = true;
        }

        //TODO 触发这个函数的时候，有些值没被修改，暂时这样让他刷新
        public void _ONGUI()
        {
            if (isChangedData)
            {
                isChangedData = false;
                //
                var asset = this.gameObject.GetComponent<BDLauncher>().ConfigText;
                var path = AssetDatabase.GetAssetPath(asset.GetInstanceID());
                GenGameConfig(Path.GetDirectoryName(path), asset.name);
            }
        }


        [TitleGroup("真机环境下,BDLauncher的Config不能为null！", alignment: TitleAlignments.Centered)]
        [LabelText("生成配置名")]
        [OnInspectorGUI("ONGUI_SelcectConfig")]
        public string ConfigFileName = "Default";

        /// <summary>
        /// 当前选择的idx
        /// </summary>
        private static int curSlectConfigIdx = -1;
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
                curSlectConfigIdx = -1;
                lastSceneName = activeScene.name;
            }
            //开始赋值
            var curFilePath = AssetDatabase.GetAssetPath(launcher.ConfigText.GetInstanceID());
            var direct = Path.GetDirectoryName(curFilePath);
            var configList = Directory.GetFiles(direct, "*.json", SearchOption.AllDirectories).Select((s) => s.Replace("\\", "/")).ToList();
            var configNames = configList.Select((s) => Path.GetFileName(s)).ToArray();
            if (curSlectConfigIdx == -1)
            {
                curSlectConfigIdx = configList.FindIndex((s) => s == curFilePath);
            }

            curSlectConfigIdx = EditorGUILayout.Popup("选择配置:", curSlectConfigIdx, configNames);
            if (configList[curSlectConfigIdx] != curFilePath)
            {
                var assetText = AssetDatabase.LoadAssetAtPath<TextAsset>(configList[curSlectConfigIdx]);
                launcher.ConfigText = assetText;
                //设置新的配置内容
                var config = GameObject.FindObjectOfType<Config>();
                config.SetNewConfig(assetText.text);
                
                //设置脏数据
                EditorUtility.SetDirty(config.gameObject);
            }

            GUI.color = GUI.backgroundColor;
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
            GenGameConfig("Assets/Scenes/Config", config.ConfigFileName);

            EditorUtility.DisplayDialog("提示:", "生成:Assets/Scenes/Config/" + config.ConfigFileName, "OK");
        }


        /// <summary>
        /// 生成GameConfig
        /// </summary>
        /// <param name="str"></param>
        /// <param name="platform"></param>
        static public void GenGameConfig(string str, string filename)
        {
            if (Application.isPlaying)
            {
                return;
            }

            //config
            var config = GameObject.FindObjectOfType<Config>();
            if (config == null)
            {
                return;
            }

            var json = JsonMapper.ToJson(config.Data);
            //根据不同场景生成配置
            //Scene scene = EditorSceneManager.GetActiveScene();
            var fs = string.Format("{0}/{1}", str, filename + ".json");
            FileHelper.WriteAllText(fs, json);
            AssetDatabase.Refresh();
            //
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fs);
            var bdconfig = GameObject.FindObjectOfType<BDLauncher>();
            if (bdconfig.ConfigText.name != filename)
            {
                bdconfig.ConfigText = textAsset;
            }

            Debug.LogFormat("[{0}] 修改配置保存成功: {1}.json - {2}", EditorSceneManager.GetActiveScene().name, filename, Time.frameCount);
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
#endif

        #endregion
    }
}
