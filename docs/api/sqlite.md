# 表格 SQLite

数据层基于 **sqlite-net + SQLCipher**，采用双库模型：`local.db`（只读、加密）+ `server.db`（读写、不加密）。

## 双库模型

| 库 | 路径 | 打开模式 | 加密 | 用途 |
|----|------|---------|------|------|
| `local.db` | `<FIRST_LOAD_DIR>/local.db` | `ReadOnly` | SqlCipher | 客户端静态配置表 |
| `server.db` | `<root>/server_data/server.db` | Editor 下 `ReadWrite \| Create` | **不加密** | 服务器侧数据 |

!!! note "`local.db` 需要先复制到可写目录"
    `ClientAssetsUtils.PersistentOnlyFiles` 只包含一项：`SqliteLoder.LOCAL_DB_PATH = "local.db"`。母包中的 `local.db` 会被复制到 `FIRST_LOAD_DIR` 后才被打开。

    Editor 下没有这套复制流程，所以需要手动 `SqliteLoder.LoadLocalDBOnEditor()`。

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

!!! warning "`Init` 的 `assetLoadPathType` 与 `secondDir` 是死参数"
    ```csharp
    static public void Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir)
    {
        Connection?.Dispose();
        var db_path = IPath.Combine(firstDir, LOCAL_DB_PATH);   // 恒定取 firstDir
        Connection = LoadDBReadOnly(db_path);
    }
    ```
    `assetLoadPathType` 与 `secondDir` **未参与任何判断**。签名与 `BResources.Init` 对齐是刻意的，但容易误解。

`LoadDBReadOnly` 在文件不存在时 `Debug.LogError("DB不存在:" + path)` 并返回 `null`（不抛异常）。

`LoadDBReadWriteCreate` 在 Editor 下会先 `EnsureEditorSqlCipherReady()`：试调 `SQLite3.LibVersionNumber()`，失败时打印含 plugin 路径/OS/CPU/Unity 版本的诊断信息并**抛异常**。

### 只读 PRAGMA 优化

`ApplyReadOnlyPragmas` 在 `LoadDBReadOnly` 路径生效：

| PRAGMA | 值 |
|--------|-----|
| `cache_size` | `-N` KB，`N = fileSizeKB × 2.0`，clamp 到 `[2000, 20000]` |
| `mmap_size` | `268435456`（256 MB） |
| `journal_mode` | `OFF` |
| `synchronous` | `OFF` |
| `temp_store` | `MEMORY` |
| `locking_mode` | `NORMAL` |

整段 `try/catch`，失败仅 `Debug.LogWarning`，**不阻断加载**。读回的 `page_size` / `cache_size` / `mmap_size` 交给 `SqlitePerformanceMonitor.RecordPragmaConfig`。

!!! note "写入路径不应用这些 PRAGMA"
    `LoadDBReadWriteCreate`（`server.db`、导表写入）走另一条路径，不做这些优化。

## `SqliteHelper` 与 `SQLiteService`

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

!!! danger "`CreateTable` 会先 DropTable"
    ```csharp
    public void CreateTable(Type t)
    {
        Connection.DropTable(t);      // ← 清空数据
        Connection.CreateTable(t);
    }
    ```
    **不要在生产逻辑里拿它"确保表存在"**。

!!! warning "`SQLiteService` 没有公开的 Update / Delete / Execute"
    写库只能通过 `.Connection` 拿底层 `SQLiteConnection`（Editor 构建管线正是这样用：`SqliteHelper.DB.Connection.CreateTable<TableLog>()`），或走 `GetTable<T>()` 取 sqlite-net 原生 `TableQuery<T>`。

`SqliteHelper.DB` 在 `SqliteLoder.Connection` 为 `null` 或已关闭时会返回 `null`；`GetDB(name)` 在连接不可用时**移除缓存并返回 `null`**（不抛异常）。

## `TableQueryForILRuntime` —— 查询构建器

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

生成的 SQL 形态：`select {selection} from {type.Name} where {where} Limit {limit}`。

!!! danger "表名 = `type.Name`（不含命名空间）"
    不同命名空间下的**同名类会撞表**。表类命名必须全局唯一。

### 典型用法

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

### 三个必须知道的陷阱

