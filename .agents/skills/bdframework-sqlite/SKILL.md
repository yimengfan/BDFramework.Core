---
name: bdframework-sqlite
description: 'BDFramework 表格与 SQLite 数据层技能。使用场景：用 SqliteHelper/SqliteLoder 读表、写 TableQueryForILRuntime 链式查询（Where/And/Or/WhereIn/WhereAnd/WhereOr/OrderBy/Limit/From/FromAll）、排查"DB不存在"/"file is not a database"/查询返回空、理解 local.db 与 server.db 双库模型与 SqlCipher 加密、定义业务表类（Game.Data.* 命名空间、[PrimaryKey]、数组字段）、Excel 导表与表类生成、只读 PRAGMA 性能优化、SQL 执行次数过多告警。关键字：SqliteLoder、SqliteHelper、SQLiteService、TableQueryForILRuntime、local.db、server.db、GetTableRuntime、FromAll、WhereAnd、WhereOr、SqlCipher、SqlitePassword、Game.Data.Local、PrimaryKey、导表、Excel、TableLog、EnableSqlCahce、PRAGMA。'
---

# BDFramework 表格 / SQLite 技能

## 1. 何时使用

命中以下任一情况时加载本技能：

- 写/改表查询代码
- 排查 `DB不存在`、`file is not a database`、查询返回空
- 新增业务表（定义表类 + Excel 源）
- 需要理解双库模型与加密
- 导表性能与查询性能调优

不适用：**打包管线**（用 `bdframework-build-pipeline`）。

## 2. 铁律（先读这 7 条）

| # | 铁律 | 违反后果 |
|---|------|---------|
| 1 | **表名 = 类名（不含命名空间）** | 不同命名空间下的同名类**会撞表** |
| 2 | **表类命名空间必须以 `Game.Data.` 开头** | `CollectTableTypes()` 收不到，导表失败 |
| 3 | **`And` / `Or` 属性是"读取即修改"** | 不能写 `if (cond) q.And;`，会拼出错误 SQL |
| 4 | **`WhereOr` / `WhereAnd` 是覆盖而非追加** | 会整体替换 `@where` |
| 5 | **Builder 状态在每次查询后重置** | `Exec(...)` 只影响下一次查询 |
| 6 | **`CreateTable` 会先 `DropTable`** | 生产逻辑里用它 = 清空数据 |
| 7 | **`SqliteLoder.Init` 的 `assetLoadPathType` / `secondDir` 是死参数** | db 恒定取 `firstDir/local.db` |

## 3. 双库模型

| 库 | 路径 | 打开模式 | 加密 | 用途 |
|----|------|---------|------|------|
| `local.db` | `<FIRST_LOAD_DIR>/local.db` | `ReadOnly` | **SqlCipher** | 客户端静态配置表 |
| `server.db` | `<root>/server_data/server.db` | Editor 下 `ReadWrite \| Create` | **不加密** | 服务器侧数据 |

!!! note "`local.db` 需要先复制到可写目录"
    `ClientAssetsUtils.PersistentOnlyFiles` 只有一项：`SqliteLoder.LOCAL_DB_PATH = "local.db"`。母包中的 `local.db` 会被复制到 `FIRST_LOAD_DIR` 后才被打开。

    Editor 下没有这套流程，需手动 `SqliteLoder.LoadLocalDBOnEditor()`。

## 4. `SqliteLoder`

```csharp
namespace BDFramework.Sql

static public partial class SqliteLoder
{
    public readonly static string LOCAL_DB_PATH  = "local.db";
    public readonly static string SERVER_DB_PATH = "server.db";

    static public SQLiteConnection Connection { get; set; }

    // 加密
    public static Func<string> PasswordFallback { get; set; }
    public static string password;
    public static string Password { get; set; }        // password 非空则用它，否则 PasswordFallback?.Invoke() ?? ""

    // 初始化
    static public void Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir);
    static public SQLiteConnection LoadDBReadOnly(string path);
    static public SQLiteConnection LoadDBReadWriteCreate(string path, bool isUsePsw = true);
    static public SQLiteConnection GetSqliteConnect(string dbname);
    static public void Close(string dbName = "");

    // 路径
    static public string GetLocalDBPath(string root, RuntimePlatform platform);
    static public string GetServerDBPath(string root);          // root/server_data/server.db

    // Editor
    static public string LoadLocalDBOnEditor(string root, RuntimePlatform platform);
    static public string LoadLocalDBOnEditor();
    static public void   LoadServerDBOnEditor(string root);
    static public void   LoadSQLOnEditor(string sqlPath, bool isUsePsw = true);
    static public string DeleteLocalDBFile(string root, RuntimePlatform platform);
    static public string DeleteServerDBFile(string root);

    // 性能
    static public void ApplyReadOnlyPragmas(SQLiteConnection con);
}
```

