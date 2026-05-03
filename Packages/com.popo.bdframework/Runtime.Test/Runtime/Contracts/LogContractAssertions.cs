using System;
using System.IO;
using System.Reflection;
using System.Text;
using BDFramework.Core.Tools;
using BDFramework.Logs;
using UnityEngine;

namespace BDFramework.RuntimeTests.Contracts
{
    /// <summary>
    /// 日志模块可打包契约断言集合。
    /// Packaged log-module contract assertion collection.
    /// 该类型覆盖加解密、日志读取导出、默认配置和保留策略等纯逻辑能力，
    /// 让真机与 BatchMode 都能复用同一套高价值日志回归校验。
    /// This type covers pure-logic capabilities such as encryption, log reading and export, default settings, and retention policy,
    /// allowing player builds and BatchMode to reuse the same high-value log regression checks.
    /// </summary>
    public static class LogContractAssertions
    {
        /// <summary>
        /// 验证日志加密与解密的往返过程保持一致。
        /// Verify that the log encryption and decryption round-trip remains stable.
        /// </summary>
        public static void VerifyEncryptAndDecryptRoundTrip()
        {
            var source = Encoding.UTF8.GetBytes("bdframework-log-roundtrip");
            var key = LogCrypto.DeriveKey("unit-test-password");

            var encrypted = LogCrypto.Encrypt(source, source.Length, key);
            var decrypted = LogCrypto.Decrypt(encrypted, key);

            EnsureByteArrayEqual(source, decrypted, "日志加解密往返结果不匹配。");
        }

        /// <summary>
        /// 验证错误密码会触发明确的解密失败。
        /// Verify that a wrong password triggers an explicit decryption failure.
        /// </summary>
        public static void VerifyDecryptWithWrongPasswordThrows()
        {
            var source = Encoding.UTF8.GetBytes("bdframework-log-roundtrip");
            var encrypted = LogCrypto.Encrypt(source, source.Length, LogCrypto.DeriveKey("correct-password"));

            try
            {
                LogCrypto.Decrypt(encrypted, "wrong-password");
                throw new Exception("错误密码解密应抛出 InvalidDataException。");
            }
            catch (InvalidDataException)
            {
            }
        }

        /// <summary>
        /// 验证日志读取与文本导出同时支持明文和加密记录。
        /// Verify that log reading and text export support both plain and encrypted records.
        /// </summary>
        public static void VerifyLogReaderReadAllAndExportWorksForPlainAndEncryptedRecords()
        {
            var tempDir = CreateTempDirectory("logs-read-export");
            try
            {
                var filePath = Path.Combine(tempDir, "playerlog_2026.04.08_10.20.30.bin");
                var ticks = new DateTime(2026, 4, 8, 10, 20, 30, DateTimeKind.Utc).Ticks;

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new BinaryWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(Persistence.FILE_MAGIC);
                    writer.Write(Persistence.FILE_VERSION);

                    WriteRecord(writer, ticks, 1, LogType.Log, "plain-message", "plain-stack", false, null);
                    WriteRecord(
                        writer,
                        ticks + TimeSpan.TicksPerSecond,
                        2,
                        LogType.Error,
                        "encrypted-message",
                        "encrypted-stack",
                        true,
                        LogCrypto.DeriveKey("reader-password"));
                }

                var entries = LogReader.ReadAll(filePath, "reader-password");
                FrameworkContractAssertions.EnsureEqual(2, entries.Count, "日志读取条目数量不匹配。");
                FrameworkContractAssertions.EnsureEqual("plain-message", entries[0].Message, "明文日志消息不匹配。");
                FrameworkContractAssertions.EnsureEqual("plain-stack", entries[0].StackTrace, "明文日志堆栈不匹配。");
                FrameworkContractAssertions.EnsureEqual("encrypted-message", entries[1].Message, "加密日志消息不匹配。");
                FrameworkContractAssertions.EnsureEqual("encrypted-stack", entries[1].StackTrace, "加密日志堆栈不匹配。");
                FrameworkContractAssertions.EnsureEqual(LogType.Error, entries[1].LogType, "加密日志类型不匹配。");

                var txtPath = BDebug.ExportPlayerLogToText(filePath, null, "reader-password");
                FrameworkContractAssertions.EnsureTrue(File.Exists(txtPath), "日志文本导出文件不存在。");

                var content = File.ReadAllText(txtPath);
                FrameworkContractAssertions.EnsureTrue(content.Contains("plain-message"), "导出文本缺少明文消息。");
                FrameworkContractAssertions.EnsureTrue(content.Contains("encrypted-message"), "导出文本缺少加密消息。");
                FrameworkContractAssertions.EnsureTrue(content.Contains("encrypted-stack"), "导出文本缺少加密堆栈。");
            }
            finally
            {
                DeleteDirectoryIfExists(tempDir);
            }
        }

