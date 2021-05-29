using System.IO;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BDFramework.Editor
{
    public class BDEditorSetting
    {
        public BDEditorSetting(string root)
        {
            savepath = IPath.Combine(root, "BDTemp/config.json");
        }

        public BDEditorSetting()
        {
            
        }
        
        [BoxGroup("Android设置")]
        [HideLabel]
        [InlineProperty]
        public AndroidConfig Android = new AndroidConfig();
        
        
        [BoxGroup("Ios设置")]
        [HideLabel]
        [InlineProperty]
        public IosConfig IosConfig =new IosConfig();
        
        
        [BoxGroup("Android设置[debug]")]
        [HideLabel]
        [InlineProperty]
        public AndroidConfig AndroidDebug = new AndroidConfig();
        
        [BoxGroup("Ios设置[debug]")]
        [HideLabel]
        [InlineProperty]
        public IosConfig IosConfigDebug =new IosConfig();


        [BoxGroup("资源设置")]
        [HideLabel]
        [InlineProperty]
        public BuildAssetConfig BuildAssetConfig = new BuildAssetConfig();
        //
        [DisableInEditorMode]
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
        public BDEditorSetting Load()
        {
            BDEditorSetting editorSetting= null;
            if (File.Exists(savepath))
            {
                var content = File.ReadAllText(savepath);
                editorSetting = JsonMapper.ToObject<BDEditorSetting>(content);
                editorSetting.savepath = savepath;
                return editorSetting;
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
            FileHelper.WriteAllText(savepath,JsonMapper.ToJson(this,true));
        }
    }
    
    public class AndroidConfig
    {
        [LabelText("Keystore路径")]
        [FilePath(Extensions = "keystore")]
        public  string keystoreName;
        [LabelText("Keystore密码")]
        public  string keystorePass;
        [LabelText("keyalias")]
        public  string keyaliasName;
        [LabelText("keyalias密码")]
        public  string keyaliasPass;
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
        public string AESCode  = "bdframe$#@!@#";
        [LabelText("是否hash命名")]
        public bool IsUseHashName = false;
        [LabelText("上传接口")]
        public string AssetBundleFileServerUrl = "http://127.0.0.1:20001";

        [LabelText("自动生成热更DLL")]
        public bool IsAutoBuildDll = true;
    }
}