using BDFramework.DataListener;
namespace Game
{
    static public class Client
    {
        /// <summary>
        /// Status监听测试
        /// </summary>
        static public StatusListenerService StatusListenerServiceTest { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            //初始化
            StatusListenerServiceTest = new StatusListenerService();
        }
    }
}