        /// <summary>
        /// 验证默认 Player 日志配置保持开启和加密。
        /// Verify that the default player log settings remain enabled and encrypted.
        /// </summary>
        public static void VerifyDefaultPlayerSettingsAreEnabledAndEncrypted()
        {
            var settings = PersistenceSettings.CreatePlayerDefault();

            FrameworkContractAssertions.EnsureTrue(settings.EnablePersistence, "默认日志配置应开启持久化。");
            FrameworkContractAssertions.EnsureTrue(settings.EnableEncryption, "默认日志配置应开启加密。");
            FrameworkContractAssertions.EnsureEqual("playerlogs", settings.DirectoryName, "默认日志目录名不匹配。");
            FrameworkContractAssertions.EnsureTrue(
                settings.FlushIntervalMs >= PersistenceSettings.MIN_FLUSH_INTERVAL_MS,
                "默认日志刷新间隔应不小于最小值。");
            FrameworkContractAssertions.EnsureTrue(!string.IsNullOrEmpty(settings.EncryptPassword), "默认日志密码不能为空。");
        }

        /// <summary>
        /// 验证序列化日志记录的本地时间会从 UTC ticks 正确转换。
        /// Verify that serialized log-entry local time converts correctly from UTC ticks.
        /// </summary>
        public static void VerifySerializedLogEntryLocalTimeConvertsFromUtcTicks()
        {
            var utcTime = new DateTime(2026, 4, 8, 12, 30, 15, DateTimeKind.Utc);
            var entry = new SerializedLogEntry() { UtcTicks = utcTime.Ticks };

            FrameworkContractAssertions.EnsureEqual(DateTimeKind.Local, entry.LocalTime.Kind, "日志本地时间 Kind 不匹配。");
            FrameworkContractAssertions.EnsureEqual(utcTime, entry.LocalTime.ToUniversalTime(), "日志本地时间的 UTC 映射不匹配。");
        }

