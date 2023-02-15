namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏配置加载器
    /// </summary>
    public class GameConfigLoder
    {
        /// <summary>
        /// 加载
        /// </summary>
        public static void Load()
        {
            if (GameConfigManager.Inst != null)
            {
                GameConfigManager.Inst.Start();
            }
            
        }
    }
}
