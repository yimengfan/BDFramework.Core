"""Talos 宿主 BaseFlow 套件源码回归测试。
Talos host BaseFlow-suite source regression tests.

覆盖范围：
Coverage:
1. step_01 的 asset-load 与 framework-integration 套件必须留在宿主包内，而不能继续依赖未被母包装载的 Runtime.Test 程序集。
1. The step_01 asset-load and framework-integration suites must remain in the host package instead of depending on the Runtime.Test assembly that is not loaded by the base package.
2. 宿主 BaseFlow 套件必须显式带 Preserve 和对应 suite 标记，避免 Windows/Android Player 再次只发现 launch。
2. The host BaseFlow suites must explicitly carry Preserve and the corresponding suite markers so Windows and Android players do not regress to discovering only launch.
3. 宿主 BaseFlow 套件必须通过反射访问热更资源与 SQLite 公开入口，而不是静态根引用 Runtime.Test。
3. The host BaseFlow suites must access the hotfix resource and SQLite public entrypoints through reflection instead of statically rooting Runtime.Test.
"""

from __future__ import annotations

import re
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[5]
HOST_BASEFLOW_TESTS_PATH = (
    REPO_ROOT
    / "Packages"
    / "com.popo.bdframework"
    / "Runtime.HostE2E"
    / "BaseFlowHostRuntimeTests.cs"
)
SQLITE_RUNTIME_PATH = (
    REPO_ROOT
    / "Packages"
    / "com.popo.bdframework"
    / "Runtime"
    / "Data"
    / "Sql"
    / "Sqlite"
    / "SqliteNet"
    / "SQLite.cs"
)
SQLITE_DLLIMPORT_PATH = (
    REPO_ROOT
    / "Packages"
    / "com.popo.bdframework"
    / "Runtime"
    / "Data"
    / "Sql"
    / "Sqlite"
    / "SqliteNet"
    / "Sqlite4SqlCipherDllImport.cs"
)


def test_host_baseflow_suites_stay_in_host_package() -> None:
    """验证 step_01 的新增基础系统套件保留在宿主侧独立程序集。
    Verify that the added foundational step_01 suites remain in a host-owned dedicated assembly.
    """
    content = HOST_BASEFLOW_TESTS_PATH.read_text(encoding="utf-8")

    assert "namespace BDFramework.HostE2E" in content
    assert "public static class BaseFlowHostRuntimeTests" in content
    assert "typeof(BDFramework.Test" not in content
    assert '"BDFramework.ResourceMgr.BResources"' in content
    assert '"SQLite4Unity3d.SQLiteConnection"' in content


def test_host_baseflow_suites_keep_preserved_entrypoints() -> None:
    """验证宿主 BaseFlow 套件显式保留 Player IL2CPP 入口。
    Verify that the host BaseFlow suites explicitly preserve the player IL2CPP entrypoints.
    """
    content = HOST_BASEFLOW_TESTS_PATH.read_text(encoding="utf-8")

    assert re.search(r"\[Preserve\]\s*public static class BaseFlowHostRuntimeTests", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"asset-load\", order: 1", content)
    assert re.search(r"\[Preserve\]\s*\[E2ETest\(suite: \"framework-integration\", order: 1", content)
    assert "RequireLoadedAssembly" in content
    assert "RequireLoadedType" in content
    assert "RequireInstanceGenericMethod" in content
    assert '"BDFramework.Core.Tools.BApplication"' in content
    assert '"SQLite4Unity3d.SQLiteConnectionString"' in content
    assert '"SQLite4Unity3d.SQLiteOpenFlags"' in content
    assert "typeof(Action<>).MakeGenericType(sqliteConnectionType)" in content
    assert "Type.Missing" in content
    assert 'GetConstructor(new[] { typeof(string), sqliteOpenFlagsType, typeof(bool) })' not in content
    assert "ResolveSqliteProbeRoot" in content
    assert "windows-systemprofile-temp-fallback" in content
    assert "android-internal-cache-dir" in content
    assert "android-internal-files-dir" in content
    assert "android-temporary-cache-path-fallback" in content
    assert "TryReadAndroidContextDirectory" in content
    assert 'TryReadAndroidContextDirectory("getCacheDir"' in content
    assert 'TryReadAndroidContextDirectory("getFilesDir"' in content
    assert 'currentActivity"' in content
    assert 'getCanonicalPath' in content
    assert "Path.GetTempPath()" in content
    assert "sqliteOpenPath" in content
    assert "fileApiPath" in content
    assert "PRAGMA temp_store=MEMORY;" in content
    assert "PRAGMA journal_mode=MEMORY;" in content
    assert "ReadRequiredStaticStringProperty" in content
    assert "CombinePath" in content
    assert "Application.persistentDataPath" in content
    assert "Application.temporaryCachePath" in content


