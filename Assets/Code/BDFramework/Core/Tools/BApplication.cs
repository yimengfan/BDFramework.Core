using UnityEngine;

namespace Code.BDFramework.Core.Tools
{
   static public class BApplication
    {
        /// <summary>
        /// 项目根目录
        /// </summary>
       static public string projroot
       {
           get { return Application.dataPath.Replace("/Assets", ""); }
       }

        /// <summary>
        /// 资源的根目录
        /// </summary>
        static public string ResourceRoot
        {
            get
            {
                 return Application.dataPath+"/Resource/Runtime"; 
            }
        }
    }
}