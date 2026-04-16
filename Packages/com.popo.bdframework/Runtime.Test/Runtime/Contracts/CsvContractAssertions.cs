using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.Serialize;

namespace BDFramework.RuntimeTests.Contracts
{
    /// <summary>
    /// CSV 工具可打包契约断言集合。
    /// Packaged CSV-tool contract assertion collection.
    /// 该类型覆盖对象列表加载、单对象加载和保存往返等纯逻辑行为，
    /// 为开发期配置表工具提供可在真机侧复用的回归保护。
    /// This type covers pure-logic behaviors such as object-list loading, single-object loading, and save round-trips,
    /// providing reusable regression protection for development-time CSV tooling on player builds.
    /// </summary>
    public static class CsvContractAssertions
    {
        /// <summary>
        /// 覆盖多对象 CSV 加载使用的测试记录类型。
        /// Test record type used by the multi-object CSV loading checks.
        /// </summary>
        private sealed class CsvRow
        {
            public int Id;
            public string Name;
            public float Score;
        }

        /// <summary>
        /// 覆盖单对象 CSV 加载使用的测试配置类型。
        /// Test configuration type used by the single-object CSV loading checks.
        /// </summary>
        private struct CsvConfig
        {
            public int Lives;
            public string DisplayName;
        }

        /// <summary>
        /// 验证 CSV 对象列表加载会按照 header 映射字段。
        /// Verify that CSV object-list loading maps fields according to the header.
        /// </summary>
        public static void VerifyLoadObjectsParsesHeaderAndValues()
        {
            const string csv = "Id,Name,Score\n1,Alice,9.5\n2,Bob,7.25";
            using (var reader = new StringReader(csv))
            {
                var rows = CsvUtil.LoadObjects<CsvRow>(reader);
                FrameworkContractAssertions.EnsureEqual(2, rows.Count, "CSV 对象列表数量不匹配。");
                FrameworkContractAssertions.EnsureEqual(1, rows[0].Id, "首行 Id 不匹配。");
                FrameworkContractAssertions.EnsureEqual("Alice", rows[0].Name, "首行 Name 不匹配。");
                if (Math.Abs(rows[0].Score - 9.5f) > 0.0001f)
                {
                    throw new Exception($"首行 Score 不匹配 expected=9.5 actual={rows[0].Score}");
                }
            }
        }

        /// <summary>
        /// 验证 CSV 解析支持忽略 # 列和带引号的内容。
        /// Verify that CSV parsing supports ignored # columns and quoted content.
        /// </summary>
        public static void VerifyLoadObjectsSupportsQuotedStringsAndIgnoredColumns()
        {
            const string csv = "Id,#Description,Name,Score\n3,ignored,\"Tom, Jr\",8.75";
            using (var reader = new StringReader(csv))
            {
                var rows = CsvUtil.LoadObjects<CsvRow>(reader);
                FrameworkContractAssertions.EnsureEqual(1, rows.Count, "带引号 CSV 对象列表数量不匹配。");
                FrameworkContractAssertions.EnsureEqual(3, rows[0].Id, "带引号 CSV 的 Id 不匹配。");
                FrameworkContractAssertions.EnsureEqual("Tom, Jr", rows[0].Name, "带引号 CSV 的 Name 不匹配。");
            }
        }

        /// <summary>
        /// 验证保存后的 CSV 可以被重新加载为等价对象列表。
        /// Verify that saved CSV can be reloaded into an equivalent object list.
        /// </summary>
        public static void VerifySaveObjectsAndLoadObjectsRoundTrip()
        {
            var source = new List<CsvRow>
            {
                new CsvRow { Id = 10, Name = "Alpha", Score = 3.5f },
                new CsvRow { Id = 11, Name = "Beta", Score = 4.5f },
            };

            using (var writer = new StringWriter())
            {
                CsvUtil.SaveObjects(source, writer);
                using (var reader = new StringReader(writer.ToString()))
                {
                    var rows = CsvUtil.LoadObjects<CsvRow>(reader);
                    FrameworkContractAssertions.EnsureEqual(source.Count, rows.Count, "CSV 保存加载往返数量不匹配。");
                    FrameworkContractAssertions.EnsureEqual(source[1].Name, rows[1].Name, "CSV 保存加载往返 Name 不匹配。");
                    if (Math.Abs(source[1].Score - rows[1].Score) > 0.0001f)
                    {
                        throw new Exception($"CSV 保存加载往返 Score 不匹配 expected={source[1].Score} actual={rows[1].Score}");
                    }
                }
            }
        }

        /// <summary>
        /// 验证单对象 CSV 加载会把字段映射到目标对象。
        /// Verify that single-object CSV loading maps fields onto the destination object.
        /// </summary>
        public static void VerifyLoadObjectMapsSingleObjectFields()
        {
            const string csv = "#Field,Value\nLives,5\nDisplayName,PlayerOne";
            var config = new CsvConfig();

            using (var reader = new StringReader(csv))
            {
                CsvUtil.LoadObject(reader, ref config);
            }

            FrameworkContractAssertions.EnsureEqual(5, config.Lives, "CSV 单对象加载 Lives 不匹配。");
            FrameworkContractAssertions.EnsureEqual("PlayerOne", config.DisplayName, "CSV 单对象加载 DisplayName 不匹配。");
        }
    }
}