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
        static public BDFrameConfig BDFrameConfig { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            BdFrameEditorSetting = BDEditorSetting.Load();
            BDFrameConfig = BDFrameConfig.Load();
        }
        
    }
}