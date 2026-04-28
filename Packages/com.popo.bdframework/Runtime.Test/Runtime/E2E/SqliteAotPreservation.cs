using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;
using UnityEngine.Scripting;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// SQLite 泛型方法 AOT 保活辅助类（已弃用，保留供参考）。
    /// SQLite generic method AOT preservation helper (DEPRECATED, kept for reference).
    ///
    /// 重要说明：本类位于 BDFramework.Test 程序集（热更 DLL），IL2CPP AOT 编译器
    /// 不会分析热更 DLL 的代码，因此本类中的 [Preserve] 和 [RuntimeInitializeOnLoadMethod]
    /// 属性在 Player 构建中无效。所有泛型方法调用不会触发 AOT 编译。
    /// IMPORTANT: This class resides in the BDFramework.Test assembly (hotfix DLL).
    /// The IL2CPP AOT compiler does NOT analyze hotfix DLL code, so the [Preserve] and
    /// [RuntimeInitializeOnLoadMethod] attributes have no effect in Player builds.
    /// None of the generic method calls here trigger AOT compilation.
    ///
    /// 当前解决方案：在 SQLite.cs 中修改 ExecuteScalar&lt;T&gt; 和 ExecuteQueryScalars&lt;T&gt;，
    /// 对值类型使用 Convert.ChangeType 替代直接 (T)colval 拆箱，绕过 AOT 泛型实例化要求；
    /// 同时添加 ExecuteScalarInt() 非泛型方法供 TableQuery&lt;T&gt;.Count() 直接调用。
    /// Current solution: Modified ExecuteScalar&lt;T&gt; and ExecuteQueryScalars&lt;T&gt; in SQLite.cs
    /// to use Convert.ChangeType instead of direct (T)colval unboxing for value types,
    /// bypassing the AOT generic instantiation requirement; also added ExecuteScalarInt()
    /// non-generic method for TableQuery&lt;T&gt;.Count() to call directly.
    ///
    /// 如未来需要为其他泛型方法（如 TableQuery&lt;T&gt;、Query&lt;T&gt;）补充 AOT 保活，
    /// 必须将保活代码放到 BDFramework.AOT 程序集中，并通过反射引用热更类型。
    /// If AOT preservation is needed for other generic methods (e.g., TableQuery&lt;T&gt;, Query&lt;T&gt;)
    /// in the future, the preservation code MUST be placed in the BDFramework.AOT assembly
    /// and reference hotfix types via reflection.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    [Obsolete("本类位于热更 DLL 中，AOT 编译器无法看到。SQLite AOT 问题已通过 Convert.ChangeType 方案在 SQLite.cs 中修复。" +
              "This class is in a hotfix DLL and invisible to the AOT compiler. SQLite AOT issues are now fixed via Convert.ChangeType in SQLite.cs.")]
    internal static class SqliteAotPreservation
    {
        /// <summary>
        /// AOT 保活入口，在 Player 启动前由 Unity 自动调用。
        /// AOT preservation entry point, automatically called by Unity before the player starts.
        /// 该方法体内的代码永远不会真正执行（isEditor 提前返回），
        /// 但 IL2CPP AOT 编译器会分析方法体中的所有泛型调用并预编译。
        /// The code inside this method never actually executes (isEditor returns early),
        /// but the IL2CPP AOT compiler analyzes all generic calls in the method body
        /// and pre-compiles them.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static internal void ForceAotCompilation()
        {
            // 编辑器不需要 AOT 保活，提前返回避免任何副作用。
            // Editor does not need AOT preservation; return early to avoid side effects.
            if (Application.isEditor)
            {
                return;
            }

            // 以下代码永远不会执行（isEditor 已返回），但 AOT 编译器会分析方法体。
            // The code below never executes (isEditor already returned), but the AOT compiler
            // analyzes the method body.
            if (!Application.isEditor)
            {
                ForceSqliteGenericInstantiations();
            }
        }

        /// <summary>
        /// 强制引用所有 E2E 测试中使用的 SQLite 泛型方法。
        /// Force-reference all SQLite generic methods used in E2E tests.
        /// 该方法仅被 ForceAotCompilation 在不可达分支中调用，
        /// 确保编译器看到泛型实例化但不产生运行时副作用。
        /// This method is only called from ForceAotCompilation in an unreachable branch,
        /// ensuring the compiler sees the generic instantiations without runtime side effects.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        static private void ForceSqliteGenericInstantiations()
        {
            // --- FrameworkIntegrationTests 使用的类型 ---
            // --- Types used by FrameworkIntegrationTests ---
            ForceGenericInstantiationsFor<IntegrationSqliteRowProxy>();

            // --- SqliteTests 使用的类型 ---
            // --- Types used by SqliteTests ---
            ForceGenericInstantiationsFor<E2ETestRowProxy>();

            // --- ExecuteScalar<int> 和 ExecuteScalar<string> ---
            // 这些是 SQLiteConnection 上最常见的值类型泛型调用，
            // CreateTable 内部的 GetTableInfo 会调用 Query<ColumnInfo>，
            // 而 ColumnInfo 已在 AOTGenericReferences 中，但 ExecuteScalar<int>
            // （用于 Count 等操作）不在。
            // --- ExecuteScalar<int> and ExecuteScalar<string> ---
            // These are the most common value-type generic calls on SQLiteConnection.
            // CreateTable internally calls Query<ColumnInfo> via GetTableInfo,
            // and ColumnInfo is already in AOTGenericReferences, but ExecuteScalar<int>
            // (used for Count etc.) is not.
            ForceExecuteScalarInstantiations();
        }

        /// <summary>
        /// 强制为指定行类型编译所有 SQLite 泛型方法。
        /// Force-compile all SQLite generic methods for the specified row type.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        static private void ForceGenericInstantiationsFor<TRow>() where TRow : new()
        {
            // 使用 :memory: 数据库避免文件系统副作用。
            // Use :memory: database to avoid filesystem side effects.
            var conn = new SQLiteConnection(":memory:", SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true);
            try
            {
                // CreateTable<T> → 内部调用 GetMapping(typeof(T)) + Execute + GetTableInfo (Query<ColumnInfo>)
                // CreateTable<T> → internally calls GetMapping(typeof(T)) + Execute + GetTableInfo (Query<ColumnInfo>)
                conn.CreateTable<TRow>();

                // Insert(object) 是非泛型方法，不需要 AOT 保活，但执行一次确保完整链路。
                // Insert(object) is a non-generic method that does not need AOT preservation,
                // but execute once to ensure the full chain.
                var row = new TRow();
                conn.Insert(row);

                // Table<T>() → 创建 TableQuery<T>，这是 LINQ 查询的入口。
                // Table<T>() → creates TableQuery<T>, the entry point for LINQ queries.
                var table = conn.Table<TRow>();

                // Table<T>().Count() → 内部调用 ExecuteScalar<int>
                // Table<T>().Count() → internally calls ExecuteScalar<int>
                var count = table.Count();

                // Query<T>() → ExecuteQuery<T> → ExecuteDeferredQuery<T>
                // Query<T>() → ExecuteQuery<T> → ExecuteDeferredQuery<T>
                var queryResults = conn.Query<TRow>("SELECT * FROM \"" + typeof(TRow).Name + "\"");

                // FindWithQuery<T>() → Query<T>().FirstOrDefault()
                // FindWithQuery<T>() → Query<T>().FirstOrDefault()
                var found = conn.FindWithQuery<TRow>("SELECT * FROM \"" + typeof(TRow).Name + "\" LIMIT 1");
            }
            finally
            {
                conn.Dispose();
            }
        }

        /// <summary>
        /// 强制编译 ExecuteScalar 的值类型泛型实例化。
        /// Force-compile ExecuteScalar value-type generic instantiations.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        static private void ForceExecuteScalarInstantiations()
        {
            var conn = new SQLiteConnection(":memory:", SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, true);
            try
            {
                // ExecuteScalar<int> — 最常见的值类型泛型调用（Count 等）
                // ExecuteScalar<int> — the most common value-type generic call (Count, etc.)
                var intResult = conn.ExecuteScalar<int>("SELECT 0");

                // ExecuteScalar<string> — 字符串标量查询
                // ExecuteScalar<string> — string scalar query
                var strResult = conn.ExecuteScalar<string>("SELECT ''");

                // ExecuteScalar<long> — Int64 标量查询
                // ExecuteScalar<long> — Int64 scalar query
                var longResult = conn.ExecuteScalar<long>("SELECT 0");

                // ExecuteScalar<float> — Single 标量查询
                // ExecuteScalar<float> — Single scalar query
                var floatResult = conn.ExecuteScalar<float>("SELECT 0.0");

                // ExecuteScalar<double> — Double 标量查询
                // ExecuteScalar<double> — Double scalar query
                var doubleResult = conn.ExecuteScalar<double>("SELECT 0.0");

                // QueryScalars<int> — 值类型标量列表查询
                // QueryScalars<int> — value-type scalar list query
                var intScalars = conn.QueryScalars<int>("SELECT 0 LIMIT 0");

                // QueryScalars<string> — 字符串标量列表查询
                // QueryScalars<string> — string scalar list query
                var strScalars = conn.QueryScalars<string>("SELECT '' LIMIT 0");
            }
            finally
            {
                conn.Dispose();
            }
        }

        // ---- 代理类型：与 E2E 测试内部类型结构相同，用于 AOT 泛型实例化 ----
        // ---- Proxy types: same structure as E2E test internal types, used for AOT generic instantiation ----

        /// <summary>
        /// IntegrationSqliteRow 的 AOT 代理类型。
        /// 与 FrameworkIntegrationTests.IntegrationSqliteRow 具有相同的属性结构，
        /// 确保 AOT 编译器为该属性签名生成泛型代码。
        /// AOT proxy for IntegrationSqliteRow.
        /// Has the same property structure as FrameworkIntegrationTests.IntegrationSqliteRow,
        /// ensuring the AOT compiler generates generic code for this property signature.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private sealed class IntegrationSqliteRowProxy
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// E2ETestRow 的 AOT 代理类型。
        /// 与 SqliteTests.E2ETestRow 具有相同的属性结构，
        /// 确保 AOT 编译器为该属性签名生成泛型代码。
        /// AOT proxy for E2ETestRow.
        /// Has the same property structure as SqliteTests.E2ETestRow,
        /// ensuring the AOT compiler generates generic code for this property signature.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        private sealed class E2ETestRowProxy
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public float Score { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
