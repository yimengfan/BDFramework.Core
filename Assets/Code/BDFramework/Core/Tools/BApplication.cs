using UnityEngine;

namespace Code.BDFramework.Core.Tools
{
    static public class BApplication
    {
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
        static public string ResourceRoot { get; private set; } = Application.dataPath + "/Resource/Runtime";
    }
}