!!! warning "`Init` 的两个死参数"
    ```csharp
    static public void Init(AssetLoadPathType assetLoadPathType, string firstDir, string secondDir)
    {
        Connection?.Dispose();
        var db_path = IPath.Combine(firstDir, LOCAL_DB_PATH);   // 恒定取 firstDir
        Connection = LoadDBReadOnly(db_path);
    }
    ```
    签名与 `BResources.Init` 对齐是刻意的，但容易误解。

`LoadDBReadOnly` 文件不存在时 `Debug.LogError("DB不存在:" + path)` 并返回 `null`（不抛异常）。

`LoadDBReadWriteCreate` 在 Editor 下先 `EnsureEditorSqlCipherReady()`：试调 `SQLite3.LibVersionNumber()`，失败时打印含 plugin 路径/OS/CPU/Unity 版本的诊断并**抛异常**。

### 只读 PRAGMA 优化

`ApplyReadOnlyPragmas` 只在 `LoadDBReadOnly` 路径生效：

| PRAGMA | 值 |
|--------|-----|
| `cache_size` | `-N` KB，`N = fileSizeKB × 2.0`，clamp 到 `[2000, 20000]` |
| `mmap_size` | `268435456`（256 MB） |
| `journal_mode` | `OFF` |
| `synchronous` | `OFF` |
| `temp_store` | `MEMORY` |
| `locking_mode` | `NORMAL` |

整段 `try/catch`，失败仅 `Debug.LogWarning`，**不阻断加载**。读回值交给 `SqlitePerformanceMonitor.RecordPragmaConfig`。

!!! note "写入路径不应用这些 PRAGMA"
    `LoadDBReadWriteCreate`（`server.db`、导表写入）走另一条路径。

## 5. `SqliteHelper` / `SQLiteService`

```csharp
namespace BDFramework.Sql

static public class SqliteHelper
{
    static public SQLiteService DB { get; }                 // 主库
    static public SQLiteService GetDB(string dbName);        // 具名库缓存
    static public void RemoveDBService(string dbName);

    public class SQLiteService
    {
        public SQLiteConnection Connection { get; private set; }
        public bool   IsClose { get; }                       // Connection == null || !IsOpen
        public string DBPath  { get; }
        public TableQueryForILRuntime ILRuntimeTable { get; }
        public TableQueryForILRuntime GetTableRuntime();      // ★ 与 ILRuntimeTable 是同一实例

        public SQLiteService(SQLiteConnection con);

        public void CreateTable<T>();
        public void CreateTable(Type t);                      // ★ 先 DropTable 再 CreateTable
        public void InsertTable(System.Collections.IEnumerable objects);
        public void Insert(object @object);
        public void InsertAll<T>(List<T> obj);                // 实为 Connection.Insert(@obj, typeof(T))
        public TableQuery<T> GetTable<T>() where T : new();
    }
}
```

!!! danger "`CreateTable` 会清空数据"
    ```csharp
    public void CreateTable(Type t)
    {
        Connection.DropTable(t);
        Connection.CreateTable(t);
    }
    ```
    不要在生产逻辑里用它"确保表存在"。

!!! warning "`SQLiteService` 没有公开的 Update / Delete / Execute"
    写库只能通过 `.Connection` 拿底层 `SQLiteConnection`（Editor 构建管线正是这样用），或走 `GetTable<T>()` 取 sqlite-net 原生 `TableQuery<T>`。

`SqliteHelper.DB` 在 `SqliteLoder.Connection` 为 `null` 或已关闭时返回 `null`；`GetDB(name)` 在连接不可用时**移除缓存并返回 `null`**。

## 6. 查询构建器

