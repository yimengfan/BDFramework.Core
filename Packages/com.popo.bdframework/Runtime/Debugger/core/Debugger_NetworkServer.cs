using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BDFramework.Core.Tools;
using Telepathy;
using UnityEngine;
using UnityEngine.Networking;

namespace BDFramework.Core.Debugger
{
    /// <summary>
    /// 游戏runtime 为Server端,
    /// 编辑器为Client端,不断请求数据,服务器进行回复
    /// </summary>
    static public class Debugger_NetworkServer
    {
        private static Server server;

        /// <summary>
        /// 启动Tcp监听
        /// </summary>
        /// <param name="point"></param>
        static public void Start(int point = 10001)
        {
            if (server != null) return;

            server = new Server();
            server.Start(point);
            Debug.Log("网络调试器启动成功,地址:" + DebuggerNetworkTools.GetIP(AddressFam.IPv4) + ":" + point);
            Task.Factory.StartNew(() =>
            {
                //这里同步可以这么做，如果是异步，必须每个都new新的
                Message msg;
                while (true)
                {
                    while (server.GetNextMessage(out msg))
                    {
                        switch (msg.eventType)
                        {
                            case Telepathy.EventType.Connected:
                                Debug.Log("调试器客户端连接成功：" + msg.connectionId);
                                break;
                            case Telepathy.EventType.Data:
                                try
                                {
                                    ParseProtocal(msg.connectionId, msg.data);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(e);
                                }

                                break;
                            case Telepathy.EventType.Disconnected:
                                Debug.Log("调试器客户端断开连接：" + msg.connectionId);
                                break;
                        }
                    }
                }

            });
        }

        /// <summary>
        /// 解析协议
        /// 前面4个字节是协议号,后面是协议内容
        /// </summary>
        /// <param name="bytes"></param>
        public static void ParseProtocal(int cid, byte[] bytes)
        {
            // sBytes = 
            byte[] pidBytes = new byte[4]; //Array.Copy(bytes,)
            byte[] contentBytes = new byte[bytes.Length - 4];

            Buffer.BlockCopy(bytes, 0, pidBytes, 0, 4);
            Buffer.BlockCopy(bytes, 4, contentBytes, 0, bytes.Length - 4);
            //protocal id
            var pid = BitConverter.ToInt32(pidBytes, 0);
            var content = System.Text.Encoding.UTF8.GetString(contentBytes);

            //发送回调
            List<OnReceiveMsg> list = null;
            if (callbackMap.TryGetValue(pid, out list))
            {
                foreach (var onReceiveMsg in list)
                {
                    //服务器返回
                    var retBytes = onReceiveMsg(content);
                    if (retBytes.Length > 0)
                    {
                        var retlist = new List<byte>();

                        //协议号+byte名
                        retlist.AddRange(BitConverter.GetBytes(pid));
                        retlist.AddRange(retBytes);
                        server.Send(cid, retlist.ToArray());
                    }
                }
            }
        }



        static Dictionary<int, List<OnReceiveMsg>> callbackMap = new Dictionary<int, List<OnReceiveMsg>>();

        public delegate byte[] OnReceiveMsg(string content);

        /// <summary>
        /// 添加逻辑处理
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        public static void AddLogicProcess(int id, OnReceiveMsg callback)
        {

            List<OnReceiveMsg> list = null;

            if (!callbackMap.TryGetValue(id, out list))
            {
                list = new List<OnReceiveMsg>();
                callbackMap[id] = list;
            }

            var t = callback.GetType();
            if (list.Find((i)=>i.GetType().FullName==t.FullName)==null)
            {
                list.Add(callback);
            }
        }
    }
}
