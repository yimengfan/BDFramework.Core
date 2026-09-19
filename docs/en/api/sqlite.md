# Tables (SQLite)

The data layer is built on **sqlite-net + SQLCipher** and uses a dual-database model: `local.db` (read-only, encrypted) + `server.db` (read/write, unencrypted).

## The dual-database model

| Database | Path | Open mode | Encryption | Purpose |
|----------|------|-----------|------------|---------|
| `local.db` | `<FIRST_LOAD_DIR>/local.db` | `ReadOnly` | SqlCipher | Client-side static configuration tables |
| `server.db` | `<root>/server_data/server.db` | `ReadWrite \| Create` in the Editor | **Unencrypted** | Server-side data |

!!! note "`local.db` has to be copied to a writable directory first"
    `ClientAssetsUtils.PersistentOnlyFiles` contains exactly one entry: `SqliteLoder.LOCAL_DB_PATH = "local.db"`. The `local.db` inside the client package is copied into `FIRST_LOAD_DIR` before it is opened.

    The Editor has no such copy flow, so you have to call `SqliteLoder.LoadLocalDBOnEditor()` by hand.

## `SqliteLoder`

```csharp
namespace BDFramework.Sql

static public partial class SqliteLoder
{
    public readonly static string LOCAL_DB_PATH  = "local.db";
    public readonly static string SERVER_DB_PATH = "server.db";

    static public SQLiteConnection Connection { get; set; }

    // ── 加密 ──
    public static Func<string> PasswordFallback { get; set; }
    public static string password;
    public static string Password { get; set; }      // password 非空则用它，否则 PasswordFallback?.Invoke() ?? ""

    // ── 初始化 ──
    static public void Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir);
    static public SQLiteConnection LoadDBReadOnly(string path);
    static public SQLiteConnection LoadDBReadWriteCreate(string path, bool isUsePsw = true);
    static public SQLiteConnection GetSqliteConnect(string dbname);
    static public void Close(string dbName = "");

    // ── 路径 ──
    static public string GetLocalDBPath(string root, RuntimePlatform platform);
    static public string GetServerDBPath(string root);          // root/server_data/server.db

    // ── Editor ──
    static public string LoadLocalDBOnEditor(string root, RuntimePlatform platform);
    static public string LoadLocalDBOnEditor();
    static public void   LoadServerDBOnEditor(string root);
    static public void   LoadSQLOnEditor(string sqlPath, bool isUsePsw = true);
    static public string DeleteLocalDBFile(string root, RuntimePlatform platform);
    static public string DeleteServerDBFile(string root);

    // ── 性能 ──
    static public void ApplyReadOnlyPragmas(SQLiteConnection con);
}
```

!!! warning "`Init`'s `assetLoadPathType` and `secondDir` are dead parameters"
    ```csharp
    static public void Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir)
    {
        Connection?.Dispose();
        var db_path = IPath.Combine(firstDir, LOCAL_DB_PATH);   // 恒定取 firstDir
        Connection = LoadDBReadOnly(db_path);
    }
    ```
    `assetLoadPathType` and `secondDir` **take part in no decision whatsoever**. Aligning the signature with `BResources.Init` is deliberate, but it is easy to misread.

`LoadDBReadOnly` does `Debug.LogError("DB不存在:" + path)` and returns `null` when the file does not exist (it does not throw).

In the Editor, `LoadDBReadWriteCreate` first runs `EnsureEditorSqlCipherReady()`: it tries `SQLite3.LibVersionNumber()` and, on failure, prints diagnostics containing the plugin path / OS / CPU / Unity version and **throws**.

### Read-only PRAGMA optimisation

`ApplyReadOnlyPragmas` takes effect on the `LoadDBReadOnly` path:

| PRAGMA | Value |
|--------|-------|
| `cache_size` | `-N` KB, `N = fileSizeKB × 2.0`, clamped to `[2000, 20000]` |
| `mmap_size` | `268435456` (256 MB) |
| `journal_mode` | `OFF` |
| `synchronous` | `OFF` |
| `temp_store` | `MEMORY` |
| `locking_mode` | `NORMAL` |

The whole block is wrapped in `try/catch`; on failure it only does `Debug.LogWarning` and **does not block loading**. The values read back for `page_size` / `cache_size` / `mmap_size` are handed to `SqlitePerformanceMonitor.RecordPragmaConfig`.

!!! note "The write path does not apply these PRAGMAs"
    `LoadDBReadWriteCreate` (`server.db`, table-export writes) takes a different path and applies none of these optimisations.

## `SqliteHelper` and `SQLiteService`

