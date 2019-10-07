using System;
using System.Collections.Generic;
using System.Linq;

namespace BDFramework.UFlux
{
    public class UIMessage
    {

        /// <summary>
        /// msg name
        /// </summary>
        public Enum Name { get; private set; }
        private  object Data { get;  set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        private UIMessage(Enum name, object obj)
        {
            this.Name = name;
            this.Data = obj;
        }
        
        /// <summary>
        /// 获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetData<T>() where T: class
        {
            return Data as T;
        }
    
    }
}