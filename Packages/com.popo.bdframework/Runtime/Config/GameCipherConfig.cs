using System;

namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏密码设置
    /// </summary>
    [Serializable]
    public class GameCipherConfig
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
