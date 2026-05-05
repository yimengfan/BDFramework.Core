using System;
using System.Reflection;
using BDFramework.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BDFramework.EditorTest.Utils
{
    /// <summary>
    /// ReflectionExtension IL2CPP 安全属性获取契约测试。
    /// Contract tests for ReflectionExtension: IL2CPP-safe attribute retrieval with manual `is T` check.
    /// 验证 GetAttributeInILRuntime 和 GetAttributeInILRuntimes 在正常、多属性、缺失属性场景下的行为。
    /// These verify GetAttributeInILRuntime and GetAttributeInILRuntimes behavior under normal,
    /// multi-attribute, and missing-attribute scenarios.
    /// </summary>
    [TestFixture]
    public class ReflectionExtensionTest
    {
        /// <summary>
        /// 测试用 Attribute
        /// Test attribute for reflection testing
        /// </summary>
        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        private class TestAttribute : Attribute
        {
            public string Label { get; }

            public TestAttribute(string label)
            {
                Label = label;
            }
        }

        /// <summary>
        /// 另一个测试用 Attribute，用于测试类型过滤
        /// Another test attribute for testing type filtering
        /// </summary>
        [AttributeUsage(AttributeTargets.All)]
        private class OtherAttribute : Attribute
        {
        }

        /// <summary>
        /// 带有 TestAttribute 的测试类
        /// Test class decorated with TestAttribute
        /// </summary>
        [Test("class-label")]
        private class DecoratedClass
        {
            [Test("field-label")]
            public int TestField;

            [Test("method-label")]
            public void TestMethod() { }
        }

        /// <summary>
        /// 带有多个同类型 Attribute 的测试类
        /// Test class decorated with multiple attributes of the same type
        /// </summary>
        [Test("first")]
        [Test("second")]
        private class MultiDecoratedClass
        {
        }

        /// <summary>
        /// 不带 TestAttribute 的测试类
        /// Test class without TestAttribute
        /// </summary>
        [OtherAttribute]
        private class UndecoratedClass
        {
        }

        [SetUp]
        public void SetUp()
        {
            LogTestPurposeAndMeans(TestContext.CurrentContext.Test.Name,
                $"验证 {TestContext.CurrentContext.Test.Name} 对应的 ReflectionExtension 属性获取契约。",
                "执行显式属性获取断言，校验单属性/多属性/缺失属性/类型过滤行为。");
        }

        /// <summary>
        /// 输出统一的测试开始日志，强制带出测试目的与实现手段。
        /// </summary>
        internal static void LogTestPurposeAndMeans(string testName, string purpose, string means)
        {
            Debug.Log($"[测试开始] name={testName} 测试目的={purpose} 实现手段={means}");
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntime 能从类上获取正确类型的 Attribute。
        /// Verify that GetAttributeInILRuntime retrieves the correct Attribute from a class.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntime_OnClass_ReturnsAttribute()
        {
            var memberInfo = typeof(DecoratedClass);
            var attr = memberInfo.GetAttributeInILRuntime<TestAttribute>();

            Assert.That(attr, Is.Not.Null);
            Assert.That(attr.Label, Is.EqualTo("class-label"));
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntime 在目标 Attribute 不存在时返回 null。
        /// Verify that GetAttributeInILRuntime returns null when the target Attribute is not present.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntime_OnUndecoratedClass_ReturnsNull()
        {
            var memberInfo = typeof(UndecoratedClass);
            var attr = memberInfo.GetAttributeInILRuntime<TestAttribute>();

            Assert.That(attr, Is.Null, "无目标 Attribute 时应返回 null / Should return null when target Attribute is absent");
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntime 在多个同类型 Attribute 时返回第一个匹配。
        /// Verify that GetAttributeInILRuntime returns the first matching Attribute when multiple exist.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntime_MultipleAttributes_ReturnsFirstMatch()
        {
            var memberInfo = typeof(MultiDecoratedClass);
            var attr = memberInfo.GetAttributeInILRuntime<TestAttribute>();

            Assert.That(attr, Is.Not.Null);
            // GetCustomAttributes 的顺序不保证，但应返回非 null
            // Order of GetCustomAttributes is not guaranteed, but should return non-null
            Assert.That(attr.Label, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntimes 能获取所有同类型 Attribute。
        /// Verify that GetAttributeInILRuntimes retrieves all Attributes of the target type.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntimes_MultipleAttributes_ReturnsAll()
        {
            var memberInfo = typeof(MultiDecoratedClass);
            var attrs = memberInfo.GetAttributeInILRuntimes<TestAttribute>();

            Assert.That(attrs, Is.Not.Null);
            // 至少有2个 TestAttribute
            // At least 2 TestAttributes
            var nonNullCount = 0;
            foreach (var a in attrs)
            {
                if (a != null) nonNullCount++;
            }

            Assert.That(nonNullCount, Is.EqualTo(2), "应返回所有匹配 Attribute / Should return all matching Attributes");
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntimes 在目标 Attribute 不存在时返回全 null 数组。
        /// Verify that GetAttributeInILRuntimes returns an all-null array when the target Attribute is not present.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntimes_OnUndecoratedClass_ReturnsArrayWithNulls()
        {
            var memberInfo = typeof(UndecoratedClass);
            var attrs = memberInfo.GetAttributeInILRuntimes<TestAttribute>();

            Assert.That(attrs, Is.Not.Null);
            // OtherAttribute 存在但不是 TestAttribute，所以对应位置为 null
            // OtherAttribute exists but is not TestAttribute, so the corresponding slot is null
            foreach (var a in attrs)
            {
                Assert.That(a, Is.Null, "不匹配的类型应返回 null / Non-matching types should return null");
            }
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntime 在字段上也能正常工作。
        /// Verify that GetAttributeInILRuntime works correctly on fields.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntime_OnField_ReturnsAttribute()
        {
            var fieldInfo = typeof(DecoratedClass).GetField("TestField");
            Assert.That(fieldInfo, Is.Not.Null);

            var attr = fieldInfo.GetAttributeInILRuntime<TestAttribute>();
            Assert.That(attr, Is.Not.Null);
            Assert.That(attr.Label, Is.EqualTo("field-label"));
        }

        /// <summary>
        /// 验证 GetAttributeInILRuntime 在方法上也能正常工作。
        /// Verify that GetAttributeInILRuntime works correctly on methods.
        /// </summary>
        [Test]
        public void GetAttributeInILRuntime_OnMethod_ReturnsAttribute()
        {
            var methodInfo = typeof(DecoratedClass).GetMethod("TestMethod");
            Assert.That(methodInfo, Is.Not.Null);

            var attr = methodInfo.GetAttributeInILRuntime<TestAttribute>();
            Assert.That(attr, Is.Not.Null);
            Assert.That(attr.Label, Is.EqualTo("method-label"));
        }
    }
}
