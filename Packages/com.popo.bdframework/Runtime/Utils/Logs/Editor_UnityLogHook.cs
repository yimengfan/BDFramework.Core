#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Logs
{
    /// <summary>
    /// Agent - 在PlayMode下劫持所有日志，退出时保存到本地文件
    /// 不影响控制台输出，仅额外记录日志
    /// </summary>
    [InitializeOnLoad]
    public class Editor_UnityLogHook
    {
        private static readonly object lockObject = new object();
        private static List<LogEntry> capturedLogs = new List<LogEntry>();
        private static string logFilePath;
        private static string captureSessionId;
        private static bool isCapturing = false;
        private const int MAX_LOG_FILES = 50;
        private const string LOG_DIR = ".unity3d";
        private const string SESSION_COUNTER_FILE = "log_session_counter.txt";

        static Editor_UnityLogHook()
        {
            // 注册PlayMode状态变化回调
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    StartLogCapture();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    StopLogCapture();
                    break;
            }
        }

        /// <summary>
        /// 开始日志捕获
        /// </summary>
        private static void StartLogCapture()
        {
            if (isCapturing) return;

            lock (lockObject)
            {
                capturedLogs.Clear();
            }

            isCapturing = true;
            captureSessionId = GenerateCaptureSessionId();

            // 注册日志回调（线程安全版本）
            Application.logMessageReceivedThreaded += OnLogMessageReceived;

            Debug.Log($"[TestAgent] 日志捕获已启动，编号: {captureSessionId}");
        }

        /// <summary>
        /// 停止日志捕获并保存
        /// </summary>
        private static void StopLogCapture()
        {
            if (!isCapturing) return;

            // 注销日志回调
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            isCapturing = false;

            // 保存日志到文件
            SaveLogsToFile();

            Debug.Log("[TestAgent] 日志已保存到: " + logFilePath);
        }

        /// <summary>
        /// 日志回调处理
        /// </summary>
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (!isCapturing) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = logString,
                StackTrace = stackTrace,
                Type = type,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
            };

            lock (lockObject)
            {
                capturedLogs.Add(entry);
            }
        }

        /// <summary>
        /// 将日志保存到文件
        /// </summary>
        private static void SaveLogsToFile()
        {
            try
            {
                List<LogEntry> snapshot;
                lock (lockObject)
                {
                    snapshot = new List<LogEntry>(capturedLogs);
                }

                if (snapshot.Count == 0)
                {
                    return;
                }

                // 创建本地.unity3d目录（如果不存在）
                string localDir = LOG_DIR;
                if (!Directory.Exists(localDir))
                {
                    Directory.CreateDirectory(localDir);
                }

                // 生成带时间戳的文件名
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                logFilePath = Path.Combine(localDir, $"{timestamp}_{captureSessionId}.log");

                // 构建日志内容
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"========================================");
                sb.AppendLine($"日志编号: {captureSessionId}");
                sb.AppendLine($"日志记录开始时间: {snapshot[0].Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"日志记录结束时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"总日志条数: {snapshot.Count}");
                sb.AppendLine($"========================================");
                sb.AppendLine();

                foreach (var entry in snapshot)
                {
                    string typePrefix = entry.Type.ToString().ToUpper();
                    sb.AppendLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{typePrefix}] [T{entry.ThreadId}] {entry.Message}");

                    if (!string.IsNullOrEmpty(entry.StackTrace))
                    {
                        sb.AppendLine(entry.StackTrace);
                    }

                    sb.AppendLine();
                }

                // 写入文件
                File.WriteAllText(logFilePath, sb.ToString(), Encoding.UTF8);

                // 清理旧日志文件，保留最新的50个
                CleanOldLogs(localDir);

                // 在Unity控制台显示保存位置
                Debug.Log($"[TestAgent] 日志已保存，共 {snapshot.Count} 条记录\n文件路径: {Path.GetFullPath(logFilePath)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestAgent] 保存日志失败: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 清理旧日志文件，保留最新的MAX_LOG_FILES个
        /// </summary>
        private static void CleanOldLogs(string logDir)
        {
            try
            {
                if (!Directory.Exists(logDir))
                    return;

                // 获取所有.log文件
                string[] logFiles = Directory.GetFiles(logDir, "*.log");

                // 如果文件数量超过限制，删除最旧的文件
                if (logFiles.Length > MAX_LOG_FILES)
                {
                    // 按文件创建时间排序（旧的在前）
                    Array.Sort(logFiles, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));

                    // 删除最旧的文件
                    int filesToDelete = logFiles.Length - MAX_LOG_FILES;
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        File.Delete(logFiles[i]);
                        Debug.Log($"[TestAgent] 已删除旧日志文件: {Path.GetFileName(logFiles[i])}");
                    }

                    Debug.Log($"[TestAgent] 已清理 {filesToDelete} 个旧日志文件，当前保留 {MAX_LOG_FILES} 个最新日志");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TestAgent] 清理旧日志文件时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 生成本次日志捕获编号
        /// </summary>
        private static string GenerateCaptureSessionId()
        {
            string localDir = LOG_DIR;
            if (!Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            string counterFilePath = Path.Combine(localDir, SESSION_COUNTER_FILE);
            int nextCounter = 1;

            if (File.Exists(counterFilePath))
            {
                string counterText = File.ReadAllText(counterFilePath).Trim();
                if (int.TryParse(counterText, out int currentCounter))
                {
                    nextCounter = currentCounter + 1;
                }
            }

            if (nextCounter > 9999)
            {
                nextCounter = 1;
            }

            File.WriteAllText(counterFilePath, nextCounter.ToString());
            return nextCounter.ToString("D6");
        }

        /// <summary>
        /// 日志条目结构
        /// </summary>
        private struct LogEntry
        {
            public DateTime Timestamp;
            public int ThreadId;
            public string Message;
            public string StackTrace;
            public LogType Type;
        }
    }
}
#endif
