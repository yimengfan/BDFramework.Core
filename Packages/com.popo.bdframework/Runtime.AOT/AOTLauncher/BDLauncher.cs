using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace BDFramework
{

    /// <summary>
    /// 启动场景里的第一阶段运行时入口。
    /// 该组件负责建立启动器单例、保存构建阶段写回的母包版本与配置文本，并在首场景启动后装载 AOT 元数据和热更程序集。
    /// 同时通过默认执行顺序把自身放到场景脚本的最前批次，尽量降低被其他普通脚本抢先访问未初始化启动器的风险。
    /// </summary>
    /// <remarks>
    /// 业务真正进入框架主体前，还需要在合适时机显式调用 <c>BDLauncherBridge.Launch()</c> 或 <c>BDLauncherHotfix.Launch()</c>。
    /// 典型时序是：启动场景挂载 <c>BDLauncher</c>，更新页完成资源校验后再调用 Bridge 或 Facade 入口。
    /// 这里的执行顺序只覆盖常规 MonoBehaviour 生命周期；如果其他脚本使用更小的执行顺序或 <c>RuntimeInitializeOnLoadMethod</c>，它们仍可能更早执行。
    /// </remarks>
    [DefaultExecutionOrder(int.MinValue)]
    public partial class BDLauncher : MonoBehaviour
    {
        private static readonly string Tag = "Launch";
        /// <summary>
        /// 框架版本号
        /// </summary>
        public const string FrameworkVersion  = "4.0.0";
        /// <summary>
        /// 母包版本号
        /// </summary>
        public  string ClientVersion  = "0.1.0";
        /// <summary>
        /// Config的Text
        /// </summary>
        public TextAsset ConfigText;


        #region 对外的生命周期

        /// <summary>
        /// 供启动器转发常规帧循环的公共委托。
        /// </summary>
        public delegate void GameLauncherDelegate();

        /// <summary>
        /// 启动器转发给业务层的 Update 生命周期回调。
        /// </summary>
        static public GameLauncherDelegate OnUpdate { get; set; }

        /// <summary>
        /// 启动器转发给业务层的 LateUpdate 生命周期回调。
        /// </summary>
        static public GameLauncherDelegate OnLateUpdate { get; set; }

        #endregion
        
        /// <summary>
        /// 当前启动场景中的启动器单例。
        /// </summary>
        static public BDLauncher Inst { get; private set; }

        /// <summary>
        /// 注册启动器单例并校验构建时写回的基础配置。
        /// </summary>
        private void Awake()
        {
            // Phase 1: 建立运行时单例并验证场景里是否挂好了配置资源。
            Inst = this;
 
            // 游戏配置
            if (this.ConfigText)
            {
                Debug.Log("配置:" + this.ConfigText.name);
            }
            else
            {
                Debug.LogError("GameConfig配置为null,请检查!");
            }

            // Phase 2: 启动场景完成后保持启动器常驻，后续更新页和业务场景都复用同一个入口。
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(this);
            }


            // Phase 3: 记录装载完成；真正的业务启动仍等待 BDLauncherBridge.Launch()。
            Debug.Log("------------------AOT Start-----------------------");
            // 检查热更 DLL 是否已在 BeforeSceneLoad 阶段加载。
            // Check if hotfix DLLs were already loaded during BeforeSceneLoad phase.
            if (ScriptLoderAOT.HasLoadedHotfixAssembliesBeforeSceneLoad)
            {
                Debug.Log("[AOT] 热更 DLL 已在 BeforeSceneLoad 阶段加载完成，跳过重复加载");
            }
            else
            {
                ScriptLoderAOT.Load(ClientVersion);
            }
            Debug.Log("<color=yellow>执行反射：ScriptLoder.Init()，装载热更代码 </color>");
            InitHotfixScriptLoder();
            Debug.Log("------------------AOT Complete！ -----------------------");
        }

        /// <summary>
        /// 在首场景启动后装载 AOT 元数据与热更程序集。
        /// </summary>
        private void Start()
        {

        }


        #region 生命周期

        /// <summary>
        /// 把普通帧循环转发给外部注册的启动器监听者。
        /// </summary>
        private void Update()
        {
            OnUpdate?.Invoke();
        }

        /// <summary>
        /// 把 LateUpdate 帧循环转发给外部注册的启动器监听者。
        /// </summary>
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }
        

        #endregion

        /// <summary>
        /// 在应用退出时转发框架收尾逻辑。
        /// </summary>
        void OnApplicationQuit()
        {
            QuitFramework();
        }
        
     
     
        /// <summary>
        /// 初始化热更域的入口。
        /// 这里通过反射查找静态的 <c>ScriptLoder.Init()</c>，以兼容运行时程序集分离后的热更加载方式。
        /// </summary>
        public void InitHotfixScriptLoder()
        {
            // Phase 1: 在当前已加载程序集里查找热更脚本入口类型，避免把启动器直接绑定到具体程序集引用。
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("BDFramework.ScriptLoder");
                if (type == null)
                {
                    continue;
                }

                // Phase 2: 入口是静态方法，必须按静态方法查找并用 null 作为调用目标。
                var method = type.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogException(new Exception("未找到 ScriptLoder.Init"));
                    return;
                }

                method.Invoke(null, null);

                Debug.Log("[Hotfix] ScriptLoder.Init() Success!!!");
                return;
            }

            Debug.LogException(new Exception("未找到 ScriptLoder.Init"));
        }
        
        
        
        

        /// <summary>
        /// 在 Editor 退出路径里转发运行时桥接层的收尾逻辑。
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public void QuitFramework()
        {
            // 框架基础设施层在这里使用轻量反射查找 Bridge，
            // 是为了兼容历史入口名与不同程序集组织方式，避免把启动器重新耦合回旧分支实现。
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("BDFramework.BDLauncherBridge");
                if (type == null)
                {
                    continue;
                }

                var method = type.GetMethod("OnApplicationQuit", BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                {
                    Debug.LogWarning("未找到 BDLauncherBridge.OnApplicationQuit");
                    return;
                }

                var instance = Activator.CreateInstance(type);
                method.Invoke(instance, null);
                return;
            }

            Debug.LogWarning("未找到类型: BDFramework.BDLauncherBridge");
        }
        
   
    }
}
