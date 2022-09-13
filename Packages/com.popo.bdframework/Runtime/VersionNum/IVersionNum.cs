namespace BDFramework.VersionNum
{
    /// <summary>
    /// 版本号信息
    /// </summary>
    public interface IVersionNum
    {
        /// <summary>
        /// 客户端版本号
        /// </summary>
        string ClientVersionNum { get; }
        
        /// <summary>
        /// 资产版本号
        /// </summary>
        string AssetsVersionNum { get; }
        
        /// <summary>
        /// 本地数据库版本号
        /// </summary>
        string LocalSqlVersionNum { get; }
        
    }
}
