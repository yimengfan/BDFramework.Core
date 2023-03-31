namespace BDFramework.DataListener
{
    /// <summary>
    /// 这个是基本数据的服务
    /// </summary>
    public class StatusListenerService : AStatusListener
    {
        /// <summary>
        /// Service名字
        /// </summary>
        public string Name { get; private set; }
        
        public StatusListenerService()
        {
            
        }
    }
}