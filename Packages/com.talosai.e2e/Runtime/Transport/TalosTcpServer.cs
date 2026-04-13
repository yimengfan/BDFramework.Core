using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Talos.E2E.Transport
{
    /// <summary>
    /// TCP 测试服务端——在 Unity Player 内运行，接受 Playwright 端连接并处理测试协议。
    /// 
    /// 设计角色：
    /// - 在 Android/iOS/Windows/macOS 真机上监听 TCP 端口。
    /// - PC 端 Playwright 通过网络直连或 ADB forward 连接到此端口。
    /// - 所有消息使用长度前缀 + UTF-8 JSON 编码。
    /// 
    /// 使用方式：
    /// <code>
    /// var server = new TalosTcpServer(10002);
    /// server.OnMessage += (msg) => { /* 处理消息 */ };
    /// server.Start();
    /// </code>
    /// </summary>
    public class TalosTcpServer
    {
        /// <summary>
        /// 监听端口，默认 10002。
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// 服务是否正在运行。
        /// </summary>
        public bool IsRunning => _listener != null && _isRunning;

        /// <summary>
        /// 已连接的客户端数量。
        /// </summary>
        public int ConnectedClients => _clients.Count;

        /// <summary>
        /// 收到消息时的回调。参数为 JSON 字符串。
        /// </summary>
        public event Action<string> OnMessage;

        /// <summary>
        /// 客户端连接时的回调。
        /// </summary>
        public event Action<int> OnClientConnected;

        /// <summary>
        /// 客户端断开时的回调。
        /// </summary>
        public event Action<int> OnClientDisconnected;

        /// <summary>
        /// 服务端内部错误回调。
        /// </summary>
        public event Action<string> OnError;

        private TcpListener _listener;
        private Thread _listenerThread;
        private volatile bool _isRunning;
        private readonly Dictionary<int, TcpClient> _clients = new Dictionary<int, TcpClient>();
        private int _clientIdCounter;

        /// <summary>
        /// 构造 TCP 测试服务端。
        /// </summary>
        /// <param name="port">监听端口号，默认使用 Protocol.DefaultPort。</param>
        public TalosTcpServer(int port = Protocol.DefaultPort)
        {
            Port = port;
        }

        /// <summary>
        /// 启动 TCP 监听，在后台线程等待客户端连接。
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _listener = TcpListener.Create(Port);
            _listener.Start();

            _listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "TalosE2E-TCP-Listener"
            };
            _listenerThread.Start();

            Debug.Log($"[TalosE2E] TCP 服务端已启动，监听端口: {Port}");
        }

        /// <summary>
        /// 停止 TCP 监听，关闭所有客户端连接。
        /// </summary>
        public void Stop()
        {
            _isRunning = false;

            // 关闭所有客户端连接
            lock (_clients)
            {
                foreach (var kvp in _clients)
                {
                    try { kvp.Value.Close(); } catch { }
                }
                _clients.Clear();
            }

            // 停止监听器
            try { _listener?.Stop(); } catch { }
            _listener = null;

            Debug.Log("[TalosE2E] TCP 服务端已停止");
        }

        /// <summary>
        /// 向所有已连接的客户端广播消息。
        /// </summary>
        /// <param name="jsonMessage">要发送的 JSON 字符串。</param>
        public void Broadcast(string jsonMessage)
        {
            byte[] payload = EncodeMessage(jsonMessage);
            lock (_clients)
            {
                foreach (var kvp in _clients)
                {
                    SendRaw(kvp.Value, payload);
                }
            }
        }

        /// <summary>
        /// 向指定客户端发送消息。
        /// </summary>
        /// <param name="clientId">客户端 ID。</param>
        /// <param name="jsonMessage">要发送的 JSON 字符串。</param>
        public void Send(int clientId, string jsonMessage)
        {
            TcpClient client;
            lock (_clients)
            {
                if (!_clients.TryGetValue(clientId, out client))
                {
                    Debug.LogWarning($"[TalosE2E] 客户端 {clientId} 不存在，无法发送消息");
                    return;
                }
            }
            SendRaw(client, EncodeMessage(jsonMessage));
        }

        #region 内部实现

        /// <summary>
        /// 监听循环，在后台线程持续接受新连接。
        /// </summary>
        private void ListenLoop()
        {
            try
            {
                while (_isRunning)
                {
                    var client = _listener.AcceptTcpClient();
                    var id = System.Threading.Interlocked.Increment(ref _clientIdCounter);

                    lock (_clients)
                    {
                        _clients[id] = client;
                    }

                    // 在线程池中处理该客户端
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(id, client));

                    OnClientConnected?.Invoke(id);
                    Debug.Log($"[TalosE2E] 客户端已连接: #{id} ({client.Client.RemoteEndPoint})");
                }
            }
            catch (SocketException ex)
            {
                if (_isRunning)
                {
                    OnError?.Invoke($"监听异常: {ex.Message}");
                    Debug.LogError($"[TalosE2E] 监听异常: {ex}");
                }
            }
        }

        /// <summary>
        /// 处理单个客户端的连接，持续读取消息直到断开。
        /// </summary>
        private void HandleClient(int clientId, TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    while (_isRunning && client.Connected)
                    {
                        var msg = ReadMessage(stream);
                        if (msg == null) break;
                        OnMessage?.Invoke(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Debug.LogWarning($"[TalosE2E] 客户端 #{clientId} 读取异常: {ex.Message}");
                }
            }
            finally
            {
                lock (_clients)
                {
                    _clients.Remove(clientId);
                }
                try { client.Close(); } catch { }
                OnClientDisconnected?.Invoke(clientId);
                Debug.Log($"[TalosE2E] 客户端已断开: #{clientId}");
            }
        }

        /// <summary>
        /// 从网络流中读取一条长度前缀的消息。
        /// 格式：4字节大端长度 + UTF-8 字符串。
        /// </summary>
        /// <returns>解码后的 JSON 字符串，连接关闭时返回 null。</returns>
        private string ReadMessage(NetworkStream stream)
        {
            // 读取 4 字节长度前缀
            var lengthBytes = new byte[4];
            if (!ReadExact(stream, lengthBytes, 4)) return null;

            // 大端序转 int
            var length = (lengthBytes[0] << 24) | (lengthBytes[1] << 16) | (lengthBytes[2] << 8) | lengthBytes[3];
            if (length <= 0 || length > 10 * 1024 * 1024) // 最大 10MB
            {
                Debug.LogError($"[TalosE2E] 消息长度异常: {length}");
                return null;
            }

            // 读取消息体
            var bodyBytes = new byte[length];
            if (!ReadExact(stream, bodyBytes, length)) return null;

            return Encoding.UTF8.GetString(bodyBytes);
        }

        /// <summary>
        /// 精确读取指定字节数，阻塞直到读完或连接关闭。
        /// </summary>
        private static bool ReadExact(NetworkStream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        /// <summary>
        /// 将 JSON 字符串编码为长度前缀格式的字节数组。
        /// </summary>
        private static byte[] EncodeMessage(string jsonMessage)
        {
            var body = Encoding.UTF8.GetBytes(jsonMessage);
            var header = new byte[4];
            header[0] = (byte)(body.Length >> 24);
            header[1] = (byte)(body.Length >> 16);
            header[2] = (byte)(body.Length >> 8);
            header[3] = (byte)(body.Length);

            var result = new byte[4 + body.Length];
            Buffer.BlockCopy(header, 0, result, 0, 4);
            Buffer.BlockCopy(body, 0, result, 4, body.Length);
            return result;
        }

        /// <summary>
        /// 向客户端发送原始字节数据。
        /// </summary>
        private void SendRaw(TcpClient client, byte[] data)
        {
            try
            {
                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TalosE2E] 发送消息失败: {ex.Message}");
            }
        }

        #endregion
    }
}
