using System;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BDFramework.Editor
{
    public class BDFrameworkEditorSetting
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        static private string SETTING_PATH { get; set; } = null;

        public enum PageType
        {
            DevOps,

            BuildPipeline,

            BuildPackage
        }


        [EnumToggleButtons]
        public PageType pageType = PageType.DevOps;

        [ShowIf(nameof(pageType), PageType.DevOps)]
        [BoxGroup("DevOps 设置")]
        [HideLabel]
        [InlineProperty]
        public DevOpsSetting DevOpsSetting = new DevOpsSetting();

        #region 资产构建

        /// <summary>
        /// 资源打包
        /// </summary>
        [ShowIf(nameof(pageType), PageType.BuildPipeline)]
        [BoxGroup("热更代码")]
        [HideLabel]
        [InlineProperty]
        public BuildHotfixDLLSetting BuildHotfixDLLSetting = new BuildHotfixDLLSetting();

        /// <summary>
        /// 资源打包
        /// </summary>
        [ShowIf(nameof(pageType), PageType.BuildPipeline)]
        [BoxGroup("表格数据")]
        [HideLabel]
        [InlineProperty]
        public BuildSqlSetting BuildSqlSetting = new BuildSqlSetting();

        /// <summary>
        /// 资源打包
        /// </summary>
        [ShowIf(nameof(pageType), PageType.BuildPipeline)]
        [BoxGroup("资源设置")]
        [HideLabel]
        [InlineProperty]
        public BuildAssetBundleSetting BuildAssetBundleSetting = new BuildAssetBundleSetting();

        #endregion


        #region 平台打包

        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Android设置")]
        [HideLabel]
        [InlineProperty]
        public AndroidSetting Android = new AndroidSetting();


        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Ios设置")]
        [HideLabel]
        [InlineProperty]
        public iOSSetting iOS = new iOSSetting();


        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Android设置[debug]")]
        [HideLabel]
        [InlineProperty]
        public AndroidSetting AndroidDebug = new AndroidSetting();

        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("iOS设置[debug]")]
        [HideLabel]
        [InlineProperty]
        public iOSSetting iOSDebug = new iOSSetting();

        //

        #endregion


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
        static public BDFrameworkEditorSetting Load()
        {
            if (string.IsNullOrEmpty(SETTING_PATH))
            {
                SETTING_PATH = IPath.Combine(BApplication.DevOpsConfigPath, "BDFrameworkSetting.conf");
                //处理旧文件
                var oldpath = IPath.Combine(BApplication.BDWorkSpace, "config.json");
                if (File.Exists(oldpath))
                {
                    var dirt = Path.GetDirectoryName(SETTING_PATH);
                    if (!Directory.Exists(dirt))
                    {
                        Directory.CreateDirectory(dirt);
                    }

                    File.Move(oldpath, SETTING_PATH);
                }
            }

            var setting = new BDFrameworkEditorSetting();

            if (File.Exists(SETTING_PATH))
            {
                var content = File.ReadAllText(SETTING_PATH);
                setting = JsonMapper.ToObject<BDFrameworkEditorSetting>(content);
            }

            return setting;
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            FileHelper.WriteAllText(SETTING_PATH, JsonMapper.ToJson(this, true));
            // Debug.Log("保存成功:"+SETTING_PATH);
        }
    }


    /// <summary>
    /// 工作流设置
    /// </summary>
    public class DevOpsSetting
    {
        /// <summary>
        /// VCS仓库信息
        /// </summary>
        [Serializable]
        public class VCSData
        {
            [LabelText("仓库地址")]
            public string Url = "http://127.0.0.1:20001";

            [LabelText("账号")]
            public string UserName = "root";

            [LabelText("密码")]
            public string Psw = "root";
        }

        [PropertySpace(10)]
        [BoxGroup("DevOps")]
        [InlineProperty]
        [LabelText("资源仓库")]
        public VCSData AssetServiceVCSData = new VCSData();

        [BoxGroup("DevOps")]
        [InlineProperty]
        [LabelText("母包仓库")]
        public VCSData PackageServiceVCSData = new VCSData();
    }


    /// <summary>
    /// 构建Hotfix脚本设置
    /// </summary>
    public class BuildHotfixDLLSetting
    {
        public enum HotfixCodeFloderTypeEnum
        {
            InUnityAssets,
            BDWorkSpace
        }

        [BoxGroup("脚本", true)]
        [LabelText("热更代码模式")]
        public HotfixCodeFloderTypeEnum HotfixCodeFloderType = HotfixCodeFloderTypeEnum.InUnityAssets;

        [BoxGroup("脚本", true)]
        [LabelText("自动编译热更DLL")]
        public bool IsAutoBuildDll = false;


        /// <summary>
        ///热更代码是否在unity外
        /// </summary>
        public bool IsHotfixCodeOutofUnityAssets()
        {
            return HotfixCodeFloderType != HotfixCodeFloderTypeEnum.InUnityAssets;
        }


        public bool IsNeedAutoBuildDll()
        {
            return IsAutoBuildDll && HotfixCodeFloderType == HotfixCodeFloderTypeEnum.InUnityAssets;
        }
    }


    /// <summary>
    /// 打包Sqlite设置
    /// </summary>
    public class BuildSqlSetting
    {
        [BoxGroup("表格", true)]
        [LabelText("play模式前强制导入变更Excel")]
        public bool IsForceImportChangedExcelOnWillEnterPlaymode = true;

        [BoxGroup("表格", true)]
        [LabelText("修改完Excel自动导表")]
        public bool IsAutoImportSqlWhenExcelChange = true;
    }

    /// <summary>
    /// 打包AssetBundle设置
    /// </summary>
    public class BuildAssetBundleSetting
    {
        [LabelText("开启混淆")]
        public bool IsEnableObfuscation = true;

        [LabelText("关闭TypeTree")]
        public bool IsDisableTypeTree = false;
    }


    public class AndroidSetting
    {
        [LabelText("Keystore路径")]
        [FilePath(ParentFolder = "", Extensions = "keystore")]
        public string keystoreName;

        [LabelText("Keystore密码")]
        public string keystorePass;

        [LabelText("keyalias")]
        public string keyaliasName;

        [LabelText("keyalias密码")]
        public string keyaliasPass;
    }


    public class iOSSetting
    {
        [LabelText("证书")]
        public string test;

        [LabelText("密钥")]
        public string test2;
    }
}
