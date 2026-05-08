using SQLite4Unity3d;

namespace BDFramework.Test.SqliteBenchmark
{
    /// <summary>
    /// 基准测试 POCO — 覆盖所有 SqliteFastJsonConvert 支持的数组类型。
    /// Benchmark POCO — covers all array types supported by SqliteFastJsonConvert.
    /// </summary>
    [SQLite4Unity3d.Table("BenchmarkRow")]
    public class BenchmarkRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public int Level { get; set; }
        public float Hp { get; set; }
        public double Attack { get; set; }
        public bool IsActive { get; set; }
        public long Exp { get; set; }

        // 数组字段 — 测试 FastJsonConvert 序列化/反序列化
        // Array fields — test FastJsonConvert serialization/deserialization
        public int[] IntArray { get; set; }
        public float[] FloatArray { get; set; }
        public double[] DoubleArray { get; set; }
        public string[] StringArray { get; set; }
        public bool[] BoolArray { get; set; }
        public long[] LongArray { get; set; }
    }

    /// <summary>
    /// 简化的只含标量字段的表，用于大批量查询测试。
    /// Simplified scalar-only table for large-batch query tests.
    /// </summary>
    [SQLite4Unity3d.Table("SimpleRow")]
    public class SimpleRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int Value1 { get; set; }
        public float Value2 { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// 英雄技能参数表 — 37列(12标量+21数组+1字符串+3空)，295行。
    /// 21个int[]数组字段已走 FastSetter + DeserializeArrayInt 快路径，
    /// 但由于数组字段最密集，仍是 FastSet 热点最集中的测试用例。
    /// Hero skill parameter table — 37 cols (12 scalar + 21 array + 1 string + 3 null), 295 rows.
    /// All 21 int[] array fields now use the FastSetter + DeserializeArrayInt fast path,
    /// but this remains the most FastSet-heavy benchmark because it is the densest array table.
    /// </summary>
    [SQLite4Unity3d.Table("HeroSkillParameter")]
    public class HeroSkillParameterRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // ── 标量字段 (12个) / Scalar fields (12) ──
        public int SkillId { get; set; }
        public int SkillType { get; set; }
        public int OugiType { get; set; }
        public int TargetType { get; set; }
        public int HitType { get; set; }
        public int Element { get; set; }
        public int RangeType { get; set; }
        public int Priority { get; set; }
        public int MaxLevel { get; set; }
        public int CostType { get; set; }
        public int CostValue { get; set; }
        public int Cooldown { get; set; }

        // ── 字符串字段 / String field ──
        public string Formula { get; set; }

        // ── 数组字段 (21个int[]) — 全部走 FastSetter + 类型化 int[] 反序列化 ──
        // Array fields (21 x int[]) — all use FastSetter + typed int[] deserialization ──
        public int[] Coefficient { get; set; }
        public int[] EffectParam1 { get; set; }
        public int[] EffectParam2 { get; set; }
        public int[] EffectParam3 { get; set; }
        public int[] EffectParam4 { get; set; }
        public int[] EffectParam5 { get; set; }
        public int[] EffectParam6 { get; set; }
        public int[] EffectParam7 { get; set; }
        public int[] EffectParam8 { get; set; }
        public int[] EffectParam9 { get; set; }
        public int[] EffectParam10 { get; set; }
        public int[] EffectParam11 { get; set; }
        public int[] EffectParam12 { get; set; }
        public int[] EffectParam13 { get; set; }
        public int[] EffectParam14 { get; set; }
        public int[] EffectParam15 { get; set; }
        public int[] EffectParam16 { get; set; }
        public int[] EffectParam17 { get; set; }
        public int[] EffectParam18 { get; set; }
        public int[] EffectParam19 { get; set; }
        public int[] EffectParam20 { get; set; }
    }

    /// <summary>
    /// 物品基础表 — 20列(15标量+2数组+1字符串+2空)，337行。
    /// 典型的游戏配置表，少量数组字段，大多数字段走 fastSetter 快路径。
    /// Item base table — 20 cols (15 scalar + 2 array + 1 string + 2 null), 337 rows.
    /// Typical game config table with few array fields; most fields use the fastSetter path.
    /// </summary>
    [SQLite4Unity3d.Table("Item")]
    public class ItemRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // ── 标量字段 (15个) / Scalar fields (15) ──
        public int ItemType { get; set; }
        public int ItemId { get; set; }
        public int Quality { get; set; }
        public int Category { get; set; }
        public int SubCategory { get; set; }
        public int MaxStack { get; set; }
        public int SellPrice { get; set; }
        public int BuyPrice { get; set; }
        public int BindType { get; set; }
        public int UseType { get; set; }
        public int ExpireType { get; set; }
        public int ExpireValue { get; set; }
        public int SortOrder { get; set; }
        public int IsHidden { get; set; }
        public int Rarity { get; set; }

        // ── 字符串字段 / String field ──
        public string Name { get; set; }

        // ── 数组字段 (2个int[]) / Array fields (2 x int[]) ──
        public int[] Attributes { get; set; }
        public int[] DropSources { get; set; }
    }

    /// <summary>
    /// 商品基础表 — 29列(18标量+7数组+2字符串+2空)，170行。
    /// 中等复杂度的混合型表，数组字段已走 FastSetter，主要用于量化中密度数组反序列化成本。
    /// Goods base table — 29 cols (18 scalar + 7 array + 2 string + 2 null), 170 rows.
    /// Medium-complexity mixed table whose array fields already use FastSetter; mainly used to measure
    /// the deserialization cost of medium-density array columns.
    /// </summary>
    [SQLite4Unity3d.Table("GoodsBase")]
    public class GoodsBaseRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // ── 标量字段 (18个) / Scalar fields (18) ──
        public int GoodsId { get; set; }
        public int GoodsType { get; set; }
        public int Category { get; set; }
        public int SubCategory { get; set; }
        public int Quality { get; set; }
        public int MaxStack { get; set; }
        public int SellPrice { get; set; }
        public int BuyPrice { get; set; }
        public int BindType { get; set; }
        public int LimitType { get; set; }
        public int LimitValue { get; set; }
        public int DiscountType { get; set; }
        public int DiscountValue { get; set; }
        public int SortOrder { get; set; }
        public int IsHidden { get; set; }
        public int ShowLevel { get; set; }
        public int CurrencyType { get; set; }
        public int CurrencyValue { get; set; }

        // ── 字符串字段 / String fields ──
        public string Name { get; set; }
        public string Description { get; set; }

        // ── 数组字段 (7个int[]) / Array fields (7 x int[]) ──
        public int[] ConditionIds { get; set; }
        public int[] RewardIds { get; set; }
        public int[] TagIds { get; set; }
        public int[] RelatedGoods { get; set; }
        public int[] PreviewIds { get; set; }
        public int[] ScheduleIds { get; set; }
        public int[] ExtraParams { get; set; }
    }

    /// <summary>
    /// 纯标量对照表 — 仅含标量字段，所有字段走 fastSetter 快路径。
    /// 用于与数组密集型表对比，量化数组反序列化带来的额外 FastSet 开销。
    /// Scalar-only control table — only scalar fields, all using fastSetter fast path.
    /// Used as baseline to contrast with array-heavy tables and quantify the extra FastSet cost
    /// introduced by array deserialization.
    /// </summary>
    [SQLite4Unity3d.Table("ScalarOnlyRow")]
    public class ScalarOnlyRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int F1 { get; set; }
        public int F2 { get; set; }
        public int F3 { get; set; }
        public int F4 { get; set; }
        public int F5 { get; set; }
        public int F6 { get; set; }
        public int F7 { get; set; }
        public int F8 { get; set; }
        public int F9 { get; set; }
        public int F10 { get; set; }
        public int F11 { get; set; }
        public int F12 { get; set; }
        public string F13 { get; set; }
        public float F14 { get; set; }
        public double F15 { get; set; }
        public bool F16 { get; set; }
        public long F17 { get; set; }
    }
}
