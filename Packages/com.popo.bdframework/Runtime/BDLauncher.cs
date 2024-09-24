using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Asset;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.GameStart;
using BDFramework.Mgr;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.StringEx;
using LitJson;
using ServiceStack;
using UnityEngine;


namespace BDFramework
{
    [RequireComponent(typeof(Config))]
    public class BDLauncher : MonoBehaviour
    {
        private static readonly string Tag = "Launch";
        /// <summary>
        /// 框架版本号
        /// </summary>
        public const string Version  = "2.5.0";

        /// <summary>
        /// 客户端配置信息
        /// </summary>
        [HideInInspector]
        public GameBaseConfigProcessor.Config Config
        {
            get { return GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>(); }
        }

        /// <summary>
        /// 客户端母包信息
        /// </summary>
        public ClientPackageBuildInfo BasePckBuildInfo { get; set; }
        
        /// <summary>
        /// 热更资源信息
        /// </summary>
        public ClientPackageBuildInfo HotfixAssetsBuildInfo { get; set; }
        
        /// <summary>
        /// Config的Text
        /// </summary>
        public TextAsset ConfigText;


        #region 对外的生命周期

        public delegate void GameLauncherDelegate();

        static public GameLauncherDelegate OnUpdate { get; set; }
        static public GameLauncherDelegate OnLateUpdate { get; set; }

        #endregion
        static public BDLauncher Inst { get; private set; }

        // Use this for initialization
        private void Awake()
        {
            Inst = this;
            //添加组件
            this.gameObject.AddComponent<IEnumeratorTool>();
            //游戏配置
            if (this.ConfigText)
            {
                BDebug.Log("配置:" + this.ConfigText.name);
            }
            else
            {
                BDebug.LogError("GameConfig配置为null,请检查!");
            }

            //添加不删除的组件
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(this);
            }
        }


        /// <summary>

        #region 启动热更逻辑

        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="launchSuccessCallback"></param>
        /// <param name="mainProjectTypes">Editor模式下,UPM隔离了DLL需要手动传入</param>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        public void Launch(string gameId = "default", Action launchSuccessCallback = null)
        {
            BDebug.EnableLog(Tag);
            BDebug.Log("框架版本:" + Version, Color.cyan);
            BDebug.Log(Tag,"Persistent:" + BApplication.persistentDataPath);
            BDebug.Log(Tag,"StreamingAsset:" + BApplication.streamingAssetsPath);

            //开始资源检测
            BDebug.Log(Tag,"框架资源版本验证!", Color.yellow);
            ClientAssetsHelper.CheckBasePackageVersion(BApplication.RuntimePlatform, () =>
            {
                BDebug.Log(Tag,"资产版本验证完毕,开始初始化资产...",Color.green);
                //1.美术资产初始化
                BResources.Init(Config.ArtRoot);
                //2.sql初始化
                SqliteLoder.Init(Config.SQLRoot);
                //3.脚本,这个启动会开启所有的逻辑
                ScriptLoder.Init(Config.CodeRoot, Config.CodeRunMode, null);

                //触发回调
                launchSuccessCallback?.Invoke();
            });
        }

        #endregion

        #region 生命周期

        //普通帧循环
        private void Update()
        {
            OnUpdate?.Invoke();
        }

        //更快的帧循环
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        void OnApplicationQuit()
        {
#if UNITY_EDITOR
            SqliteLoder.Close();
            ILRuntimeHelper.Dispose();
#endif
        }

        #endregion
    }
}
