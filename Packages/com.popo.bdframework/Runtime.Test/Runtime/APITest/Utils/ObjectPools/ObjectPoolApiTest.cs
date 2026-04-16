using System.Collections.Generic;
using BDFramework.Utils;

namespace BDFramework.RuntimeTests.ApiTest.Utils.ObjectPools
{
    /// <summary>
    /// 通用对象池公开 API 的 Runtime 测试主体。
    /// Runtime test body for the public API of the generic object pool.
    /// 该类型把预热、复用、扩容与销毁回调规则固定在 Runtime.Test 的 APITest 层内，
    /// 让 Editor 包装和真机 Talos 套件共享同一套对象池基础契约断言。
    /// This type fixes warm-up, reuse, growth, and destroy-callback rules inside the Runtime.Test APITest layer,
    /// allowing editor wrappers and packaged Talos suites to share the same baseline object-pool contract assertions.
    /// </summary>
    public sealed class ObjectPoolApiTest
    {
        /// <summary>
        /// 输出统一日志，记录对象池 API 测试的目的与手段。
        /// Emit a unified log that records the purpose and means of the object-pool API test.
        /// </summary>
        public void SetUp(string testName)
        {
            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(ObjectPoolApiTest) : testName,
                "验证通用对象池的预热、复用、扩容与销毁回调规则保持稳定。",
                "通过构造轻量探针对象池并直接调用 ObjectPool 公开 API，断言计数、实例复用和销毁回调结果。"
            );
        }

        /// <summary>
        /// 验证构造时会按初始容量完成预热，但已使用计数保持为零。
        /// Verify that construction warms the pool to the initial capacity while keeping the used-item count at zero.
        /// </summary>
        public void Constructor_WithInitialSize_WarmsPoolAndKeepsUsedCountZero()
        {
            var createdCount = 0;
            var pool = new ObjectPool<PooledProbe>(() => new PooledProbe { Id = ++createdCount }, 2);

            ApiTestAssert.AreEqual(2, createdCount, "对象池构造时应按初始容量预热对象。");
            ApiTestAssert.AreEqual(2, pool.Count, "对象池预热后的总容量应等于初始容量。");
            ApiTestAssert.AreEqual(0, pool.CountUsedItems, "对象池预热后已使用计数应保持为零。");
        }

        /// <summary>
        /// 验证释放对象后，再次获取会复用原实例而不是创建新对象。
        /// Verify that after an item is released, the next get reuses the original instance instead of creating a new object.
        /// </summary>
        public void GetItem_AfterRelease_ReusesSameInstance()
        {
            var createdCount = 0;
            var pool = new ObjectPool<PooledProbe>(() => new PooledProbe { Id = ++createdCount }, 1);
            var first = pool.GetItem();

            pool.ReleaseItem(first);
            var second = pool.GetItem();

            ApiTestAssert.AreSame(first, second, "释放后的对象应被后续获取请求复用。");
            ApiTestAssert.AreEqual(1, pool.Count, "复用对象时对象池总容量不应增长。");
            ApiTestAssert.AreEqual(1, pool.CountUsedItems, "复用对象后已使用计数应回到一。");
        }

        /// <summary>
        /// 验证池内对象全部占用时，会创建新实例并扩容池容量。
        /// Verify that when all pooled items are in use, the pool creates a new instance and grows its capacity.
        /// </summary>
        public void GetItem_WhenPoolExhausted_CreatesNewItemAndGrowsCount()
        {
            var createdCount = 0;
            var pool = new ObjectPool<PooledProbe>(() => new PooledProbe { Id = ++createdCount }, 1);
            var first = pool.GetItem();
            var second = pool.GetItem();

            ApiTestAssert.AreNotSame(first, second, "池内对象全部占用时应创建新的实例。");
            ApiTestAssert.AreEqual(2, pool.Count, "池内对象不足时对象池总容量应增长。");
            ApiTestAssert.AreEqual(2, pool.CountUsedItems, "扩容后已使用计数应覆盖所有借出的实例。");
        }

        /// <summary>
        /// 验证带销毁回调的对象池销毁时，会覆盖所有已创建实例并清空池容量。
        /// Verify that destroying a pool with a destroy callback covers all created instances and clears the pool capacity.
        /// </summary>
        public void Destroy_WithDestroyCallback_InvokesCallbackForAllItemsAndClearsPool()
        {
            var createdCount = 0;
            var destroyedIds = new List<int>();
            var pool = new ObjectPool<PooledProbe>(
                () => new PooledProbe { Id = ++createdCount },
                item => destroyedIds.Add(item.Id),
                2);

            pool.GetItem();
            pool.GetItem();
            var extra = pool.GetItem();
            pool.ReleaseItem(extra);

            pool.Destroy();

            ApiTestAssert.AreEqual(3, destroyedIds.Count, "销毁对象池时应回调所有已创建实例。");
            ApiTestAssert.AreEqual(0, pool.Count, "销毁对象池后总容量应被清空。");
        }

        /// <summary>
        /// 用于验证对象池行为的测试探针。
        /// Test probe used to verify object-pool behavior.
        /// </summary>
        public sealed class PooledProbe
        {
            /// <summary>
            /// 记录探针编号。
            /// Store the probe identifier.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// 初始化对象池探针。
            /// Initialize the pooled probe.
            /// </summary>
            public PooledProbe()
            {
            }
        }
    }
}