def test_windows_sqlite_runtime_keeps_string_open_strategy() -> None:
    """验证 Windows standalone 的 SQLite 连接保留 string open 分支。
    Verify that the Windows standalone SQLite connection keeps the string-based open branch.
    """
    content = SQLITE_RUNTIME_PATH.read_text(encoding="utf-8")

    assert "#elif UNITY_STANDALONE_WIN" in content
    assert "TeamCity service-account player" in content
    assert "SQLite3.Open(connectionString.DatabasePath, out handle, (int)connectionString.OpenFlags, connectionString.VfsName);" in content
    assert "connectionString.OpenFlags == (SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create)" in content
    assert "SQLite3.Open16(connectionString.DatabasePath, out handle);" in content
    assert "var databasePathAsBytes = GetNullTerminatedUtf8(connectionString.DatabasePath);" in content


def test_windows_sqlite_runtime_keeps_unmanaged_utf8_prepare_strategy() -> None:
    """验证 Windows standalone 的 SQL prepare 走 unmanaged UTF-8 缓冲区路线。
    Verify that the Windows standalone SQL prepare path uses an unmanaged UTF-8 buffer route.
    """
    content = SQLITE_DLLIMPORT_PATH.read_text(encoding="utf-8")

    assert "public static extern Result Prepare2(IntPtr db, IntPtr sql, int numBytes, out IntPtr stmt, IntPtr pzTail);" in content
    assert "public static extern Result Prepare2(IntPtr db, byte[] sql, int numBytes, out IntPtr stmt, IntPtr pzTail);" in content
    assert "public static extern Result Prepare16(IntPtr db, [MarshalAs(UnmanagedType.LPWStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);" in content
    assert "AllocateNullTerminatedUtf8Sql(query)" in content
    assert "var r = Prepare2(db, queryPointer, -1, out stmt, IntPtr.Zero);" in content
    assert "Marshal.AllocHGlobal(queryBytes.Length + 1);" in content
    assert "Marshal.WriteByte(queryPointer, queryBytes.Length, 0);" in content
    assert "Encoding.UTF8.GetBytes(query + \"\\0\")" not in content


def test_windows_sqlite_runtime_keeps_utf8_bind_and_column_string_strategy() -> None:
    """验证 Windows standalone 的 SQLite 字符串绑定与读取保留 UTF-8 路线。
    Verify that the Windows standalone SQLite string bind and read path stays on the UTF-8 route.
    """
    content = SQLITE_DLLIMPORT_PATH.read_text(encoding="utf-8")

    assert 'EntryPoint = "sqlite3_bind_text"' in content
    assert "private static extern int BindTextUtf8Internal(IntPtr stmt, int index, IntPtr val, int n, IntPtr free);" in content
    assert "Encoding.UTF8.GetBytes(val)" in content
    assert "return BindTextUtf8Internal(stmt, index, utf8Pointer, utf8Bytes.Length, SqliteTransientUtf8Value);" in content
    assert "return ReadUtf8String(SQLite3.ColumnText(stmt, index), ColumnBytes(stmt, index));" in content
    assert "return Encoding.UTF8.GetString(valueBytes);" in content
    assert "Marshal.PtrToStringUni(SQLite3.ColumnText16(stmt, index));" not in content


def test_sqlite_execute_nonquery_accepts_row_for_pragma_results() -> None:
    """验证 SQLite 非查询执行路径会接受返回 Row 的 PRAGMA 结果。
    Verify that the SQLite non-query execution path accepts PRAGMA results that first return Row.
    """
    content = SQLITE_RUNTIME_PATH.read_text(encoding="utf-8")

    assert "ConsumeRemainingRowsForNonQuery" in content
    assert "if (r == SQLite3.Result.Row)" in content
    assert "r = ConsumeRemainingRowsForNonQuery(stmt);" in content
    assert "while (stepResult == SQLite3.Result.Row)" in content
