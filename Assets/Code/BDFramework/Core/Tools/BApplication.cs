using UnityEngine;

namespace Code.BDFramework.Core.Tools
{
    static public class BApplication
    {

        #region 路径相关

        /// <summary>
        /// 项目根目录
        /// </summary>
        static public string ProjectRoot { get; private set; } = Application.dataPath.Replace("/Assets", "");
        /// <summary>
        /// Library
        /// </summary>
        static public string Library { get; private set; } = ProjectRoot + "/Library";
        /// <summary>
        /// 资源的根目录
        /// </summary>
        static public string RuntimeResourceLoadPath { get; private set; } =  "Assets/Resource/Runtime";
        
        /// <summary>
        /// Editor的资源路径
        /// </summary>
        public static string EditorResourcePath { get; private set; } = "Assets/Resource_SVN";
        /// <summary>
        /// Editor的资源路径
        /// </summary>
        public static string EditorResourceLoadPath { get; private set; } = EditorResourcePath + "/Runtime";
        #endregion
        
    }
}