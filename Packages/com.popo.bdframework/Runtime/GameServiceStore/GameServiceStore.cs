using System;
using System.Collections.Generic;

namespace BDFramework.GameServiceStore
{
    /// <summary>
    /// 游戏服务仓库
    /// </summary>
    static public class GameServiceStore
    {
        static private Dictionary<Type, GameService> serviceStoreMap = new Dictionary<Type, GameService>();
    }
}
