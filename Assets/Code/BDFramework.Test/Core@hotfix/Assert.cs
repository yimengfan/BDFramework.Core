using System;
using UnityEngine;

namespace BDFramework.UnitTest
{
    static public class Assert
    {
        /// <summary>
        /// 判断是否为空
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        static public bool IsNull(object o, string failMessage = "Fail")
        {
            var ret = o == null;
            Log(!ret, failMessage);
            return ret;
        }

        /// <summary>
        /// 是否为false
        /// </summary>
        /// <returns></returns>
        static public bool IsPass(bool b, string failMessage = "Fail")
        {
            Log(b, failMessage);
            return b;
        }

        /// <summary>
        /// 比较值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="obj2"></param>
        static public bool Equals(object obj, object obj2, string failMessage = "Fail")
        {
            var ret = obj.Equals(obj2);

            Log(ret, failMessage);
            return ret;
        }


        /// <summary>
        /// log
        /// </summary>
        /// <param name="ret"></param>
        /// <exception cref="Exception"></exception>
        static void Log(bool ret, string failMessage = "")
        {
            if (ILRuntimeHelper.IsRunning)
            {
            }
            else
            {
                if (!ret)
                {
                    throw new Exception(failMessage);
                }
            }
        }
    }
}