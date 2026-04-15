namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏配置加载器。
    /// Game configuration loader.
    /// </summary>
    public class GameConfigLoder
    {
        /// <summary>
        /// 加载框架基础配置。
        /// Load the framework base configuration.
        /// 该入口只负责在配置管理器实例已经可用时触发正式启动，避免在启动早期误调用空实例。
        /// This entry only starts the real configuration workflow when the configuration manager instance is already available, avoiding accidental calls against a missing instance during early startup.
        /// </summary>
        public static void LoadFrameworkConfig()
        {
            if (!GameConfigStartupPureLogic.ShouldLoadFrameworkConfigManager(GameConfigManager.Inst != null))
            {
                return;
            }

            GameConfigManager.Inst.Start();
        }
    }
}
