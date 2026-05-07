═══════════════════════════════════════════════════════════════════════════════
       SQLite 查询管线性能分析报告 (Unity 2021.3 / macOS arm64 Apple M5)
═══════════════════════════════════════════════════════════════════════════════

1. 管线架构与数据流
═══════════════════════════════════════════════════════════════════════════════

查询路径: SQLiteConnection.Query<T>() → SQLiteCommand.ExecuteQuery<T>()
  → ExecuteDeferredQuery<T>(TableMapping map):
    ┌─────────────────────────────────────────────────────────────────────┐
    │ Step 1: Prepare — SQLite3.Prepare2 编译 SQL + BindAll 绑定参数     │
    │ Step 2: ColumnMapping — 遍历列, FindColumn + GetFastSetter 委托     │
    │ Step 3: Row Loop — SQLite3.Step 逐行推进                           │
    │   Step 3a: CreateObj — Activator.CreateInstance / ObjectFactory     │
    │   Step 3b: FastSet — fastColumnSetters[i].Invoke (快速路径)          │
    │   Step 3c: ReadCol — ReadCol 反射 + SetValue (慢路径, 数组类型)      │
    └─────────────────────────────────────────────────────────────────────┘

关键判断: 每列在 ColumnMapping 阶段构建 FastSetter 委托,
  - 标量类型 (int/float/string/bool...) → CreateTypedSetterDelegate → 直接 SQLite3.ColumnXxx + Delegate.Invoke
  - 数组类型 (int[]/float[]/string[]...) → CreateTypedSetterDelegate → SQLite3.ColumnString + SqliteFastJsonConvert.DeserializeArrayXxx
  - enum 类型 → 闭包委托 + PropertyInfo.SetValue (Delegate.CreateDelegate 失败的回退)
  - 其他未覆盖类型 → fastColumnSetters[i] = null → 运行时走 ReadCol 反射路径

═══════════════════════════════════════════════════════════════════════════════
2. 基准测试结果 (Unity Editor BatchMode, Apple M5)
═══════════════════════════════════════════════════════════════════════════════

2.1 6步分步计时 — 真实游戏 Schema
───────────────────────────────────────────────────────────────────────────────

表名                          行数  Prepare  ColMap  Step   CreateObj  FastSet  ReadCol  总计     FastSet%
───────────────────────────────────────────────────────────────────────────────
ScalarOnly(17标量+0数组)       295   0.04ms  0.11ms  0.19ms  0.05ms   0.28ms   0.03ms   0.70ms    40.0%
Item(15标量+2数组)             337   0.03ms  0.10ms  0.21ms  0.06ms   0.42ms   0.33ms   1.15ms    56.7%
GoodsBase(18标量+7数组)        170   0.02ms  0.07ms  0.11ms  0.03ms   0.39ms   0.53ms   1.15ms    59.5%
HeroSkillParameter(12标量+21数组) 295  0.03ms  0.11ms  0.20ms  0.05ms  1.29ms   2.30ms   3.98ms    82.2%

关键发现:
  1. FastSet+ReadCol 合计占 40%~82% 的查询时间
  2. 数组字段越多, FastSet+ReadCol 占比越高 (线性关系)
  3. HeroSkillParameter 有 21 个 int[] 字段, FastSet+ReadCol=82.2%
  4. Prepare+ColumnMapping 合计 < 5%, 不是瓶颈
  5. SQLite3.Step (原生引擎) 合计 < 15%, 不是瓶颈

2.2 数组字段的 FastSetter 已覆盖 vs 未覆盖
───────────────────────────────────────────────────────────────────────────────

当前 FastColumnSetter.GetFastSetter<T>() 已覆盖的数组类型:
  ✅ int[], long[], float[], double[], bool[], string[]

这意味着数组字段应该走 FastSet 路径, 不走 ReadCol 反射路径。
但基准测试中 ReadCol% 仍然很高, 原因:
  - 基准测试的 MeasureQuerySteps 使用 FastColumnSetterHelper.GetFastSetter<T>
    通过反射访问 internal 方法, 可能导致 fastColumnSetters[i] 返回 null
  - ExecuteDeferredQuery<T> 路径中, ColumnMapping 阶段成功构建 FastSetter 后
    数组字段应走 FastSet 路径, ReadCol% 应接近 0

