using System;
using System.Runtime.InteropServices;
using System.Text;
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3BackupHandle = System.IntPtr;

namespace SQLite4Unity3d
{
	/// <summary>
	/// 中文：封装 SQLCipher 原生 sqlite3 入口，统一处理 Windows Player 与其他平台的字符串封送差异。
	/// English: Wraps the native SQLCipher sqlite3 entrypoints and centralizes string marshalling differences between the Windows player and other platforms.
	/// </summary>
	public static partial class SQLite3
    {
        public enum ColType
        {
            Integer = 1,
            Float = 2,
            Text = 3,
            Blob = 4,
            Null = 5
        }

        public enum ConfigOption
        {
            SingleThread = 1,
            MultiThread = 2,
            Serialized = 3
        }

        public enum ExtendedResult
        {
			IOErrorRead = (Result.IOError | (1 << 8)),
			IOErrorShortRead = (Result.IOError | (2 << 8)),
			IOErrorWrite = (Result.IOError | (3 << 8)),
			IOErrorFsync = (Result.IOError | (4 << 8)),
			IOErrorDirFSync = (Result.IOError | (5 << 8)),
			IOErrorTruncate = (Result.IOError | (6 << 8)),
			IOErrorFStat = (Result.IOError | (7 << 8)),
			IOErrorUnlock = (Result.IOError | (8 << 8)),
			IOErrorRdlock = (Result.IOError | (9 << 8)),
			IOErrorDelete = (Result.IOError | (10 << 8)),
			IOErrorBlocked = (Result.IOError | (11 << 8)),
			IOErrorNoMem = (Result.IOError | (12 << 8)),
			IOErrorAccess = (Result.IOError | (13 << 8)),
			IOErrorCheckReservedLock = (Result.IOError | (14 << 8)),
			IOErrorLock = (Result.IOError | (15 << 8)),
			IOErrorClose = (Result.IOError | (16 << 8)),
			IOErrorDirClose = (Result.IOError | (17 << 8)),
			IOErrorSHMOpen = (Result.IOError | (18 << 8)),
			IOErrorSHMSize = (Result.IOError | (19 << 8)),
			IOErrorSHMLock = (Result.IOError | (20 << 8)),
			IOErrorSHMMap = (Result.IOError | (21 << 8)),
			IOErrorSeek = (Result.IOError | (22 << 8)),
			IOErrorDeleteNoEnt = (Result.IOError | (23 << 8)),
			IOErrorMMap = (Result.IOError | (24 << 8)),
			LockedSharedcache = (Result.Locked | (1 << 8)),
			BusyRecovery = (Result.Busy | (1 << 8)),
			CannottOpenNoTempDir = (Result.CannotOpen | (1 << 8)),
			CannotOpenIsDir = (Result.CannotOpen | (2 << 8)),
			CannotOpenFullPath = (Result.CannotOpen | (3 << 8)),
			CorruptVTab = (Result.Corrupt | (1 << 8)),
			ReadonlyRecovery = (Result.ReadOnly | (1 << 8)),
			ReadonlyCannotLock = (Result.ReadOnly | (2 << 8)),
			ReadonlyRollback = (Result.ReadOnly | (3 << 8)),
			AbortRollback = (Result.Abort | (2 << 8)),
			ConstraintCheck = (Result.Constraint | (1 << 8)),
			ConstraintCommitHook = (Result.Constraint | (2 << 8)),
			ConstraintForeignKey = (Result.Constraint | (3 << 8)),
			ConstraintFunction = (Result.Constraint | (4 << 8)),
			ConstraintNotNull = (Result.Constraint | (5 << 8)),
			ConstraintPrimaryKey = (Result.Constraint | (6 << 8)),
			ConstraintTrigger = (Result.Constraint | (7 << 8)),
			ConstraintUnique = (Result.Constraint | (8 << 8)),
			ConstraintVTab = (Result.Constraint | (9 << 8)),
			NoticeRecoverWAL = (Result.Notice | (1 << 8)),
			NoticeRecoverRollback = (Result.Notice | (2 << 8))
		}

        public enum Result
        {
			OK = 0,
			Error = 1,
			Internal = 2,
			Perm = 3,
			Abort = 4,
			Busy = 5,
			Locked = 6,
			NoMem = 7,
			ReadOnly = 8,
			Interrupt = 9,
			IOError = 10,
			Corrupt = 11,
			NotFound = 12,
			Full = 13,
			CannotOpen = 14,
			LockErr = 15,
			Empty = 16,
			SchemaChngd = 17,
			TooBig = 18,
			Constraint = 19,
			Mismatch = 20,
			Misuse = 21,
			NotImplementedLFS = 22,
			AccessDenied = 23,
			Format = 24,
			Range = 25,
			NonDBFile = 26,
			Notice = 27,
			Warning = 28,
			Row = 100,
			Done = 101
		}
    }

