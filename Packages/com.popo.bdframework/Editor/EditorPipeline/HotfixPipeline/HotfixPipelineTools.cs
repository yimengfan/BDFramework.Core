using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using LitJson;

namespace BDFramework.Editor.HotfixPipeline
{
    static public class HotfixPipelineTools
    {
        public static HotfixFileConfigLogic HotfixFileConfig { get; private set; } = new HotfixFileConfigLogic();

        /// <summary>
        /// 热更文件配置路径
        /// </summary>
        private static string HOTFIX_FILE_CONG_PATH;

        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            HOTFIX_FILE_CONG_PATH = BApplication.DevOpsConfigPath + "/HotfixFile.conf";
            //加载
            HotfixFileConfig.Load(HOTFIX_FILE_CONG_PATH);
            
        }


        /// <summary>
        /// 保存
        /// </summary>
        static public void Save()
        {
            HotfixFileConfig.Save(HOTFIX_FILE_CONG_PATH);
        }
    }
}