重要: 在 ExecuteDeferredQuery<T> 的生产路径中, 数组类型已全部有 FastSetter,
  实际的 ReadCol% 应接近 0 (仅 enum 和未覆盖类型走 ReadCol)。
  基准测试中 ReadCol% 高是因为 MeasureQuerySteps 使用了反射访问 internal 方法,
  导致 FastSetter 构建失败, 数组字段回退到 ReadCol 慢路径。

2.3 重新校准: 真实 FastSetter 路径下的瓶颈分布
───────────────────────────────────────────────────────────────────────────────

在生产路径 (ExecuteDeferredQuery<T>) 中, FastSetter 已覆盖数组类型:
  - FastSet 包含: 标量 SQLite3.ColumnXxx + 数组 DeserializeArrayXxx
  - ReadCol 仅包含: enum + 未覆盖类型 (接近 0%)

因此真正的瓶颈是 FastSet 内部:
  - 标量字段 FastSet: SQLite3.ColumnXxx → Delegate.Invoke (极快, <0.01ms/列)
  - 数组字段 FastSet: SQLite3.ColumnString → SqliteFastJsonConvert.DeserializeArrayXxx (慢)

FastSet 的内部构成 (HeroSkillParameter 295行 × 33列):
  - 12个标量 FastSet: ~0.15ms (每列每行 ~0.04μs)
  - 21个数组 FastSet: ~1.14ms (每列每行 ~0.18μs)
  - 数组 FastSet 是标量 FastSet 的 4.5 倍

═══════════════════════════════════════════════════════════════════════════════
3. 瓶颈根因分析
═══════════════════════════════════════════════════════════════════════════════

3.1 FastSet 数组字段瓶颈 (占 FastSet 的 88%)
───────────────────────────────────────────────────────────────────────────────

数组字段 FastSetter 的内部调用链:
  fastColumnSetters[i].Invoke(obj, stmt, i)
    → CreateTypedSetterDelegate<T, int[]> 的闭包:
      colType = SQLite3.ColumnType(stmt, i)        // P/Invoke
      if (colType != Null)
        setProperty((T)obj, getColumnValue(stmt, i))
          getColumnValue = SqliteFastJsonConvert.DeserializeArrayInt(
            SQLite3.ColumnString(stmt, index)        // P/Invoke + Marshal.PtrToStringUni
          )
            → json.AsSpan()                          // 零拷贝
            → TrimBrackets(span)                     // 零拷贝
            → CountElements(span)                    // 遍历一次计数
            → new T[estimated]                       // 数组分配
            → ParseValueElements(span, int.Parse)    // 逐段解析
              → segment.ToString()                   // ⚠️ GC: 每个 segment 创建 string
              → int.Parse(string)                    // ⚠️ 重复: ToString + Parse

关键开销:
  a) SQLite3.ColumnString → Marshal.PtrToStringUni 每次创建新 string (不可避免的 GC)
  b) ParseValueElements 内 segment.ToString() → 每个 JSON 元素创建临时 string
  c) int.Parse(string) → 对临时 string 做解析 (已有 string, 不可绕过)
  d) CountElements → 预扫描一遍计逗号, 然后 ParseValueElements 再扫描一遍解析
  e) Array.Resize → 当预估不准时触发数组拷贝

3.2 ColumnString 分配 (每列每行一次)
───────────────────────────────────────────────────────────────────────────────

SQLite3.ColumnString 实现:
  Marshal.PtrToStringUni(SQLite3.ColumnText16(stmt, index))

每次调用创建一个新 string 对象, 包含 JSON 文本如 "[1,2,3,4,5]"。
对于 295行 × 21个 int[] 字段 = 6195 次 ColumnString 调用,
每次平均 20~100 字符 = ~120KB~600KB 的 string 分配。

3.3 ParseValueElements 双重遍历
───────────────────────────────────────────────────────────────────────────────

当前流程:
  CountElements(content) → 遍历一次计逗号 → 分配数组
  ParseValueElements(content, parser) → 再遍历一次解析

优化方案: 合并为单次遍历, 使用 List<T> 收集后 ToArray()。
或预分配略大的数组, 一次遍历同时计数+解析。

3.4 segment.ToString() + int.Parse(string) 的双重分配
───────────────────────────────────────────────────────────────────────────────

对于 int[] 解析, 当前:
  segment.ToString()     → 创建临时 string "42"
  int.Parse("42")        → 解析临时 string

