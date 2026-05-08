using System;
using System.IO;
using BDFramework.Test.SqliteBenchmark;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.SqliteBenchmark
{
    /// <summary>
    /// SQLite 优化性能基准测试 — Editor 菜单入口。
    /// 全部测试逻辑已迁移至 Runtime.Test/Runtime/SqliteBenchmark/SqliteBenchmarkRunner.cs，
    /// 支持真机和 Editor 运行。此类仅保留 MenuItem 菜单桥接。
    /// SQLite optimization benchmark — Editor menu entry.
    /// All test logic has been migrated to Runtime.Test/Runtime/SqliteBenchmark/SqliteBenchmarkRunner.cs,
    /// supporting device and Editor execution. This class retains only the MenuItem menu bridge.
    /// </summary>
    static public class SqliteOptimizationBenchmark
    {
        private const string MENU_PATH = "BDFramework/测试/SQLite优化性能基准 ▶";

        /// <summary>
        /// 真实表数据目录（Editor 环境下的默认路径，真机运行时通过参数传入）。
        /// Real table data directory (default path in Editor; on device pass via parameter).
        /// </summary>
        private static readonly string DefaultRealTableDir = "/Users/naipaopao/Documents/DarkDuck/TheCity/__Arts__/Table";

        [MenuItem(MENU_PATH)]
        static public void RunBenchmark()
        {
            // 委托给 Runtime 兼容的 SqliteBenchmarkRunner，并保存报告到文件
            // 输出到 Library/BDFrameCache/SqliteBenchmark/ 临时目录，不写入 Unity 工程 persistentDataPath
            // Delegate to Runtime-compatible SqliteBenchmarkRunner and save report to file.
            // Output to Library/BDFrameCache/SqliteBenchmark/ temp dir, not Unity project persistentDataPath.
            var report = SqliteBenchmarkRunner.RunAll(realTableDir: DefaultRealTableDir);
            var outputDir = System.IO.Path.Combine(
                BDFramework.Core.Tools.BApplication.Library,
                "BDFrameCache", "SqliteBenchmark");
            var outputPath = System.IO.Path.Combine(outputDir,
                $"SqliteBenchmark_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            report.SaveToFile(outputPath);
        }
    }
}
