using System;
using BDFramework.Core.Tools;
using BDFramework.Editor.Inspector.Config;
using BDFramework.L2;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏基本数据处理器
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
            [VerticalGroup("a")]
            [HorizontalGroup("a/a1")]
            [LabelText("代码路径")]
            public AssetLoadPathType CodeRoot = AssetLoadPathType.Editor;

            [LabelText("SQLite路径")]
            [HorizontalGroup("a/a2")]
            public AssetLoadPathType SQLRoot = AssetLoadPathType.Editor;

            [LabelText("资源路径")]
            [HorizontalGroup("a/a3")]
            public AssetLoadPathType ArtRoot = AssetLoadPathType.Editor;


            [LabelText("热更代码执行模式")]
            [HorizontalGroup("a/a4")]
            public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ILRuntime;

            [LabelText("是否开启ILRuntime调试")]
            [HorizontalGroup("a/a5")]
            public bool IsDebuggerILRuntime = false;

            [LabelText("是否打印日志")]
            [HorizontalGroup("a/a6")]
            public bool IsDebugLog = true;


            [LabelText("是否热更")]
            [HorizontalGroup("a/a10")]
            public bool IsHotfix = false;

            [Space(5)]
            [LabelText("客户端版本")]
            [HorizontalGroup("a/a12")]
            public string ClientVersionNum = "0.0.0";
            
#if UNITY_EDITOR
            [HorizontalGroup("a/a12", width: 150)]
            [LabelText("更新所有配置版本号")]
            [Button]
            [GUIColor(0, 1, 0)]
            public void UpdateClientToAllConfig()
            {
                ConfigEditorUtil.UpdateClientVersionToAll(ClientVersionNum);
            }
#endif
            
            [LabelText("语言包")]
            [HorizontalGroup("a/a13")]
            public L2Type L2Type = L2Type.zh_CN;
        }
        


        public  void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            //log
            if (BDLauncher.Inst)
            {
                BDLauncher.Inst.GetComponent<BDebug>().IsLog = con.IsDebugLog;
            }
            
            //纠正配置
            if (!Application.isEditor)
            {
                con.ArtRoot = AssetLoadPathType.Persistent;
            }
        }


        /// <summary>
        /// 获取加载路径
        /// </summary>
        /// <param name="assetLoadPathType"></param>
        static public string GetLoadPath(AssetLoadPathType assetLoadPathType)
        {
            var path = "";
            //Editor下按照加载路径区分
            if (Application.isEditor)
            {
                switch (assetLoadPathType)
                {
                    case AssetLoadPathType.Persistent:
                        path = Application.persistentDataPath;
                        break;
                    case AssetLoadPathType.Editor:
                    case AssetLoadPathType.StreamingAsset:
                    {
                        path = Application.streamingAssetsPath;
                    }
                        break;
                    case AssetLoadPathType.DevOpsPublish:
                    {
                        path = BApplication.DevOpsPublishAssetsPath;
                    }
                        break;
                }
            }
            else
            {
                //真机环境默认都在persistent下，
                //因为需要io.不在的各个模块会自行拷贝
                path = Application.persistentDataPath;
            }

            return path;
        }
    }
}
