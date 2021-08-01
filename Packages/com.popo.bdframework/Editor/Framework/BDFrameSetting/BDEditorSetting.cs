using System;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BDFramework.Editor
{
    public class BDEditorSetting
    {
        [HideInInspector]
        static public string PATH = null;

        public enum PageType
        {
            工作流,

            资源构建,

            打包设置
        }


        [EnumToggleButtons]
        public PageType pageType = PageType.工作流;


        [ShowIf(nameof(pageType), PageType.工作流)]
        [BoxGroup("WorkFollow设置")]
        [HideLabel]
        [InlineProperty]
        public WorkFollowSetting WorkFollow = new WorkFollowSetting();

        [ShowIf(nameof(pageType), PageType.资源构建)]
        [BoxGroup("资源设置")]
        [HideLabel]
        [InlineProperty]
        public BuildAssetConfig BuildAssetBundle = new BuildAssetConfig();


        [ShowIf(nameof(pageType), PageType.打包设置)]
        [BoxGroup("Android设置")]
        [HideLabel]
        [InlineProperty]
        public AndroidConfig Android = new AndroidConfig();


        [ShowIf(nameof(pageType), PageType.打包设置)]
        [BoxGroup("Ios设置")]
        [HideLabel]
        [InlineProperty]
        public IosConfig IOSConfig = new IosConfig();


        [ShowIf(nameof(pageType), PageType.打包设置)]
        [BoxGroup("Android设置[debug]")]
        [HideLabel]
        [InlineProperty]
        public AndroidConfig AndroidDebug = new AndroidConfig();

        [ShowIf(nameof(pageType), PageType.打包设置)]
        [BoxGroup("Ios设置[debug]")]
        [HideLabel]
        [InlineProperty]
        public IosConfig IOSConfigDebug = new IosConfig();

        //


        /// <summary>
        /// 检查config设置
        /// </summary>
        /// <returns></returns>
        public bool IsSetConfig()
        {
            return File.Exists(PATH);
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        static public BDEditorSetting Load()
        {
            if (string.IsNullOrEmpty(PATH))
            {
                PATH = IPath.Combine(BDApplication.BDWorkSpace, "config.json");
            }

            var setting = new BDEditorSetting();

            if (File.Exists(PATH))
            {
                var content = File.ReadAllText(PATH);
                setting = JsonMapper.ToObject<BDEditorSetting>(content);
            }

            return setting;
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            FileHelper.WriteAllText(PATH, JsonMapper.ToJson(this, true));
        }
    }

    public class AndroidConfig
    {
        [LabelText("Keystore路径")]
        [FilePath(Extensions = "keystore")]
        public string keystoreName;

        [LabelText("Keystore密码")]
        public string keystorePass;

        [LabelText("keyalias")]
        public string keyaliasName;

        [LabelText("keyalias密码")]
        public string keyaliasPass;
    }


    public class IosConfig
    {
        [LabelText("证书")]
        public string test;

        [LabelText("密钥")]
        public string test2;
    }


    /// <summary>
    /// 打包设置
    /// </summary>
    public class BuildAssetConfig
    {
        [LabelText("ASE密钥")]
        public string AESCode = "bdframe$#@!@#";

        [LabelText("是否hash命名")]
        public bool IsUseHashName = false;
    }


    /// <summary>
    /// 工作流设置
    /// </summary>
    public class WorkFollowSetting
    {
        public enum HotfixCodeFloderTypeEnum
        {
            InUnityAssets,
            BDWorkSpace
        }

        [BoxGroup("热更", true)]
        [LabelText("热更代码模式")]
        public HotfixCodeFloderTypeEnum HotfixCodeFloderType = HotfixCodeFloderTypeEnum.InUnityAssets;

        /// <summary>
        ///热更代码是否在unity外
        /// </summary>
        public bool IsHotfixCodeOutofUnityAssets()
        {
            return HotfixCodeFloderType != HotfixCodeFloderTypeEnum.InUnityAssets;
        }

        [BoxGroup("热更")]
        [LabelText("自动编译热更DLL")]
        public bool IsAutoBuildDll = true;

        public bool IsNeedAutoBuildDll()
        {
            return  IsAutoBuildDll && HotfixCodeFloderType == HotfixCodeFloderTypeEnum.InUnityAssets;
        }
        
        [PropertySpace(10)]
        [BoxGroup("DevOps")]
        [LabelText("AssetBundleServer")]
        public string AssetBundleFileServerUrl = "http://127.0.0.1:20001";
    }
}