```csharp
namespace SQLite4Unity3d            // ★ 名字是历史遗留

public class TableQueryForILRuntime : BaseTableQuery
{
    public SQLiteConnection Connection { get; private set; }
    public static bool EnableEditorSqlLog = true;      // 基准测试期可关

    public void EnableSqlCahce(int triggerCacheNum = 5, float triggerChacheTimer = 0.05f);

    public TableQueryForILRuntime Exec(string sql);
    public TableQueryForILRuntime Where(string where, object value);   // string value 自动加单引号
    public TableQueryForILRuntime Where(string where);                 // 原样拼接
    public TableQueryForILRuntime And { get; }                         // ★ 读取即修改
    public TableQueryForILRuntime Or  { get; }                         // ★ 读取即修改
    public TableQueryForILRuntime WhereIn<T>(string field, IEnumerable<T> values);
    public TableQueryForILRuntime WhereIn(string field, params object[] values);
    public TableQueryForILRuntime WhereEqual(string where, object value);
    public TableQueryForILRuntime WhereOr(string field, string operation = "", params object[] objs);
    public TableQueryForILRuntime WhereAnd(string field, string operation = "", params object[] objs);
    public TableQueryForILRuntime Limit(int limitValue);
    public TableQueryForILRuntime OrderByDesc(string field);
    public TableQueryForILRuntime OrderBy(string field);

    public T From<T>(string selection = "*");
    public object From(Type type, string selection = "*");             // 内部自动 Limit(1)
    public List<T> FromAll<T>(string selection = "*");
    public List<object> FromAll(Type type, string selection = "*");
}
```

生成的 SQL：`select {selection} from {type.Name} where {where} Limit {limit}`。

!!! danger "表名 = `type.Name`（不含命名空间）"
    不同命名空间下的同名类**会撞表**。

### 标准用法

```csharp
// ① 单条件
var hero = SqliteHelper.DB.GetTableRuntime().Where("id = {0}", 1).FromAll<Hero>();

// ② And 追加
var ds = SqliteHelper.DB.GetTableRuntime().Where("id > 1").And.Where("id < 3").FromAll<Hero>();

// ③ Or 追加
ds = SqliteHelper.DB.GetTableRuntime().Where("id = 1").Or.Where("id = 3").FromAll<Hero>();

// ④ 同字段多值
ds = SqliteHelper.DB.GetTableRuntime().WhereAnd("id", "=", 1, 2).FromAll<Hero>();
ds = SqliteHelper.DB.GetTableRuntime().WhereOr("id", "=", 2, 3).FromAll<Hero>();

// ⑤ 取单条
var one = SqliteHelper.DB.GetTableRuntime().Where("id = {0}", 1).From<Hero>();

// ⑥ 排序 + 限制
var top = SqliteHelper.DB.GetTableRuntime()
    .Where("level > {0}", 10)
    .OrderByDesc("level")
    .Limit(10)
    .FromAll<Hero>();

// ⑦ 原生 SQL
var list = SqliteHelper.DB.GetTableRuntime()
    .Exec("select * from Hero where id > 5")
    .FromAll<Hero>();
```

### 三个必须知道的陷阱

!!! danger "1. `And` / `Or` 是读取即修改"
    ```csharp
    public TableQueryForILRuntime And { get { @where += " and"; return this; } }
    ```
    `get` 里就拼 SQL，**不能写分支代码**：
    ```csharp
    // ✗ 无论条件真假都追加了 " and"
    q.Where("id > 0");
    if (needFilter) q.And;
    q.Where("level > 5");
    ```

!!! danger "2. `WhereOr` / `WhereAnd` 是覆盖而非追加"
    它们**整体替换 `@where`**。

!!! danger "3. Builder 状态在每次查询后重置"
    `GenerateCommand` 后 `@sql` / `@limit` / `@where` 都被清空。`Exec(...)` 只影响**下一次**查询。

### Prepared Statement 缓存

只有调用过 `EnableSqlCahce()` 才生效（★ 方法名拼写是 `Cahce`）。同一 SQL 执行次数 ≥ `triggerCacheNum` 时走 `Connection.GetPreparedStatement` / `SetPreparedStatement`。

Editor 下同一 SQL 执行 **> 10 次**会 `Debug.LogError("Sql执行次数过多:...")`。

```csharp
// 基准测试时关掉
TableQueryForILRuntime.EnableEditorSqlLog = false;
```

## 7. 业务表定义契约

