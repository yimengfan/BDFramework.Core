using System;
using System.Collections;
using System.IO;
using Code.BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

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

    [Serializable]
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

        //只在非Editor模式下生效
        public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ByILRuntime;

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

    public class Config : MonoBehaviour
    {
        [HideLabel]
        [InlinePropertyAttribute]
        public GameConfig Data;
        //本地配置
        [LabelText("本地配置")]
        public TextAsset localConfig;
        
        /// <summary>
        /// 全局的单例
        /// </summary>
        static public Config Inst { get; private set; }
        private void Awake()
        {
            Inst = this;
         
            ShowFPS();
            
            if (localConfig != null)
            {
                var newconfig= JsonMapper.ToObject<GameConfig>(localConfig.text);
                SetNewConfig(newconfig);
            }
            
            UseServerConfig(null);
        }

        #region Config设置
        
        /// <summary>
        /// 使用服务器配置 
        /// </summary>
        /// <param name="callback"></param>
        public void UseServerConfig(Action callback)
        {
            IEnumeratorTool.StartCoroutine(UpdateServerConfig(callback));
        }


        /// <summary>
        /// 更新服务器配置
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator UpdateServerConfig(Action callback)
        {
            var url = string.Format("{0}/{1}/{2}", Data.FileServerUrl,BApplication.GetPlatformPath(Application.platform) ,"GameConfig.json");
            Debug.Log(url);
            UnityWebRequest uwq = UnityWebRequest.Get(url);
            GameConfig gconfig = null;
            yield return uwq.SendWebRequest();
            if (uwq.isDone && uwq.error==null)
            {
                var text = uwq.downloadHandler.text;
                if (!string.IsNullOrEmpty(text))
                {
                    gconfig = JsonMapper.ToObject<GameConfig>(text);
                    BDebug.Log("使用服务器配置:\n"+ text) ;
                }
            }
            else
            {
                BDebug.LogError("Game配置无法更新,使用本地");
            }
            
            SetNewConfig(gconfig);
            callback?.Invoke();
        }

        /// <summary>
        /// 使用新的配置
        /// </summary>
        /// <param name="newConfig"></param>
        private void SetNewConfig(GameConfig newConfig)
        {
            if (newConfig != null)
            {
                this.Data = newConfig;
            }
        }
        #endregion
        
        
        #region    FPS计算

        float fps;
        float deltaTime = 0.0f;
        float msec;
        Rect rect;
        GUIStyle style = new GUIStyle();

        void ShowFPS()
        {
            int w = Screen.width, h = Screen.height;
            rect = new Rect(w - 350, 0, 300, h * 4 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 4 / 100;
            style.normal.textColor = new Color(1.0f, 0.0f, 0f, 1.0f);
        }

        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()
        {
            msec = deltaTime * 1000.0f;
            fps = 1.0f / deltaTime;

            GUI.Label(rect, string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps), style);
        }

        #endregion

        #region 编辑器

#if UNITY_EDITOR


        [ButtonGroup("1")]
        [Button("清空Persistent",ButtonSizes.Medium)]
        public static void DeletePersistent()
        {
            Directory.Delete(Application.persistentDataPath, true);
        }

        [ButtonGroup("1")]
        [Button("生成Config", ButtonSizes.Medium)]
        public static void GenConfig()
        {
            GenGameConfig(Application.streamingAssetsPath, BApplication.GetPlatformPath(Application.platform));
        }
        
        
        static public void GenGameConfig(string str, string platform)
        {
            //config
            var config = GameObject.Find("BDFrame").GetComponent<Config>();
            var json = JsonMapper.ToJson(config.Data);
            //
            var fs = string.Format("{0}/{1}/{2}", str, platform, "GameConfig.json");
            FileHelper.WriteAllText(fs, json);
            AssetDatabase.Refresh();
            Debug.Log("导出成功：" + fs);
        }
#endif
        
        

        #endregion
    }
}