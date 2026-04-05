using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace BDFramework
{

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

        public delegate void GameLauncherDelegate();

        static public GameLauncherDelegate OnUpdate { get; set; }
        static public GameLauncherDelegate OnLateUpdate { get; set; }

        #endregion
        
        static public BDLauncher Inst { get; private set; }

        // Use this for initialization
        private void Awake()
        {
            Inst = this;
 
            //游戏配置
            if (this.ConfigText)
            {
                Debug.Log("配置:" + this.ConfigText.name);
            }
            else
            {
                Debug.LogError("GameConfig配置为null,请检查!");
            }


            //添加不删除的组件
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(this);
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        private void Start()
        {
            Debug.Log("------------------AOT Start: -----------------------");
            //启动 aot 脚本
            ScriptLoderAOT.Load(ClientVersion);
            Debug.Log("------------------AOT Complete！ -----------------------");
            //启动 aot 脚本
        }


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
        

        #endregion

        void OnApplicationQuit()
        {
            QuitFramework();
        }
        
     

        /// <summary>
        /// 退出框架
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public void QuitFramework()
        {

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("BDFramework.BDLauncherHotfix");
                if (type == null)
                {
                    continue;
                }

                var method = type.GetMethod("OnApplicationQuit", BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                {
                    Debug.LogWarning("未找到 BDLauncherHotfix.OnApplicationQuit");
                    return;
                }

                var instance = Activator.CreateInstance(type);
                method.Invoke(instance, null);
                return;
            }

            Debug.LogWarning("未找到类型: BDFramework.BDLauncherHotfix");
        }
        
   
    }
}
