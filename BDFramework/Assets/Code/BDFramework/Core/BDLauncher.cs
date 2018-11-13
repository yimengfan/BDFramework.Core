using System;
using System.Reflection;
using BDFramework;
using SQLite4Unity3d;
using UnityEngine;
using BDFramework.ResourceMgr;
using UnityEngine.Serialization;

namespace BDFramework
{
    public class BDLauncher : MonoBehaviour
    {
        public bool IsCodeHotfix = false;
        public bool IsLoadPdb = false;
        public bool IsAssetbundleModel = false;
        public string FileServerUrl = "127.0.0.1";
        static public Action OnStart { get; set; }
        static public Action OnUpdate { get; set; }
        static public Action OnLateUpdate { get; set; }

        
        // Use this for initialization
        private void Awake()
        {
            this.gameObject.AddComponent<IEnumeratorTool>();
            Launch();
        }
        

        
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
                    StartHotfixScrpitLogic();
                });
            }
            else
            {
                StartHotfixScrpitLogic();
            }
            
        }

        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        private void StartHotfixScrpitLogic()
        {
            if (IsCodeHotfix) //热更代码模式
            {
                ILRuntimeHelper.LoadHotfix(IsLoadPdb);

                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null,
                    new object[] {IsCodeHotfix, IsAssetbundleModel});
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

        private void Start()
        {
            if (OnStart != null)
            {
                OnStart();
            }
        }


        // Update is called once per frame
        private void Update()
        {
            if (OnUpdate != null)
            {
                OnUpdate();
            }
        }

        private void LateUpdate()
        {
            if (OnLateUpdate != null)
            {
                OnLateUpdate();
            }
        }



    }
}