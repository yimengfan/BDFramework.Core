using System;
using BDFramework.Mgr;

namespace BDFramework.Core.Debugger
{
    public class DebuggerServerProcessManager:ManagerBase<DebuggerServerProcessManager, DebuggerServerProcessAttribute>
    {
        
        public override void Start()
        {
            base.Start();
            //这里把所有的对象实例化
            //并注册解析回调
            foreach (var item in this.ClassDataMap)
            {
                var process = CreateInstance<IDebuggerServerProcess>(item.Key);
                if (process != null)
                {
                    int pid= -1;
                    if (int.TryParse(item.Value.Attribute.Tag,out pid))
                    {
                        Debugger_NetworkServer.AddLogicProcess(pid, process.OnReceiveMsg);
                    }
                }
            }
        }
    }
}