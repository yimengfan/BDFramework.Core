using System;
using System.Text;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Editor.Inspector.Config;
using BDFramework.L2;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BDFramework.Configure
{
    /// <summary>
    /// 框架基础配置处理器。
    /// Framework base-configuration processor.
    /// 该处理器负责把基础配置中的日志、资源与代码路径等全局选项同步到运行时环境，
    /// 并在启动场景脚本因为 HybridCLR 占位反序列化而缺失时补齐关键调试组件，避免基础配置阶段直接中断启动链。
    /// This processor synchronizes global options such as logging, resource paths, and code paths from the base configuration into the runtime environment,
    /// and restores critical debug components when startup-scene scripts are missing because of HybridCLR placeholder deserialization so the startup chain does not stop during base-config loading.
    /// </summary>
    [GameConfig(-9999, "框架基础")]
    public class GameBaseConfigProcessor : IConfigProcessor
    {
        /// <summary>
        /// 游戏基础设置
        /// </summary>
        [Serializable]
        public class Config : ConfigDataBase
        {
            [VerticalGroupAttribute("a")]
            [HorizontalGroupAttribute("a/a1")]
            [LabelTextAttribute("代码路径")]
            public AssetLoadPathType CodeRoot = AssetLoadPathType.Editor;

            [LabelTextAttribute("SQLite路径")]
            [HorizontalGroupAttribute("a/a2")]
            public AssetLoadPathType SQLRoot = AssetLoadPathType.Editor;

            [LabelTextAttribute("资源路径")]
            [HorizontalGroupAttribute("a/a3")]
            public AssetLoadPathType ArtRoot = AssetLoadPathType.Editor;


            [LabelTextAttribute("热更代码执行模式")]
            [HorizontalGroupAttribute("a/a4")]
            public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.HyCLR;


            [LabelTextAttribute("是否打印日志")]
            [HorizontalGroupAttribute("a/a6")]
            public bool IsDebugLog = true;


            [Space(5)]
            [LabelTextAttribute("客户端版本")]
            [HorizontalGroupAttribute("a/a12")]
            public string ClientVersionNum = "0.0.0";

#if UNITY_EDITOR
            [HorizontalGroupAttribute("a/a12", width: 150)]
            [LabelTextAttribute("更新所有配置版本号")]
            [ButtonAttribute]
            [GUIColorAttribute(0, 1, 0)]
            public void UpdateClientToAllConfig()
            {
                ConfigEditorUtil.UpdateClientVersionToAll(ClientVersionNum);
            }
#endif

            [LabelTextAttribute("语言包")]
            [HorizontalGroupAttribute("a/a13")]
            public L2Type L2Type = L2Type.zh_CN;

            /// <summary>
            /// 获取ios bundleVersion
            /// The string can only contain numeric characters (0-9) and periods.
            /// 长度<=18
            /// </summary>
            /// <returns></returns>
            public string GetClientVersionNumForIOS()
            {
                string bv = ClientVersionNum;
#if UNITY_EDITOR
                ASCIIEncoding asciiEncoding = new ASCIIEncoding();
                char[] words = new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.'};
                var bundleVersion = PlayerSettings.bundleVersion.ToUpper().ToCharArray();
                var count = bundleVersion.Length;
                StringBuilder sb = new StringBuilder();
                for (var i = 0; i < count; i++)
                {
                    var ch = bundleVersion[i];
                    if (!words.Contains(ch))
                    {
                        if (ch >= 'A' && ch <= 'Z')
                        {
                            var intAsciiCode = asciiEncoding.GetBytes(bundleVersion)[i].ToString();
                            sb.Append(intAsciiCode);
                        }
                        else
                        {
                            sb.Append('.');
                        }
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }

                bv = sb.ToString();
                bv = bv.Substring(0, Mathf.Min(bv.Length, 18));
#endif
                return bv;
            }
        }

        /// <summary>
        /// 在基础配置装载完成后同步日志组件开关。
        /// Synchronize the debug-log component switch after the base configuration has been loaded.
        /// </summary>
        public void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            if (con == null)
            {
                Debug.LogError("基础配置类型不匹配,无法同步日志开关!");
                return;
            }

            //log
            if (BDLauncher.Inst)
            {
                Debug.Log("日志打印:" + con.IsDebugLog);
                var debugComponent = EnsureDebugComponent(BDLauncher.Inst.gameObject);
                if (debugComponent != null)
                {
                    debugComponent.IsLog = con.IsDebugLog;
                }
            }
        }


        /// <summary>
        /// 确保启动器物体上始终存在 <c>BDebug</c> 组件。
        /// Ensure that the launcher GameObject always has a <c>BDebug</c> component.
        /// Android/HybridCLR 启动时，场景可能先反序列化出缺失脚本占位，再在后续阶段装载热更程序集；
        /// 如果原有的 <c>BDebug</c> 组件在这个窗口期丢失，这里会补挂一个新的实例，避免基础配置同步因为空引用而打断启动流程。
        /// During Android/HybridCLR startup the scene may deserialize missing-script placeholders first and load hotfix assemblies later;
        /// if the original <c>BDebug</c> component is lost during that window, this method adds a fresh instance so base-configuration synchronization does not break the startup flow with a null reference.
        /// </summary>
        /// <param name="owner">持有调试组件的启动器物体。</param>
        /// <param name="owner">Launcher GameObject that should own the debug component.</param>
        /// <returns>现有或新补挂的 <c>BDebug</c> 组件。</returns>
        /// <returns>The existing or newly restored <c>BDebug</c> component.</returns>
        public static BDebug EnsureDebugComponent(GameObject owner)
        {
            if (!owner)
            {
                return null;
            }

            var debugComponent = owner.GetComponent<BDebug>();
            if (!debugComponent)
            {
                Debug.LogWarning("BDebug组件缺失,运行时补挂以继续启动链");
                debugComponent = owner.AddComponent<BDebug>();
            }

            return debugComponent;
        }


        /// <summary>
        /// 获取加载路径
        /// </summary>
        /// <param name="assetLoadPathType"></param>
        static public string GetLoadPath(AssetLoadPathType assetLoadPathType)
        {
            var path = "";

            //Editor下 全部走DevOpsPublishAssetsPath
            switch (assetLoadPathType)
            {
                case AssetLoadPathType.Editor:
                {
                    path = BApplication.DevOpsPublishAssetsPath;
                }
                    break;
                case AssetLoadPathType.Hotfix:
                {
                    //开了hotfix,环境默认都在persistent下. 
                    //因为需要io.不在的各个模块会自行拷贝
                    path = BApplication.persistentDataPath;
                }
                    break;
            }
            return path;
        }
    }
}
