using System;

namespace BDFramework.Mgr
{
    /// <summary>
    /// 管理器的执行顺序
    /// </summary>
    public class ManagerOrder : Attribute
    {
        public int Order { get; set; } = 0;
    }
}
