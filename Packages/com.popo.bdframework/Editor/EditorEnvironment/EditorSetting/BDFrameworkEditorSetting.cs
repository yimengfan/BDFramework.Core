using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [BoxGroup("Android设置[Release]")]
        [HideLabel]
        [InlineProperty]
        public AndroidSetting Android = new AndroidSetting();

        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Android设置[Debug]")]
        [HideLabel]
        [InlineProperty]
        public AndroidSetting AndroidDebug = new AndroidSetting();

        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("iOS设置[Release]")]
        [HideLabel]
        [InlineProperty]
        public iOSSetting iOS = new iOSSetting();


        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("iOS设置[Debug]")]
        [HideLabel]
        [InlineProperty]
        public iOSSetting iOSDebug = new iOSSetting();

        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Windows设置[Release]")]
        [HideLabel]
        [InlineProperty]
        public WindowsPlayerSetting WindowsPlayer = new WindowsPlayerSetting();
        
        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Windows设置[Debug]")]
        [HideLabel]
        [InlineProperty]
        public WindowsPlayerSetting WindowsPlayerDebug = new WindowsPlayerSetting();

        
        
        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Mac OSX设置[Release]")]
        [HideLabel]
        [InlineProperty]
        public MacOSXSetting MacOSX = new MacOSXSetting();
        
        [ShowIf(nameof(pageType), PageType.BuildPackage)]
        [BoxGroup("Mac OSX设置[Debug]")]
        [HideLabel]
        [InlineProperty]
        public MacOSXSetting  MacOSXDebug = new MacOSXSetting();
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
    /// devops 设置
    /// </summary>
    public class DevOpsSetting
    {
        /// <summary>
        /// VCS仓库信息
        /// </summary>
        [Serializable]
        public class VCSData
        {
            public enum VCSType
            {
                Git,
                SVN,
                P4,
            }
            [LabelText("标记")]
            public string Tag = "";
            
            [LabelText("仓库类型")]
            public VCSType VcsType = VCSType.Git;
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

        [LabelText("自定义仓库")]
        public List<VCSData> CustomVCSDataList = new List<VCSData>();


        /// <summary>
        /// 获取一个VCS
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public VCSData GetVCSData(string tag)
        {
            return CustomVCSDataList.FirstOrDefault((vcs) => vcs.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
        }
    }


    /// <summary>
    /// 构建Hotfix脚本设置
    /// </summary>
    [Serializable]
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
    [Serializable]
    public class BuildSqlSetting
    {
        [BoxGroup("表格", true)]
        [LabelText("play模式强制导入变更Excel")]
        public bool IsForceImportChangedExcelOnWillEnterPlaymode = true;

        [BoxGroup("表格", true)]
        [LabelText("监听Excel改动")]
        public bool IsAutoImportSqlWhenExcelChange = true;
    }

    /// <summary>
    /// 打包AssetBundle设置
    /// </summary>
    [Serializable]
    public class BuildAssetBundleSetting
    {
        [LabelText("开启混淆")]
        public bool IsEnableObfuscation = true;

        [LabelText("关闭TypeTree")]
        public bool IsDisableTypeTree = false;
    }

    /// <summary>
    /// Android打包设置
    /// </summary>
    [Serializable]
    public class AndroidSetting
    {
        [LabelText("Keystore路径" )]
        [LabelWidth(100)]
        [FilePath(ParentFolder = "", Extensions = "keystore")]
        public string keystoreName;

        [LabelText("Keystore密码")]
        [LabelWidth(100)]
        public string keystorePass;

        [LabelText("keyalias")]
        [LabelWidth(100)]
        public string keyaliasName;

        [LabelText("keyalias密码")]
        [LabelWidth(100)]
        public string keyaliasPass;
    }

    /// <summary>
    /// iOS打包设置
    /// </summary>
    [Serializable]
    public class iOSSetting
    {
        [LabelText("证书")]
        [LabelWidth(100)]
        public string Licese;
        
        [LabelText("密钥")]
        [LabelWidth(100)]
        public string Key;

        [FilePath(ParentFolder = "")]
        [LabelText("生成Xcode后执行")]
        [LabelWidth(200)]
        public string ExcuteShell = "DevOps/CI/BuildIOS.shell";
    }

    /// <summary>
    /// windows平台设置
    /// </summary>
    [Serializable]
    public class WindowsPlayerSetting
    {
        public string NULL;
    }
    
    /// <summary>
    /// MacOSX平台设置
    /// </summary>
    [Serializable]
    public class MacOSXSetting
    {
        public string NULL;
    }
}
