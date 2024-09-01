namespace BDFramework.Configure
{
    /// <summary>
    /// Config处理器的基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigProcessor
    {
        /// <summary>
        /// 当config加载
        /// </summary>
        /// <param name="config"></param>
        void OnConfigLoad(ConfigDataBase config);
    }
}