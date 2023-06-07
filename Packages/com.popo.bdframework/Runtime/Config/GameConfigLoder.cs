using System.Linq;

namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏配置加载器
    /// </summary>
    public class GameConfigLoder
    {
        /// <summary>
        /// 加载框架基本配置
        /// </summary>
        public static void LoadFrameBaseConfig()
        {
            if (GameConfigManager.Inst != null)
            {
                GameConfigManager.Inst.Start();
            }
        }
    }
}