        /// <summary>
        /// 验证日志保留策略会删除最旧文件并保留当前活跃文件。
        /// Verify that the log retention policy deletes the oldest files while keeping the active file.
        /// </summary>
        public static void VerifyCleanupOldLogFilesDeletesOldestAndKeepsActiveFile()
        {
            var tempDir = CreateTempDirectory("logs-cleanup");
            try
            {
                for (var index = 0; index < 22; index++)
                {
                    var file = Path.Combine(
                        tempDir,
                        $"playerlog_{new DateTime(2026, 4, 1).AddMinutes(index):yyyy.MM.dd_HH.mm.ss}.bin");
                    File.WriteAllText(file, index.ToString());
                }

                var activeFile = Path.Combine(tempDir, "playerlog_2026.04.01_00.00.00.bin");
                FrameworkContractAssertions.EnsureTrue(File.Exists(activeFile), "活跃日志文件前置条件不满足。");

                InvokeCleanup(tempDir, activeFile);

                var files = Directory.GetFiles(tempDir, "playerlog_*.bin", SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                FrameworkContractAssertions.EnsureEqual(20, files.Length, "日志保留数量不匹配。");
                FrameworkContractAssertions.EnsureTrue(File.Exists(activeFile), "活跃日志文件不应被清理。");
                FrameworkContractAssertions.EnsureTrue(
                    !File.Exists(Path.Combine(tempDir, "playerlog_2026.04.01_00.01.00.bin")),
                    "最旧的非活跃日志文件应被清理。");
                FrameworkContractAssertions.EnsureTrue(
                    File.Exists(Path.Combine(tempDir, "playerlog_2026.04.01_00.21.00.bin")),
                    "最新日志文件应被保留。");
            }
            finally
            {
                DeleteDirectoryIfExists(tempDir);
            }
        }

        private static string CreateTempDirectory(string scenario)
        {
            var root = ResolveWritableRoot();
            var tempDir = Path.Combine(root, "runtime-test-logs", scenario, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        private static string ResolveWritableRoot()
        {
            // 优先使用 BApplication.persistentDataPath，但验证其可写性。
            // CI 环境中 persistentDataPath 可能指向不存在的目录（如 dataPath + "/.AppData"），
            // 如果创建目录失败则回退到系统临时目录。
            // Prefer BApplication.persistentDataPath but verify writability.
            // In CI environments, persistentDataPath may point to a non-existent directory
            // (e.g., dataPath + "/.AppData"). Fall back to the system temp directory if creation fails.
            if (!string.IsNullOrEmpty(BApplication.persistentDataPath))
            {
                try
                {
                    Directory.CreateDirectory(BApplication.persistentDataPath);
                    var probeFile = Path.Combine(BApplication.persistentDataPath, $"._write_probe_{Guid.NewGuid():N}");
                    File.WriteAllText(probeFile, "probe");
                    File.Delete(probeFile);
                    return BApplication.persistentDataPath;
                }
                catch
                {
                    // persistentDataPath 不可写，继续尝试后续选项
                    // persistentDataPath not writable, try next option
                }
            }

            if (!string.IsNullOrEmpty(Application.persistentDataPath))
            {
                try
                {
                    Directory.CreateDirectory(Application.persistentDataPath);
                    var probeFile = Path.Combine(Application.persistentDataPath, $"._write_probe_{Guid.NewGuid():N}");
                    File.WriteAllText(probeFile, "probe");
                    File.Delete(probeFile);
                    return Application.persistentDataPath;
                }
                catch
                {
                    // Unity persistentDataPath 不可写，继续尝试后续选项
                    // Unity persistentDataPath not writable, try next option
                }
            }

            return Path.GetTempPath();
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static void WriteRecord(
            BinaryWriter writer,
            long ticks,
            int threadId,
            LogType logType,
            string message,
            string stackTrace,
            bool encrypt,
            byte[] key)
        {
            byte[] payload;
            using (var payloadStream = new MemoryStream())
            using (var payloadWriter = new BinaryWriter(payloadStream, new UTF8Encoding(false)))
            {
                payloadWriter.Write(ticks);
                payloadWriter.Write(threadId);
                payloadWriter.Write((byte)logType);
                payloadWriter.Write(message ?? string.Empty);
                payloadWriter.Write(stackTrace ?? string.Empty);
                payloadWriter.Flush();
                payload = payloadStream.ToArray();
            }

            if (encrypt)
            {
                var encryptedPayload = LogCrypto.Encrypt(payload, payload.Length, key);
                writer.Write(encryptedPayload.Length);
                writer.Write(Persistence.RECORD_FLAG_ENCRYPTED);
                writer.Write(encryptedPayload, 0, encryptedPayload.Length);
            }
            else
            {
                writer.Write(payload.Length);
                writer.Write(Persistence.RECORD_FLAG_NONE);
                writer.Write(payload, 0, payload.Length);
            }
        }

        private static void InvokeCleanup(string rootDir, string activeFile)
        {
            var method = typeof(Persistence).GetMethod("CleanupOldLogFiles", BindingFlags.NonPublic | BindingFlags.Static);
            FrameworkContractAssertions.EnsureTrue(method != null, "CleanupOldLogFiles 应存在，供保留策略测试使用。");
            method.Invoke(null, new object[] { rootDir, activeFile });
        }

        private static void EnsureByteArrayEqual(byte[] expected, byte[] actual, string message)
        {
            if (expected == null || actual == null)
            {
                throw new Exception($"{message} expectedNull={expected == null} actualNull={actual == null}");
            }

            if (expected.Length != actual.Length)
            {
                throw new Exception($"{message} expectedLength={expected.Length} actualLength={actual.Length}");
            }

            for (var index = 0; index < expected.Length; index++)
            {
                if (expected[index] != actual[index])
                {
                    throw new Exception($"{message} index={index} expected={expected[index]} actual={actual[index]}");
                }
            }
        }
    }
}