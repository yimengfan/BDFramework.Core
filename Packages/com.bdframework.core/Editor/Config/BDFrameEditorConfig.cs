using System.IO;
using LitJson;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace BDFramework.Editor
{
/// <summary>
/// 编辑器所有设置
/// </summary>
    public class BDFrameEditorConfig
    {
        public BDFrameEditorConfig(string root)
        {
            savepath = IPath.Combine(root, "BDTemp/config.json");
        }

        public BDFrameEditorConfig()
        {
        }
#if ODIN_INSPECTOR
        [BoxGroup("Android设置")]
        [HideLabel]
        [InlineProperty]
#endif
        public AndroidConfig Android = new AndroidConfig();

#if ODIN_INSPECTOR
        [BoxGroup("Ios设置")]
        [HideLabel]
        [InlineProperty]
#endif
        public IosConfig IosConfig = new IosConfig();

#if ODIN_INSPECTOR
        [BoxGroup("Android设置[debug]")]
        [HideLabel]
        [InlineProperty]
#endif
        public AndroidConfig AndroidDebug = new AndroidConfig();
#if ODIN_INSPECTOR
        [BoxGroup("Ios设置[debug]")]
        [HideLabel]
        [InlineProperty]
#endif
        public IosConfig IosConfigDebug = new IosConfig();

#if ODIN_INSPECTOR
        [BoxGroup("资源设置")]
        [HideLabel]
        [InlineProperty]
#endif
        public BuildAssetConfig BuildAssetConfig = new BuildAssetConfig();
        //
#if ODIN_INSPECTOR
        [DisableInEditorMode]
#endif
        public string savepath = "";

        /// <summary>
        /// 检查config设置
        /// </summary>
        /// <returns></returns>
        public bool IsSetConfig()
        {
            return File.Exists(savepath);
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public BDFrameEditorConfig Load()
        {
            BDFrameEditorConfig frameEditorConfig = null;
            if (File.Exists(savepath))
            {
                var content = File.ReadAllText(savepath);
                frameEditorConfig = JsonMapper.ToObject<BDFrameEditorConfig>(content);
                frameEditorConfig.savepath = savepath;
                return frameEditorConfig;
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            FileHelper.WriteAllText(savepath, JsonMapper.ToJson(this, true));
        }
    }

    /// <summary>
    /// Android配置
    /// </summary>
    public class AndroidConfig
    {
#if ODIN_INSPECTOR
        [LabelText("Keystore路径")]
        [FilePath(Extensions = "keystore")]
#endif
        public string keystoreName;
#if ODIN_INSPECTOR
        [LabelText("Keystore密码")]
#endif
        public string keystorePass;
#if ODIN_INSPECTOR
        [LabelText("keyalias")]
#endif
        public string keyaliasName;
#if ODIN_INSPECTOR
        [LabelText("keyalias密码")]
#endif
        public string keyaliasPass;
    }


    /// <summary>
    /// IOS配置
    /// </summary>
    public class IosConfig
    {
#if ODIN_INSPECTOR
        [LabelText("证书")]
#endif
        public string test;

#if ODIN_INSPECTOR
        [LabelText("密钥")]
#endif
        public string test2;
    }


    /// <summary>
    /// 打包设置
    /// </summary>
    public class BuildAssetConfig
    {
#if ODIN_INSPECTOR
        [LabelText("ASE密钥")]
#endif
        public string AESCode = "bdframe$#@!@#";

#if ODIN_INSPECTOR
        [LabelText("是否hash命名")]
#endif
        public bool IsUseHashName = false;

#if ODIN_INSPECTOR
        [LabelText("上传接口")]
#endif
        public string AssetBundleFileServerUrl = "http://127.0.0.1:20001";

#if ODIN_INSPECTOR
        [LabelText("自动生成热更DLL")]
#endif
        public bool IsAutoBuildDll = true;
    }
}