!!! danger "1. `And` / `Or` 属性是「读取即修改」"
    ```csharp
    public TableQueryForILRuntime And { get { @where += " and"; return this; } }
    ```
    `get` 里就拼 SQL。**不能写分支代码**：

    ```csharp
    // ✗ 错误：无论条件真假都追加了 " and"
    q.Where("id > 0");
    if (needFilter) q.And;
    q.Where("level > 5");
    ```

!!! danger "2. `WhereOr` / `WhereAnd` 是覆盖而非追加"
    它们会**整体替换 `@where`**（不是追加）。要"同字段多条件"必须用 `Where(...).And.Where(...)` 形式。

!!! danger "3. Builder 状态在每次查询后重置"
    `GenerateCommand` 后 `@sql` / `@limit` / `@where` 都会被清空。因此 `Exec(...)` 只影响**下一次**查询。

### Prepared Statement 缓存

只有调用过 `EnableSqlCahce()` 才生效。同一 SQL 执行次数 ≥ `triggerCacheNum` 时走 `Connection.GetPreparedStatement` / `SetPreparedStatement`。

Editor 下同一 SQL 执行 **> 10 次**会 `Debug.LogError("Sql执行次数过多:...")`。做基准测试时：

```csharp
TableQueryForILRuntime.EnableEditorSqlLog = false;
```

## 业务表格声明契约

!!! note "没有 `[Table]` 属性"
    表类是**代码生成的普通 POCO + sqlite-net 属性**，不需要标记 `[Table]`。

| 项 | 约定 |
|----|------|
| 生成位置 | `Assets/Code/Game/Table/Local/<Name>.xlsx.cs`（本地表）<br/>`Assets/Code/Game/Table/Server/<Name>.xlsx.cs`（服务器表） |
| 命名空间 | `Game.Data.Local` / `Game.Data.Server` —— **必须是 `Game.Data.*` 前缀** |
| 生成头 | `// <auto-generated> // Genera by BDFramework` |
| 主键 | `[PrimaryKey]`（如 `Hero.Id`）；自增用 `[AutoIncrement]` |
| 数组字段 | 直接声明（`string[] AttributeName`、`int[] Skills`） |
| 表名 | **类名**（不含命名空间） |

真实样例（`Assets/Code/Game/Table/Local/Hero.xlsx.cs`）：

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

!!! danger "改命名空间会导致导表失败"
    `BuildTools_Excel2SQLite.CollectTableTypes()` 靠 `Namespace.StartsWith("Game.Data.")` 收集类型。

## 加密配置

`GameCipherConfigProcessor`（`[GameConfig(2, "加密")]`）在 `OnConfigLoad` 中注入：

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

**`ScriptPubKey` / `ScriptPrivateKey` 只做配置存储，仓库内没有任何消费逻辑。**

## 性能监控

`SqlitePerformanceMonitor` 记录查询耗时与 PRAGMA 配置。基准测试入口：

```bash
# 菜单：BDFramework/测试/SQLite优化性能基准 ▶
```

基准报告输出路径约定为 `Library/BDFrameCache/SqliteBenchmark/`（不写 `persistentDataPath`）。

## 常见故障

| 现象 | 根因 |
|------|------|
| `DB不存在:<path>` | `local.db` 不在 `FIRST_LOAD_DIR`；Editor 下需 `LoadLocalDBOnEditor()` |
| `SQLiteException: file is not a database` | 密码不对 |
| `GetDB(name)` 返回 `null` | 连接已关闭 |
| `CreateTable` 后数据没了 | 它内部先 `DropTable` |
| 查询返回空但数据存在 | 表名（类名）与命名空间不匹配；或 `Where` 条件字符串写错 |
| 数据莫名被其他表覆盖 | 两个命名空间下有同名类 → 撞表 |
| `Sql执行次数过多:...` | Editor 下同一 SQL 超过 10 次（基准测试需 `EnableEditorSqlLog = false`） |
| `And` 拼接出错误 SQL | 把 `And` / `Or` 写进了 `if` 分支 |

## 相关页面

- [表格打包](../pipeline/build-table.md) —— Excel → SQLite 流程
- [资源加载寻址](../guide/asset-load-path.md) —— 双寻址与 `local.db` 复制
- [配置中心 GameConfig](game-config.md) —— 加密配置注入
- [Runtime 模块地图](../architecture/runtime-modules.md)
