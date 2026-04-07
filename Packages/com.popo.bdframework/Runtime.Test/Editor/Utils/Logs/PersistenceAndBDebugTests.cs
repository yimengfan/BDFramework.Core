using System;
using System.IO;
using System.Reflection;
using BDFramework.Logs;
using NUnit.Framework;

namespace BDFramework.EditorTest.Logs
{
    public class PersistenceAndBDebugTests
    {
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "BDebugPersistenceTests", Guid.NewGuid().ToString("N"));
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
        public void PersistenceSettings_DefaultPlayerSettings_AreEnabledAndEncrypted()
        {
            var settings = PersistenceSettings.CreatePlayerDefault();

            Assert.That(settings.EnablePersistence, Is.True);
            Assert.That(settings.EnableEncryption, Is.True);
            Assert.That(settings.DirectoryName, Is.EqualTo("playerlogs"));
            Assert.That(settings.FlushIntervalMs, Is.GreaterThanOrEqualTo(PersistenceSettings.MIN_FLUSH_INTERVAL_MS));
            Assert.That(settings.EncryptPassword, Is.Not.Empty);
        }

        [Test]
        public void SerializedLogEntry_LocalTime_ConvertsFromUtcTicks()
        {
            var utcTime = new DateTime(2026, 4, 8, 12, 30, 15, DateTimeKind.Utc);
            var entry = new SerializedLogEntry() { UtcTicks = utcTime.Ticks };

            Assert.That(entry.LocalTime.Kind, Is.EqualTo(DateTimeKind.Local));
            Assert.That(entry.LocalTime.ToUniversalTime(), Is.EqualTo(utcTime));
        }

        [Test]
        public void CleanupOldLogFiles_DeletesOldestAndKeepsActiveFile()
        {
            for (var i = 0; i < 22; i++)
            {
                var file = Path.Combine(tempDir, $"playerlog_{new DateTime(2026, 4, 1).AddMinutes(i):yyyy.MM.dd_HH.mm.ss}.bin");
                File.WriteAllText(file, i.ToString());
            }

            var activeFile = Path.Combine(tempDir, "playerlog_2026.04.01_00.00.00.bin");
            Assert.That(File.Exists(activeFile), Is.True);

            InvokeCleanup(tempDir, activeFile);

            var files = Directory.GetFiles(tempDir, "playerlog_*.bin", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            Assert.That(files.Length, Is.EqualTo(20));
            Assert.That(File.Exists(activeFile), Is.True);
            Assert.That(File.Exists(Path.Combine(tempDir, "playerlog_2026.04.01_00.01.00.bin")), Is.False);
            Assert.That(File.Exists(Path.Combine(tempDir, "playerlog_2026.04.01_00.21.00.bin")), Is.True);
        }

        [Test]
        public void BDebug_PlayerLogPaths_AreEmptyInEditor()
        {
            Assert.That(BDebug.PlayerLogRootPath, Is.EqualTo(string.Empty));
            Assert.That(BDebug.CurrentPlayerLogFilePath, Is.EqualTo(string.Empty));
        }

        private static void InvokeCleanup(string rootDir, string activeFile)
        {
            var method = typeof(Persistence).GetMethod("CleanupOldLogFiles", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "CleanupOldLogFiles should exist for retention testing.");
            method.Invoke(null, new object[] { rootDir, activeFile });
        }
    }
}

