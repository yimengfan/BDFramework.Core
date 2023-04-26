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


            [LabelText("是否热更资产")]
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
                char[] words = new char[] {'0','1','2','3','4','5','6','7','8','9','.' };
                var bundleVersion = PlayerSettings.bundleVersion.ToUpper().ToCharArray();
                var count = bundleVersion.Length;
                StringBuilder sb = new StringBuilder();
                for (var i = 0; i < count; i++)
                {
                    var ch = bundleVersion[i];
                    if (!words.Contains(ch))
                    {
                        if(ch>='A'&&ch<='Z') 
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
