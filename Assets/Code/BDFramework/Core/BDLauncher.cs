using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BDFramework.GameStart;
using SQLite4Unity3d;
using UnityEngine;
using BDFramework.ResourceMgr;
using UnityEngine.Networking;
using Utils = BDFramework.Helper.Utils;

namespace BDFramework
{
    public class BDLauncher : MonoBehaviour
    {
        static public Action OnStart { get; set; }
        static public Action OnUpdate { get; set; }
        static public Action OnLateUpdate { get; set; }

        //当BDFrame启动完整后执行
        static public Action OnBDFrameLaunch { get; set; }

        //全局Config
        [HideInInspector] public Config Config;

        // Use this for initialization
        private void Awake()
        {
            this.gameObject.AddComponent<IEnumeratorTool>();
            this.Config = this.gameObject.GetComponent<Config>();
            LaunchLocal();
            //
        }

        #region 启动非热更逻辑

        /// <summary>
        /// 启动本地代码
        /// </summary>
        public void LaunchLocal()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();


            var istartType = typeof(IGameStart);
            foreach (var t in types)
            {
                if (t.IsClass && t.GetInterface("IGameStart") != null)
                {
                    var attr = t.GetCustomAttribute(typeof(GameStartAtrribute), false);
                    if (attr != null)
                    {
                        var gs = Activator.CreateInstance(t) as IGameStart;

                        //注册
                        gs.Start();

                        //
                        BDLauncher.OnUpdate = gs.Update;
                        BDLauncher.OnLateUpdate = gs.LateUpdate;
                    }
                }
            }
        }

        #endregion

        #region 启动热更逻辑

        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        public void Launch(string GameId = "")
        {
            //初始化资源加载
            string coderoot = "";
            string sqlroot = "";
            string artroot = "";

            //各自的路径
            //art
            if (Config.ArtRoot == AssetLoadPath.Editor)
            {
                if (Application.isEditor)
                {
                    //默认不走AssetBundle
                    artroot = "";
                }
                else
                {
                    //手机默认直接读取Assetbundle
                    artroot = Application.persistentDataPath;
                }
            }
            else if (Config.ArtRoot == AssetLoadPath.Persistent)
            {
                artroot = Application.persistentDataPath;
            }

            else if (Config.ArtRoot == AssetLoadPath.StreamingAsset)
            {
                if (string.IsNullOrEmpty(Config.CustomArtRoot) == false)
                {
                    artroot = Config.CustomArtRoot;
                }
                else
                {
                    artroot = Application.streamingAssetsPath;
                }
            }

            //sql
            if (Config.SQLRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                sqlroot = Application.streamingAssetsPath;
            }

            else if (Config.SQLRoot == AssetLoadPath.Persistent)
            {
                sqlroot = Application.persistentDataPath;
            }
            else if (Config.SQLRoot == AssetLoadPath.StreamingAsset)
            {
                sqlroot = Application.streamingAssetsPath;
            }

            //code
            if (Config.CodeRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                coderoot = "";
            }
            else if (Config.CodeRoot == AssetLoadPath.Persistent)
            {
                coderoot = Application.persistentDataPath;
            }
            else if (Config.CodeRoot == AssetLoadPath.StreamingAsset)
            {
                coderoot = Application.streamingAssetsPath;
            }

            //多游戏更新逻辑
            if (Application.isEditor == false)
            {
                if (GameId != "")
                {
                    artroot = artroot + "/" + GameId;
                    coderoot = coderoot + "/" + GameId;
                    sqlroot = sqlroot + "/" + GameId;
                }
            }

            //sql
            SqliteLoder.Load(sqlroot);
            //art
            BResources.Load(artroot);
            //code
            LoadScrpit(coderoot);


            if (OnBDFrameLaunch != null)
            {
                OnBDFrameLaunch();
            }
        }


        //
        /// <summary>
        /// 游戏逻辑的Assembly
        /// </summary>
        //        public static  Assembly GameAssembly { get; private set; }
        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        private void LoadScrpit(string root)
        {
            if (root != "") //热更代码模式
            {
                if (Config.CodeRunMode == HotfixCodeRunMode.ByILRuntime)
                {
                    //解释执行模式
                    ILRuntimeHelper.LoadHotfix(root);
                    ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null, new object[] {true,false});
                }
                else
                {
                    //
                    //反射模式
                    string dllPath = root + "/" + Utils.GetPlatformPath(Application.platform) + "/hotfix/hotfix.dll";

                    IEnumeratorTool.StartCoroutine(this.IE_LoadDLL_AndroidOrPC(dllPath));
                }
            }
            else
            {
                //PC 模式非热更

                //这里用反射是为了 不访问逻辑模块的具体类，防止编译失败
                var assembly = Assembly.GetExecutingAssembly();
                //
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false,false});
            }
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IEnumerator IE_LoadDLL_AndroidOrPC(string path)
        {
            path = "file:///" + path;
            
            var www = new WWW(path);

            yield return www;
            if (www.isDone && www.error==null)
            {


                var assembly = Assembly.Load(www.bytes);

                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false , true});
            }
            else
            {
                BDebug.LogError("DLL加载失败:"+www.error);
            }
        }


        //普通帧循环
        private void Update()
        {
            if (OnUpdate != null)
            {
                OnUpdate();
            }
        }

        //更快的帧循环
        private void LateUpdate()
        {
            if (OnLateUpdate != null)
            {
                OnLateUpdate();
            }
        }

        void OnApplicationQuit()
        {
#if UNITY_EDITOR
            if (BDFramework.Sql.SqliteHelper.DB != null)
            {
                BDFramework.Sql.SqliteHelper.DB.Close();
            }

            ILRuntimeHelper.Close();
#endif
        }
    }
}