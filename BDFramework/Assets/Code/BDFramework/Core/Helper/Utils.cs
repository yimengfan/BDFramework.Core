using UnityEngine;

namespace BDFramework.Helper
{
   static public class Utils
    {
        /// <summary>
        /// 平台资源的父路径
        /// </summary>
        public static string ResourcePlatformPath
        {
            get
            {
                switch (Application.platform)
                {    
                    case RuntimePlatform.Android:
                        return "Android";
                    case RuntimePlatform.IPhonePlayer:
                        return "iOS";
                    case  RuntimePlatform.WindowsEditor:
                        case RuntimePlatform.WindowsPlayer:
                        return "Windows";                  
                }
                return "";
            }
        }
    }
}