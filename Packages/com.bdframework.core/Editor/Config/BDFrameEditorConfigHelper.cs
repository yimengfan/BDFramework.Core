using System.IO;
using System.Net.Mime;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.Editor
{
    static public class BDFrameEditorConfigHelper
    {
        /// <summary>
        /// 编辑器设置
        /// </summary>
        static public BDEditorConfig EditorConfig { get; private set; }
        
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            var config = new BDEditorConfig(BDApplication.ProjectRoot);
            EditorConfig = config.Load();
        }
    }
}