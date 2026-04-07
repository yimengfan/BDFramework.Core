using System;
using System.IO;
using System.Text;
using BDFramework.Logs;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.Logs
{
    public class LogCryptoAndReaderTests
    {
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "BDebugTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void EncryptAndDecrypt_RoundTrip_Succeeds()
        {
            var source = Encoding.UTF8.GetBytes("bdframework-log-roundtrip");
            var key = LogCrypto.DeriveKey("unit-test-password");

            var encrypted = LogCrypto.Encrypt(source, source.Length, key);
            var decrypted = LogCrypto.Decrypt(encrypted, key);

            CollectionAssert.AreEqual(source, decrypted);
        }

        [Test]
        public void Decrypt_WithWrongPassword_Throws()
        {
            var source = Encoding.UTF8.GetBytes("bdframework-log-roundtrip");
            var encrypted = LogCrypto.Encrypt(source, source.Length, LogCrypto.DeriveKey("correct-password"));

            Assert.Throws<InvalidDataException>(() => LogCrypto.Decrypt(encrypted, "wrong-password"));
        }

        [Test]
        public void LogReader_ReadAllAndExport_Works_ForPlainAndEncryptedRecords()
        {
            var filePath = Path.Combine(tempDir, "playerlog_2026.04.08_10.20.30.bin");
            var ticks = new DateTime(2026, 4, 8, 10, 20, 30, DateTimeKind.Utc).Ticks;

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(Persistence.FILE_MAGIC);
                writer.Write(Persistence.FILE_VERSION);

                WriteRecord(writer, ticks, 1, LogType.Log, "plain-message", "plain-stack", false, null);
                WriteRecord(writer, ticks + TimeSpan.TicksPerSecond, 2, LogType.Error, "encrypted-message", "encrypted-stack", true,
                    LogCrypto.DeriveKey("reader-password"));
            }

            var entries = LogReader.ReadAll(filePath, "reader-password");

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Message, Is.EqualTo("plain-message"));
            Assert.That(entries[0].StackTrace, Is.EqualTo("plain-stack"));
            Assert.That(entries[1].Message, Is.EqualTo("encrypted-message"));
            Assert.That(entries[1].StackTrace, Is.EqualTo("encrypted-stack"));
            Assert.That(entries[1].LogType, Is.EqualTo(LogType.Error));

            var txtPath = BDebug.ExportPlayerLogToText(filePath, null, "reader-password");
            Assert.That(File.Exists(txtPath), Is.True);

            var content = File.ReadAllText(txtPath);
            StringAssert.Contains("plain-message", content);
            StringAssert.Contains("encrypted-message", content);
            StringAssert.Contains("encrypted-stack", content);
        }

        private static void WriteRecord(BinaryWriter writer, long ticks, int threadId, LogType logType, string message,
            string stackTrace, bool encrypt, byte[] key)
        {
            byte[] payload;
            using (var payloadStream = new MemoryStream())
            using (var payloadWriter = new BinaryWriter(payloadStream, new UTF8Encoding(false)))
            {
                payloadWriter.Write(ticks);
                payloadWriter.Write(threadId);
                payloadWriter.Write((byte) logType);
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
    }
}

