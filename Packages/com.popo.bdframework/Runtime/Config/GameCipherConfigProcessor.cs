using System;

namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏加密处理器
    /// </summary>
    [GameConfig(2)]
    public class GameCipherConfigProcessor : AConfigProcessor
    {
        /// <summary>
        /// 游戏加密设置
        /// </summary>
        public class Config : ConfigDataBase
        {
            /// <summary>
            /// 数据库密码
            /// </summary>
            public string SqlitePassword = "password123";
            /// <summary>
            /// 公钥
            /// </summary>
            public string ScriptPubKey = "";
            /// <summary>
            /// 私钥
            /// </summary>
            public string ScriptPrivateKey = "";
        }
        
        
        
    }
    
    

}
