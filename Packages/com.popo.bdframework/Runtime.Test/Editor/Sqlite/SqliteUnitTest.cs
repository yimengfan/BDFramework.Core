using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BDFramework.Sql;
using BDFramework.Test;
using LitJson;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;


namespace BDFramework.EditorTest.SQLite
{
    /// <summary>
    /// 覆盖 SQLite 读写、查询与密码回退契约的编辑器单元测试集合。
    /// 常规情况下通过 Unity Test Runner 执行；当项目初始化会干扰 -runTests 时，可改走 BatchMode 显式验证入口。
    /// </summary>
    public class SqliteUnitTest
    {
        /// <summary>
        /// 提供给 batchmode 的显式验证入口。
        /// 当项目级初始化会干扰 Unity 原生 -runTests 流程时，可以通过 -executeMethod 直接调用这组受影响用例。
        /// </summary>
        public static void RunBatchVerification()
        {
            SqliteUnitTestBatchVerification.RunBatchVerification();
        }

        /// <summary>
        /// 单元测试的数据库
        /// </summary>
        private static string dbname = "Unitest_LiteDB";

        /// <summary>
        /// 在每个测试入口打印统一日志，方便本地与 CI 直接看到测试目的和实现手段。
        /// </summary>
        [SetUp]
        public void LogCurrentTestPurpose()
        {
            Debug.Log($"[测试开始] name={TestContext.CurrentContext.Test.Name} 测试目的=验证 SQLite 相关能力在当前用例下符合预期。 实现手段=执行 NUnit 测试并断言数据库操作结果、密码回退和查询结果。");
        }

        /// <summary>
        /// 配置单元测试
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            //构造插入数据
            var insertList = new List<UniTestSqliteType>();
            for (int i = 0; i < 1000; i++)
            {
                var t = new UniTestSqliteType()
                {
                    Id = i,
                    IdStr = i.ToString(),
                };
                insertList.Add(t);
            }

            //创建测试db
            var dbpath = IPath.Combine(Application.persistentDataPath, dbname);
            if (File.Exists(dbpath))
            {
                File.Delete(dbpath);
            }

            Debug.Log("打开数据库:" + dbname);
            SqliteLoder.LoadDBReadWriteCreate(dbpath);
            //if (!ILRuntimeHelper.IsRunning)
            {
                //Drop table
                SqliteHelper.GetDB(dbname).CreateTable<UniTestSqliteType>();
                SqliteHelper.GetDB(dbname).InsertTable(insertList);
            }


            Assert.That(true);
        }

        /// <summary>
        /// 验证当未显式设置密码时，SqliteLoder 会使用 PasswordFallback 返回配置层注入的默认密码。
        /// </summary>
        [Test]
        public void PasswordFallback_ReturnsFallbackWhenExplicitPasswordIsEmpty()
        {
            var oldPassword = SqliteLoder.password;
            var oldPasswordFallback = SqliteLoder.PasswordFallback;
            SqliteLoder.password = string.Empty;
            SqliteLoder.PasswordFallback = () => "fallback-password";

            try
            {
                Assert.AreEqual("fallback-password", SqliteLoder.Password,
                    "未显式设置密码时，应返回 PasswordFallback 提供的默认密码。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldPasswordFallback;
            }
        }

        /// <summary>
        /// 验证当显式密码已设置时，SqliteLoder 会优先使用显式值而不是 PasswordFallback。
        /// </summary>
        [Test]
        public void PasswordFallback_PrefersExplicitPasswordOverFallback()
        {
            var oldPassword = SqliteLoder.password;
            var oldPasswordFallback = SqliteLoder.PasswordFallback;
            SqliteLoder.password = "explicit-password";
            SqliteLoder.PasswordFallback = () => "fallback-password";

            try
            {
                Assert.AreEqual("explicit-password", SqliteLoder.Password,
                    "显式密码存在时，应优先返回显式密码而不是回退密码。");
            }
            finally
            {
                SqliteLoder.password = oldPassword;
                SqliteLoder.PasswordFallback = oldPasswordFallback;
            }
        }
        
        
        /// <summary>
        /// 数据库反序列化测试
        /// </summary>
        [Test, Order(1), Performance]
        static public void SqlItemDeSerializeTest()
        {
                        var rets = SqliteHelper.GetDB(dbname).GetTableRuntime().FromAll<UniTestSqliteType>();
            Debug.Log($"<color=yellow>插入条目：{rets.Count}</color>");
            //反序列化判断
            var source = new UniTestSqliteType() { };
            foreach (var item in rets)
            {
                //判断是否相等
                var ret = source.TestBool.Equals(item.TestBool);
                if (!ret)
                {
                    Assert.Fail("Bool类型校验失败");
                }
                ret = source.TestInt.Equals(item.TestInt);
                if (!ret)
                {
                    Assert.Fail("Int类型校验失败");
                }
                ret = source.TestString.Equals(item.TestString);
                if (!ret)
                {
                    Assert.Fail("String类型校验失败");
                }
                ret = source.TestFloat - item.TestFloat < Double.Epsilon;
                if (!ret)
                {
                    Assert.Fail("Float类型校验失败");
                }
                ret = source.TestDouble - item.TestDouble < Double.Epsilon;
                if (!ret)
                {
                    Assert.Fail("Double类型校验失败");
                }
                //数组判断相等
                var boolExceptArray = source.TestBoolArray.Except(item.TestBoolArray);
                if (boolExceptArray.Count() != 0)
                {
                    Assert.Fail("BoolArray类型校验失败");
                }
                var intExceptArray = source.TestIntArray.Except(item.TestIntArray);
                if (intExceptArray.Count() != 0)
                {
                    Assert.Fail("intArray类型校验失败");
                }
                var stringExceptArray = source.TestStringArray.Except(item.TestStringArray);
                if (stringExceptArray.Count() != 0)
                {
                    Assert.Fail("stringArray类型校验失败");
                }

                for (int i = 0; i < source.TestFloatArray.Length; i++)
                {
                    ret = source.TestFloatArray[i] - item.TestFloatArray[i] < Double.Epsilon;
                    if (!ret)
                    {
                        Assert.Fail("FloatArray类型校验失败");
                    }
                }
                
                for (int i = 0; i < source.TestDoubleArray.Length; i++)
                {
                    ret = source.TestDoubleArray[i] - item.TestDoubleArray[i] < Double.Epsilon;
                    if (!ret)
                    {
                        Assert.Fail("DoubleArray类型校验失败");
                    }
                }
            }

        }
        
        /// <summary>
        /// sselect语句
        /// </summary>
        [Test, Order(1), Performance]
        static public void Where()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        [Test, Order(1), Performance]
        static public void Where_String()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("idstr = '1'").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        
        [Test, Order(1), Performance]
        static public void WhereEqual()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereEqual("id",1).FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        [Test, Order(1), Performance]
        static public void WhereEqual_String()
        {
            //单条件查询
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereEqual("idstr","1").FromAll<UniTestSqliteType>();
            //对比返回数量和id
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 1);
        }
        
