using BDFramework.RuntimeTests.ApiTest.Utils.ObjectPools;
using Talos.E2E;

namespace BDFramework.Test.E2E
{
    /// <summary>
    /// 对象池 Runtime API 契约测试套件。
    /// Runtime API contract suite for the object-pool layer.
    /// 该套件把对象池的纯逻辑 API 断言迁移为可打包执行的 Talos E2E 用例，
    /// 让真机环境也能持续覆盖对象池预热、复用、扩容与销毁回调规则。
    /// This suite moves the pure-logic API assertions for object pools into packaged Talos E2E cases,
    /// allowing device runs to keep covering object-pool warm-up, reuse, growth, and destroy-callback rules.
    /// </summary>
    public static class ObjectPoolApiContractTests
    {
        /// <summary>
        /// 验证构造时会按初始容量完成预热，但已使用计数保持为零。
        /// Verify that construction warms the pool to the initial capacity while keeping the used-item count at zero.
        /// </summary>
        [E2ETest(suite: "object-pool-api", order: 1, des: "object-pool-warm-initial-size")]
        public static void ObjectPoolWarmInitialSize()
        {
            var test = new ObjectPoolApiTest();
            test.SetUp(nameof(ObjectPoolWarmInitialSize));
            test.Constructor_WithInitialSize_WarmsPoolAndKeepsUsedCountZero();
        }

        /// <summary>
        /// 验证释放对象后，再次获取会复用原实例而不是创建新对象。
        /// Verify that after an item is released, the next get reuses the original instance instead of creating a new object.
        /// </summary>
        [E2ETest(suite: "object-pool-api", order: 2, des: "object-pool-reuse-after-release")]
        public static void ObjectPoolReuseAfterRelease()
        {
            var test = new ObjectPoolApiTest();
            test.SetUp(nameof(ObjectPoolReuseAfterRelease));
            test.GetItem_AfterRelease_ReusesSameInstance();
        }

        /// <summary>
        /// 验证池内对象全部占用时，会创建新实例并扩容池容量。
        /// Verify that when all pooled items are in use, the pool creates a new instance and grows its capacity.
        /// </summary>
        [E2ETest(suite: "object-pool-api", order: 3, des: "object-pool-grow-when-exhausted")]
        public static void ObjectPoolGrowWhenExhausted()
        {
            var test = new ObjectPoolApiTest();
            test.SetUp(nameof(ObjectPoolGrowWhenExhausted));
            test.GetItem_WhenPoolExhausted_CreatesNewItemAndGrowsCount();
        }

        /// <summary>
        /// 验证带销毁回调的对象池销毁时，会覆盖所有已创建实例并清空池容量。
        /// Verify that destroying a pool with a destroy callback covers all created instances and clears the pool capacity.
        /// </summary>
        [E2ETest(suite: "object-pool-api", order: 4, des: "object-pool-destroy-callback")]
        public static void ObjectPoolDestroyCallback()
        {
            var test = new ObjectPoolApiTest();
            test.SetUp(nameof(ObjectPoolDestroyCallback));
            test.Destroy_WithDestroyCallback_InvokesCallbackForAllItemsAndClearsPool();
        }
    }
}