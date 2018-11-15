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
    public class BDLauncher : MonoBehaviour
    {
        public  delegate void OnLife();
        public bool IsCodeHotfix = false;
        public bool IsLoadPdb = false;
        public bool IsAssetbundleModel = false;
        public string FileServerUrl = "127.0.0.1";
        static public OnLife OnStart { get; set; }
        static public OnLife OnUpdate { get; set; }
        static public OnLife OnLateUpdate { get; set; }

        
        // Use this for initialization
        private void Awake()
        {
            this.gameObject.AddComponent<IEnumeratorTool>();
            LaunchLocal();
            //非runtime下 全部使用assetbundle
            if (!Application.isEditor)
            {
                this.IsAssetbundleModel = true;
            }
        }

        #region 启动非热更逻辑


        /// <summary>
        /// 启动本地代码
        /// </summary>
        public void LaunchLocal()
        {

            var types = Assembly.GetExecutingAssembly().GetTypes();

            
            var istartType = typeof(IGameStart);
            IGameStart gs = null;
            foreach (var t in types)
            {
                if (t.IsClass && t.GetInterface("IGameStart")!= null)
                {
                    var attr = t.GetCustomAttribute(typeof(GameStartAtrribute), false);
                    if (attr != null && (attr as GameStartAtrribute).Index == 0)
                    {                       
                        gs = Activator.CreateInstance(t) as IGameStart;
                        break;
                    }
                }
            }

            //注册update
            if (gs != null)
            {
                gs.Start();

                BDLauncher.OnUpdate += gs.Update;
                BDLauncher.OnLateUpdate += gs.LateUpdate;
            }

        }
        
        

        #endregion

        #region 启动热更逻辑
        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="scriptPath"></param>
        /// <param name="artPath"></param>
        /// <param name=""></param>
        public void Launch()
        {
            //初始化资源加载
            BResources.Init(IsAssetbundleModel);
            SqliteLoder.Init();
            //热更资源模式
            if (IsAssetbundleModel)
            {
                //开始启动逻辑  
                var dd = DataListenerServer.Create("BDFrameLife");
                dd.AddData("OnAssetBundleOever");
                dd.AddListener("OnAssetBundleOever", (o) =>
                {
                    //等待ab完成后，开始脚本逻辑
                    LaunchScrpit();
                });
            }
            else
            {
                LaunchScrpit();
            }
            
        }

        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        private void LaunchScrpit()
        {
            if (IsCodeHotfix) //热更代码模式
            {
                ILRuntimeHelper.LoadHotfix(IsLoadPdb);
                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null,new object[] {IsCodeHotfix, IsAssetbundleModel});
            }
            else
            {
                //这里用反射是为了 不访问逻辑模块的具体类，防止编译失败
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {IsCodeHotfix, IsAssetbundleModel});
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