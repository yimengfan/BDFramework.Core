using System;
using System.Collections.Generic;
using SQLite4Unity3d;

namespace BDFramework.Test.SqliteBenchmark
{
    /// <summary>
    /// 基准测试数据生成器 — 生成各 POCO 类型的随机测试数据。
    /// Benchmark data generator — generates random test data for each POCO type.
    /// </summary>
    static public class SqliteBenchmarkDataGenerator
    {
        /// <summary>
        /// 生成随机 int[] 数组，长度在 [minLen, maxLen] 之间，值在 [minVal, maxVal) 范围内。
        /// Generate a random int[] with length in [minLen, maxLen] and values in [minVal, maxVal).
        /// </summary>
        static public int[] GenerateRandomIntArray(Random rng, int minLen, int maxLen, int minVal, int maxVal)
        {
            var len = rng.Next(minLen, maxLen + 1);
            var arr = new int[len];
            for (int i = 0; i < len; i++)
            {
                arr[i] = rng.Next(minVal, maxVal);
            }
            return arr;
        }

        /// <summary>
        /// 返回 [0, max) 范围内的随机 long，避免极端值。
        /// Returns a random long in [0, max), avoiding extreme values.
        /// </summary>
        static public long NextLong(Random rng, long max = 100000000L)
        {
            var high = (long)rng.Next();
            var low = (long)rng.Next();
            return Math.Abs((high << 32 | low) % max);
        }

        static public List<BenchmarkRow> GenerateBenchmarkRows(int count)
        {
            var rng = new Random(42);
            var rows = new List<BenchmarkRow>(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new BenchmarkRow
                {
                    Name = $"Row_{i}",
                    Level = rng.Next(1, 100),
                    Hp = rng.Next(100, 10000),
                    Attack = rng.NextDouble() * 1000,
                    IsActive = rng.Next(2) == 0,
                    Exp = NextLong(rng),
                    IntArray = new int[] {rng.Next(0, 100), rng.Next(0, 100), rng.Next(0, 100)},
                    FloatArray = new float[] {(float)rng.NextDouble() * 100f, (float)rng.NextDouble() * 100f},
                    DoubleArray = new double[] {rng.NextDouble(), rng.NextDouble()},
                    StringArray = new string[] {$"str_{i}_a", $"str_{i}_b"},
                    BoolArray = new bool[] {true, false, true},
                    LongArray = new long[] {NextLong(rng), NextLong(rng)},
                });
            }
            return rows;
        }

