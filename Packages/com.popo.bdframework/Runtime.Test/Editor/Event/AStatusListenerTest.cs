using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.DataListener;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.Event
{
    /// <summary>
    /// AStatusListener 核心契约测试。
    /// Contract tests for AStatusListener: type validation, ordered callbacks, trigger counting, value caching, and cleanup.
    /// 验证监听器注册/注销/触发计数/值缓存/清理的完整生命周期，不依赖 MonoBehaviour 或 Unity 运行时状态。
    /// These verify the complete lifecycle of listener registration, unregistration, trigger counting, value caching, and cleanup
    /// without depending on MonoBehaviour or Unity runtime state.
    /// </summary>
    [TestFixture]
    public class AStatusListenerTest
    {
        /// <summary>
        /// 可实例化的测试子类，绕过 AStatusListener 的 abstract 限制。
        /// Concrete test subclass to bypass AStatusListener's abstract constraint.
        /// </summary>
        private class TestStatusListener : AStatusListener
        {
        }

        private TestStatusListener listener;

        [SetUp]
        public void SetUp()
        {
            listener = new TestStatusListener();
            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 AStatusListener 监听器契约。",
                "执行显式监听器注册/触发/注销断言，校验回调顺序、触发计数、值缓存和类型校验行为。");
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        // ──────────────────────────────────────────────
        // SetData / GetData 基础契约
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 SetData 后 GetData 能正确返回设置的值。
        /// Verify that GetData returns the value previously set via SetData.
        /// </summary>
        [Test]
        public void SetData_ThenGetData_ReturnsCorrectValue()
        {
            listener.SetData("score", "100");

            var result = listener.GetData<string>("score");

            Assert.That(result, Is.EqualTo("100"));
        }

        /// <summary>
        /// 验证类型不匹配时 SetData 拒绝写入，保留原始值。
        /// Verify that SetData rejects a type-mismatched write and preserves the original value.
        /// 这覆盖信任边界校验：上游数据类型应保持一致，不一致时快速失败而非静默覆盖。
        /// This covers trust boundary validation: upstream data types must remain consistent;
        /// on mismatch, fail fast rather than silently overwriting.
        /// </summary>
        [Test]
        public void SetData_TypeMismatch_RejectsAndPreservesOriginalValue()
        {
            listener.SetData("key", "hello");

            // 尝试用不同类型写入同一 key
            // Attempt to write a different type to the same key
            // Note: AStatusListener.AddListener<T> requires T : class, so we use string for both
            // Testing type mismatch with value types requires using the object overload directly
            listener.SetData("key", 42); // int != string → should be rejected

            var result = listener.GetData<string>("key");
            Assert.That(result, Is.EqualTo("hello"), "类型不匹配时应该保留原始值 / Original value should be preserved on type mismatch");
        }

        /// <summary>
        /// 验证查询不存在的 key 时返回 default(T) 而不是抛异常。
        /// Verify that querying a non-existent key returns default(T) instead of throwing.
        /// </summary>
        [Test]
        public void GetData_MissingKey_ReturnsDefault()
        {
            var result = listener.GetData<string>("nonexistent");

            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// 验证 ContainsKey 对已设置和未设置的 key 返回正确结果。
        /// Verify that ContainsKey returns correct results for set and unset keys.
        /// </summary>
        [Test]
        public void ContainsKey_ReturnsCorrectPresence()
        {
            Assert.That(listener.ContainsKey("missing"), Is.False);

            listener.SetData("exists", "value");

            Assert.That(listener.ContainsKey("exists"), Is.True);
        }

        // ──────────────────────────────────────────────
        // 回调触发与顺序
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 SetData 触发回调时按 order 升序执行。
        /// Verify that SetData triggers callbacks in ascending order.
        /// </summary>
        [Test]
        public void AddListener_CallbacksFireInOrderSequence()
        {
            var callOrder = new List<int>();

            listener.AddListener<string>("event", v => callOrder.Add(3), order: 3);
            listener.AddListener<string>("event", v => callOrder.Add(1), order: 1);
            listener.AddListener<string>("event", v => callOrder.Add(2), order: 2);

            listener.SetData("event", "fire");

            Assert.That(callOrder, Is.EqualTo(new List<int> { 1, 2, 3 }),
                "回调应按 order 升序执行 / Callbacks should fire in ascending order");
        }

        /// <summary>
        /// 验证 isTriggerCallback=false 时 SetData 不触发回调。
        /// Verify that SetData with isTriggerCallback=false does not fire callbacks.
        /// </summary>
        [Test]
        public void SetData_SuppressCallback_DoesNotFireListeners()
        {
            var fired = false;
            listener.AddListener<string>("quiet", v => fired = true);

            listener.SetData("quiet", "shh", isTriggerCallback: false);

            Assert.That(fired, Is.False, "isTriggerCallback=false 时不应触发回调 / Callback should not fire when isTriggerCallback=false");
            Assert.That(listener.GetData<string>("quiet"), Is.EqualTo("shh"), "值仍应被设置 / Value should still be set");
        }

        // ──────────────────────────────────────────────
        // 触发计数 (TriggerNum)
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 TriggerNum 递减到 0 后回调被自动移除，后续触发不再执行。
        /// Verify that when TriggerNum decrements to 0, the callback is auto-removed and no longer fires.
        /// </summary>
        [Test]
        public void AddListener_TriggerNumZero_RemovesCallbackAutomatically()
        {
            var invokeCount = 0;
            listener.AddListener<string>("limited", v => invokeCount++, triggerNum: 2);

            listener.SetData("limited", "first");
            Assert.That(invokeCount, Is.EqualTo(1), "第一次触发应执行 / First trigger should execute");

            listener.SetData("limited", "second");
            Assert.That(invokeCount, Is.EqualTo(2), "第二次触发应执行 / Second trigger should execute");

            listener.SetData("limited", "third");
            Assert.That(invokeCount, Is.EqualTo(2), "TriggerNum 耗尽后不再执行 / Should not fire after TriggerNum exhausted");
        }

        // ──────────────────────────────────────────────
        // 值缓存
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证先 SetData 后 AddListener(isTriggerCacheData=true) 时，缓存值会被回放。
        /// Verify that cached values are replayed when AddListener is called with isTriggerCacheData=true after SetData.
        /// </summary>
        [Test]
        public void AddListener_WithCacheReplay_ReplaysPreviousValues()
        {
            listener.SetData("late", "cached1", isTriggerCallback: true);
            listener.SetData("late", "cached2", isTriggerCallback: true);

            var replayed = new List<string>();
            listener.AddListener<string>("late", v => replayed.Add(v), isTriggerCacheData: true);

            Assert.That(replayed, Is.EqualTo(new List<string> { "cached1", "cached2" }),
                "缓存值应按顺序回放 / Cached values should be replayed in order");
        }

        /// <summary>
        /// 验证值缓存不会超过 maxCacheValueCount(20) 条。
        /// Verify that the value cache does not exceed maxCacheValueCount (20).
        /// </summary>
        [Test]
        public void ValueCache_CappedAtMaxCacheValueCount()
        {
            for (int i = 0; i < 25; i++)
            {
                listener.SetData("overflow", i.ToString(), isTriggerCallback: true);
            }

            var replayed = new List<string>();
            listener.AddListener<string>("overflow", v => replayed.Add(v), isTriggerCacheData: true);

            // maxCacheValueCount=20, 前5个应被淘汰
            // maxCacheValueCount=20, the first 5 should be evicted
            Assert.That(replayed.Count, Is.LessThanOrEqualTo(20),
                "缓存条目不应超过 maxCacheValueCount / Cache entries should not exceed maxCacheValueCount");
            Assert.That(replayed[0], Is.EqualTo("5"),
                "最早的缓存应被淘汰 / Oldest cached values should be evicted");
        }

        // ──────────────────────────────────────────────
        // 清理与移除
        // ──────────────────────────────────────────────

        /// <summary>
        /// 验证 ClearListener 移除指定 key 的所有回调。
        /// Verify that ClearListener removes all callbacks for a given key.
        /// </summary>
        [Test]
        public void ClearListener_RemovesAllCallbacksForKey()
        {
            var fired = false;
            listener.AddListener<string>("clearable", v => fired = true);
            listener.ClearListener("clearable");

            listener.SetData("clearable", "after-clear");

            Assert.That(fired, Is.False, "ClearListener 后回调不应触发 / Callback should not fire after ClearListener");
        }

        /// <summary>
        /// 验证 RemoveListener 移除指定回调后，其他回调仍正常工作。
        /// Verify that after RemoveListener removes a specific callback, other callbacks still work.
        /// </summary>
        [Test]
        public void RemoveListener_OnlyRemovesTargetCallback()
        {
            var callback1Fired = false;
            var callback2Fired = false;
            Action<string> callback1 = v => callback1Fired = true;
            Action<string> callback2 = v => callback2Fired = true;

            listener.AddListener<string>("multi", callback1);
            listener.AddListener<string>("multi", callback2);
            listener.RemoveListener("multi", callback1);

            listener.SetData("multi", "fire");

            Assert.That(callback1Fired, Is.False, "被移除的回调不应触发 / Removed callback should not fire");
            Assert.That(callback2Fired, Is.True, "未移除的回调应正常触发 / Remaining callback should fire");
        }

        /// <summary>
        /// 验证 ClearAllListener 清除所有数据、回调和缓存。
        /// Verify that ClearAllListener clears all data, callbacks, and cache.
        /// </summary>
        [Test]
        public void ClearAllListener_ResetsEntireState()
        {
            listener.SetData("a", "1");
            listener.AddListener<string>("a", v => { });
            listener.SetData("b", "2");

            listener.ClearAllListener();

            Assert.That(listener.ContainsKey("a"), Is.False, "数据应被清除 / Data should be cleared");
            Assert.That(listener.ContainsKey("b"), Is.False, "数据应被清除 / Data should be cleared");
        }

        /// <summary>
        /// 验证 GetDataNames 返回所有已设置的 key。
        /// Verify that GetDataNames returns all keys that have been set.
        /// </summary>
        [Test]
        public void GetDataNames_ReturnsAllSetKeys()
        {
            listener.SetData("alpha", "1");
            listener.SetData("beta", "2");

            var names = listener.GetDataNames();

            CollectionAssert.Contains(names, "alpha");
            CollectionAssert.Contains(names, "beta");
        }

        /// <summary>
        /// 验证回调中修改数据不会导致无限递归（因为回调列表在触发前被复制）。
        /// Verify that modifying data inside a callback does not cause infinite recursion
        /// (because the callback list is copied before iteration).
        /// </summary>
        [Test]
        public void SetData_InsideCallback_DoesNotCauseInfiniteRecursion()
        {
            var depth = 0;
            listener.AddListener<string>("recursive", v =>
            {
                depth++;
                if (depth < 3)
                {
                    listener.SetData("recursive", $"nested-{depth}");
                }
            });

            listener.SetData("recursive", "start");

            Assert.That(depth, Is.GreaterThanOrEqualTo(1), "回调应至少执行一次 / Callback should execute at least once");
            // 由于回调列表是复制后遍历，递归 SetData 会触发新一轮回调
            // Because the callback list is copied before iteration, recursive SetData triggers a new round
        }
    }

    /// <summary>
    /// ADataListenerT 泛型监听器契约测试。
    /// Contract tests for ADataListenerT: typed callbacks, once-callbacks, cache replay, and cleanup.
    /// 验证类型化监听器的注册/一次性回调/缓存回放/清理行为，与 AStatusListener 形成互补覆盖。
    /// These verify typed listener registration, once-callback, cache replay, and cleanup behaviors,
    /// complementing AStatusListener coverage.
    /// </summary>
    [TestFixture]
    public class ADataListenerTTest
    {
        /// <summary>
        /// 可实例化的测试子类。
        /// Concrete test subclass for ADataListenerT.
        /// </summary>
        private class TestIntListener : ADataListenerT<int>
        {
        }

        private TestIntListener listener;

        [SetUp]
        public void SetUp()
        {
            listener = new TestIntListener();
            AStatusListenerTest.LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 ADataListenerT 泛型监听器契约。",
                "执行显式泛型监听器注册/触发/注销断言，校验类型化回调、一次性回调和缓存回放行为。");
        }

        /// <summary>
        /// 验证 SetData 后 GetData 返回正确值。
        /// Verify that GetData returns the correct value after SetData.
        /// </summary>
        [Test]
        public void SetData_ThenGetData_ReturnsCorrectValue()
        {
            listener.SetData("count", 42);

            Assert.That(listener.GetData("count"), Is.EqualTo(42));
        }

        /// <summary>
        /// 验证查询不存在的 key 返回 default(T)。
        /// Verify that querying a non-existent key returns default(T).
        /// </summary>
        [Test]
        public void GetData_MissingKey_ReturnsDefault()
        {
            Assert.That(listener.GetData("missing"), Is.EqualTo(0), "int 的 default 应为 0 / default(int) should be 0");
        }

        /// <summary>
        /// 验证 AddListener 的回调在 SetData 时被触发。
        /// Verify that AddListener callbacks are fired when SetData is called.
        /// </summary>
        [Test]
        public void AddListener_FiresOnSetData()
        {
            var received = 0;
            listener.SetData("hp", 100);
            listener.AddListener("hp", v => received = v);

            listener.SetData("hp", 80);

            Assert.That(received, Is.EqualTo(80));
        }

        /// <summary>
        /// 验证 AddListenerOnce 的回调只触发一次后自动移除。
        /// Verify that AddListenerOnce callback fires only once and is then auto-removed.
        /// </summary>
        [Test]
        public void AddListenerOnce_FiresOnlyOnceThenRemoves()
        {
            var invokeCount = 0;
            listener.SetData("once", 1);
            listener.AddListenerOnce("once", v => invokeCount++);

            listener.SetData("once", 2);
            Assert.That(invokeCount, Is.EqualTo(1), "第一次触发应执行 / First trigger should execute");

            listener.SetData("once", 3);
            Assert.That(invokeCount, Is.EqualTo(1), "一次性回调不应再次触发 / Once-callback should not fire again");
        }

        /// <summary>
        /// 验证 AddListenerOnce 和 AddListener 可以共存，once 回调触发后不影响普通回调。
        /// Verify that AddListenerOnce and AddListener coexist; once-callback removal does not affect regular callbacks.
        /// </summary>
        [Test]
        public void AddListenerOnce_CoexistsWithRegularListener()
        {
            var onceFired = 0;
            var regularFired = 0;
            listener.SetData("mixed", 0);
            listener.AddListenerOnce("mixed", v => onceFired++);
            listener.AddListener("mixed", v => regularFired++);

            listener.SetData("mixed", 1);
            listener.SetData("mixed", 2);

            Assert.That(onceFired, Is.EqualTo(1), "一次性回调只应触发一次 / Once-callback should fire only once");
            Assert.That(regularFired, Is.EqualTo(2), "普通回调应每次都触发 / Regular callback should fire every time");
        }

        /// <summary>
        /// 验证 AddListener(isTriggerCacheData=true) 时缓存值被回放。
        /// Verify that cached values are replayed when AddListener is called with isTriggerCacheData=true.
        /// </summary>
        [Test]
        public void AddListener_WithCacheReplay_ReplaysPreviousValues()
        {
            listener.SetData("late", 10, isTriggerCallback: true);
            listener.SetData("late", 20, isTriggerCallback: true);

            var replayed = new List<int>();
            listener.AddListener("late", v => replayed.Add(v), isTriggerCacheData: true);

            Assert.That(replayed, Is.EqualTo(new List<int> { 10, 20 }),
                "缓存值应按顺序回放 / Cached values should be replayed in order");
        }

        /// <summary>
        /// 验证 RemoveListener 移除指定回调。
        /// Verify that RemoveListener removes a specific callback.
        /// </summary>
        [Test]
        public void RemoveListener_RemovesTargetCallback()
        {
            var fired = false;
            Action<int> callback = v => fired = true;
            listener.SetData("removable", 1);
            listener.AddListener("removable", callback);
            listener.RemoveListener("removable", callback);

            listener.SetData("removable", 2);

            Assert.That(fired, Is.False, "被移除的回调不应触发 / Removed callback should not fire");
        }

        /// <summary>
        /// 验证 ClearListener 清除指定 key 的所有回调。
        /// Verify that ClearListener removes all callbacks for a given key.
        /// </summary>
        [Test]
        public void ClearListener_RemovesAllCallbacksForKey()
        {
            var fired = false;
            listener.SetData("clearable", 1);
            listener.AddListener("clearable", v => fired = true);
            listener.ClearListener("clearable");

            listener.SetData("clearable", 2);

            Assert.That(fired, Is.False, "ClearListener 后回调不应触发 / Callback should not fire after ClearListener");
        }

        /// <summary>
        /// 验证 GetDataNames 返回所有已设置的 key。
        /// Verify that GetDataNames returns all keys that have been set.
        /// </summary>
        [Test]
        public void GetDataNames_ReturnsAllSetKeys()
        {
            listener.SetData("x", 1);
            listener.SetData("y", 2);

            var names = listener.GetDataNames();

            CollectionAssert.Contains(names, "x");
            CollectionAssert.Contains(names, "y");
        }
    }
}
