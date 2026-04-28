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
    /// SQLite 泛型方法 AOT 保活辅助类。
    /// SQLite generic method AOT preservation helper.
    ///
    /// IL2CPP 要求泛型方法在 AOT 阶段就被编译；如果泛型参数是值类型且
    /// AOT 编译时没有直接代码引用，运行时会抛 ExecutionEngineException。
    /// IL2CPP requires generic methods to be compiled at AOT time; if a generic
    /// type parameter is a value type and there is no direct code reference at
    /// AOT compile time, the runtime throws ExecutionEngineException.
    ///
    /// HybridCLR 的 AOTGenericReferences.cs 只生成注释，不生成可执行代码，
    /// 因此 E2E 测试中使用的 SQLite 泛型方法（ExecuteScalar&lt;int&gt;、
    /// Query&lt;T&gt;、Table&lt;T&gt;、CreateTable&lt;T&gt; 等）不会自动被 AOT 编译器发现。
    /// HybridCLR's AOTGenericReferences.cs only generates comments, not executable
    /// code, so the SQLite generic methods used in E2E tests (ExecuteScalar&lt;int&gt;,
    /// Query&lt;T&gt;, Table&lt;T&gt;, CreateTable&lt;T&gt;, etc.) are not automatically
    /// discovered by the AOT compiler.
    ///
    /// 本类通过 [Preserve] + [RuntimeInitializeOnLoadMethod] 确保这些泛型实例
    /// 在 AOT 阶段被编译器看到，且不会被 IL2CPP linker 裁剪。
    /// This class uses [Preserve] + [RuntimeInitializeOnLoadMethod] to ensure
    /// these generic instantiations are visible to the AOT compiler and are not
    /// stripped by the IL2CPP linker.
    ///
    /// 注意：本类不执行真实数据库操作，仅通过类型系统引用强制 AOT 编译。
    /// Note: This class does not perform real database operations; it only forces
    /// AOT compilation through type-system references.
    /// </summary>
    [Preserve]
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
        [Preserve]
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
        [Preserve]
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
        [Preserve]
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
        [Preserve]
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
        [Preserve]
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
        [Preserve]
        private sealed class E2ETestRowProxy
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public float Score { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