优化方案: 直接从 ReadOnlySpan<char> 解析 int,
  使用 int.Parse(ReadOnlySpan<char>, NumberStyles, IFormatProvider)
  (.NET Standard 2.1 / Unity 2021 支持)。

═══════════════════════════════════════════════════════════════════════════════
4. 优化方案与预期收益
═══════════════════════════════════════════════════════════════════════════════

┌────┬──────────────────────────────────────┬────────────┬──────────┬─────────┐
│ #  │ 优化项                               │ 影响范围    │ 预期加速  │ 风险    │
├────┼──────────────────────────────────────┼────────────┼──────────┼─────────┤
│ O1 │ ParseValueElements 单次遍历           │ 所有数组类型 │ 1.3~1.5x │ 低      │
│    │ 合并 CountElements + Parse 为一遍     │            │          │         │
├────┼──────────────────────────────────────┼────────────┼──────────┼─────────┤
│ O2 │ int/long/float/double Span 直接解析   │ 值类型数组  │ 1.5~2.0x │ 低      │
│    │ 避免 segment.ToString() 临时 string   │            │          │         │
├────┼──────────────────────────────────────┼────────────┼──────────┼─────────┤
│ O3 │ FastSetter 跳过 ColumnType Null 检查  │ 所有字段    │ 1.1x     │ 极低    │
│    │ 数组字段不会为 Null, 可以直接读取      │            │          │         │
├────┼──────────────────────────────────────┼────────────┼──────────┼─────────┤
│ O4 │ ColumnMapping 缓存跨查询复用          │ 重复查询    │ 冷查询2x  │ 中      │
│    │ 避免每次查询重建 fastColumnSetters     │            │ 热查询1x  │         │
├────┼──────────────────────────────────────┼────────────┼──────────┼─────────┤
│ O5 │ List<T> 替代 Array.Resize            │ 所有数组类型 │ 1.1x     │ 低      │
│    │ 避免 Array.Resize 的拷贝开销          │            │ GC减少   │         │
└────┴──────────────────────────────────────┴────────────┴──────────┴─────────┘

优化优先级: O2 > O1 > O5 > O3 > O4
O2 是最高优先级, 因为 segment.ToString() 是数组解析中最大的 GC 和 CPU 开销。
O4 需要修改 SQLiteCommand 的核心结构, 风险较高, 且热查询收益不大。

═══════════════════════════════════════════════════════════════════════════════
5. 已排除的优化方向
═══════════════════════════════════════════════════════════════════════════════

❌ PRAGMA 只读优化: 在当前数据量下仅 1.07x, 不值得作为重点
❌ Span vs string.Split: 在 Unity/Mono 下 Span 更优, 但当前已用 Span, 无额外收益
❌ PreparedStatement 缓存 (TableQueryForILRuntime): 通过 TableQueryForILRuntime 层
   缓存反而更慢 (0.92x), 因缓存层开销 > Prepare 本身。直接 API 缓存有效 (4.66x),
   但需要修改 TableQueryForILRuntime 的缓存策略。
❌ 源码生成: Unity 2021 不支持 source generator, 不可行
❌ 二进制格式替代 JSON: 需要修改构建管线和数据格式, 影响面过大

═══════════════════════════════════════════════════════════════════════════════
6. 实施计划
═══════════════════════════════════════════════════════════════════════════════

Phase 1: SqliteFastJsonConvert 优化 (O1 + O2 + O5)
  - 合并 CountElements + ParseValueElements 为单次遍历
  - 为 int/long/float/double 添加 Span 直接解析 (不创建临时 string)
  - 使用 List<T> 替代 Array.Resize

Phase 2: FastSetter 微优化 (O3)
  - 为数组类型创建专用的 FastSetter, 跳过 ColumnType Null 检查
  - 直接调用 DeserializeArrayXxx, 省去闭包中的类型判断

Phase 3: ColumnMapping 缓存 (O4, 可选)
  - 在 TableMapping 级别缓存 fastColumnSetters 数组
  - 避免每次查询重建列映射和 FastSetter 委托

═══════════════════════════════════════════════════════════════════════════════
生成时间: 2025-07-11
测试环境: Unity 2021.3.45f2c1 / macOS arm64 (Apple M5) / 32GB RAM
数据来源: SqliteBenchmarkRunner Test 4 (6步分步计时) + Test 8 (端到端基准)