!!! note "没有 `[Table]` 属性"
    表类是**代码生成的普通 POCO + sqlite-net 属性**。

| 项 | 约定 |
|----|------|
| 生成位置 | `Assets/Code/Game/Table/Local/<Name>.xlsx.cs`（本地表）<br/>`Assets/Code/Game/Table/Server/<Name>.xlsx.cs`（服务器表） |
| 命名空间 | `Game.Data.Local` / `Game.Data.Server` —— **必须 `Game.Data.*` 前缀** |
| 生成头 | `// <auto-generated> // Genera by BDFramework` |
| 主键 | `[PrimaryKey]`；自增用 `[AutoIncrement]` |
| 数组字段 | 直接声明（`string[]`、`int[]`） |
| 表名 | **类名** |

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

## 8. 加密

`GameCipherConfigProcessor`（`[GameConfig(2, "加密")]`）在 `OnConfigLoad` 中注入：

```csharp
public class Config : ConfigDataBase
{
    public string SqlitePassword   = "password123!!!";
    public string ScriptPubKey     = "";
    public string ScriptPrivateKey = "";
}

public void OnConfigLoad(ConfigDataBase config)
{
    var con = config as Config;
    SqliteLoder.PasswordFallback = () => con.SqlitePassword;   // 解耦 Config → Sql 依赖
    SqliteLoder.Password = con.SqlitePassword;
}
```

!!! note "`ScriptPubKey` / `ScriptPrivateKey` 未被消费"
    这两个字段只做配置存储，仓库内没有任何读取逻辑。

!!! warning "`server.db` 不加密是刻意的"
    `LoadServerDBOnEditor` 的原话：`"Server.db 不使用加密,否则服务器不好处理!!!"`

## 9. 故障对照

| 现象 | 根因 | 处理 |
|------|------|------|
| `DB不存在:<path>` | `local.db` 不在 `FIRST_LOAD_DIR` | Editor 用 `SqliteLoder.LoadLocalDBOnEditor()`；真机检查母包复制 |
| `SQLiteException: file is not a database` | 密码不对 | 检查 `GameCipherConfigProcessor.Config.SqlitePassword` |
| `GetDB(name)` 返回 `null` | 连接已关闭 | 重新 `Init` |
| `CreateTable` 后数据没了 | 它内部先 `DropTable` | 不要在生产逻辑用 |
| 查询返回空但数据存在 | 表名（类名）与命名空间不匹配；或 `Where` 条件字符串写错 | 核对类名与 SQL |
| 数据莫名被其他表覆盖 | 两个命名空间下有同名类 → 撞表 | 保证类名全局唯一 |
| `Sql执行次数过多:...` | Editor 下同一 SQL 超 10 次 | 基准测试设 `EnableEditorSqlLog = false` |
| `And` 拼接出错误 SQL | 把 `And` / `Or` 写进了 `if` 分支 | 改为方法调用或调整结构 |
| 导表后查不到 | 表类命名空间不是 `Game.Data.*` | 修正命名空间 |
| 数组字段为空 | Excel 格式不是 `[a,b]`；或引号数量为奇数 | 修正 Excel |

## 10. 详细参考

| 文件 | 内容 |
|------|------|
| [references/table-pipeline.md](./references/table-pipeline.md) | Excel 格式约定、导表流程、表类生成、增量 hash |

在线文档：

- [表格 SQLite](https://yimengfan.github.io/BDFramework.Core/api/sqlite.md)
- [表格打包](https://yimengfan.github.io/BDFramework.Core/pipeline/build-table.md)
- [配置中心 GameConfig](https://yimengfan.github.io/BDFramework.Core/api/game-config.md)

## 11. 改动前检查清单

- [ ] 表类命名空间以 `Game.Data.` 开头
- [ ] 表类名全局唯一（表名 = 类名）
- [ ] `And` / `Or` 未写进 `if` 分支
- [ ] 未把 `WhereOr` / `WhereAnd` 当作追加使用
- [ ] 未在生产逻辑里调 `CreateTable`
- [ ] 批量查询循环外没有重复构建 query 对象
- [ ] 基准测试已设 `TableQueryForILRuntime.EnableEditorSqlLog = false`
- [ ] 写库操作走 `.Connection` 而非期待 `SQLiteService` 提供 Update/Delete
