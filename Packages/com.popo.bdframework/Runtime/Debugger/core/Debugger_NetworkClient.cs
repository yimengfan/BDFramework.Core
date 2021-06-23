using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telepathy;
using UnityEngine;

namespace BDFramework.Core.Debugger
{
    static public class Debugger_NetworkClient
    {
        static public Client Client { get; private set; }


        static public void Connect(string ip,int port)
        {
            Client = new Client();
            Client.Connect(ip,port);

            Task.Factory.StartNew(() =>
            {
                //这里同步可以这么做，如果是异步，必须每个都new新的
                Message msg;
                while (true)
                {
                    while (Client.GetNextMessage(out msg))
                    {
                        switch (msg.eventType)
                        {
                            case Telepathy.EventType.Connected:
                                break;
                            case Telepathy.EventType.Data:
                                ParseProtocal(msg.data);
                                break;
                            case Telepathy.EventType.Disconnected:

                                break;
                        }
                    }
                }
            });
            
        }
        
        static public void Send(int pid,string content)
        {
            if (Client == null|| !Client.Connected)
            {
                Connect("127.0.0.1", 10001);
                
                return;
            }
                
            List<byte> list = new List<byte>();
            var b1 = BitConverter.GetBytes(pid);
            var b2 = System.Text.Encoding.UTF8.GetBytes(content);
            list.AddRange(b1);
            list.AddRange(b2);
            Client.Send(list.ToArray());
        }

        #region 添加处理回调


        /// <summary>
        /// 解析协议
        /// 前面4个字节是协议号,后面是协议内容
        /// </summary>
        /// <param name="bytes"></param>
        public static void ParseProtocal(byte[] bytes)
        {
            byte[] pidBytes = new byte[4];//Array.Copy(bytes,)
            byte[] contentBytes = new byte[bytes.Length - 4];
            Buffer.BlockCopy(bytes,0, pidBytes,0, 4);
            Buffer.BlockCopy(bytes,4, contentBytes,0, bytes.Length - 4);
            var pid = BitConverter.ToInt32(pidBytes,0);
            //发送回调
            List<Action<byte[]>> list = null;
            if (callbackMap.TryGetValue(pid, out list))
            {
                foreach (var callback in list)
                {
                    //处理服务器返回
                    try
                    {
                        callback(contentBytes);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                   
                }
            }
        }



        static Dictionary<int,List<Action<byte[]>>> callbackMap = new Dictionary<int, List<Action<byte[]>>>();
        /// <summary>
        /// 添加逻辑处理
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        public static void AddListener(int id, Action<byte[]> callback)
        {
            List<Action<byte[]>> list = null;
            if (!callbackMap.TryGetValue(id, out list))
            {
                list = new List<Action<byte[]>>();
                callbackMap[id] = list;
            }
            if (!list.Contains(callback))
            {
                list.Add(callback);
            }
        }
        

        #endregion
        
        

    }
}