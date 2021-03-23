using System;
using System.Collections;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace BDFramework
{
    public enum AssetLoadPath
    {
        Editor = 0,
        Persistent,
        StreamingAsset
    }

//
    public enum HotfixCodeRunMode
    {
        ByILRuntime = 0,
        ByReflection,
    }

    public enum AssetBundleManagerVersion
    {
        V1,
        V2,
    }

    public class GameConfig
    {
        [LabelText("代码路径")]
        public AssetLoadPath CodeRoot = AssetLoadPath.Editor;

        [LabelText("SQLite路径")]
        public AssetLoadPath SQLRoot = AssetLoadPath.Editor;

        [LabelText("资源路径")]
        public AssetLoadPath ArtRoot = AssetLoadPath.Editor;

        [InfoBox("StreamingAsset下生效")]
        [LabelText("配置到其他路径")]
        public string CustomArtRoot = "";

        [LabelText("热更代码执行模式")]
        public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ByILRuntime;

        [LabelText("AssetBundleManager版本")]
        public AssetBundleManagerVersion AssetBundleManagerVersion = AssetBundleManagerVersion.V1;

        [LabelText("是否开启ILRuntime调试")]
        public bool IsDebuggerILRuntime = false;

        [LabelText("是否执行热更单元测试")]
        public bool IsExcuteHotfixUnitTest = false;

        [LabelText("文件服务器")]
        public string FileServerUrl = "192.168.8.68";

        [LabelText("Gate服务器")]
        public string GateServerIp = "";

        public int Port;

        [LabelText("是否热更")]
        public bool IsHotfix = false;

        [LabelText("是否联网")]
        public bool IsNeedNet = false;
    }

    public class Config :
#if UNITY_EDITOR
        SerializedMonoBehaviour
#else
        MonoBehaviour
#endif

    {
        [HideLabel]
        [InlinePropertyAttribute]
        public GameConfig Data;

        //本地配置
        [TitleGroup("真机环境下,BDLauncher的Config不能为null！(且保持Odin设置:Editor Only,无视该面板错误)", alignment: TitleAlignments.Centered)]
        [LabelText("本地配置")]
        [InfoBox("使用内置打包工具,会自动加载内置生成Config.\n或点击【生成Config】按钮生成，拖拽赋值可预览", InfoMessageType.Warning)]
        [OnValueChanged("OnValueChanged_GameConfig")]
        public TextAsset GameConfig;


        private void Awake()
        {
            if (GameConfig != null)
            {
                var newconfig = JsonMapper.ToObject<GameConfig>(GameConfig.text);
                SetNewConfig(newconfig);
                // UseServerConfig(null);
            }
            // else if (!Application.isEditor)
            // {
            //     BDebug.LogError("不存在GameConfig,请检查!");
            // }
        }

        #region Config设置

        //
        // /// <summary>
        // /// 使用服务器配置 
        // /// </summary>
        // /// <param name="callback"></param>
        // public void UseServerConfig(Action callback)
        // {
        //     IEnumeratorTool.StartCoroutine(UpdateServerConfig(callback));
        // }
        //
        //
        // /// <summary>
        // /// 更新服务器配置
        // /// </summary>
        // /// <param name="callback"></param>
        // /// <returns></returns>
        // private IEnumerator UpdateServerConfig(Action callback)
        // {
        //     var url = string.Format("{0}/{1}/{2}", Data.FileServerUrl, BApplication.GetPlatformPath(Application.platform), "GameConfig.json");
        //     BDebug.Log(url);
        //     UnityWebRequest uwq = UnityWebRequest.Get(url);
        //     GameConfig gconfig = null;
        //     yield return uwq.SendWebRequest();
        //     if (uwq.isDone && uwq.error == null)
        //     {
        //         var text = uwq.downloadHandler.text;
        //         if (!string.IsNullOrEmpty(text))
        //         {
        //             gconfig = JsonMapper.ToObject<GameConfig>(text);
        //             BDebug.Log("使用服务器配置:\n" + text);
        //         }
        //     }
        //     else
        //     {
        //         BDebug.LogError("Game配置无法更新,使用本地");
        //     }
        //
        //     SetNewConfig(gconfig);
        //     callback?.Invoke();
        // }
        //
        // /// <summary>
        // /// 使用新的配置
        // /// </summary>
        // /// <param name="newConfig"></param>
        private void SetNewConfig(GameConfig newConfig)
        {
            if (newConfig != null)
            {
                this.Data = newConfig;
            }
        }

        #endregion


        #region 编辑器

#if UNITY_EDITOR


        [ButtonGroup("1")]
        [Button("清空Persistent", ButtonSizes.Medium)]
        public static void DeletePersistent()
        {
            Directory.Delete(Application.persistentDataPath, true);
        }

        [ButtonGroup("1")]
        [Button("生成Config", ButtonSizes.Medium)]
        public static void GenConfig()
        {
            GenGameConfig("Assets/Scenes/Config", BDApplication.GetPlatformPath(Application.platform));
        }


        /// <summary>
        /// 生成GameConfig
        /// </summary>
        /// <param name="str"></param>
        /// <param name="platform"></param>
        static public void GenGameConfig(string str, string platform)
        {
            //config
            var config = GameObject.Find("BDFrame").GetComponent<Config>();
            var json = JsonMapper.ToJson(config.Data);
            //根据不同场景生成配置
            Scene scene = EditorSceneManager.GetActiveScene();
            var fs = string.Format("{0}/{1}", str, scene.name + "_GameConfig.json");
            FileHelper.WriteAllText(fs, json);
            Debug.Log("当前场景配置：" + fs);
            AssetDatabase.Refresh();
            //
            var content = AssetDatabase.LoadAssetAtPath<TextAsset>(fs);
            config.GameConfig = content;
            var bdconfig = GameObject.Find("BDFrame").GetComponent<BDLauncher>();
            bdconfig.ConfigContent = content;

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
                var config = GameObject.Find("BDFrame").GetComponent<Config>();
                config.Data = configData;
            }

            BDebug.Log("配置改变,刷新!");
        }
#endif

        #endregion
    }
}