using System.IO;
using System.Net.Mime;
using BDFramework.Core.Tools;
using LitJson;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// 编辑器下application的帮助
    /// </summary>
    static public class BDEditorApplication
    {
        /// <summary>
        /// 编辑器设置
        /// </summary>
        static public BDEditorSetting BdFrameEditorSetting { get; private set; }

        /// <summary>
        /// Runtime的config
        /// </summary>
        static public BDFrameConfig  BDFrameConfig { get; private set; }
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            var config = new BDEditorSetting(BDApplication.ProjectRoot);
            BdFrameEditorSetting = config.Load();
            LoadFrameConfig();
        }
        
                
        /// <summary>
        /// 加载框架配置
        /// </summary>
        static private void LoadFrameConfig()
        {
            var content = Resources.Load<TextAsset>("BDFrameConfig").text;
            BDFrameConfig = JsonMapper.ToObject<BDFrameConfig>(content);
            //框架版本
            BDebug.Log("框架版本:" + BDFrameConfig.Version, "red");
        }
    }
}