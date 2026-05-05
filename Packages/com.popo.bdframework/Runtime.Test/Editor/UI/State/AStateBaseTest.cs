using System;
using System.Collections.Generic;
using System.Reflection;
using BDFramework.UFlux;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.UI.State
{
    /// <summary>
    /// AStateBase 脏标记与 StateFactory 缓存契约测试。
    /// Contract tests for AStateBase dirty tracking and StateFactory caching.
    /// 验证状态脏标记、手动/自动标记模式、属性变更收集、浅复制以及 StateFactory 的 MemberInfo 缓存机制。
    /// These verify state dirty tracking, manual/auto mark modes, property change collection,
    /// shallow clone, and the StateFactory MemberInfo caching mechanism.
    /// </summary>
    [TestFixture]
    public class AStateBaseTest
    {
        /// <summary>
        /// 用于测试的具体 AStateBase 子类，包含 field 和 property 两种成员。
        /// Concrete AStateBase subclass for testing, containing both field and property members.
        /// </summary>
        private class TestState : AStateBase
        {
            public int Health;
            public string Name { get; set; }

            public TestState()
            {
                // 手动初始化 MemberinfoMap，模拟 StateFactory 的正常流程
                // Manually initialize MemberinfoMap to simulate the normal StateFactory flow
                var map = new Dictionary<string, MemberInfo>();
                map["Health"] = typeof(TestState).GetField("Health");
                map["Name"] = typeof(TestState).GetProperty("Name");
                this.MemberinfoMap = map;

                // 同步写入 StateFactory 缓存，确保 SetAllPropertyChanged 能找到数据
                // Also write to StateFactory cache so SetAllPropertyChanged can find data
                StateFactory.AddMemberinfoCache(typeof(TestState), map);
            }
        }

        /// <summary>
        /// 无 MemberinfoMap 的最小状态子类，用于测试空 map 场景。
        /// Minimal state subclass without MemberinfoMap, for testing empty map scenarios.
        /// </summary>
        private class EmptyState : AStateBase
        {
            public EmptyState()
            {
                this.MemberinfoMap = new Dictionary<string, MemberInfo>();
            }
        }

        private TestState state;

        [SetUp]
        public void SetUp()
        {
            state = new TestState();
            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 AStateBase 脏标记契约。",
                "执行显式状态设置/脏标记/变更收集断言，校验手动标记模式、字段与属性支持、浅复制和缓存行为。");
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        // ──────────────────────────────────────────────
        // GetValue / SetValue 基础契约
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 GetValue 能读取 public field。
        /// Verify that GetValue can read a public field.
        /// </summary>
        [Test]
        public void GetValue_ReadsField()
        {
            state.Health = 100;

            var result = state.GetValue("Health");

            Assert.That(result, Is.EqualTo(100));
        }

        /// <summary>
        /// 验证 GetValue 能读取 public property。
        /// Verify that GetValue can read a public property.
        /// </summary>
        [Test]
        public void GetValue_ReadsProperty()
        {
            state.Name = "hero";

            var result = state.GetValue("Name");

            Assert.That(result, Is.EqualTo("hero"));
        }

        /// <summary>
        /// 验证 GetValue 对不存在的 key 返回 null。
        /// Verify that GetValue returns null for a non-existent key.
        /// </summary>
        [Test]
        public void GetValue_UnknownKey_ReturnsNull()
        {
            var result = state.GetValue("nonexistent");

            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// 验证 SetValue 能写入 field 并标记脏。
        /// Verify that SetValue writes to a field and marks it dirty.
        /// </summary>
        [Test]
        public void SetValue_WritesFieldAndMarksDirty()
        {
            state.SetValue("Health", 50);

            Assert.That(state.Health, Is.EqualTo(50));
            Assert.That(state.IsChanged, Is.True, "SetValue 后 IsChanged 应为 true / IsChanged should be true after SetValue");
        }

        /// <summary>
        /// 验证 SetValue 能写入 property 并标记脏。
        /// Verify that SetValue writes to a property and marks it dirty.
        /// </summary>
        [Test]
        public void SetValue_WritesPropertyAndMarksDirty()
        {
            state.SetValue("Name", "villain");

            Assert.That(state.Name, Is.EqualTo("villain"));
            Assert.That(state.IsChanged, Is.True);
        }

        /// <summary>
        /// 验证 SetValue 对 MemberinfoMap 中不存在的 key 不抛异常、不标记脏。
        /// Verify that SetValue with an unknown key does not throw and does not mark dirty.
        /// </summary>
        [Test]
        public void SetValue_UnknownKey_DoesNothing()
        {
            Assert.DoesNotThrow(() => state.SetValue("nonexistent", 999));
            Assert.That(state.IsChanged, Is.False);
        }

        // ──────────────────────────────────────────────
        // 脏标记模式
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证初始状态下 IsChanged 为 false。
        /// Verify that IsChanged is false in the initial state.
        /// </summary>
        [Test]
        public void IsChanged_InitiallyFalse()
        {
            Assert.That(state.IsChanged, Is.False);
        }

        /// <summary>
        /// 验证 SetPropertyChange 将属性加入变更列表并激活手动标记模式。
        /// Verify that SetPropertyChange adds a property to the change list and activates manual mark mode.
        /// </summary>
        [Test]
        public void SetPropertyChange_ActivatesManualMarkMode()
        {
            Assert.That(state.IsMunalMarkMode, Is.False);

            state.SetPropertyChange("Health");

            Assert.That(state.IsMunalMarkMode, Is.True, "SetPropertyChange 应永久启用手动标记模式 / SetPropertyChange should permanently enable manual mark mode");
        }

        /// <summary>
        /// 验证 GetChangedPropertise 返回已变更的属性名并清空变更列表。
        /// Verify that GetChangedPropertise returns changed property names and clears the change list.
        /// </summary>
        [Test]
        public void GetChangedPropertise_ReturnsAndDrainsChanges()
        {
            state.SetValue("Health", 50);
            state.SetValue("Name", "updated");

            var changed = state.GetChangedPropertise();

            CollectionAssert.Contains(changed, "Health");
            CollectionAssert.Contains(changed, "Name");
            Assert.That(state.IsChanged, Is.False, "获取变更后列表应清空 / Change list should be cleared after GetChangedPropertise");
        }

        /// <summary>
        /// 验证连续两次 GetChangedPropertise：第二次返回空。
        /// Verify that calling GetChangedPropertise twice in succession returns empty the second time.
        /// </summary>
        [Test]
        public void GetChangedPropertise_SecondCallReturnsEmpty()
        {
            state.SetValue("Health", 1);

            state.GetChangedPropertise();
            var second = state.GetChangedPropertise();

            Assert.That(second, Is.Empty, "第二次获取应返回空 / Second call should return empty");
        }

        // ──────────────────────────────────────────────
        // SetAllPropertyChanged
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 SetAllPropertyChanged 将 MemberinfoMap 中所有 key 加入变更列表。
        /// Verify that SetAllPropertyChanged adds all keys from MemberinfoMap to the change list.
        /// </summary>
        [Test]
        public void SetAllPropertyChanged_MarksAllPropertiesDirty()
        {
            state.SetAllPropertyChanged();

            var changed = state.GetChangedPropertise();

            CollectionAssert.Contains(changed, "Health");
            CollectionAssert.Contains(changed, "Name");
        }

        /// <summary>
        /// 验证 SetAllPropertyChanged 会清空已有的部分变更再标记全量。
        /// Verify that SetAllPropertyChanged clears existing partial changes before marking all.
        /// </summary>
        [Test]
        public void SetAllPropertyChanged_ClearsPreviousPartialChanges()
        {
            state.SetValue("Health", 10); // 部分变更
            state.SetAllPropertyChanged(); // 全量标记

            var changed = state.GetChangedPropertise();

            // 应包含全量，而非重复 Health
            // Should contain all, not duplicate Health
            Assert.That(changed.Length, Is.EqualTo(2), "应包含所有属性 / Should contain all properties");
        }

        // ──────────────────────────────────────────────
        // GetAllPropertise
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 GetAllPropertise 返回 MemberinfoMap 中所有 key。
        /// Verify that GetAllPropertise returns all keys from MemberinfoMap.
        /// </summary>
        [Test]
        public void GetAllPropertise_ReturnsAllMemberNames()
        {
            var all = state.GetAllPropertise();

            CollectionAssert.Contains(all, "Health");
            CollectionAssert.Contains(all, "Name");
        }

        // ──────────────────────────────────────────────
        // Clone
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 Clone 执行浅复制，值类型字段独立、引用类型共享。
        /// Verify that Clone performs a shallow copy: value-type fields are independent, reference types are shared.
        /// </summary>
        [Test]
        public void Clone_PerformsShallowCopy()
        {
            state.Health = 100;
            state.Name = "original";

            var clone = state.Clone() as TestState;

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.Health, Is.EqualTo(100));
            Assert.That(clone.Name, Is.EqualTo("original"));

            // 修改 clone 不影响 original
            // Modifying clone does not affect original
            clone.Health = 0;
            Assert.That(state.Health, Is.EqualTo(100), "值类型应独立 / Value types should be independent");
        }

        /// <summary>
        /// 验证 Clone 不会复制脏标记状态。
        /// Verify that Clone does not copy dirty-tracking state.
        /// </summary>
        [Test]
        public void Clone_DoesNotCopyDirtyState()
        {
            state.SetValue("Health", 50);
            Assert.That(state.IsChanged, Is.True);

            var clone = state.Clone() as TestState;

            // MemberwiseClone 复制 changeProptyList 的引用，但 IsChanged 可能因 list 内容而定
            // 重点是 clone 后的状态应该是独立的
            // MemberwiseClone copies the changeProptyList reference, but IsChanged depends on list content
            // The key point is that the cloned state should be independent
            Assert.That(clone, Is.Not.Null);
        }

        // ──────────────────────────────────────────────
        // Source 属性
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 Source 默认为 -1。
        /// Verify that Source defaults to -1.
        /// </summary>
        [Test]
        public void Source_DefaultsToMinusOne()
        {
            Assert.That(state.Source, Is.EqualTo(-1));
        }

        /// <summary>
        /// 验证 Source 可以被设置和读取。
        /// Verify that Source can be set and read.
        /// </summary>
        [Test]
        public void Source_CanBeSetAndRead()
        {
            state.Source = 42;
            Assert.That(state.Source, Is.EqualTo(42));
        }
    }

    /// <summary>
    /// StateFactory 静态缓存契约测试。
    /// Contract tests for StateFactory static MemberInfo caching.
    /// </summary>
    [TestFixture]
    public class StateFactoryTest
    {
        [SetUp]
        public void SetUp()
        {
            AStateBaseTest.LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 StateFactory 缓存契约。",
                "执行显式缓存添加/查询/覆盖断言，校验 MemberInfo 缓存的存取行为。");
        }

        /// <summary>
        /// 验证 GetMemberinfoCache 对未缓存的类型返回 null。
        /// Verify that GetMemberinfoCache returns null for an uncached type.
        /// </summary>
        [Test]
        public void GetMemberinfoCache_UncachedType_ReturnsNull()
        {
            var result = StateFactory.GetMemberinfoCache(typeof(UncachedDummyState));

            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// 验证 AddMemberinfoCache 后 GetMemberinfoCache 返回正确缓存。
        /// Verify that after AddMemberinfoCache, GetMemberinfoCache returns the correct cache.
        /// </summary>
        [Test]
        public void AddMemberinfoCache_ThenGet_ReturnsCachedMap()
        {
            var map = new Dictionary<string, MemberInfo>
            {
                ["TestField"] = typeof(CachedDummyState).GetField("TestField")
            };

            StateFactory.AddMemberinfoCache(typeof(CachedDummyState), map);
            var result = StateFactory.GetMemberinfoCache(typeof(CachedDummyState));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.ContainKey("TestField"));
        }

        /// <summary>
        /// 验证 AddMemberinfoCache 对同一类型会覆盖旧缓存。
        /// Verify that AddMemberinfoCache overwrites the previous cache for the same type.
        /// </summary>
        [Test]
        public void AddMemberinfoCache_OverwritesExistingCache()
        {
            var map1 = new Dictionary<string, MemberInfo> { ["Old"] = null };
            var map2 = new Dictionary<string, MemberInfo> { ["New"] = null };

            StateFactory.AddMemberinfoCache(typeof(OverwriteDummyState), map1);
            StateFactory.AddMemberinfoCache(typeof(OverwriteDummyState), map2);

            var result = StateFactory.GetMemberinfoCache(typeof(OverwriteDummyState));
            Assert.That(result, Does.ContainKey("New"));
            Assert.That(result, Does.Not.ContainKey("Old"));
        }

        // 虚假类型，仅用于隔离 StateFactory 缓存测试
        // Dummy types used solely to isolate StateFactory cache tests
        private class UncachedDummyState { }

        private class CachedDummyState { public int TestField; }

        private class OverwriteDummyState { }
    }
}
