using System;
using System.Reflection;
using BDFramework.GameStart;
using UnityEngine;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using LitJson;
using Sirenix.OdinInspector;


namespace BDFramework
{
    /// <summary>
    /// 框架的配置
    /// </summary>
    public class BDFrameConfig
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// 加载框架配置
        /// </summary>
       public static BDFrameConfig  Load()
        {
            var content = Resources.Load<TextAsset>("BDFrameConfig").text;
             var config = JsonMapper.ToObject<BDFrameConfig>(content);
            //框架版本
            BDebug.Log("框架版本:" + config, "red");
            return config;
        }
    }

    [RequireComponent(typeof(Config))]
    public class BDLauncher : MonoBehaviour
    {
        /// <summary>
        /// 框架的相关配置
        /// </summary>
        public BDFrameConfig FrameConfig { get; private set; }

        /// <summary>
        /// GameConfig
        /// </summary>
        [HideInInspector]
        public GameConfig GameConfig { get; private set; }


        /// <summary>
        /// Config的Text
        /// </summary>
        public TextAsset ConfigText;

        #region 对外的生命周期

        public delegate void GameLauncherDelegate();

        static public GameLauncherDelegate OnUpdate     { get; set; }
        static public GameLauncherDelegate OnLateUpdate { get; set; }

        /// <summary>
        /// 当框架初始化完成
        /// </summary>
        static public Action OnBDFrameInitialized { get; set; }

        /// <summary>
        /// 当框架初始化完成
        /// </summary>
        static public Action OnBDFrameInitializedForTest { get; set; }

        #endregion

        static public BDLauncher Inst { get; private set; }

        // Use this for initialization
        private void Awake()
        {
            Inst = this;
            this.gameObject.AddComponent<IEnumeratorTool>();
            var debug = this.gameObject.GetComponent<BDebug>();
            //框架配置
            LoadFrameConfig();
            //游戏配置
            if (this.ConfigText)
            {
                this.GameConfig = JsonMapper.ToObject<GameConfig>(this.ConfigText.text);
            }
            else
            {
                BDebug.LogError("GameConfig配置为null,请检查!");
            }
            //日志打印
            debug.IsLog = this.GameConfig.IsDebugLog;
        }

        /// <summary>
        /// 加载框架配置
        /// </summary>
        private void LoadFrameConfig()
        {
            var content = Resources.Load<TextAsset>("BDFrameConfig").text;
            FrameConfig = JsonMapper.ToObject<BDFrameConfig>(content);
            //框架版本
            BDebug.Log("框架版本:" + FrameConfig.Version, "red");
        }


        /// <summary>

        #region 启动热更逻辑

        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="mainProjectTypes">Editor模式下,UPM隔离了DLL需要手动传入</param>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        public void Launch(Type[] mainProjectTypes, Action<bool> clrBindingAction, string gameId = "default")
        {
            BDebug.Log("Persistent:"     + Application.persistentDataPath);
            BDebug.Log("StreamingAsset:" + Application.streamingAssetsPath);
            //主工程启动
            IGameStart mainStart;
            foreach (var type in mainProjectTypes)
            {
                if (type.GetInterface(nameof(IGameStart)) != null)
                {
                    mainStart = Activator.CreateInstance(type) as IGameStart;
                    //注册
                    mainStart.Start();
                    OnUpdate     += mainStart.Update;
                    OnLateUpdate += mainStart.LateUpdate;
                    break;
                }
            }

            //开始资源检测
            AssetHelper.AssetHelper.CheckAssetPackageVersion(Application.platform, () =>
            {
                //1.美术目录
                BResources.Load(GameConfig.ArtRoot, GameConfig.CustomArtRoot);
                //2.sql
                SqliteLoder.Load(GameConfig.SQLRoot);
                //3.脚本,这个启动会开启所有的逻辑
                ScriptLoder.Load(GameConfig.CodeRoot, GameConfig.CodeRunMode, mainProjectTypes, clrBindingAction);
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
            ILRuntimeHelper.Close();
#endif
        }

        #endregion
        
    }
}