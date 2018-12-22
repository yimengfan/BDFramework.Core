using System;
using System.Reflection;
using BDFramework;
using BDFramework.GameStart;
using SQLite4Unity3d;
using UnityEngine;
using BDFramework.ResourceMgr;
using UnityEngine.Serialization;

namespace BDFramework
{
    public enum AssetLoadPath
    {
        Editor,
        Persistent,
        StreamingAsset
    }

    public class BDLauncher : MonoBehaviour
    {
        public AssetLoadPath CodeRoot = AssetLoadPath.Editor;
        public AssetLoadPath SQLRoot = AssetLoadPath.Editor;
        public AssetLoadPath ArtRoot = AssetLoadPath.Editor;
        public string FileServerUrl = "192.168.8.68";
        static public Action OnStart { get; set; }
        static public Action OnUpdate { get; set; }
        static public Action OnLateUpdate { get; set; }


        // Use this for initialization
        private void Awake()
        {
            this.gameObject.AddComponent<IEnumeratorTool>();
            LaunchLocal();
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
        public void Launch(string GameId ="")
        {
            //初始化资源加载
            string coderoot = "";
            string sqlroot = "";
            string artroot = "";
            
            //各自的路径
            //art
            if (ArtRoot == AssetLoadPath.Editor)
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
            else if (ArtRoot == AssetLoadPath.Persistent)
            {
                artroot = Application.persistentDataPath;
            }

            else if (ArtRoot == AssetLoadPath.StreamingAsset)
            {
                artroot = Application.streamingAssetsPath;
            }
            //sql
            if (SQLRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                sqlroot = Application.streamingAssetsPath;
            }

            else if (SQLRoot == AssetLoadPath.Persistent)
            {
                sqlroot = Application.persistentDataPath;
            }
            else if (SQLRoot == AssetLoadPath.StreamingAsset)
            {
               sqlroot = Application.streamingAssetsPath;
            }

            //code
            if (CodeRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                coderoot = "";
            }

            else if (CodeRoot == AssetLoadPath.Persistent)
            {
                coderoot = Application.persistentDataPath;
            }
            else if (CodeRoot == AssetLoadPath.StreamingAsset)
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
    
            

        }

        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        private void LoadScrpit(string root)
        {
            if (root != "") //热更代码模式
            {
                ILRuntimeHelper.LoadHotfix(root);
                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null,
                    new object[] {true});
            }
            else
            {
                //这里用反射是为了 不访问逻辑模块的具体类，防止编译失败
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false});
            }
        }

        #endregion


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
    }
}