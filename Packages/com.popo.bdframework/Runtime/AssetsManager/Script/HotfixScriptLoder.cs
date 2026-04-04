using BDFramework.Mgr;


namespace BDFramework
{
    /// <summary>
    /// 脚本加载器
    /// </summary>
    static public class HotfixScriptLoder
    {
        private static readonly string Tag = "ScriptLoder";

        /// <summary>
        /// aot patch路径
        /// </summary>
        static readonly public string HYCLR_AOT_PATCH_PATH = $"script/aot_patch";
        /// <summary>
        /// 热更dll定义
        /// </summary>
        static readonly public string HOTFIX_DLL_PATH = $"script/hotfix";
        /// <summary>
        /// 热更代码后缀
        /// </summary>
        static readonly public string HOT_DLL_EXTENSION = ".zlua.bytes";
        

        /// <summary>
        /// 整个游戏的启动器
        /// </summary>
        /// <param name="mainProjectTypes"></param>
        static public void Start()
        {
            //list
            var mainProjectTypes = ManagerInstHelper.GetHostingTypes();
            //启动主工程的管理器
            ManagerInstHelper.LoadManager(mainProjectTypes);
            //开始
            ManagerInstHelper.Start();
        }

   
    }
}
