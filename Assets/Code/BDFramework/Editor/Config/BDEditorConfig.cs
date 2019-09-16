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
        {}
        [BoxGroup("设置")]
        [LabelText("Android打包设置")]
        public AndroidConfig Android = new AndroidConfig();
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
}