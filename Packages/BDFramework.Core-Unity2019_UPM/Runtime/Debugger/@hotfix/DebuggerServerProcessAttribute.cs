
using BDFramework.Mgr;

namespace BDFramework.Core.Debugger
{
    public class DebuggerServerProcessAttribute : ManagerAttribute
    {
        /// <summary>
        /// ID 是协议号
        /// </summary>
        /// <param name="Id"></param>
        public DebuggerServerProcessAttribute(int Id) : base(Id)
        {
            
        }
    }
}