    public static partial class SQLite3
    {
#if UNITY_EDITOR
        private const string DLL_NAME = "sqlcipher";
#elif UNITY_STANDALONE
        private const string DLL_NAME = "sqlcipher";
#elif UNITY_WSA // define directive for Universal Windows Platform.
		private const string DLL_NAME = "sqlcipher";
#elif UNITY_ANDROID
		private const string DLL_NAME = "sqlcipher";
#elif UNITY_IOS
		private const string DLL_NAME = "__Internal";
#endif

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_threadsafe", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Threadsafe();

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db, int flags, [MarshalAs(UnmanagedType.LPStr)] string zvfs);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Open(byte[] filename, out IntPtr db, int flags, [MarshalAs(UnmanagedType.LPStr)] string zvfs);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_enable_load_extension", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result EnableLoadExtension(IntPtr db, int onoff);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Close(IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_close_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Close2(IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_initialize", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Initialize();

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_shutdown", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Shutdown();

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Config(ConfigOption option);

		//[DllImport(DLL_NAME, EntryPoint = "sqlite3_win32_set_directory", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		//public static extern int SetDirectory(uint directoryType, string directoryPath);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_busy_timeout", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result BusyTimeout(IntPtr db, int milliseconds);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Changes(IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Prepare2(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Prepare2(IntPtr db, IntPtr sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Prepare2(IntPtr db, byte[] sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_prepare16_v2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		public static extern Result Prepare16(IntPtr db, [MarshalAs(UnmanagedType.LPWStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

		/// <summary>
		/// 中文：使用手动分配的 UTF-8 SQL 缓冲区准备语句，避免 Windows Player 下 byte[] P/Invoke 封送污染 PRAGMA 文本。
		/// English: Prepares a statement with a manually allocated UTF-8 SQL buffer so the Windows player avoids byte[] P/Invoke marshalling corrupting PRAGMA text.
		/// </summary>
		public static IntPtr Prepare2(IntPtr db, string query)
		{
			IntPtr stmt;
			var queryPointer = IntPtr.Zero;
			try
			{
				queryPointer = AllocateNullTerminatedUtf8Sql(query);
				var r = Prepare2(db, queryPointer, -1, out stmt, IntPtr.Zero);
				if (r != Result.OK)
				{
					throw SQLiteException.New(r, GetErrmsg(db));
				}
			}
			finally
			{
				if (queryPointer != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(queryPointer);
				}
			}
			return stmt;
		}

