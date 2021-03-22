using System.IO;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.BDFramework.Editor
{
    public class BDEditorConfig
    {
        public BDEditorConfig(string root)
        {
            savepath = IPath.Combine(root, "BDTemp/config.json");
        }

        public BDEditorConfig()
        {
            
        }
        
        [BoxGroup("Android设置")]
        [HideLabel]
        [InlinePropertyAttribute]
        public AndroidConfig Android = new AndroidConfig();
        
        
        [BoxGroup("Ios设置")]
        [HideLabel]
        [InlinePropertyAttribute]
        public IosConfig IosConfig =new IosConfig();
        
        
        [BoxGroup("Android设置[debug]")]
        [HideLabel]
        [InlinePropertyAttribute]
        public AndroidConfig AndroidDebug = new AndroidConfig();
        
        [BoxGroup("Ios设置[debug]")]
        [HideLabel]
        [InlinePropertyAttribute]
        public IosConfig IosConfigDebug =new IosConfig();


        [BoxGroup("资源设置")]
        [HideLabel]
        [InlinePropertyAttribute]
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
        public BDEditorConfig Load()
        {
            BDEditorConfig editorConfig= null;
            if (File.Exists(savepath))
            {
                var content = File.ReadAllText(savepath);
                editorConfig = JsonMapper.ToObject<BDEditorConfig>(content);
                editorConfig.savepath = savepath;
                return editorConfig;
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
        public string AssetBundleFileServerUrl = "http://127.0.0.1:20000";
    }
}