        static public List<SimpleRow> GenerateSimpleRows(int count)
        {
            var rng = new Random(42);
            var rows = new List<SimpleRow>(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new SimpleRow
                {
                    Value1 = rng.Next(0, 100000),
                    Value2 = (float)rng.NextDouble() * 1000f,
                    Text = $"Text_{i}_{rng.Next(10000)}",
                });
            }
            return rows;
        }

        /// <summary>
        /// 生成 HeroSkillParameter 测试数据 — 295行，12标量+21数组字段。
        /// Generate HeroSkillParameter test data — 295 rows, 12 scalar + 21 array fields.
        /// </summary>
        static public List<HeroSkillParameterRow> GenerateHeroSkillParameterRows(int count)
        {
            var rng = new Random(42);
            var rows = new List<HeroSkillParameterRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new HeroSkillParameterRow
                {
                    SkillId = 100001 + i,
                    SkillType = rng.Next(1, 10),
                    OugiType = rng.Next(0, 5),
                    TargetType = rng.Next(1, 8),
                    HitType = rng.Next(1, 4),
                    Element = rng.Next(0, 7),
                    RangeType = rng.Next(1, 5),
                    Priority = rng.Next(1, 100),
                    MaxLevel = rng.Next(5, 20),
                    CostType = rng.Next(1, 4),
                    CostValue = rng.Next(10, 200),
                    Cooldown = rng.Next(0, 30),
                    Formula = $"f_{i}_{rng.Next(1000, 9999)}",
                    Coefficient = GenerateRandomIntArray(rng, 1, 5, 0, 10000),
                    EffectParam1 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam2 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam3 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam4 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam5 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam6 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam7 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam8 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam9 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam10 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam11 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam12 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam13 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam14 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam15 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam16 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam17 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam18 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam19 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                    EffectParam20 = GenerateRandomIntArray(rng, 1, 5, 0, 1000),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成 Item 测试数据 — 337行，15标量+2数组字段。
        /// Generate Item test data — 337 rows, 15 scalar + 2 array fields.
        /// </summary>
        static public List<ItemRow> GenerateItemRows(int count)
        {
            var rng = new Random(42);
            var rows = new List<ItemRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new ItemRow
                {
                    ItemType = rng.Next(1, 10),
                    ItemId = 200001 + i,
                    Quality = rng.Next(1, 6),
                    Category = rng.Next(1, 20),
                    SubCategory = rng.Next(1, 50),
                    MaxStack = rng.Next(1, 999),
                    SellPrice = rng.Next(0, 10000),
                    BuyPrice = rng.Next(0, 50000),
                    BindType = rng.Next(0, 3),
                    UseType = rng.Next(0, 5),
                    ExpireType = rng.Next(0, 3),
                    ExpireValue = rng.Next(0, 86400),
                    SortOrder = rng.Next(0, 1000),
                    IsHidden = rng.Next(2),
                    Rarity = rng.Next(1, 6),
                    Name = $"Item_{i}",
                    Attributes = GenerateRandomIntArray(rng, 1, 4, 100, 200),
                    DropSources = GenerateRandomIntArray(rng, 1, 3, 300, 400),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成 GoodsBase 测试数据 — 170行，18标量+7数组字段。
        /// Generate GoodsBase test data — 170 rows, 18 scalar + 7 array fields.
        /// </summary>
        static public List<GoodsBaseRow> GenerateGoodsBaseRows(int count)
        {
            var rng = new Random(42);
            var rows = new List<GoodsBaseRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new GoodsBaseRow
                {
                    GoodsId = 300001 + i,
                    GoodsType = rng.Next(1, 8),
                    Category = rng.Next(1, 15),
                    SubCategory = rng.Next(1, 30),
                    Quality = rng.Next(1, 6),
                    MaxStack = rng.Next(1, 999),
                    SellPrice = rng.Next(0, 50000),
                    BuyPrice = rng.Next(0, 100000),
                    BindType = rng.Next(0, 3),
                    LimitType = rng.Next(0, 4),
                    LimitValue = rng.Next(0, 100),
                    DiscountType = rng.Next(0, 3),
                    DiscountValue = rng.Next(0, 100),
                    SortOrder = rng.Next(0, 500),
                    IsHidden = rng.Next(2),
                    ShowLevel = rng.Next(1, 100),
                    CurrencyType = rng.Next(1, 5),
                    CurrencyValue = rng.Next(10, 9999),
                    Name = $"Goods_{i}",
                    Description = $"Desc for goods {i}",
                    ConditionIds = GenerateRandomIntArray(rng, 1, 3, 500, 600),
                    RewardIds = GenerateRandomIntArray(rng, 1, 5, 600, 700),
                    TagIds = GenerateRandomIntArray(rng, 1, 4, 700, 800),
                    RelatedGoods = GenerateRandomIntArray(rng, 1, 3, 800, 900),
                    PreviewIds = GenerateRandomIntArray(rng, 1, 2, 900, 1000),
                    ScheduleIds = GenerateRandomIntArray(rng, 1, 2, 1000, 1100),
                    ExtraParams = GenerateRandomIntArray(rng, 1, 3, 1100, 1200),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 生成 ScalarOnly 对照组数据 — 295行，17标量字段，无数组。
        /// Generate ScalarOnly control group data — 295 rows, 17 scalar fields, no arrays.
        /// </summary>
        static public List<ScalarOnlyRow> GenerateScalarOnlyRows(int count)
        {
            var rng = new Random(42);
            var rows = new List<ScalarOnlyRow>(count);
            for (int i = 0; i < count; i++)
            {
                var row = new ScalarOnlyRow
                {
                    F1 = rng.Next(0, 10000),
                    F2 = rng.Next(0, 10000),
                    F3 = rng.Next(0, 10000),
                    F4 = rng.Next(0, 10000),
                    F5 = rng.Next(0, 10000),
                    F6 = rng.Next(0, 10000),
                    F7 = rng.Next(0, 10000),
                    F8 = rng.Next(0, 10000),
                    F9 = rng.Next(0, 10000),
                    F10 = rng.Next(0, 10000),
                    F11 = rng.Next(0, 10000),
                    F12 = rng.Next(0, 10000),
                    F13 = $"Str_{i}_{rng.Next(1000)}",
                    F14 = (float)rng.NextDouble() * 1000f,
                    F15 = rng.NextDouble() * 1000.0,
                    F16 = rng.Next(2) == 0,
                    F17 = NextLong(rng),
                };
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>
        /// 比较两个数组是否相等。
        /// Compare two arrays for equality.
        /// </summary>
        static public bool ArraysEqual<T>(T[] a, T[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(a[i], b[i]))
                    return false;
            }
            return true;
        }
    }
}
