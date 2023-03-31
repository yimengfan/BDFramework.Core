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
        /// <summary>
        /// 框架版本号
        /// </summary>
        public const string Version  = "2.2.0-preview.2";

        /// <summary>
        /// 客户端配置信息
        /// </summary>
        [HideInInspector]
        public GameBaseConfigProcessor.Config Config
        {
            get { return GameConfigManager.Inst.GetConfig<GameBaseConfigProcessor.Config>(); }
        }

        /// <summary>
        /// 客户端包信息
        /// </summary>
        public ClientPackageBuildInfo ClientBuildInfo { get; set; }

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
        /// <param name="mainProjectTypes">Editor模式下,UPM隔离了DLL需要手动传入</param>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        public void Launch(Action<bool> clrBindingAction, string gameId = "default", Action launchSuccessCallback = null)
        {
            BDebug.Log("【Launch】Persistent:" + Application.persistentDataPath);
            BDebug.Log("【Launch】StreamingAsset:" + Application.streamingAssetsPath);

            //list
            var types = ManagerInstHelper.GetMainProjectTypes();
            //主工程启动
            IGameStart mainStart;
            foreach (var type in types)
            {
                //TODO 这里有可能先访问到 IGamestart的Adaptor
                if (type.IsClass && type.GetInterface(nameof(IGameStart)) != null)
                {
                    BDebug.Log("【Launch】主工程 Start： " + type.FullName);
                    mainStart = Activator.CreateInstance(type) as IGameStart;
                    if (mainStart != null)
                    {
                        //注册
                        mainStart.Start();
                        OnUpdate += mainStart.Update;
                        OnLateUpdate += mainStart.LateUpdate;
                        break;
                    }
                }
            }


            //执行主工程逻辑
            BDebug.Log("【Launch】主工程管理器初始化..", Color.red);
            ManagerInstHelper.Load(types);
            //加载框架配置
            GameConfigLoder.LoadFrameBaseConfig(); 
            //开始资源检测
            BDebug.Log("【Launch】框架资源版本验证!");
            ClientAssetsHelper.CheckBasePackageVersion(BApplication.RuntimePlatform, () =>
            {
                //1.美术资产初始化
                BResources.Init(Config.ArtRoot);
                //2.sql初始化
                SqliteLoder.Init(Config.SQLRoot);
                //3.脚本,这个启动会开启所有的逻辑
                ScriptLoder.Init(Config.CodeRoot, Config.CodeRunMode, types, clrBindingAction);
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