        /// <summary>
        /// limit语句
        /// </summary>
        [Test, Order(2), Performance]
        static public void Limit()
        {
            //单条件查询
            var d = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id != 1").Limit(1)
                .From<UniTestSqliteType>();
            if (d != null)
            {
                Debug.Log(JsonMapper.ToJson(d));
            }

            Assert.True(d != null && d.Id != 1);
        }

        /// <summary>
        /// 选择、or、and语句
        /// </summary>
        [Test, Order(3), Performance]
        static public void MultiResult_OR_And()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id > 1").And.Where("id < 3")
                .FromAll<UniTestSqliteType>();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 2);
            //

            ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id = 1").Or.Where("id = 3")
                .FromAll<UniTestSqliteType>();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 1);
            Assert.AreEqual(ds[1].Id, 3);
        }
        
        [Test, Order(3), Performance]
        static public void MultiResult_OR_And_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("id == 2").And.Where("idstr == '2'")
                .FromAll<UniTestSqliteType>();
            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 1);
            Assert.AreEqual(ds[0].Id, 2);
            //

            ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("idstr = '1'").Or.Where("idstr = '3'")
                .FromAll<UniTestSqliteType>();

            Debug.Log(JsonMapper.ToJson(ds));
            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 1);
            Assert.AreEqual(ds[1].Id, 3);
        }


        /// <summary>
        /// 多返回Whereand语句
        /// </summary>
        [Test, Order(4), Performance]
        static public void MultiResult_WhereAnd()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereAnd("id", "=", 1, 2)
                .FromAll<UniTestSqliteType>();
            Assert.AreEqual(ds.Count, 0);
        }
        [Test, Order(4), Performance]
        static public void MultiResult_WhereAnd_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereAnd("idstr", "=", "1", "2")
                .FromAll<UniTestSqliteType>();
            Assert.AreEqual(ds.Count, 0);
        }

        /// <summary>
        /// where or语句
        /// </summary>
        [Test, Order(5), Performance]
        static public void MultiResult_WhereOr()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereOr("id", "=", 2, 3)
                .FromAll<UniTestSqliteType>();

            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }
        [Test, Order(5), Performance]
        static public void MultiResult_WhereOr_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereOr("idstr", "=", "2", "3")
                .FromAll<UniTestSqliteType>();

            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }
        /// <summary>
        /// 多返回 Whre in 语句
        /// </summary>
        [Test, Order(6), Performance]
        static public void MultiResult_WhereIn()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereIn("id", 2, 3).FromAll<UniTestSqliteType>();
            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }

        [Test, Order(6), Performance]
        static public void MultiResult_WhereIn_String()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().WhereIn("idstr", "2", "3").FromAll<UniTestSqliteType>();
            Assert.AreEqual(ds.Count, 2);
            Assert.AreEqual(ds[0].Id, 2);
            Assert.AreEqual(ds[1].Id, 3);
        }

        /// <summary>
        /// 多返回-排序
        /// </summary>
        [Test, Order(7), Performance]
        static public void MultiResult_OrderByDesc()
        {
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("Id >= 0").OrderByDesc("Id")
                .FromAll<UniTestSqliteType>();

            //降序检测
            bool isPass = true;
            for (int i = 0; i < ds.Count - 1; i++)
            {
                if (ds[i].Id < ds[i + 1].Id)
                {
                    isPass = false;
                    break;
                }
            }

            Assert.True(isPass);
        }


        /// <summary>
        /// 多返回并且排序
        /// </summary>
        [Test, Order(8), Performance]
        static public void MultiResult_OrderBy()
        {
            BDebug.LogWatchBegin("order by");
            var ds = SqliteHelper.GetDB(dbname).GetTableRuntime().Where("Id >= 0").OrderBy("Id")
                .FromAll<UniTestSqliteType>();
            BDebug.LogWatchEnd("order by");

            //升序检测
            bool isPass = true;
            for (int i = 0; i < ds.Count - 1; i++)
            {
                if (ds[i].Id > ds[i + 1].Id)
                {
                    isPass = false;
                    break;
                }
            }

            Assert.True(isPass);
        }

        /// <summary>
        /// 关闭数据库
        /// </summary>
        [OneTimeTearDown]
        static public void Close()
        {
            SqliteLoder.Close(dbname);
            SqliteLoder.PasswordFallback = null;
            var dbpath = IPath.Combine(Application.persistentDataPath, dbname);
            if (File.Exists(dbpath))
            {
                File.Delete(dbpath);
            }

            Debug.Log("关闭数据库:" + dbname);
        }
    }

    /// <summary>
    /// SQLite 受影响用例的 BatchMode 显式验证入口。
    /// 用于在 Unity Test Runner 被项目初始化流程干扰时，仍能稳定验证密码回退与基础可读性契约。
    /// </summary>
    public static class SqliteUnitTestBatchVerification
    {
        /// <summary>
        /// 顺序执行受影响 SQLite 用例，写出批验证报告，并用退出码反馈结果。
        /// </summary>
        public static void RunBatchVerification()
        {
            Debug.Log("[测试开始] name=SqliteUnitTestBatchVerification 测试目的=验证 SQLite 密码回退与基础读写契约在 BatchMode 下保持稳定。 实现手段=直接调用 SqliteUnitTest 的初始化、受影响用例与清理入口，并写出批验证报告。");
            var reportBuilder = new StringBuilder();
            var failedCount = 0;
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Library",
                "SqliteUnitTestBatchVerification.txt");
            var test = new SqliteUnitTest();
            var checks = new (string Name, Action Action)[]
            {
                (nameof(SqliteUnitTest.SqlItemDeSerializeTest), SqliteUnitTest.SqlItemDeSerializeTest),
                (nameof(SqliteUnitTest.PasswordFallback_ReturnsFallbackWhenExplicitPasswordIsEmpty),
                    test.PasswordFallback_ReturnsFallbackWhenExplicitPasswordIsEmpty),
                (nameof(SqliteUnitTest.PasswordFallback_PrefersExplicitPasswordOverFallback),
                    test.PasswordFallback_PrefersExplicitPasswordOverFallback),
            };

            try
            {
                // Phase 1: 先完成测试数据库初始化，再顺序执行受影响用例。
                test.Setup();
                for (var index = 0; index < checks.Length; index++)
                {
                    var check = checks[index];
                    RunCheck(index + 1, checks.Length, check.Name, check.Action, reportBuilder, ref failedCount);
                }
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL SetupOrSuite {exception}");
            }
            finally
            {
                // Phase 2: 无论成功失败都清理测试数据库，避免污染后续本地与 CI 验证。
                try
                {
                    SqliteUnitTest.Close();
                }
                catch (Exception exception)
                {
                    failedCount++;
                    reportBuilder.AppendLine($"FAIL TearDown {exception}");
                }
            }

            // Phase 3: 持久化报告并通过显式退出码反馈整体结果。
            reportBuilder.Insert(0,
                $"Summary: total={checks.Length} passed={checks.Length - failedCount} failed={failedCount}{Environment.NewLine}");
            File.WriteAllText(outputPath, reportBuilder.ToString(), Encoding.UTF8);
            if (failedCount > 0)
            {
                Debug.LogError($"Sqlite batch verification failed. Report: {outputPath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"Sqlite batch verification passed. Report: {outputPath}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 执行单个 SQLite 验证步骤并把结果写入批验证报告。
        /// </summary>
        private static void RunCheck(int currentIndex, int totalCount, string checkName, Action checkAction,
            StringBuilder reportBuilder, ref int failedCount)
        {
            Debug.Log($"[测试进度] suite=SqliteUnitTestBatchVerification current={currentIndex}/{totalCount} name={checkName} 测试目的=验证 {checkName} 对应的 SQLite 行为契约。 实现手段=直接调用受影响测试入口并断言返回结果。");
            try
            {
                checkAction();
                reportBuilder.AppendLine($"PASS {checkName}");
            }
            catch (Exception exception)
            {
                failedCount++;
                reportBuilder.AppendLine($"FAIL {checkName} {exception}");
            }
        }
    }
}
