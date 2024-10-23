using System;
using System.Collections.Generic;
using System.Linq;

namespace BDFramework.UFlux
{ 
    /// <summary>
    /// uimsgdata基类
    /// </summary>
    abstract  public class UIMsgData
    {
        /// <summary>
        /// 获取msg实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetMsg<T>() where T : UIMsgData
        {
            return this as T;
        }
    }
}