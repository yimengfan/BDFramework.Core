using SQLite4Unity3d;

namespace BDFramework.Sql
{
    /// <summary>
    /// Excel版本信息
    /// </summary>
    public class TableLog
    {       
        // id
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// hash
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// 时间
        /// </summary>
        public string Date { get; set; }
        
        
        /// <summary>
        /// Unity版本
        /// </summary>
        public string UnityVersion { get; set; }
 
    }
}
