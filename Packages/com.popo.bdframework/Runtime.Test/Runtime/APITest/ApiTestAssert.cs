using System;
using System.Collections.Generic;

namespace BDFramework.RuntimeTests.ApiTest
{
    /// <summary>
    /// Runtime API 测试使用的轻量断言辅助器。
    /// Lightweight assertion helper used by runtime API tests.
    /// 该辅助器通过抛出异常表达失败，便于同一套 API 测试在 Editor NUnit、BatchMode 与 Runtime Talos E2E 三条路径中复用。
    /// This helper expresses failures through exceptions so the same API tests can be reused across editor NUnit, BatchMode, and runtime Talos E2E paths.
    /// </summary>
    public static class ApiTestAssert
    {
        /// <summary>
        /// 验证条件为真，否则抛出异常。
        /// Verify that a condition is true; otherwise throw an exception.
        /// </summary>
        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 验证条件为假，否则抛出异常。
        /// Verify that a condition is false; otherwise throw an exception.
        /// </summary>
        public static void IsFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 验证两个值相等，否则抛出异常。
        /// Verify that two values are equal; otherwise throw an exception.
        /// </summary>
        public static void AreEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"{message} Expected=<{expected}> Actual=<{actual}>.");
            }
        }

        /// <summary>
        /// 验证两个对象引用相同，否则抛出异常。
        /// Verify that two object references are the same; otherwise throw an exception.
        /// </summary>
        public static void AreSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 验证两个对象引用不同，否则抛出异常。
        /// Verify that two object references are different; otherwise throw an exception.
        /// </summary>
        public static void AreNotSame(object first, object second, string message)
        {
            if (ReferenceEquals(first, second))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 验证对象不为空，否则抛出异常。
        /// Verify that an object is not null; otherwise throw an exception.
        /// </summary>
        public static void IsNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 验证对象为空，否则抛出异常。
        /// Verify that an object is null; otherwise throw an exception.
        /// </summary>
        public static void IsNull(object value, string message)
        {
            if (value != null)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// 验证两个字节序列完全一致，否则抛出异常。
        /// Verify that two byte sequences are identical; otherwise throw an exception.
        /// </summary>
        public static void SequenceEqual(byte[] expected, byte[] actual, string message)
        {
            if (expected == null || actual == null)
            {
                if (expected == actual)
                {
                    return;
                }

                throw new InvalidOperationException(message);
            }

            if (expected.Length != actual.Length)
            {
                throw new InvalidOperationException($"{message} ExpectedLength={expected.Length} ActualLength={actual.Length}.");
            }

            for (var index = 0; index < expected.Length; index++)
            {
                if (expected[index] != actual[index])
                {
                    throw new InvalidOperationException($"{message} MismatchIndex={index} ExpectedByte={expected[index]} ActualByte={actual[index]}.");
                }
            }
        }
    }
}