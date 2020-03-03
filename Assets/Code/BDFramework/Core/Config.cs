using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using BDFramework.Helper;
using BDFramework.Http;
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

    public class GameConfig
    {
        public int CodeRoot; // = AssetLoadPath.Editor;
        public int SQLRoot; //= AssetLoadPath.Editor;
        public int ArtRoot; //= AssetLoadPath.Editor;

        public string CustomArtRoot = "";

        //只在非Editor模式下生效
        public int CodeRunMode; //= HotfixCodeRunMode.ByILRuntime;

        public string FileServerUrl = "192.168.8.68";

        //
        public bool IsHotfix = false;
        public string GateServerIp = "";
        public int Port;
        public bool IsNeedNet = false;
    }

    public class Config : SerializedMonoBehaviour
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
        
        [LabelText("文件服务器")]
        public string FileServerUrl = "192.168.8.68";
        [LabelText("Gate服务器")]
        public string GateServerIp = "";
        public int Port;
        
        [LabelText("是否热更")]
        public bool IsHotfix = false;
        [LabelText("是否联网")]
        public bool IsNeedNet = false;
        //本地配置
        [LabelText("本地配置")]
        public TextAsset localConfig;

        private void Awake()
        {
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
            var url = string.Format("{0}/{1}/{2}", FileServerUrl,BDUtils.GetPlatformPath(Application.platform) ,"GameConfig.json");
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
            //TODO 未来改成反射实现,暂时用这一版
            if (newConfig != null)
            {
                this.CodeRoot = (AssetLoadPath) newConfig.CodeRoot;
                this.SQLRoot = (AssetLoadPath) newConfig.SQLRoot;
                this.ArtRoot = (AssetLoadPath) newConfig.ArtRoot;
                //
                this.CodeRunMode = (HotfixCodeRunMode) newConfig.CodeRunMode;
                //ip相关
                this.FileServerUrl = newConfig.FileServerUrl;
                this.IsHotfix = newConfig.IsHotfix;
                this.GateServerIp = newConfig.GateServerIp;
                this.Port = newConfig.Port;
                if (!Application.isEditor)
                {
                    this.IsHotfix = this.IsNeedNet = true;
                }
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
            GenGameConfig(Application.streamingAssetsPath, BDUtils.GetPlatformPath(Application.platform));
        }
        
        
        static public void GenGameConfig(string str, string platform)
        {
            var gameConfig = new GameConfig();
            var gcType = gameConfig.GetType();

            //config
            var config = GameObject.Find("BDFrame").GetComponent<Config>();
            var configType = config.GetType();
            //
            foreach (var f in gcType.GetFields())
            {
                var ctf = configType.GetField(f.Name);
                //反射赋值
                if (f.FieldType == ctf.FieldType)
                {
                    f.SetValue(gameConfig, ctf.GetValue(config));
                }
                else if (f.FieldType == typeof(int) && ctf.FieldType.IsEnum)
                {
                    f.SetValue(gameConfig, (int) ctf.GetValue(config));
                }
                else
                {
                    BDebug.LogError("类型不匹配:" + f.Name);
                }
            }

            var json = JsonMapper.ToJson(gameConfig);

            var fs = string.Format("{0}/{1}/{2}", str, platform, "GameConfig.json");

            FileHelper.WriteAllText(fs, json);

            AssetDatabase.Refresh();
            Debug.Log("导出成功：" + fs);
        }
#endif
        
        

        #endregion
    }
}