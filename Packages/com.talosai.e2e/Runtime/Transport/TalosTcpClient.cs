using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Talos.E2E.Transport
{
    /// <summary>
    /// TCP 测试客户端——用于 Playwright 端（或 Unity Editor 模式下）连接到 TalosTcpServer。
    /// 此类主要用于 Editor 调试和本地集成测试场景；CI 流程中 Playwright 端使用 Node.js 实现连接。
    /// 
    /// 设计角色：
    /// - 提供与 TalosTcpServer 相同的长度前缀协议的客户端实现。
    /// - 在 Editor 中可用于模拟 Playwright 端发送测试指令。
    /// </summary>
    public class TalosTcpClient
    {
        /// <summary>
        /// 是否已连接到服务端。
        /// </summary>
        public bool Connected => _client != null && _client.Connected;

        /// <summary>
        /// 收到消息时的回调。参数为 JSON 字符串。
        /// </summary>
        public event Action<string> OnMessage;

        /// <summary>
        /// 连接断开时的回调。
        /// </summary>
        public event Action OnDisconnected;

        private TcpClient _client;
        private Thread _receiveThread;
        private volatile bool _isRunning;

        /// <summary>
        /// 连接到指定的 TCP 测试服务端。
        /// </summary>
        /// <param name="host">服务端 IP 地址。</param>
        /// <param name="port">服务端端口号。</param>
        /// <param name="timeoutMs">连接超时毫秒数，默认 5000ms。</param>
        public void Connect(string host, int port, int timeoutMs = 5000)
        {
            _client = new TcpClient();
            var result = _client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(timeoutMs);

            if (!success)
            {
                _client.Close();
                throw new TimeoutException($"[TalosE2E] 连接超时: {host}:{port}");
            }

            _client.EndConnect(result);
            _isRunning = true;

            // 启动接收线程
            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "TalosE2E-TCP-Client-Recv"
            };
            _receiveThread.Start();

            Debug.Log($"[TalosE2E] 已连接到服务端: {host}:{port}");
        }

        /// <summary>
        /// 断开连接并释放资源。
        /// </summary>
        public void Disconnect()
        {
            _isRunning = false;
            try { _client?.Close(); } catch { }
            _client = null;
        }

        /// <summary>
        /// 发送 JSON 消息到服务端。
        /// </summary>
        /// <param name="jsonMessage">要发送的 JSON 字符串。</param>
        public void Send(string jsonMessage)
        {
            if (!Connected) throw new InvalidOperationException("[TalosE2E] 未连接，无法发送消息");

            var body = Encoding.UTF8.GetBytes(jsonMessage);
            var header = new byte[4];
            header[0] = (byte)(body.Length >> 24);
            header[1] = (byte)(body.Length >> 16);
            header[2] = (byte)(body.Length >> 8);
            header[3] = (byte)(body.Length);

            var stream = _client.GetStream();
            stream.Write(header, 0, 4);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        #region 内部实现

        /// <summary>
        /// 接收消息循环，在后台线程持续读取服务端推送的消息。
        /// </summary>
        private void ReceiveLoop()
        {
            try
            {
                var stream = _client.GetStream();
                while (_isRunning && _client.Connected)
                {
                    // 读取长度前缀
                    var lengthBytes = new byte[4];
                    int offset = 0;
                    while (offset < 4)
                    {
                        int read = stream.Read(lengthBytes, offset, 4 - offset);
                        if (read == 0) goto disconnected;
                        offset += read;
                    }

                    var length = (lengthBytes[0] << 24) | (lengthBytes[1] << 16) | (lengthBytes[2] << 8) | lengthBytes[3];
                    if (length <= 0 || length > 10 * 1024 * 1024) continue;

                    // 读取消息体
                    var bodyBytes = new byte[length];
                    offset = 0;
                    while (offset < length)
                    {
                        int read = stream.Read(bodyBytes, offset, length - offset);
                        if (read == 0) goto disconnected;
                        offset += read;
                    }

                    var msg = Encoding.UTF8.GetString(bodyBytes);
                    OnMessage?.Invoke(msg);
                }

            disconnected:
                if (_isRunning)
                {
                    OnDisconnected?.Invoke();
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Debug.LogWarning($"[TalosE2E] 客户端接收异常: {ex.Message}");
                    OnDisconnected?.Invoke();
                }
            }
        }

        #endregion
    }
}