```csharp
namespace BDFramework.Sql

static public class SqliteHelper
{
    static public SQLiteService DB { get; }                 // 主库
    static public SQLiteService GetDB(string dbName);       // 具名库缓存
    static public void RemoveDBService(string dbName);

    public class SQLiteService
    {
        public SQLiteConnection Connection { get; private set; }
        public bool   IsClose { get; }                       // Connection == null || !IsOpen
        public string DBPath  { get; }                       // Connection.DatabasePath
        public TableQueryForILRuntime ILRuntimeTable { get; }
        public TableQueryForILRuntime GetTableRuntime();      // 与 ILRuntimeTable 是同一实例

        public SQLiteService(SQLiteConnection con);

        public void CreateTable<T>();
        public void CreateTable(Type t);
        public void InsertTable(System.Collections.IEnumerable objects);
        public void Insert(object @object);
        public void InsertAll<T>(List<T> obj);
        public TableQuery<T> GetTable<T>() where T : new();
    }
}
```

!!! danger "`CreateTable` drops the table first"
    ```csharp
    public void CreateTable(Type t)
    {
        Connection.DropTable(t);      // ← 清空数据
        Connection.CreateTable(t);
    }
    ```
    **Do not use it in production logic to "make sure the table exists".**

!!! warning "`SQLiteService` exposes no public Update / Delete / Execute"
    Writing to the database means either reaching for the underlying `SQLiteConnection` through `.Connection` (which is exactly what the Editor build pipeline does: `SqliteHelper.DB.Connection.CreateTable<TableLog>()`), or going through `GetTable<T>()` to get sqlite-net's native `TableQuery<T>`.

`SqliteHelper.DB` returns `null` when `SqliteLoder.Connection` is `null` or already closed; `GetDB(name)` **removes the cache entry and returns `null`** when the connection is unusable (it does not throw).

## `TableQueryForILRuntime` — the query builder

```csharp
namespace SQLite4Unity3d

public class TableQueryForILRuntime : BaseTableQuery
{
    public SQLiteConnection Connection { get; private set; }
    public static bool EnableEditorSqlLog = true;      // 基准测试期可关

    public TableQueryForILRuntime(SQLiteConnection connection);
    public void EnableSqlCahce(int triggerCacheNum = 5, float triggerChacheTimer = 0.05f);

    public TableQueryForILRuntime Exec(string sql);
    public TableQueryForILRuntime Where(string where, object value);
    public TableQueryForILRuntime Where(string where);
    public TableQueryForILRuntime And { get; }          // ★ 读取即修改
    public TableQueryForILRuntime Or  { get; }          // ★ 读取即修改
    public TableQueryForILRuntime WhereIn<T>(string field, IEnumerable<T> values);
    public TableQueryForILRuntime WhereIn(string field, params object[] values);
    public TableQueryForILRuntime WhereEqual(string where, object value);
    public TableQueryForILRuntime WhereOr(string field, string operation = "", params object[] objs);
    public TableQueryForILRuntime WhereAnd(string field, string operation = "", params object[] objs);
    public TableQueryForILRuntime Limit(int limitValue);
    public TableQueryForILRuntime OrderByDesc(string field);
    public TableQueryForILRuntime OrderBy(string field);

    public T From<T>(string selection = "*");
    public object From(Type type, string selection = "*");          // 内部自动 Limit(1)
    public List<T> FromAll<T>(string selection = "*");
    public List<object> FromAll(Type type, string selection = "*");
}
```

The SQL it generates looks like: `select {selection} from {type.Name} where {where} Limit {limit}`.

!!! danger "The table name is `type.Name` (without the namespace)"
    **Classes with the same name in different namespaces collide on the same table.** Table class names must be globally unique.

### Typical usage

```csharp
// 单条件
var hero = SqliteHelper.DB.GetTableRuntime()
    .Where("id = {0}", 1)
    .FromAll<Hero>();

// 多条件（And / Or 追加）
var ds = SqliteHelper.DB.GetTableRuntime()
    .Where("id > 1").And.Where("id < 3")
    .FromAll<Hero>();

// 同字段多值
var ds2 = SqliteHelper.DB.GetTableRuntime()
    .WhereAnd("id", "=", 1, 2)       // id = 1 and id = 2 → 通常配合 Or 使用
    .FromAll<Hero>();

// 排序 + 限制
var top = SqliteHelper.DB.GetTableRuntime()
    .Where("level > {0}", 10)
    .OrderByDesc("level")
    .Limit(10)
    .FromAll<Hero>();

// 取单条
var one = SqliteHelper.DB.GetTableRuntime().Where("id = {0}", 1).From<Hero>();

// 原生 SQL
var list = SqliteHelper.DB.GetTableRuntime().Exec("select * from Hero where id > 5").FromAll<Hero>();
```

### Three traps you have to know about

!!! danger "1. The `And` / `Or` properties mutate on read"
    ```csharp
    public TableQueryForILRuntime And { get { @where += " and"; return this; } }
    ```
    The SQL is concatenated inside `get`. **You must not write branching code around it**:

    ```csharp
    // ✗ 错误：无论条件真假都追加了 " and"
    q.Where("id > 0");
    if (needFilter) q.And;
    q.Where("level > 5");
    ```

