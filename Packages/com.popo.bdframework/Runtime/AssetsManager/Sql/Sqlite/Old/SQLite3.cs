//
// Copyright (c) 2009-2012 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using BDFramework;
using ILRuntime.Reflection;
using LitJson;
using Debug = UnityEngine.Debug;
#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;

#endif

namespace SQLite4Unity3d
{
    /// <summary>
    /// Represents an open connection to a SQLite database.
    /// </summary>
    public partial class SQLiteConnection : IDisposable
    {
        private bool _open;

        private TimeSpan _busyTimeout;

        //assembly -  classfullname,table mapping
        private Dictionary<int, Dictionary<string, TableMapping>> _mappings = null;
        private Dictionary<string, TableMapping> _tables = null;
        private System.Diagnostics.Stopwatch _sw;
        private TimeSpan _elapsed = default(TimeSpan);

        private int _transactionDepth = 0;
        private Random _rand = new Random();

        public Sqlite3DatabaseHandle Handle { get; private set; }
        internal static readonly Sqlite3DatabaseHandle NullHandle = default(Sqlite3DatabaseHandle);

        public string DatabasePath { get; private set; }

        /// <summary>
        /// is open
        /// </summary>
        public bool IsOpen
        {
            get { return _open; }
        }

        // Dictionary of synchronization objects.
        //
        // To prevent database disruption, a database file must be accessed *synchronously*.
        // For the purpose we create synchronous objects for each database file and store in the
        // static dictionary to share it among all connections.
        // The key of the dictionary is database file path and its value is an object to be used
        // by lock() statement.
        //
        // Use case:
        // - database file lock is done implicitly and automatically.
        // - To prepend deadlock, application may lock a database file explicity by either way:
        //   - RunInTransaction(Action) locks the database during the transaction (for insert/update)
        //   - RunInDatabaseLock(Action) similarly locks the database but no transaction (for query)
        private static Dictionary<string, object> syncObjects = new Dictionary<string, object>();

        #region debug tracing

        public bool Trace { get; set; }
        public bool TimeExecution { get; set; }

        public delegate void TraceHandler(string message);

        public event TraceHandler TraceEvent;

        internal void InvokeTrace(string message)
        {
            if (TraceEvent != null)
            {
                TraceEvent(message);
            }
        }

        public delegate void TimeExecutionHandler(TimeSpan executionTime, TimeSpan totalExecutionTime);

        public event TimeExecutionHandler TimeExecutionEvent;

        internal void InvokeTimeExecution(TimeSpan executionTime, TimeSpan totalExecutionTime)
        {
            if (TimeExecutionEvent != null)
            {
                TimeExecutionEvent(executionTime, totalExecutionTime);
            }
        }

        #endregion

        public bool StoreDateTimeAsTicks { get; private set; }

        /// <summary>
        /// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        /// Specifies the path to the database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        /// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        /// absolutely do want to store them as Ticks in all new projects. The default of false is
        /// only here for backwards compatibility. There is a *significant* speed advantage, with no
        /// down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, bool storeDateTimeAsTicks = false) : this(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks)
        {
        }

