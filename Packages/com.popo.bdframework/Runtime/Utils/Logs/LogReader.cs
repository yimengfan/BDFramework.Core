using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BDFramework.Logs
{
    public static class LogReader
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public static List<SerializedLogEntry> ReadAll(string filePath, string password = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var entries = new List<SerializedLogEntry>();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Utf8NoBom))
            {
                ValidateFileHeader(reader);

                while (stream.Position < stream.Length)
                {
                    if (stream.Length - stream.Position < sizeof(int) + sizeof(byte))
                    {
                        break;
                    }

                    var payloadLength = reader.ReadInt32();
                    var flags = reader.ReadByte();
                    if (payloadLength < 0 || payloadLength > stream.Length - stream.Position)
                    {
                        break;
                    }

                    var payload = reader.ReadBytes(payloadLength);
                    if (payload.Length != payloadLength)
                    {
                        break;
                    }

                    if ((flags & Persistence.RECORD_FLAG_ENCRYPTED) != 0)
                    {
                        payload = LogCrypto.Decrypt(payload, password);
                    }

                    entries.Add(DeserializePayload(payload));
                }
            }

            return entries;
        }

        public static string ExportToText(string filePath, string outputPath = null, string password = null)
        {
            var entries = ReadAll(filePath, password);
            if (string.IsNullOrEmpty(outputPath))
            {
                var dir = Path.GetDirectoryName(filePath) ?? string.Empty;
                var name = Path.GetFileNameWithoutExtension(filePath);
                outputPath = Path.Combine(dir, $"{name}_decrypt.txt");
            }

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using (var writer = new StreamWriter(outputPath, false, Utf8NoBom))
            {
                foreach (var entry in entries)
                {
                    writer.WriteLine($"[{entry.LocalTime:yyyy-MM-dd HH:mm:ss.fff}] [{entry.LogType}] [T{entry.ThreadId}] {entry.Message}");
                    if (!string.IsNullOrEmpty(entry.StackTrace))
                    {
                        writer.WriteLine(entry.StackTrace);
                    }

                    writer.WriteLine();
                }
            }

            return outputPath;
        }

        private static void ValidateFileHeader(BinaryReader reader)
        {
            if (reader.BaseStream.Length < sizeof(int) + sizeof(byte))
            {
                throw new InvalidDataException("BDebug playerlog 文件头损坏或为空。");
            }

            var magic = reader.ReadInt32();
            var version = reader.ReadByte();
            if (magic != Persistence.FILE_MAGIC)
            {
                throw new InvalidDataException("不是有效的 BDebug playerlog 文件。");
            }

            if (version != Persistence.FILE_VERSION)
            {
                throw new InvalidDataException($"不支持的 playerlog 版本: {version}");
            }
        }

        private static SerializedLogEntry DeserializePayload(byte[] payload)
        {
            using (var stream = new MemoryStream(payload, false))
            using (var reader = new BinaryReader(stream, Utf8NoBom))
            {
                return new SerializedLogEntry()
                {
                    UtcTicks = reader.ReadInt64(),
                    ThreadId = reader.ReadInt32(),
                    LogType = (UnityEngine.LogType) reader.ReadByte(),
                    Message = reader.ReadString(),
                    StackTrace = reader.ReadString(),
                };
            }
        }
    }
}


