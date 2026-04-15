using System;
using BDFramework.Sql;
using Sirenix.OdinInspector;

namespace BDFramework.Configure
{
    /// <summary>
    /// 游戏加密处理器。
    /// 负责将加密配置（SQLite 密码、DLL 公私钥）注入到运行时对应模块，
    /// 并通过 <see cref="SqliteLoder.PasswordFallback"/> 解除 Config→Sql 的双向依赖。
    /// </summary>
    [GameConfig(2,"加密")]
    public class GameCipherConfigProcessor : IConfigProcessor
    {
        /// <summary>
        /// 游戏加密设置
        /// </summary>
        public class Config : ConfigDataBase
        {
            /// <summary>
            /// 数据库密码
            /// </summary>
            [LabelTextAttribute("Sqlite密码")]
            public string SqlitePassword = "password123!!!";
            /// <summary>
            /// 公钥
            /// </summary>
            [LabelTextAttribute("DLL公钥")]
            public string ScriptPubKey = "";
            /// <summary>
            /// 私钥
            /// </summary>
            [LabelTextAttribute("DLL私钥")]
            public string ScriptPrivateKey = "";
        }


        /// <summary>
        /// 当加载成功时，将加密配置注入到 SQLite 加载器和脚本系统。
        /// 同时注册 <see cref="SqliteLoder.PasswordFallback"/>，确保未显式设置密码时也能通过配置获取默认值。
        /// </summary>
        /// <param name="config">加密配置数据</param>
        public  void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            //注入密码回退回调，解除 Config→Sql 直接依赖
            SqliteLoder.PasswordFallback = () => con.SqlitePassword;
            //Sqlite秘钥
            SqliteLoder.Password = con.SqlitePassword;

        }
    }
    
    

}