        /// <summary>
        /// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        /// Specifies the path to the database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        /// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        /// absolutely do want to store them as Ticks in all new projects. The default of false is
        /// only here for backwards compatibility. There is a *significant* speed advantage, with no
        /// down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, SQLiteOpenFlags openFlags, bool storeDateTimeAsTicks = false)
        {
            if (string.IsNullOrEmpty(databasePath))
                throw new ArgumentException("Must be specified", "databasePath");

            DatabasePath = databasePath;
            mayCreateSyncObject(databasePath);

#if NETFX_CORE
			SQLite3.SetDirectory(/*temp directory type*/2, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
#endif

            Sqlite3DatabaseHandle handle;

#if SILVERLIGHT || USE_CSHARP_SQLITEd
			var r = SQLite3.Open (databasePath, out handle, (int)openFlags, IntPtr.Zero);
#else
            // open using the byte[]
            // in the case where the path may include Unicode
            // force open to using UTF-8 using sqlite3_open_v2
            var databasePathAsBytes = GetNullTerminatedUtf8(DatabasePath);
            var r = SQLite3.Open(databasePathAsBytes, out handle, (int) openFlags, IntPtr.Zero);
#endif

            Handle = handle;
            if (r != SQLite3.Result.OK)
            {
                throw SQLiteException.New(r, String.Format("Could not open database file: {0} ({1})", DatabasePath, r));
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("<color=red>连接sqlite!</color>");
#endif
            }

            _open = true;

            StoreDateTimeAsTicks = storeDateTimeAsTicks;

            BusyTimeout = TimeSpan.FromSeconds(0.1);
        }

        static SQLiteConnection()
        {
            if (_preserveDuringLinkMagic)
            {
                var ti = new ColumnInfo();
                ti.Name = "magic";
            }
        }

        void mayCreateSyncObject(string databasePath)
        {
            if (!syncObjects.ContainsKey(databasePath))
            {
                syncObjects[databasePath] = new object();
            }
        }

        /// <summary>
        /// Gets the synchronous object, to be lock the database file for updating.
        /// </summary>
        /// <value>The sync object.</value>
        public object SyncObject
        {
            get { return syncObjects[DatabasePath]; }
        }

        public void EnableLoadExtension(int onoff)
        {
            SQLite3.Result r = SQLite3.EnableLoadExtension(Handle, onoff);
            if (r != SQLite3.Result.OK)
            {
                string msg = SQLite3.GetErrmsg(Handle);
                throw SQLiteException.New(r, msg);
            }
        }

        static byte[] GetNullTerminatedUtf8(string s)
        {
            var utf8Length = System.Text.Encoding.UTF8.GetByteCount(s);
            var bytes = new byte [utf8Length + 1];
            utf8Length = System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Used to list some code that we want the MonoTouch linker
        /// to see, but that we never want to actually execute.
        /// </summary>
#pragma warning disable 649
        static bool _preserveDuringLinkMagic;
#pragma warning restore 649

        /// <summary>
        /// Sets a busy handler to sleep the specified amount of time when a table is locked.
        /// The handler will sleep multiple times until a total time of <see cref="BusyTimeout"/> has accumulated.
        /// </summary>
        public TimeSpan BusyTimeout
        {
            get { return _busyTimeout; }
            set
            {
                _busyTimeout = value;
                if (Handle != NullHandle)
                {
                    SQLite3.BusyTimeout(Handle, (int) _busyTimeout.TotalMilliseconds);
                }
            }
        }

        /// <summary>
        /// Returns the mappings from types to tables that the connection
        /// currently understands.
        /// </summary>
        public IEnumerable<TableMapping> TableMappings
        {
            get { return _tables != null ? _tables.Values : Enumerable.Empty<TableMapping>(); }
        }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        /// The type whose mapping to the database is returned.
        /// </param>         
        /// <param name="createFlags">
        /// Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>     
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            //FOR ILR
            if (_mappings == null)
            {
                _mappings = new Dictionary<int, Dictionary<string, TableMapping>>();
            }

            //这里使用2级缓存,防止不同asseembly中，同名对象导致错误类型
            Dictionary<string, TableMapping> maps = null;
            var key = type.Assembly.GetHashCode();
            if (!_mappings.TryGetValue(key, out maps))
            {
                maps = new Dictionary<string, TableMapping>();
                _mappings[key] = maps;
            }

            //获取具体tableMap
            if (!maps.TryGetValue(type.FullName, out var map))
            {
                map = new TableMapping(type, createFlags);
                maps[type.FullName] = map;
            }

            return map;
        }


        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        private struct IndexedColumn
        {
            public int Order;
            public string ColumnName;
        }

        private struct IndexInfo
        {
            public string IndexName;
            public string TableName;
            public bool Unique;
            public List<IndexedColumn> Columns;
        }

        /// <summary>
        /// Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        public int DropTable<T>()
        {
            var map = GetMapping(typeof(T));

            var query = string.Format("drop table if exists \"{0}\"", map.TableName);

            return Execute(query);
        }


        /// <summary>
        /// Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        public int DropTableByType(Type type)
        {
            var map = GetMapping(type);

            var query = string.Format("drop table if exists \"{0}\"", map.TableName);

            return Execute(query);
        }

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        public int CreateTable<T>(CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(typeof(T), createFlags);
        }

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        public int CreateTableByType(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(type, createFlags);
        }

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>  
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        public int CreateTable(Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            if (_tables == null)
            {
                _tables = new Dictionary<string, TableMapping>();
            }

            TableMapping map;
            if (!_tables.TryGetValue(ty.FullName, out map))
            {
                map = GetMapping(ty, createFlags);
                _tables.Add(ty.FullName, map);
            }

            var query = "create table if not exists \"" + map.TableName + "\"(\n";

            var decls = map.Columns.Select(p => Orm.SqlDecl(p, StoreDateTimeAsTicks));
            var decl = string.Join(",\n", decls.ToArray());
            query += decl;
            query += ")";

            var count = Execute(query);

            if (count == 0)
            {
                //Possible bug: This always seems to return 0?
                // Table already exists, migrate it
                MigrateTable(map);
            }

            var indexes = new Dictionary<string, IndexInfo>();
            foreach (var c in map.Columns)
            {
                foreach (var i in c.Indices)
                {
                    var iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                        throw new Exception("All the columns in an index must have the same value for their Unique property");

                    iinfo.Columns.Add(new IndexedColumn {Order = i.Order, ColumnName = c.Name});
                }
            }

            foreach (var indexName in indexes.Keys)
            {
                var index = indexes[indexName];
                string[] columnNames = new string[index.Columns.Count];
                if (index.Columns.Count == 1)
                {
                    columnNames[0] = index.Columns[0].ColumnName;
                }
                else
                {
                    index.Columns.Sort((lhs, rhs) => { return lhs.Order - rhs.Order; });
                    for (int i = 0, end = index.Columns.Count; i < end; ++i)
                    {
                        columnNames[i] = index.Columns[i].ColumnName;
                    }
                }

                count += CreateIndex(indexName, index.TableName, columnNames, index.Unique);
            }

            return count;
        }

        /// <summary>
        /// Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string[] columnNames, bool unique = false)
        {
            const string sqlFormat = "create {2} index if not exists \"{3}\" on \"{0}\"(\"{1}\")";
            var sql = String.Format(sqlFormat, tableName, string.Join("\", \"", columnNames), unique ? "unique" : "", indexName);
            return Execute(sql);
        }

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(indexName, tableName, new string[] {columnName}, unique);
        }

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(tableName + "_" + columnName, tableName, columnName, unique);
        }

        /// <summary>
        /// Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string[] columnNames, bool unique = false)
        {
            return CreateIndex(tableName + "_" + string.Join("_", columnNames), tableName, columnNames, unique);
        }

        /// <summary>
        /// Creates an index for the specified object property.
        /// e.g. CreateIndex<Client>(c => c.Name);
        /// </summary>
        /// <typeparam name="T">Type to reflect to a database table.</typeparam>
        /// <param name="property">Property to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public void CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false)
        {
            MemberExpression mx;
            if (property.Body.NodeType == ExpressionType.Convert)
            {
                mx = ((UnaryExpression) property.Body).Operand as MemberExpression;
            }
            else
            {
                mx = (property.Body as MemberExpression);
            }

            var propertyInfo = mx.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }

            var propName = propertyInfo.Name;

            var map = GetMapping<T>();
            var colName = map.FindColumnWithPropertyName(propName).Name;