!!! danger "2. `WhereOr` / `WhereAnd` overwrite rather than append"
    They **replace `@where` wholesale** (they do not append). To express "several conditions on the same field" you must use the `Where(...).And.Where(...)` form.

!!! danger "3. The builder state resets after every query"
    After `GenerateCommand`, `@sql` / `@limit` / `@where` are all cleared. So `Exec(...)` only affects the **next** query.

### Prepared statement caching

It only takes effect once `EnableSqlCahce()` has been called. When the same SQL has executed at least `triggerCacheNum` times, it goes through `Connection.GetPreparedStatement` / `SetPreparedStatement`.

In the Editor, the same SQL executing **more than 10 times** triggers `Debug.LogError("Sql执行次数过多:...")`. When running benchmarks:

```csharp
TableQueryForILRuntime.EnableEditorSqlLog = false;
```

## Business table declaration contract

!!! note "There is no `[Table]` attribute"
    Table classes are **ordinary code-generated POCOs plus sqlite-net attributes**; there is no `[Table]` marker.

| Item | Convention |
|----|------|
| Generated location | `Assets/Code/Game/Table/Local/<Name>.xlsx.cs` (local tables)<br/>`Assets/Code/Game/Table/Server/<Name>.xlsx.cs` (server tables) |
| Namespace | `Game.Data.Local` / `Game.Data.Server` — **must use the `Game.Data.*` prefix** |
| Generated header | `// <auto-generated> // Genera by BDFramework` |
| Primary key | `[PrimaryKey]` (e.g. `Hero.Id`); use `[AutoIncrement]` for auto-increment |
| Array fields | Declared directly (`string[] AttributeName`, `int[] Skills`) |
| Table name | **The class name** (without the namespace) |

A real sample (`Assets/Code/Game/Table/Local/Hero.xlsx.cs`):

```csharp
namespace Game.Data.Local
{
    [Serializable()]
    public class Hero
    {
        [PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public string Level { get; set; }
        public int StarLevel { get; set; }
        public int NextLevel { get; set; }
        public string[] AttributeName { get; set; }
        public int[] AttributeValue { get; set; }
        public int[] Skills { get; set; }
    }
}
```

!!! danger "Changing the namespace breaks table export"
    `BuildTools_Excel2SQLite.CollectTableTypes()` collects types by `Namespace.StartsWith("Game.Data.")`.

## Encryption configuration

`GameCipherConfigProcessor` (`[GameConfig(2, "加密")]`) injects this in `OnConfigLoad`:

```csharp
public class Config : ConfigDataBase
{
    public string SqlitePassword = "password123!!!";   // LabelTextAttribute("Sqlite密码")
    public string ScriptPubKey   = "";                 // LabelTextAttribute("DLL公钥")
    public string ScriptPrivateKey = "";               // LabelTextAttribute("DLL私钥")
}

public void OnConfigLoad(ConfigDataBase config)
{
    var con = config as Config;
    SqliteLoder.PasswordFallback = () => con.SqlitePassword;   // 解耦 Config → Sql 直接依赖
    SqliteLoder.Password = con.SqlitePassword;
}
```

**`ScriptPubKey` / `ScriptPrivateKey` are storage-only; nothing in the repository consumes them.**

## Performance monitoring

`SqlitePerformanceMonitor` records query durations and PRAGMA configuration. The benchmark entry point:

```bash
# 菜单：BDFramework/测试/SQLite优化性能基准 ▶
```

By convention the benchmark report is written to `Library/BDFrameCache/SqliteBenchmark/` (never to `persistentDataPath`).

## Common failures

| Symptom | Root cause |
|---------|------------|
| `DB不存在:<path>` | `local.db` is not in `FIRST_LOAD_DIR`; in the Editor you need `LoadLocalDBOnEditor()` |
| `SQLiteException: file is not a database` | Wrong password |
| `GetDB(name)` returns `null` | The connection is already closed |
| Data disappears after `CreateTable` | It calls `DropTable` internally first |
| A query returns nothing while the data exists | The table name (class name) does not match the namespace; or the `Where` clause string is wrong |
| Data mysteriously overwritten by another table | Two classes with the same name in different namespaces → table collision |
| `Sql执行次数过多:...` | The same SQL ran more than 10 times in the Editor (turn off with `EnableEditorSqlLog = false` for benchmarks) |
| `And` produces malformed SQL | `And` / `Or` was placed inside an `if` branch |

## Related pages

- [Table Packing](../pipeline/build-table.md) — the Excel → SQLite flow
- [Asset Load Paths](../guide/asset-load-path.md) — dual addressing and the `local.db` copy
- [Configuration (GameConfig)](game-config.md) — how the encryption config is injected
- [Runtime Module Map](../architecture/runtime-modules.md)
