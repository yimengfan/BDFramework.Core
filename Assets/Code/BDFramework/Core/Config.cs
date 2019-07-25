using System;
using System.Collections;
using System.Net.Sockets;
using BDFramework.Helper;
using BDFramework.Http;
using LitJson;
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

    public class Config : MonoBehaviour
    {
        public AssetLoadPath CodeRoot = AssetLoadPath.Editor;
        public AssetLoadPath SQLRoot = AssetLoadPath.Editor;
        public AssetLoadPath ArtRoot = AssetLoadPath.Editor;

        public string CustomArtRoot = "";

        //只在非Editor模式下生效
        public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ByILRuntime;
        public string FileServerUrl = "192.168.8.68";

        //
        public bool IsHotfix = false;
        public string GateServerIp = "";
        public int Port;
        public bool IsNeedNet = false;


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


            //TODO 未来改成反射实现,暂时用这一版
            if (gconfig != null)
            {
                this.CodeRoot = (AssetLoadPath) gconfig.CodeRoot;
                this.SQLRoot = (AssetLoadPath) gconfig.SQLRoot;
                this.ArtRoot = (AssetLoadPath) gconfig.ArtRoot;
                //
                this.CodeRunMode = (HotfixCodeRunMode) gconfig.CodeRunMode;
                //ip相关
                this.FileServerUrl = gconfig.FileServerUrl;
                this.IsHotfix = gconfig.IsHotfix;
                this.GateServerIp = gconfig.GateServerIp;
                this.Port = gconfig.Port;
                if (!Application.isEditor)
                {
                    this.IsHotfix = this.IsNeedNet = true;
                }
            }

            if (callback != null)
            {
                callback();
            }
        }


        #region    FPS计算

        float fps;
        float deltaTime = 0.0f;
        float msec;
        Rect rect;
        GUIStyle style = new GUIStyle();

        void Start()
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
    }
}