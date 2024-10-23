using System;
using BDFramework.Mgr;

namespace BDFramework.Core.Debugger
{
    public class
        DebuggerServerProcessManager : ManagerBase<DebuggerServerProcessManager, DebuggerServerProcessAttribute>
    {
        public override void Start()
        {
            base.Start();
            //这里把所有的对象实例化
            //并注册解析回调
            foreach (var cd in this.GetAllClassDatas())
            {
                var process = CreateInstance<IDebuggerServerProcess>(cd);
                if (process != null)
                {
                    int pid = cd.Attribute.IntTag;
                    Debugger_NetworkServer.AddLogicProcess(pid, process.OnReceiveMsg);
                }
            }
        }
    }
}