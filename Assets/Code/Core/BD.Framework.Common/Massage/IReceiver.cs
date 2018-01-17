using System;

public interface IReceiver {
    /// <summary>
    /// 接受消息
    /// </summary>
    /// <param name="msg">消息枚举</param>
    void ReceiveMessage(int msg, object _params = null, Action callback = null);

}

