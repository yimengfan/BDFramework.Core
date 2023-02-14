namespace BDFramework.Configure
{
    /// <summary>
    /// Config处理器的基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract public class AConfigProcessor
    {

        /// <summary>
        /// 当config加载
        /// </summary>
        /// <param name="config"></param>
        virtual public void OnConfigLoad(ConfigDataBase config)
        {
            
        }
    }
}
