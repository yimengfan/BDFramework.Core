using System;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BDFramework.Editor
{
    public class BDFrameWorkEditorSetting
    {
        
        /// <summary>
        /// 配置文件路径
        /// </summary>
        static private string SETTING_PATH { get; set; } = null;

        public enum PageType
        {
            工作流,

            资源构建,

            打包设置
        }


        [EnumToggleButtons]
        public PageType pageType = PageType.工作流;


        [ShowIf(nameof(pageType), PageType.工作流)]
        [BoxGroup("DevOps设置")]
        [HideLabel]
        [InlineProperty]
        public DevOpsSetting DevOpsSetting = new DevOpsSetting();

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
            return File.Exists(SETTING_PATH);
        }
        
        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        static public BDFrameWorkEditorSetting Load()
        {
            if (string.IsNullOrEmpty(SETTING_PATH))
            {
                SETTING_PATH = IPath.Combine(BDApplication.DevOpsConfigPath, "BDFrameworkSetting.conf");
                //处理旧文件
                var oldpath =   IPath.Combine(BDApplication.BDWorkSpace, "config.json");
                if (File.Exists(oldpath))
                {
                    File.Move(oldpath,SETTING_PATH);
                }
            }

            var setting = new BDFrameWorkEditorSetting();

            if (File.Exists(SETTING_PATH))
            {
                var content = File.ReadAllText(SETTING_PATH);
                setting = JsonMapper.ToObject<BDFrameWorkEditorSetting>(content);
            }

            return setting;
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            FileHelper.WriteAllText(SETTING_PATH, JsonMapper.ToJson(this, true));
        }
    }

    public class AndroidConfig
    {
        [LabelText("Keystore路径")]
        [FilePath(ParentFolder = "../",Extensions = "keystore")]
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
        // [LabelText("ASE密钥")]
        // public string AESCode = "bdframe$#@!@#";

        // [LabelText("是否hash命名")]
        // public bool IsUseHashName = false;
    }


    /// <summary>
    /// 工作流设置
    /// </summary>
    public class DevOpsSetting
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
        public bool IsAutoBuildDll = false;

        public bool IsNeedAutoBuildDll()
        {
            return  IsAutoBuildDll && HotfixCodeFloderType == HotfixCodeFloderTypeEnum.InUnityAssets;
        }
        
        [PropertySpace(10)]
        [BoxGroup("DevOps")]
        [LabelText("AssetBundle SVN地址")]
        public string AssetBundleSVNUrl = "http://127.0.0.1:20001";
        [BoxGroup("DevOps")]
        [LabelText("SVN账号")]
        public string AssetBundleSVNAccount = "root";
        [LabelText("SVN密码")]
        public string AssetBundleSVNPsw = "root";
    }
}