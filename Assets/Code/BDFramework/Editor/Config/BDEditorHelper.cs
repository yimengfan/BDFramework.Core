using System.IO;
using System.Net.Mime;
using UnityEngine;

namespace Code.BDFramework.Editor
{
    static public class BDEditorHelper
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
            var absRoot = Application.dataPath.Replace("/Assets", "");
            var config = new BDEditorConfig(absRoot);
            EditorConfig = config.Load();
        }
    }
}