            CreateIndex(map.TableName, colName, unique);
        }

        public class ColumnInfo
        {
//			public int cid { get; set; }

            [Column("name")]
            public string Name { get; set; }

//			[Column ("type")]
//			public string ColumnType { get; set; }

            public int notnull { get; set; }

//			public string dflt_value { get; set; }

//			public int pk { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public List<ColumnInfo> GetTableInfo(string tableName)
        {
            var query = "pragma table_info(\"" + tableName + "\")";
            return Query<ColumnInfo>(query);
        }

        void MigrateTable(TableMapping map)
        {
            var existingCols = GetTableInfo(map.TableName);

            var toBeAdded = new List<TableMapping.Column>();

            foreach (var p in map.Columns)
            {
                var found = false;
                foreach (var c in existingCols)
                {
                    found = (string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (found)
                        break;
                }

                if (!found)
                {
                    toBeAdded.Add(p);
                }
            }

            foreach (var p in toBeAdded)
            {
                var addCol = "alter table \"" + map.TableName + "\" add column " + Orm.SqlDecl(p, StoreDateTimeAsTicks);
                Execute(addCol);
            }
        }

        /// <summary>
        /// Creates a new SQLiteCommand. Can be overridden to provide a sub-class.
        /// </summary>
        /// <seealso cref="SQLiteCommand.OnInstanceCreated"/>
        protected virtual SQLiteCommand NewCommand()
        {
            return new SQLiteCommand(this);
        }

        /// <summary>
        /// Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        /// in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        /// A <see cref="SQLiteCommand"/>
        /// </returns>
        public SQLiteCommand CreateCommand(string cmdText, params object[] ps)
        {
            if (!_open)
                throw SQLiteException.New(SQLite3.Result.Error, "the database is not open");

            var cmd = NewCommand();
            cmd.CommandText = cmdText;
            foreach (var o in ps)
            {
                cmd.Bind(o);
            }

            return cmd;
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// Use this method instead of Query when you don't expect rows back. Such cases include
        /// INSERTs, UPDATEs, and DELETEs.
        /// You can set the Trace or TimeExecution properties of the connection
        /// to profile execution.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// The number of rows modified in the database as a result of this execution.
        /// </returns>
        public int Execute(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (_sw == null)
                {
                    _sw = new Stopwatch();
                }

                _sw.Reset();
                _sw.Start();
            }

            var r = cmd.ExecuteNonQuery();

            if (TimeExecution)
            {
                _sw.Stop();
                _elapsed += _sw.Elapsed;
                this.InvokeTimeExecution(_sw.Elapsed, _elapsed);
            }

            return r;
        }

        public T ExecuteScalar<T>(string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);

            if (TimeExecution)
            {
                if (_sw == null)
                {
                    _sw = new Stopwatch();
                }

                _sw.Reset();
                _sw.Start();
            }

            var r = cmd.ExecuteScalar<T>();

            if (TimeExecution)
            {
                _sw.Stop();
                _elapsed += _sw.Elapsed;
                this.InvokeTimeExecution(_sw.Elapsed, _elapsed);
            }

            return r;
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string query, params object[] args) where T : new()
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<T>();
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// The enumerator will call sqlite3_step on each call to MoveNext, so the database
        /// connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : new()
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<T>();
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the specified mapping. This function is
        /// only used by libraries in order to query the database via introspection. It is
        /// normally not used.
        /// </summary>
        /// <param name="map">
        /// A <see cref="TableMapping"/> to use to convert the resulting rows
        /// into objects.
        /// </param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<object> Query(TableMapping map, string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<object>(map);
        }

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the specified mapping. This function is
        /// only used by libraries in order to query the database via introspection. It is
        /// normally not used.
        /// </summary>
        /// <param name="map">
        /// A <see cref="TableMapping"/> to use to convert the resulting rows
        /// into objects.
        /// </param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// The enumerator will call sqlite3_step on each call to MoveNext, so the database
        /// connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable<object> DeferredQuery(TableMapping map, string query, params object[] args)
        {
            var cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<object>(map);
        }

        /// <summary>
        /// Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// A queryable object that is able to translate Where, OrderBy, and Take
        /// queries into native SQL.
        /// </returns>
//        public TableQuery<T> Table<T>() where T : new()
//        {
//            return new TableQuery<T>(this);
//        }

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        public T Get<T>(object pk) where T : new()
        {
            var map = GetMapping(typeof(T));
            return Query<T>(map.GetByPrimaryKeySql, pk).First();
        }

        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate. Throws a not found exception
        /// if the object is not found.
        /// </returns>
