using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework.Logs
{
    public static class Persistence
    {
        private const int MAX_PLAYER_LOG_FILES = 20;
        public const int FILE_MAGIC = 0x474C4442;
        public const byte FILE_VERSION = 1;
        public const byte RECORD_FLAG_NONE = 0;
        public const byte RECORD_FLAG_ENCRYPTED = 1;

        private static readonly object StateLock = new object();
        private static readonly ConcurrentQueue<SerializedLogEntry> PendingQueue =
            new ConcurrentQueue<SerializedLogEntry>();
        private static readonly AutoResetEvent FlushSignal = new AutoResetEvent(false);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly MemoryStream SerializeStream = new MemoryStream(8 * 1024);
        private static readonly BinaryWriter SerializeWriter = new BinaryWriter(SerializeStream, Utf8NoBom, true);

        private static Thread writerThread;
        private static FileStream currentStream;
        private static BinaryWriter currentWriter;
        private static string currentFilePath;
        private static string sessionTimestamp;
        private static bool isRunning;
        private static bool isShuttingDown;
        private static bool isLogHookRegistered;
        private static bool isQuitHookRegistered;
        private static PersistenceSettings currentSettings = PersistenceSettings.CreatePlayerDefault();
        private static byte[] currentEncryptKey = LogCrypto.DeriveKey(LogCrypto.DEFAULT_PASSWORD);

        public static string LogRootDirectory
        {
            get
            {
                lock (StateLock)
                {
                    return Path.Combine(BApplication.persistentDataPath, currentSettings.DirectoryName);
                }
            }
        }

        public static string CurrentFilePath
        {
            get
            {
                lock (StateLock)
                {
                    return currentFilePath;
                }
            }
        }

        public static void Initialize(PersistenceSettings settings)
        {
            if (Application.isEditor)
            {
                return;
            }

            var normalizedSettings = (settings ?? PersistenceSettings.CreatePlayerDefault()).CloneNormalized();
            lock (StateLock)
            {
                currentSettings = normalizedSettings;
                currentEncryptKey = normalizedSettings.EnableEncryption
                    ? LogCrypto.DeriveKey(normalizedSettings.EncryptPassword)
                    : null;

                if (!isLogHookRegistered)
                {
                    Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
                    isLogHookRegistered = true;
                }

                if (!isQuitHookRegistered)
                {
                    Application.quitting += Shutdown;
                    isQuitHookRegistered = true;
                }

                if (string.IsNullOrEmpty(sessionTimestamp))
                {
                    sessionTimestamp = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss");
                }

                if (!isRunning)
                {
                    isShuttingDown = false;
                    isRunning = true;
                    writerThread = new Thread(WriterLoop)
                    {
                        IsBackground = true,
                        Name = "BDebug.PlayerLog.Writer",
                    };
                    writerThread.Start();
                }
            }
        }

        public static void Flush()
        {
            FlushSignal.Set();
        }

        public static void Shutdown()
        {
            Thread threadToJoin = null;

            lock (StateLock)
            {
                if (!isRunning && !isLogHookRegistered)
                {
                    return;
                }

                isShuttingDown = true;
                threadToJoin = writerThread;
            }

            FlushSignal.Set();

            if (threadToJoin != null && threadToJoin.IsAlive)
            {
                threadToJoin.Join(3000);
            }

            lock (StateLock)
            {
                if (isLogHookRegistered)
                {
                    Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
                    isLogHookRegistered = false;
                }

                if (isQuitHookRegistered)
                {
                    Application.quitting -= Shutdown;
                    isQuitHookRegistered = false;
                }

                DisposeCurrentWriter();
                writerThread = null;
                isRunning = false;
                isShuttingDown = false;
            }
        }

        private static void OnUnityLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            var settings = currentSettings;
            if (settings == null || !settings.EnablePersistence || isShuttingDown)
            {
                return;
            }

            PendingQueue.Enqueue(new SerializedLogEntry()
            {
                UtcTicks = DateTime.UtcNow.Ticks,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                LogType = type,
                Message = logString ?? string.Empty,
                StackTrace = stackTrace ?? string.Empty,
            });
        }

        private static void WriterLoop()
        {
            try
            {
                while (true)
                {
                    var settings = GetSettingsSnapshot();
                    FlushSignal.WaitOne(settings.FlushIntervalMs);
                    FlushPendingEntries();

                    if (isShuttingDown)
                    {
                        FlushPendingEntries();
                        break;
                    }
                }
            }
            finally
            {
                lock (StateLock)
                {
                    DisposeCurrentWriter();
                    writerThread = null;
                    isRunning = false;
                }
            }
        }

        private static PersistenceSettings GetSettingsSnapshot()
        {
            lock (StateLock)
            {
                return currentSettings.CloneNormalized();
            }
        }

        private static byte[] GetEncryptKeySnapshot()
        {
            lock (StateLock)
            {
                return currentEncryptKey;
            }
        }

        private static void FlushPendingEntries()
        {
            var settings = GetSettingsSnapshot();
            if (!settings.EnablePersistence)
            {
                while (PendingQueue.TryDequeue(out _))
                {
                }

                return;
            }

            var encryptKey = GetEncryptKeySnapshot();
            var hasWritten = false;
            while (PendingQueue.TryDequeue(out var entry))
            {
                EnsureWriter(settings);
                WriteEntry(entry, settings, encryptKey);
                hasWritten = true;
            }

            if (hasWritten)
            {
                currentWriter?.Flush();
                currentStream?.Flush();
            }
        }

        private static void EnsureWriter(PersistenceSettings settings)
        {
            if (currentWriter != null)
            {
                return;
            }

            var persistenceRoot = BApplication.persistentDataPath;
            if (string.IsNullOrEmpty(persistenceRoot))
            {
                Debug.LogWarning("[BDebug.Persistence] BApplication.persistentDataPath 尚未初始化，跳过本次日志文件创建，后续日志会重试。");
                return;
            }

            var rootDir = Path.Combine(persistenceRoot, settings.DirectoryName);
            Directory.CreateDirectory(rootDir);

            currentFilePath = Path.Combine(rootDir, $"playerlog_{sessionTimestamp}.bin");
            currentStream = new FileStream(currentFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 64 * 1024);
            currentWriter = new BinaryWriter(currentStream, Utf8NoBom, true);
            CleanupOldLogFiles(rootDir, currentFilePath);

            if (currentStream.Length == 0)
            {
                WriteFileHeader(currentWriter);
            }
            else
            {
                currentStream.Seek(0, SeekOrigin.End);
            }
        }

        private static void WriteEntry(SerializedLogEntry entry, PersistenceSettings settings, byte[] encryptKey)
        {
            SerializeStream.Position = 0;
            SerializeStream.SetLength(0);
            SerializeWriter.Write(entry.UtcTicks);
            SerializeWriter.Write(entry.ThreadId);
            SerializeWriter.Write((byte)entry.LogType);
            SerializeWriter.Write(entry.Message ?? string.Empty);
            SerializeWriter.Write(entry.StackTrace ?? string.Empty);
            SerializeWriter.Flush();

            var payloadLength = (int)SerializeStream.Length;
            var buffer = SerializeStream.GetBuffer();
            if (settings.EnableEncryption)
            {
                byte[] encryptedPayload = LogCrypto.Encrypt(buffer, payloadLength, encryptKey);
                currentWriter.Write((int) encryptedPayload.Length);
                currentWriter.Write(RECORD_FLAG_ENCRYPTED);
                currentWriter.Write(encryptedPayload, 0, encryptedPayload.Length);
            }
            else
            {
                currentWriter.Write(payloadLength);
                currentWriter.Write(RECORD_FLAG_NONE);
                currentWriter.Write(buffer, 0, payloadLength);
            }
        }

        private static void WriteFileHeader(BinaryWriter writer)
        {
            writer.Write(FILE_MAGIC);
            writer.Write(FILE_VERSION);
            writer.Flush();
        }

        private static void DisposeCurrentWriter()
        {
            currentWriter?.Flush();
            currentWriter?.Dispose();
            currentWriter = null;

            currentStream?.Flush();
            currentStream?.Dispose();
            currentStream = null;
            currentFilePath = null;
        }

        private static void CleanupOldLogFiles(string rootDir, string activeFilePath)
        {
            if (!Directory.Exists(rootDir))
            {
                return;
            }

            var files = Directory.GetFiles(rootDir, "playerlog_*.bin", SearchOption.TopDirectoryOnly);
            if (files.Length <= MAX_PLAYER_LOG_FILES)
            {
                return;
            }

            Array.Sort(files, CompareLogFile);

            var deleteCount = files.Length - MAX_PLAYER_LOG_FILES;
            for (var i = 0; i < files.Length && deleteCount > 0; i++)
            {
                var file = files[i];
                if (string.Equals(file, activeFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                    deleteCount--;
                }
                catch
                {
                }
            }
        }

        private static int CompareLogFile(string left, string right)
        {
            var hasLeft = TryParseLogFileTime(left, out var leftTime);
            var hasRight = TryParseLogFileTime(right, out var rightTime);
            if (hasLeft && hasRight)
            {
                var compare = leftTime.CompareTo(rightTime);
                if (compare != 0)
                {
                    return compare;
                }
            }
            else if (hasLeft != hasRight)
            {
                return hasLeft ? 1 : -1;
            }

            return string.Compare(Path.GetFileName(left), Path.GetFileName(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseLogFileTime(string filePath, out DateTime timestamp)
        {
            const string prefix = "playerlog_";
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrEmpty(fileName) && fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var raw = fileName.Substring(prefix.Length);
                if (DateTime.TryParseExact(raw, "yyyy.MM.dd_HH.mm.ss", null,
                        System.Globalization.DateTimeStyles.None, out timestamp))
                {
                    return true;
                }
            }

            timestamp = default;
            return false;
        }
    }
}

