namespace BDFramework.Core.Debugger
{
    public interface IDebuggerServerProcess
    {

        /// <summary>
        /// 当接收到消息
        /// </summary>
        /// <returns></returns>
        byte[] OnReceiveMsg(string content);

    }
}