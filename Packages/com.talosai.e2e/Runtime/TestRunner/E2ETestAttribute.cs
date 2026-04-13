using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace Talos.E2E
{
    /// <summary>
    /// E2E 测试用例属性标记。
    /// 标记在静态方法上，表示该方法是一个 E2E 测试用例。
    /// 与 BDFramework 原生 UnitTestAttribute 类似，但增加了 suite（测试套件）和 timeout（超时）支持。
    /// 
    /// 使用示例：
    /// <code>
    /// [E2ETest(suite: "启动流程", order: 1, des: "验证框架初始化", timeout: 30000)]
    /// static public void TestFrameworkInit() { ... }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class E2ETestAttribute : Attribute
    {
        /// <summary>
        /// 测试所属套件名称，用于分组和按套件执行。
        /// </summary>
        public string Suite { get; private set; }

        /// <summary>
        /// 执行顺序，数字越小越先执行。
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// 测试描述，说明测试目的和验证场景。
        /// </summary>
        public string Des { get; private set; }

        /// <summary>
        /// 单个测试用例的超时时间（毫秒），默认 60 秒。
        /// 超时后标记为失败。
        /// </summary>
        public int Timeout { get; private set; }

        public E2ETestAttribute(string suite = "默认", int order = 0, string des = "", int timeout = 60000)
        {
            Suite = suite;
            Order = order;
            Des = des;
            Timeout = timeout;
        }
    }
}
