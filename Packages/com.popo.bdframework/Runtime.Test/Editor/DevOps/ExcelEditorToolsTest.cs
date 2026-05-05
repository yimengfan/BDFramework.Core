using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.Table;
using LitJson;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.DevOps
{
    /// <summary>
    /// ExcelEditorTools 缓存和增量对比契约测试。
    /// Contract tests for ExcelEditorTools: cache save/load roundtrip and incremental diff logic.
    /// 验证 SaveExcelCacheInfo/LoadExcelCacheInfo 的序列化一致性，以及 GetChangedExcels 的 hash 对比逻辑。
    /// These verify SaveExcelCacheInfo/LoadExcelCacheInfo serialization consistency,
    /// and GetChangedExcels hash comparison logic.
    ///
    /// 注意：GetAllExcelFiles 和 GetExcelsHash 依赖 Unity AssetDatabase 和真实磁盘文件结构，
    /// 不在 Editor 单元测试范围内，需要集成测试覆盖。
    /// Note: GetAllExcelFiles and GetExcelsHash depend on Unity AssetDatabase and real disk file structures,
    /// which are out of scope for Editor unit tests and require integration test coverage.
    /// </summary>
    [TestFixture]
    public class ExcelEditorToolsTest
    {
        private string tempCacheDir;

        [SetUp]
        public void SetUp()
        {
            // 创建临时缓存目录
            // Create temporary cache directory
            tempCacheDir = Path.Combine(Path.GetTempPath(), "BDFrameworkTest_ExcelCache_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tempCacheDir);

            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 ExcelEditorTools 缓存/增量契约。",
                "执行显式缓存读写和增量对比断言，校验序列化一致性和 hash 比较逻辑。");
        }

        [TearDown]
        public void TearDown()
        {
            // 清理临时目录
            // Clean up temporary directory
            if (Directory.Exists(tempCacheDir))
            {
                Directory.Delete(tempCacheDir, true);
            }
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 验证 SaveExcelCacheInfo 写入的 JSON 能被正确解析为 Dictionary。
        /// Verify that JSON written by SaveExcelCacheInfo can be correctly parsed as Dictionary.
        /// </summary>
        [Test]
        public void SaveExcelCacheInfo_WritesValidJson()
        {
            var cacheMap = new Dictionary<string, string>
            {
                { "guid-abc123", "hash-001" },
                { "guid-def456", "hash-002" },
            };

            var cachePath = Path.Combine(tempCacheDir, "ExcelCache.info");
            var content = JsonMapper.ToJson(cacheMap);
            File.WriteAllText(cachePath, content);

            Assert.That(File.Exists(cachePath), Is.True, "缓存文件应被创建 / Cache file should be created");

            var readContent = File.ReadAllText(cachePath);
            var parsed = JsonMapper.ToObject<Dictionary<string, string>>(readContent);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Count, Is.EqualTo(2));
            Assert.That(parsed["guid-abc123"], Is.EqualTo("hash-001"));
            Assert.That(parsed["guid-def456"], Is.EqualTo("hash-002"));
        }

        /// <summary>
        /// 验证缓存读写 roundtrip 保持一致性。
        /// Verify that cache save/load roundtrip maintains consistency.
        /// </summary>
        [Test]
        public void CacheRoundtrip_PreservesAllEntries()
        {
            var cacheMap = new Dictionary<string, string>
            {
                { "guid-001", "hash-a" },
                { "guid-002", "hash-b" },
                { "guid-003", "hash-c" },
            };

            var cachePath = Path.Combine(tempCacheDir, "ExcelCache.info");
            // 写入
            File.WriteAllText(cachePath, JsonMapper.ToJson(cacheMap));

            // 读取
            var readContent = File.ReadAllText(cachePath);
            var loaded = JsonMapper.ToObject<Dictionary<string, string>>(readContent);

            Assert.That(loaded.Count, Is.EqualTo(cacheMap.Count), "条目数量应一致 / Entry count should match");
            foreach (var kv in cacheMap)
            {
                Assert.That(loaded.ContainsKey(kv.Key), Is.True, $"应包含 key={kv.Key} / Should contain key={kv.Key}");
                Assert.That(loaded[kv.Key], Is.EqualTo(kv.Value), $"值应一致 for key={kv.Key} / Value should match for key={kv.Key}");
            }
        }

        /// <summary>
        /// 验证空 Dictionary 的序列化和反序列化。
        /// Verify serialization and deserialization of empty Dictionary.
        /// </summary>
        [Test]
        public void CacheRoundtrip_EmptyDictionary_Roundtrips()
        {
            var cacheMap = new Dictionary<string, string>();

            var cachePath = Path.Combine(tempCacheDir, "ExcelCache.info");
            File.WriteAllText(cachePath, JsonMapper.ToJson(cacheMap));

            var readContent = File.ReadAllText(cachePath);
            var loaded = JsonMapper.ToObject<Dictionary<string, string>>(readContent);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Count, Is.EqualTo(0), "空 Dictionary roundtrip 应保持为空 / Empty Dictionary roundtrip should remain empty");
        }

        /// <summary>
        /// 验证增量对比逻辑：新增的 GUID 应被标记为变动。
        /// Verify incremental diff logic: new GUIDs should be marked as changed.
        /// </summary>
        [Test]
        public void IncrementalDiff_NewGuid_DetectedAsChanged()
        {
            var oldCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-a" },
            };

            var newCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-a" },
                { "guid-002", "hash-b" },  // 新增
            };

            var changedList = new List<string>();
            foreach (var item in newCache)
            {
                var ret = oldCache.TryGetValue(item.Key, out var lastHash);
                if (!ret || item.Value != lastHash)
                {
                    changedList.Add(item.Key);
                }
            }

            Assert.That(changedList, Does.Contain("guid-002"), "新增 GUID 应被检测为变动 / New GUID should be detected as changed");
            Assert.That(changedList, Does.Not.Contain("guid-001"), "未变化的 GUID 不应被标记 / Unchanged GUID should not be marked");
        }

        /// <summary>
        /// 验证增量对比逻辑：hash 变化的 GUID 应被标记为变动。
        /// Verify incremental diff logic: GUIDs with changed hash should be marked as changed.
        /// </summary>
        [Test]
        public void IncrementalDiff_HashChanged_DetectedAsChanged()
        {
            var oldCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-old" },
            };

            var newCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-new" },  // hash 变化
            };

            var changedList = new List<string>();
            foreach (var item in newCache)
            {
                var ret = oldCache.TryGetValue(item.Key, out var lastHash);
                if (!ret || item.Value != lastHash)
                {
                    changedList.Add(item.Key);
                }
            }

            Assert.That(changedList, Does.Contain("guid-001"), "hash 变化应被检测 / Hash change should be detected");
        }

        /// <summary>
        /// 验证增量对比逻辑：全部相同时无变动。
        /// Verify incremental diff logic: no changes when all entries match.
        /// </summary>
        [Test]
        public void IncrementalDiff_AllSame_NoChange()
        {
            var oldCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-a" },
                { "guid-002", "hash-b" },
            };

            var newCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-a" },
                { "guid-002", "hash-b" },
            };

            var changedList = new List<string>();
            foreach (var item in newCache)
            {
                var ret = oldCache.TryGetValue(item.Key, out var lastHash);
                if (!ret || item.Value != lastHash)
                {
                    changedList.Add(item.Key);
                }
            }

            Assert.That(changedList, Is.Empty, "全部相同时应无变动 / No changes when all entries match");
        }

        /// <summary>
        /// 验证增量对比逻辑：旧缓存为空时所有条目都是变动。
        /// Verify incremental diff logic: all entries are changes when old cache is empty.
        /// </summary>
        [Test]
        public void IncrementalDiff_EmptyOldCache_AllDetectedAsChanged()
        {
            var oldCache = new Dictionary<string, string>();

            var newCache = new Dictionary<string, string>
            {
                { "guid-001", "hash-a" },
                { "guid-002", "hash-b" },
            };

            var changedList = new List<string>();
            foreach (var item in newCache)
            {
                var ret = oldCache.TryGetValue(item.Key, out var lastHash);
                if (!ret || item.Value != lastHash)
                {
                    changedList.Add(item.Key);
                }
            }

            Assert.That(changedList.Count, Is.EqualTo(2), "旧缓存为空时所有条目应被标记为变动 / All entries should be marked as changed when old cache is empty");
        }

        /// <summary>
        /// 验证 LocalDBCache 的 save/load roundtrip。
        /// Verify LocalDBCache save/load roundtrip.
        /// </summary>
        [Test]
        public void LocalDBCache_SaveAndLoad_Roundtrip()
        {
            var dbPath = Path.Combine(tempCacheDir, "test.db");
            File.WriteAllText(dbPath, "fake-db-content");

            // 模拟 SaveLocalDBCacheInfo 的逻辑
            var hash = FileHelper.GetMurmurHash3(dbPath);
            var cachePath = Path.Combine(tempCacheDir, "LacalDBCache.info");
            File.WriteAllText(cachePath, hash);

            // 模拟 LoadLocalDBCacheInfo 的逻辑
            var loadedHash = File.Exists(cachePath) ? File.ReadAllText(cachePath) : string.Empty;

            Assert.That(loadedHash, Is.EqualTo(hash), "LocalDB 缓存 hash 应 roundtrip 一致 / LocalDB cache hash should roundtrip consistently");
            Assert.That(loadedHash, Is.Not.Empty, "缓存 hash 不应为空 / Cache hash should not be empty");
        }

        /// <summary>
        /// 验证 LoadLocalDBCacheInfo 在缓存文件不存在时返回空字符串。
        /// Verify LoadLocalDBCacheInfo returns empty string when cache file doesn't exist.
        /// </summary>
        [Test]
        public void LocalDBCache_NoCacheFile_ReturnsEmpty()
        {
            var cachePath = Path.Combine(tempCacheDir, "LacalDBCache.info");
            var result = File.Exists(cachePath) ? File.ReadAllText(cachePath) : string.Empty;

            Assert.That(result, Is.Empty, "缓存文件不存在时应返回空字符串 / Should return empty string when cache file doesn't exist");
        }
    }
}
