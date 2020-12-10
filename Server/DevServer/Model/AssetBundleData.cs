using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DevServer.Model
{

    
    public class AssetBundleData
    {

        /// <summary>
        /// 自增id
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 工程名
        /// </summary>
        public string ProjName { get; set; }
        
        /// <summary>
        /// 平台
        /// </summary>
        public string Platform { get; set; }
        /// <summary>
        /// 最新的version
        /// </summary>
        public int Version { get; set; }
        
        //时间
        public string Timer { get; set; }
    }
}