//        public T Get<T>(Expression<Func<T, bool>> predicate) where T : new()
//        {
//            return Table<T>().Where(predicate).First();
//        }

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key or null
        /// if the object is not found.
        /// </returns>
        public T Find<T>(object pk) where T : new()
        {
            var map = GetMapping(typeof(T));
            return Query<T>(map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <param name="map">
        /// The TableMapping used to identify the object type.
        /// </param>
        /// <returns>
        /// The object with the given primary key or null
        /// if the object is not found.
        /// </returns>
        public object Find(object pk, TableMapping map)
        {
            return Query(map, map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate or null
        /// if the object is not found.
        /// </returns>
//        public T Find<T>(Expression<Func<T, bool>> predicate) where T : new()
//        {
//            return Table<T>().Where(predicate).FirstOrDefault();
//        }

        /// <summary>
        /// Whether <see cref="BeginTransaction"/> has been called and the database is waiting for a <see cref="Commit"/>.
        /// </summary>
        public bool IsInTransaction
        {
            get { return _transactionDepth > 0; }
        }

        /// <summary>
        /// Begins a new transaction. Call <see cref="Commit"/> to end the transaction.
        /// </summary>
        /// <example cref="System.InvalidOperationException">Throws if a transaction has already begun.</example>
        public void BeginTransaction()
        {
            // The BEGIN command only works if the transaction stack is empty, 
            //    or in other words if there are no pending transactions. 
            // If the transaction stack is not empty when the BEGIN command is invoked, 
            //    then the command fails with an error.
            // Rather than crash with an error, we will just ignore calls to BeginTransaction
            //    that would result in an error.
            if (Interlocked.CompareExchange(ref _transactionDepth, 1, 0) == 0)
            {
                try
                {
                    Execute("begin transaction");
                }
                catch (Exception ex)
                {
                    var sqlExp = ex as SQLiteException;
                    if (sqlExp != null)
                    {
                        // It is recommended that applications respond to the errors listed below 
                        //    by explicitly issuing a ROLLBACK command.
                        // TODO: This rollback failsafe should be localized to all throw sites.
                        switch (sqlExp.Result)
                        {
                            case SQLite3.Result.IOError:
                            case SQLite3.Result.Full:
                            case SQLite3.Result.Busy:
                            case SQLite3.Result.NoMem:
                            case SQLite3.Result.Interrupt:
                                RollbackTo(null, true);
                                break;
                        }
                    }
                    else
                    {
                        // Call decrement and not VolatileWrite in case we've already 
                        //    created a transaction point in SaveTransactionPoint since the catch.
                        Interlocked.Decrement(ref _transactionDepth);
                    }

                    throw;
                }
            }
            else
            {
                // Calling BeginTransaction on an already open transaction is invalid
                throw new InvalidOperationException("Cannot begin a transaction while already in a transaction.");
            }
        }

        /// <summary>
        /// Creates a savepoint in the database at the current point in the transaction timeline.
        /// Begins a new transaction if one is not in progress.
        /// 
        /// Call <see cref="RollbackTo"/> to undo transactions since the returned savepoint.
        /// Call <see cref="Release"/> to commit transactions after the savepoint returned here.
        /// Call <see cref="Commit"/> to end the transaction, committing all changes.
        /// </summary>
        /// <returns>A string naming the savepoint.</returns>
        public string SaveTransactionPoint()
        {
            int depth = Interlocked.Increment(ref _transactionDepth) - 1;
            string retVal = "S" + _rand.Next(short.MaxValue) + "D" + depth;

            try
            {
                Execute("savepoint " + retVal);
            }
            catch (Exception ex)
            {
                var sqlExp = ex as SQLiteException;
                if (sqlExp != null)
                {
                    // It is recommended that applications respond to the errors listed below 
                    //    by explicitly issuing a ROLLBACK command.
                    // TODO: This rollback failsafe should be localized to all throw sites.
                    switch (sqlExp.Result)
                    {
                        case SQLite3.Result.IOError:
                        case SQLite3.Result.Full:
                        case SQLite3.Result.Busy:
                        case SQLite3.Result.NoMem:
                        case SQLite3.Result.Interrupt:
                            RollbackTo(null, true);
                            break;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref _transactionDepth);
                }

                throw;
            }

            return retVal;
        }

        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="BeginTransaction"/> or <see cref="SaveTransactionPoint"/>.
        /// </summary>
        public void Rollback()
        {
            RollbackTo(null, false);
        }

        /// <summary>
        /// Rolls back the savepoint created by <see cref="BeginTransaction"/> or SaveTransactionPoint.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint"/>.  If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback"/></param>
        public void RollbackTo(string savepoint)
        {
            RollbackTo(savepoint, false);
        }

        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="BeginTransaction"/>.
        /// </summary>
        /// <param name="noThrow">true to avoid throwing exceptions, false otherwise</param>
        void RollbackTo(string savepoint, bool noThrow)
        {
            // Rolling back without a TO clause rolls backs all transactions 
            //    and leaves the transaction stack empty.   
            try
            {
                if (String.IsNullOrEmpty(savepoint))
                {
                    if (Interlocked.Exchange(ref _transactionDepth, 0) > 0)
                    {
                        Execute("rollback");
                    }
                }
                else
                {
                    DoSavePointExecute(savepoint, "rollback to ");
                }
            }
            catch (SQLiteException)
            {
                if (!noThrow)
                    throw;
            }

            // No need to rollback if there are no transactions open.
        }

        /// <summary>
        /// Releases a savepoint returned from <see cref="SaveTransactionPoint"/>.  Releasing a savepoint 
        ///    makes changes since that savepoint permanent if the savepoint began the transaction,
        ///    or otherwise the changes are permanent pending a call to <see cref="Commit"/>.
        /// 
        /// The RELEASE command is like a COMMIT for a SAVEPOINT.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to release.  The string should be the result of a call to <see cref="SaveTransactionPoint"/></param>
        public void Release(string savepoint)
        {
            DoSavePointExecute(savepoint, "release ");
        }

        void DoSavePointExecute(string savepoint, string cmd)
        {
            // Validate the savepoint
            int firstLen = savepoint.IndexOf('D');
            if (firstLen >= 2 && savepoint.Length > firstLen + 1)
            {
                int depth;
                if (Int32.TryParse(savepoint.Substring(firstLen + 1), out depth))
                {
                    // TODO: Mild race here, but inescapable without locking almost everywhere.
                    if (0 <= depth && depth < _transactionDepth)
                    {
#if NETFX_CORE
						Volatile.Write (ref _transactionDepth, depth);
#elif SILVERLIGHT
						_transactionDepth = depth;
#else
                        Thread.VolatileWrite(ref _transactionDepth, depth);
#endif
                        Execute(cmd + savepoint);
                        return;
                    }
                }
            }

            throw new ArgumentException("savePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savePoint");
        }

        /// <summary>
        /// Commits the transaction that was begun by <see cref="BeginTransaction"/>.
        /// </summary>
        public void Commit()
        {
            if (Interlocked.Exchange(ref _transactionDepth, 0) != 0)
            {
                Execute("commit");
            }

            // Do nothing on a commit with no open transaction
        }

        /// <summary>
        /// Executes <param name="action"> within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
        /// exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
        /// is rethrown.
        /// </summary>
        /// <param name="action">
        /// The <see cref="Action"/> to perform within a transaction. <param name="action"> can contain any number
        /// of operations on the connection but should never call <see cref="BeginTransaction"/> or
        /// <see cref="Commit"/>.
        /// </param>
        public void RunInTransaction(Action action)
        {
            try
            {
                lock (syncObjects[DatabasePath])
                {
                    var savePoint = SaveTransactionPoint();
                    action();
                    Release(savePoint);
                }
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        /// Executes <param name="action"> while blocking other threads to access the same database.
        /// </summary>
        /// <param name="action">
        /// The <see cref="Action"/> to perform within a lock.
        /// </param>
        public void RunInDatabaseLock(Action action)
        {
            lock (syncObjects[DatabasePath])
            {
                action();
            }
        }

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int InsertAll(System.Collections.IEnumerable objects)
        {
            var c = 0;
            RunInTransaction(() =>
            {
                foreach (var r in objects)
                {
                    c += Insert(r);
                }
            });
            return c;
        }

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int InsertAll(System.Collections.IEnumerable objects, string extra)
        {
            var c = 0;
            RunInTransaction(() =>
            {
                foreach (var r in objects)
                {
                    c += Insert(r, extra);
                }
            });
            return c;
        }

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int InsertAll(System.Collections.IEnumerable objects, Type objType)
        {
            var c = 0;
            RunInTransaction(() =>
            {
                foreach (var r in objects)
                {
                    c += Insert(r, objType);
                }
            });
            return c;
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return Insert(obj, "", obj.GetType());
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// If a UNIQUE constraint violation occurs with
        /// some pre-existing object, this function deletes
        /// the old object.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int InsertOrReplace(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return Insert(obj, "OR REPLACE", obj.GetType());
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, Type objType)
        {
            return Insert(obj, "", objType);
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// If a UNIQUE constraint violation occurs with
        /// some pre-existing object, this function deletes
        /// the old object.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int InsertOrReplace(object obj, Type objType)
        {
            return Insert(obj, "OR REPLACE", objType);
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, string extra)
        {
            if (obj == null)
            {
                return 0;
            }

            return Insert(obj, extra, obj.GetType());
        }

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, string extra, Type objType)
        {
            if (obj == null || objType == null)
            {
                return 0;
            }


            var map = GetMapping(objType);

#if NETFX_CORE
			if (map.PK != null && map.PK.IsAutoGuid)
			{
				// no GetProperty so search our way up the inheritance chain till we find it
				PropertyInfo prop;
				while (objType != null)
				{
					var info = objType.GetTypeInfo();
					prop = info.GetDeclaredProperty(map.PK.PropertyName);
					if (prop != null) 
					{
						if (prop.GetValue(obj, null).Equals(Guid.Empty))
						{
							prop.SetValue(obj, Guid.NewGuid(), null);
						}
						break; 
					}

					objType = info.BaseType;
				}
			}
#else
            if (map.PK != null && map.PK.IsAutoGuid)
            {
                var prop = objType.GetProperty(map.PK.PropertyName);
                if (prop != null)
                {
                    //if (prop.GetValue(obj, null).Equals(Guid.Empty)) { 
                    if (prop.GetGetMethod().Invoke(obj, null).Equals(Guid.Empty))
                    {
                        prop.SetValue(obj, Guid.NewGuid(), null);
                    }
                }
            }
#endif


            var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

            var cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
            var vals = new object[cols.Length];
            for (var i = 0; i < vals.Length; i++)
            {
                vals[i] = cols[i].GetValue(obj);
            }

            var insertCmd = map.GetInsertCommand(this, extra);
            int count;

            try
            {
                count = insertCmd.ExecuteNonQuery(vals);
            }
            catch (SQLiteException ex)
            {
                if (SQLite3.ExtendedErrCode(this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex.Result, ex.Message, map, obj);
                }

                throw;
            }

            if (map.HasAutoIncPK)
            {
                var id = SQLite3.LastInsertRowid(Handle);
                map.SetAutoIncPK(obj, id);
            }

            return count;
        }

        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return Update(obj, obj.GetType());
        }

        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        public int Update(object obj, Type objType)
        {
            int rowsAffected = 0;
            if (obj == null || objType == null)
            {
                return 0;
            }

            var map = GetMapping(objType);

            var pk = map.PK;

            if (pk == null)
            {
                throw new NotSupportedException("Cannot update " + map.TableName + ": it has no PK");
            }

            var cols = from p in map.Columns where p != pk select p;
            var vals = from c in cols select c.GetValue(obj);
            var ps = new List<object>(vals);
            ps.Add(pk.GetValue(obj));
            var q = string.Format("update \"{0}\" set {1} where {2} = ? ", map.TableName, string.Join(",", (from c in cols select "\"" + c.Name + "\" = ? ").ToArray()), pk.Name);

            try
            {
                rowsAffected = Execute(q, ps.ToArray());
            }
            catch (SQLiteException ex)
            {
                if (ex.Result == SQLite3.Result.Constraint && SQLite3.ExtendedErrCode(this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(ex, map, obj);
                }

                throw ex;
            }

            return rowsAffected;
        }

        /// <summary>
        /// Updates all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        public int UpdateAll(System.Collections.IEnumerable objects)
        {
            var c = 0;
            RunInTransaction(() =>
            {
                foreach (var r in objects)
                {
                    c += Update(r);
                }
            });
            return c;
        }

        /// <summary>
        /// Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        /// The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows deleted.
        /// </returns>
        public int Delete(object objectToDelete)
        {
            var map = GetMapping(objectToDelete.GetType());
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }

            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, pk.GetValue(objectToDelete));
        }

        /// <summary>
        /// Deletes the object with the specified primary key.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key of the object to delete.
        /// </param>
        /// <returns>
        /// The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        /// The type of object.
        /// </typeparam>
        public int Delete<T>(object primaryKey)
        {
            var map = GetMapping(typeof(T));
            var pk = map.PK;
            if (pk == null)
            {
                throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            }

            var q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, primaryKey);
        }

        /// <summary>
        /// Deletes all the objects from the specified table.
        /// WARNING WARNING: Let me repeat. It deletes ALL the objects from the
        /// specified table. Do you really want to do that?
        /// </summary>
        /// <returns>
        /// The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        /// The type of objects to delete.
        /// </typeparam>
        public int DeleteAll<T>()
        {
            var map = GetMapping(typeof(T));
            var query = string.Format("delete from \"{0}\"", map.TableName);
            return Execute(query);
        }

        ~SQLiteConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
#if UNITY_EDITOR
            Debug.Log("<color=red>关闭sqlite!</color>");
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            Close();
        }

        public void Close()
        {
            if (_open && Handle != NullHandle)
            {
                try
                {
                    if (_mappings != null)
                    {
                        foreach (var map in _mappings.Values)
                        {
                            foreach (var sqlInsertCommand in map.Values)
                            {
                                sqlInsertCommand.Dispose();
                            }
                        }
                    }

                    var r = SQLite3.Close(Handle);
                    if (r != SQLite3.Result.OK)
                    {
                        string msg = SQLite3.GetErrmsg(Handle);
                        throw SQLiteException.New(r, msg);
                    }
                }
                finally
                {
                    Handle = NullHandle;
                    _open = false;
                }
            }
        }
    }

    /// <summary>
    /// Represents a parsed connection string.
    /// </summary>
    class SQLiteConnectionString
    {
        public string ConnectionString { get; private set; }
        public string DatabasePath { get; private set; }
        public bool StoreDateTimeAsTicks { get; private set; }

#if NETFX_CORE
		static readonly string MetroStyleDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#endif

        public SQLiteConnectionString(string databasePath, bool storeDateTimeAsTicks)
        {
            ConnectionString = databasePath;
            StoreDateTimeAsTicks = storeDateTimeAsTicks;

#if NETFX_CORE
			DatabasePath = System.IO.IPath.Combine (MetroStyleDataPath, databasePath);
#else
            DatabasePath = databasePath;
#endif
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; set; }

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IndexedAttribute : Attribute
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public virtual bool Unique { get; set; }

        public IndexedAttribute()
        {
        }

        public IndexedAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : IndexedAttribute
    {
        public override bool Unique
        {
            get { return true; }
            set
            {
                /* throw?  */
            }
        }

        public UniqueAttribute() : base()
        {
        }

        public UniqueAttribute(string name, int order) : base(name, order)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MaxLengthAttribute : Attribute
    {
        public int Value { get; private set; }

        public MaxLengthAttribute(int length)
        {
            Value = length;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CollationAttribute : Attribute
    {
        public string Value { get; private set; }

        public CollationAttribute(string collation)
        {
            Value = collation;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NotNullAttribute : Attribute
    {
    }


    public static class Orm
    {
        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks) + " ";

            if (p.IsPK)
            {
                decl += "primary key ";
            }

            if (p.IsAutoInc)
            {
                decl += "autoincrement ";
            }

            if (!p.IsNullable)
            {
                decl += "not null ";
            }

            if (!string.IsNullOrEmpty(p.Collation))
            {
                decl += "collate " + p.Collation + " ";
            }

            return decl;
        }

        public static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            var clrType = p.ColumnType;
            if (clrType == typeof(Boolean) || clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32))
            {
                return "integer";
            }
            else if (clrType == typeof(UInt32) || clrType == typeof(Int64))
            {
                return "bigint";
            }
            else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal))
            {
                return "float";
            }
            else if (clrType == typeof(String))
            {
                int? len = p.MaxStringLength;

                if (len.HasValue)
                    return "varchar(" + len.Value + ")";

                return "varchar";
            }
            else if (clrType == typeof(TimeSpan))
            {
                return "bigint";
            }
            else if (clrType == typeof(DateTime))
            {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
            }
            else if (clrType == typeof(DateTimeOffset))
            {
                return "bigint";
#if !NETFX_CORE
            }
            else if (clrType.IsEnum)
            {
#else
			} else if (clrType.GetTypeInfo().IsEnum) {
#endif
                return "integer";
            }
            else if (clrType == typeof(byte[]))
            {
                return "blob";
            }
            else if (clrType == typeof(Guid))
            {
                return "varchar(36)";
            }
            //判断是否是list 或者array
            else if (clrType.FullName.Contains("System.Collections.Generic.List") || clrType.IsArray)
            {
//                Debug.Log("数组将以json形式保存:" + clrType.Name);
                return "varchar(4000)";
            }
            else
            {
                throw new NotSupportedException("Don't know about " + clrType);
            }
        }

        public static bool IsPK(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
			return attrs.Count() > 0;
#endif
        }

        public static string Collation(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(CollationAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0)
            {
                return ((CollationAttribute) attrs[0]).Value;
#else
			if (attrs.Count() > 0) {
				return ((CollationAttribute)attrs.First()).Value;
#endif
            }
            else
            {
                return string.Empty;
            }
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(AutoIncrementAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
			return attrs.Count() > 0;
#endif
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int? MaxStringLength(PropertyInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(MaxLengthAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0)
                return ((MaxLengthAttribute) attrs[0]).Value;
#else
			if (attrs.Count() > 0)
				return ((MaxLengthAttribute)attrs.First()).Value;
#endif

            return null;
        }

        public static bool IsMarkedNotNull(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(NotNullAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
	return attrs.Count() > 0;
#endif
        }
    }

    public partial class SQLiteCommand
    {
        SQLiteConnection _conn;
        private List<Binding> _bindings;

        public string CommandText { get; set; }

        internal SQLiteCommand(SQLiteConnection conn)
        {
            _conn = conn;
            _bindings = new List<Binding>();
            CommandText = "";
        }

        public int ExecuteNonQuery()
        {
            if (_conn.Trace)
            {
                _conn.InvokeTrace("Executing: " + this);
            }

            var r = SQLite3.Result.OK;
            lock (_conn.SyncObject)
            {
                var stmt = Prepare();
                r = SQLite3.Step(stmt);
                Finalize(stmt);
            }

            if (r == SQLite3.Result.Done)
            {
                int rowsAffected = SQLite3.Changes(_conn.Handle);
                return rowsAffected;
            }
            else if (r == SQLite3.Result.Error)
            {
                string msg = SQLite3.GetErrmsg(_conn.Handle);
                throw SQLiteException.New(r, msg);
            }
            else if (r == SQLite3.Result.Constraint)
            {
                if (SQLite3.ExtendedErrCode(_conn.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(r, SQLite3.GetErrmsg(_conn.Handle));
                }
            }

            throw SQLiteException.New(r, r.ToString());
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T)));
        }

        public List<T> ExecuteQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T))).ToList();
        }

        /// <summary>
        /// For ILRuntime
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public List<object> ExecuteQueryForILR(Type t)
        {
            var ret = ExecuteDeferredQueryForILR(t).ToList();
            return ret;
        }

        public List<T> ExecuteQuery<T>(TableMapping map)
        {
            return ExecuteDeferredQuery<T>(map).ToList();
        }

        /// <summary>
        /// Invoked every time an instance is loaded from the database.
        /// </summary>
        /// <param name='obj'>
        /// The newly created object.
        /// </param>
        /// <remarks>
        /// This can be overridden in combination with the <see cref="SQLiteConnection.NewCommand"/>
        /// method to hook into the life-cycle of objects.
        ///
        /// Type safety is not possible because MonoTouch does not support virtual generic methods.
        /// </remarks>
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            if (_conn.Trace)
            {
                _conn.InvokeTrace("Executing Query: " + this);
            }

            lock (_conn.SyncObject)
            {
                var stmt = Prepare();
                try
                {
                    var cols = new TableMapping.Column[SQLite3.ColumnCount(stmt)];

                    for (int i = 0; i < cols.Length; i++)
                    {
                        var name = SQLite3.ColumnName16(stmt, i);
                        cols[i] = map.FindColumn(name);
                    }

                    while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                    {
                        var obj = ILRuntimeHelper.CreateInstance(map.MappedType);
                        for (int i = 0; i < cols.Length; i++)
                        {
                            if (cols[i] == null)
                                continue;
                            var colType = SQLite3.ColumnType(stmt, i);
                            var val = ReadCol(stmt, i, colType, cols[i].ColumnType);
                            cols[i].SetValue(obj, val);
                        }

                        OnInstanceCreated(obj);
                        yield return (T) obj;
                    }
                }
                finally
                {
                    SQLite3.Finalize(stmt);
                }
            }
        }


        /// <summary>
        /// For ILRuntime
        /// </summary>
        /// <param name="mapType"></param>
        /// <returns></returns>
        public IEnumerable<object> ExecuteDeferredQueryForILR(Type mapType)
        {
#if UNITY_EDITOR
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            var map = _conn.GetMapping(mapType);
            if (_conn.Trace)
            {
                _conn.InvokeTrace("Executing Query: " + this);
            }

            lock (_conn.SyncObject)
            {
                //查询
                var stmt = Prepare();
                //
                try
                {
                    var cols = new TableMapping.Column[SQLite3.ColumnCount(stmt)];
                    for (int i = 0; i < cols.Length; i++)
                    {
                        var name = SQLite3.ColumnName16(stmt, i);
                        cols[i] = map.FindColumn(name);
                    }
#if UNITY_EDITOR
                    sw.Stop();
                    var serchSqlTime = sw.ElapsedTicks / 10000f;
                    sw.Restart();
#endif
                    //反序列化
                    var count = 0;
                    while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                    {
                        var obj = ILRuntimeHelper.CreateInstance(map.MappedType);
                        for (int i = 0; i < cols.Length; i++)
                        {
                            if (cols[i] != null)
                            {
                                var colType = SQLite3.ColumnType(stmt, i);
                                var val = ReadCol(stmt, i, colType, cols[i].ColumnType);
                                cols[i].SetValue(obj, val);
                            }
                        }

                        count++;
                        yield return obj;
                    }
#if UNITY_EDITOR
                    sw.Stop();
                    var deSerializeTime = sw.ElapsedTicks / 10000f;

                    var total = serchSqlTime + deSerializeTime;
                    if (total > 10)
                    {
                        Debug.LogError($"<color=white>消耗较高!</color>:<color=yellow>{total}ms</color>，元素数量:<color=red>{count}</color>, 执行sql耗时: <color=yellow>{serchSqlTime} ms</color>,反序列化耗时：<color=yellow>{deSerializeTime}ms</color>");
                    }
#endif
                }
                finally
                {
                    SQLite3.Finalize(stmt);
                }
            }
        }

        public T ExecuteScalar<T>()
        {
            if (_conn.Trace)
            {
                _conn.InvokeTrace("Executing Query: " + this);
            }

            T val = default(T);

            lock (_conn.SyncObject)
            {
                var stmt = Prepare();

                try
                {
                    var r = SQLite3.Step(stmt);
                    if (r == SQLite3.Result.Row)
                    {
                        var colType = SQLite3.ColumnType(stmt, 0);
                        val = (T) ReadCol(stmt, 0, colType, typeof(T));
                    }
                    else if (r == SQLite3.Result.Done)
                    {
                    }
                    else
                    {
                        throw SQLiteException.New(r, SQLite3.GetErrmsg(_conn.Handle));
                    }
                }
                finally
                {
                    Finalize(stmt);
                }
            }

            return val;
        }

        public void Bind(string name, object val)
        {
            _bindings.Add(new Binding {Name = name, Value = val});
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        public override string ToString()
        {
            var parts = new string[1 + _bindings.Count];
            parts[0] = CommandText;
            var i = 1;
            foreach (var b in _bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }

            return string.Join(Environment.NewLine, parts);
        }

        Sqlite3Statement Prepare()
        {
            var stmt = SQLite3.Prepare2(_conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }

        void Finalize(Sqlite3Statement stmt)
        {
            SQLite3.Finalize(stmt);
        }

        void BindAll(Sqlite3Statement stmt)
        {
            int nextIdx = 1;
            foreach (var b in _bindings)
            {
                if (b.Name != null)
                {
                    b.Index = SQLite3.BindParameterIndex(stmt, b.Name);
                }
                else
                {
                    b.Index = nextIdx++;
                }

                BindParameter(stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks);
            }
        }

        internal static IntPtr NegativePointer = new IntPtr(-1);

        internal static void BindParameter(Sqlite3Statement stmt, int index, object value, bool storeDateTimeAsTicks)
        {
            if (value == null)
            {
                SQLite3.BindNull(stmt, index);
            }
            else
            {
                if (value is Int32)
                {
                    SQLite3.BindInt(stmt, index, (int) value);
                }
                else if (value is String)
                {
                    SQLite3.BindText(stmt, index, (string) value, -1, NegativePointer);
                }
                else if (value is Byte || value is UInt16 || value is SByte || value is Int16)
                {
                    SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is Boolean)
                {
                    SQLite3.BindInt(stmt, index, (bool) value ? 1 : 0);
                }
                else if (value is UInt32 || value is Int64)
                {
                    SQLite3.BindInt64(stmt, index, Convert.ToInt64(value));
                }
                else if (value is Single || value is Double || value is Decimal)
                {
                    SQLite3.BindDouble(stmt, index, Convert.ToDouble(value));
                }
                else if (value is TimeSpan)
                {
                    SQLite3.BindInt64(stmt, index, ((TimeSpan) value).Ticks);
                }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks)
                    {
                        SQLite3.BindInt64(stmt, index, ((DateTime) value).Ticks);
                    }
                    else
                    {
                        SQLite3.BindText(stmt, index, ((DateTime) value).ToString("yyyy-MM-dd HH:mm:ss"), -1, NegativePointer);
                    }
                }
                else if (value is DateTimeOffset)
                {
                    SQLite3.BindInt64(stmt, index, ((DateTimeOffset) value).UtcTicks);
#if !NETFX_CORE
                }
                else if (value.GetType().IsEnum)
                {
#else
				} else if (value.GetType().GetTypeInfo().IsEnum) {
#endif
                    SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is byte[])
                {
                    SQLite3.BindBlob(stmt, index, (byte[]) value, ((byte[]) value).Length, NegativePointer);
                }
                else if (value is Guid)
                {
                    SQLite3.BindText(stmt, index, ((Guid) value).ToString(), 72, NegativePointer);
                }
                //ForILR:数组当成json串存储,文档存储
                else if (value.GetType().IsArray || value.GetType().FullName.Contains(".List"))
                {
                    var v = JsonMapper.ToJson(value);
                    SQLite3.BindText(stmt, index, v, -1, NegativePointer);
                }
                else
                {
                    throw new NotSupportedException("Cannot store type: " + value.GetType());
                }
            }
        }

        class Binding
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public int Index { get; set; }
        }

        object ReadCol(Sqlite3Statement stmt, int index, SQLite3.ColType type, Type clrType)
        {
            if (type == SQLite3.ColType.Null)
            {
                return null;
            }
            else
            {
                if (clrType.FullName == typeof(String).FullName)
                {
                    return SQLite3.ColumnString(stmt, index);
                }
                else if (clrType.FullName == typeof(Int32).FullName)
                {
                    return (int) SQLite3.ColumnInt(stmt, index);
                }
                else if (clrType.FullName == typeof(Boolean).FullName)
                {
                    return SQLite3.ColumnInt(stmt, index) == 1;
                }
                else if (clrType.FullName == typeof(double).FullName)
                {
                    return SQLite3.ColumnDouble(stmt, index);
                }
                else if (clrType.FullName == typeof(float).FullName)
                {
                    return (float) SQLite3.ColumnDouble(stmt, index);
                }
                else if (clrType.FullName == typeof(TimeSpan).FullName)
                {
                    return new TimeSpan(SQLite3.ColumnInt64(stmt, index));
                }
                else if (clrType.FullName == typeof(DateTime).FullName)
                {
                    if (_conn.StoreDateTimeAsTicks)
                    {
                        return new DateTime(SQLite3.ColumnInt64(stmt, index));
                    }
                    else
                    {
                        var text = SQLite3.ColumnString(stmt, index);
                        return DateTime.Parse(text);
                    }
                }
                else if (clrType.FullName == typeof(DateTimeOffset).FullName)
                {
                    return new DateTimeOffset(SQLite3.ColumnInt64(stmt, index), TimeSpan.Zero);
#if !NETFX_CORE
                }
                else if (clrType.IsEnum)
                {
#else
				} else if (clrType.GetTypeInfo().IsEnum) {
#endif
                    return SQLite3.ColumnInt(stmt, index);
                }
                else if (clrType.FullName == typeof(Int64).FullName)
                {
                    return SQLite3.ColumnInt64(stmt, index);
                }
                else if (clrType.FullName == typeof(UInt32).FullName)
                {
                    return (uint) SQLite3.ColumnInt64(stmt, index);
                }
                else if (clrType.FullName == typeof(decimal).FullName)
                {
                    return (decimal) SQLite3.ColumnDouble(stmt, index);
                }
                else if (clrType.FullName == typeof(Byte).FullName)
                {
                    return (byte) SQLite3.ColumnInt(stmt, index);
                }
                else if (clrType.FullName == typeof(UInt16).FullName)
                {
                    return (ushort) SQLite3.ColumnInt(stmt, index);
                }
                else if (clrType.FullName == typeof(Int16).FullName)
                {
                    return (short) SQLite3.ColumnInt(stmt, index);
                }
                else if (clrType.FullName == typeof(sbyte).FullName)
                {
                    return (sbyte) SQLite3.ColumnInt(stmt, index);
                }
                else if (clrType.FullName == typeof(byte[]).FullName)
                {
                    return SQLite3.ColumnByteArray(stmt, index);
                }
                else if (clrType.FullName == typeof(Guid).FullName)
                {
                    var text = SQLite3.ColumnString(stmt, index);
                    return new Guid(text);
                }
                else if (clrType.FullName.Contains(".List") || clrType.IsArray)
                {
                    var text = SQLite3.ColumnString(stmt, index);
                    return JsonMapper.ToObject(clrType, text);
                }
                else
                {
                    throw new NotSupportedException("Don't know how to read " + clrType);
                }
            }
        }
    }

    /// <summary>
    /// Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        public bool Initialized { get; set; }

        protected SQLiteConnection Connection { get; set; }

        public string CommandText { get; set; }

        protected Sqlite3Statement Statement { get; set; }
        internal static readonly Sqlite3Statement NullStatement = default(Sqlite3Statement);

        internal PreparedSqlLiteInsertCommand(SQLiteConnection conn)
        {
            Connection = conn;
        }

        public int ExecuteNonQuery(object[] source)
        {
            if (Connection.Trace)
            {
                Connection.InvokeTrace("Executing: " + CommandText);
            }

            var r = SQLite3.Result.OK;

            if (!Initialized)
            {
                Statement = Prepare();
                Initialized = true;
            }

            //bind the values.
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    SQLiteCommand.BindParameter(Statement, i + 1, source[i], Connection.StoreDateTimeAsTicks);
                }
            }

            r = SQLite3.Step(Statement);

            if (r == SQLite3.Result.Done)
            {
                int rowsAffected = SQLite3.Changes(Connection.Handle);
                SQLite3.Reset(Statement);
                return rowsAffected;
            }
            else if (r == SQLite3.Result.Error)
            {
                string msg = SQLite3.GetErrmsg(Connection.Handle);
                SQLite3.Reset(Statement);
                throw SQLiteException.New(r, msg);
            }
            else if (r == SQLite3.Result.Constraint && SQLite3.ExtendedErrCode(Connection.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
            {
                SQLite3.Reset(Statement);
                throw NotNullConstraintViolationException.New(r, SQLite3.GetErrmsg(Connection.Handle));
            }
            else
            {
                SQLite3.Reset(Statement);
                throw SQLiteException.New(r, r.ToString());
            }
        }

        protected virtual Sqlite3Statement Prepare()
        {
            var stmt = SQLite3.Prepare2(Connection.Handle, CommandText);
            return stmt;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Statement != NullStatement)
            {
                try
                {
                    SQLite3.Finalize(Statement);
                }
                finally
                {
                    Statement = NullStatement;
                    Connection = null;
                }
            }
        }

        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }
    }


    public class NotNullConstraintViolationException : Exception
    {
        public IEnumerable<TableMapping.Column> Columns { get; protected set; }

        protected NotNullConstraintViolationException(SQLite3.Result r, string message) : this(r, message, null, null)
        {
        }

        protected NotNullConstraintViolationException(SQLite3.Result r, string message, TableMapping mapping, object obj)
        {
            if (mapping != null && obj != null)
            {
                this.Columns = from c in mapping.Columns where c.IsNullable == false && c.GetValue(obj) == null select c;
            }
        }

        public static new NotNullConstraintViolationException New(SQLite3.Result r, string message)
        {
            return new NotNullConstraintViolationException(r, message);
        }

        public static NotNullConstraintViolationException New(SQLite3.Result r, string message, TableMapping mapping, object obj)
        {
            return new NotNullConstraintViolationException(r, message, mapping, obj);
        }

        public static NotNullConstraintViolationException New(SQLiteException exception, TableMapping mapping, object obj)
        {
            return new NotNullConstraintViolationException(exception.Result, exception.Message, mapping, obj);
        }
    }
}