		/// <summary>
		/// 中文：构造以 NUL 结尾的 UTF-8 SQL 缓冲区，确保 sqlite3_prepare_v2 读取到稳定的原始字节序列。
		/// English: Builds a NUL-terminated UTF-8 SQL buffer so sqlite3_prepare_v2 reads a stable raw byte sequence.
		/// </summary>
		private static IntPtr AllocateNullTerminatedUtf8Sql(string query)
		{
			var queryBytes = Encoding.UTF8.GetBytes(query);
			var queryPointer = Marshal.AllocHGlobal(queryBytes.Length + 1);
			Marshal.Copy(queryBytes, 0, queryPointer, queryBytes.Length);
			Marshal.WriteByte(queryPointer, queryBytes.Length, 0);
			return queryPointer;
		}

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Step(IntPtr stmt);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Reset(IntPtr stmt);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result Finalize(IntPtr stmt);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_last_insert_rowid", CallingConvention = CallingConvention.Cdecl)]
		public static extern long LastInsertRowid(IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr Errmsg(IntPtr db);

		public static string GetErrmsg(IntPtr db)
		{
			return Marshal.PtrToStringUni(Errmsg(db));
		}

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_parameter_index", CallingConvention = CallingConvention.Cdecl)]
		public static extern int BindParameterIndex(IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
		public static extern int BindNull(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
		public static extern int BindInt(IntPtr stmt, int index, int val);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
		public static extern int BindInt64(IntPtr stmt, int index, long val);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
		public static extern int BindDouble(IntPtr stmt, int index, double val);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_text", CallingConvention = CallingConvention.Cdecl)]
		private static extern int BindTextUtf8Internal(IntPtr stmt, int index, IntPtr val, int n, IntPtr free);

		private static readonly IntPtr SqliteTransientUtf8Value = new IntPtr(-1);

		/// <summary>
		/// 中文：使用手动分配的 UTF-8 文本缓冲区绑定参数，避免 Windows Player 下 SQLCipher 的 UTF-16 文本接口污染字符串内容。
		/// English: Binds parameters through a manually allocated UTF-8 text buffer so the Windows player avoids SQLCipher UTF-16 text APIs corrupting string payloads.
		/// </summary>
		public static int BindText(IntPtr stmt, int index, string val, int n, IntPtr free)
		{
			if (val == null)
			{
				return BindNull(stmt, index);
			}

			var utf8Pointer = IntPtr.Zero;
			try
			{
				var utf8Bytes = Encoding.UTF8.GetBytes(val);
				utf8Pointer = Marshal.AllocHGlobal(utf8Bytes.Length + 1);
				Marshal.Copy(utf8Bytes, 0, utf8Pointer, utf8Bytes.Length);
				Marshal.WriteByte(utf8Pointer, utf8Bytes.Length, 0);
				return BindTextUtf8Internal(stmt, index, utf8Pointer, utf8Bytes.Length, SqliteTransientUtf8Value);
			}
			finally
			{
				if (utf8Pointer != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(utf8Pointer);
				}
			}
		}

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_bind_blob", CallingConvention = CallingConvention.Cdecl)]
		public static extern int BindBlob(IntPtr stmt, int index, byte[] val, int n, IntPtr free);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
		public static extern int ColumnCount(IntPtr stmt);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ColumnName(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_name16", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr ColumnName16Internal(IntPtr stmt, int index);
		public static string ColumnName16(IntPtr stmt, int index)
		{
			return Marshal.PtrToStringUni(ColumnName16Internal(stmt, index));
		}

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
		public static extern ColType ColumnType(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
		public static extern int ColumnInt(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
		public static extern long ColumnInt64(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
		public static extern double ColumnDouble(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ColumnText(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ColumnText16(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ColumnBlob(IntPtr stmt, int index);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
		public static extern int ColumnBytes(IntPtr stmt, int index);

		/// <summary>
		/// 中文：按 sqlite3_column_text 返回的 UTF-8 字节读取字符串，避免 Windows Player 将 UTF-8 结果误按 UTF-16 解释。
		/// English: Reads strings from the UTF-8 bytes returned by sqlite3_column_text so the Windows player does not misinterpret UTF-8 result payloads as UTF-16.
		/// </summary>
		public static string ColumnString(IntPtr stmt, int index)
		{
			return ColumnStringFromUtf8(SQLite3.ColumnText(stmt, index), ColumnBytes(stmt, index));
		}

		/// <summary>
		/// 中文：把已经读取到的 UTF-8 文本指针和字节长度转换为托管字符串，供热路径回退复用同一列缓冲区。
		/// English: Converts an already-fetched UTF-8 text pointer and byte length into a managed string so hot-path fallbacks can reuse the same column buffer.
		/// </summary>
		public static string ColumnStringFromUtf8(IntPtr valuePointer, int valueByteLength)
		{
			return ReadUtf8String(valuePointer, valueByteLength);
		}

		/// <summary>
		/// 中文：把 sqlite3 暴露的 UTF-8 文本指针按字节长度转换为托管字符串，并保持空指针与空字符串的区别。
		/// English: Converts a sqlite3 UTF-8 text pointer into a managed string using the reported byte length while preserving the distinction between null and empty values.
		/// </summary>
		private static string ReadUtf8String(IntPtr valuePointer, int valueByteLength)
		{
			if (valuePointer == IntPtr.Zero)
			{
				return null;
			}

			if (valueByteLength == 0)
			{
				return string.Empty;
			}

			var valueBytes = new byte[valueByteLength];
			Marshal.Copy(valuePointer, valueBytes, 0, valueByteLength);
			return Encoding.UTF8.GetString(valueBytes);
		}

		public static byte[] ColumnByteArray(IntPtr stmt, int index)
		{
			int length = ColumnBytes(stmt, index);
			var result = new byte[length];
			if (length > 0)
				Marshal.Copy(ColumnBlob(stmt, index), result, 0, length);
			return result;
		}

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_errcode", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result GetResult(Sqlite3DatabaseHandle db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
		public static extern ExtendedResult ExtendedErrCode(IntPtr db);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
		public static extern int LibVersionNumber();

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_backup_init", CallingConvention = CallingConvention.Cdecl)]
		public static extern Sqlite3BackupHandle BackupInit(Sqlite3DatabaseHandle destDb, [MarshalAs(UnmanagedType.LPStr)] string destName, Sqlite3DatabaseHandle sourceDb, [MarshalAs(UnmanagedType.LPStr)] string sourceName);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_backup_step", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result BackupStep(Sqlite3BackupHandle backup, int numPages);

		[DllImport(DLL_NAME, EntryPoint = "sqlite3_backup_finish", CallingConvention = CallingConvention.Cdecl)]
		public static extern Result BackupFinish(Sqlite3BackupHandle backup);

	}
}