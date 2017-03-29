using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections;
namespace ConsoleApplication1
{
    public class UDPClient
    {

        private UdpClient udpClient;
        private Dictionary<string, Action<string>> cmdDictionay;

        public UDPClient()
        {
            this.cmdDictionay = new Dictionary<string, Action<string>>();
        }

        /// <summary>
        /// 开启
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Start(string ip = "127.0.0.1",int port = 8888)
        {
            Console.WriteLine("=>等待untiy3d消息...");
            System.Net.IPEndPoint localip = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port);
            udpClient = new UdpClient(localip);
        }

       
        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            udpClient.Close();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ip"></param>
        public void SendMsg(string cmd,string msg,IPEndPoint ip)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(EncodeMsg(cmd,msg));
            udpClient.Send(bytes,bytes.Length,ip);
        }

        /// <summary>
        /// 接受消息
        /// </summary>
        private void Receive()
        {
            new Thread(new ThreadStart(() =>
            {
                IPEndPoint remoteIpep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    try
                    {
                        var bytes = udpClient.Receive(ref remoteIpep);
                        string msg = Encoding.Unicode.GetString(bytes, 0, bytes.Length);
                        DecodeMsg(msg);
                    }
                    catch
                    {

                    }
                }


            })).Start();


        }
        /// <summary>
        /// 加码消息体
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private string EncodeMsg(string cmd,string content)
        {

            return string.Format("{{0}}{1}",cmd,content);
        }
        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="msg"></param>
        private void DecodeMsg(string msg)
        {

        }
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="action"></param>
        public void RegisterCmdListener(string cmd,Action<string,IPEndPoint> action)
        {

        }

    }
}
