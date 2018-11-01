using UnityEngine;

namespace BDFramework.Core.Helper
{
   static public class Config
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