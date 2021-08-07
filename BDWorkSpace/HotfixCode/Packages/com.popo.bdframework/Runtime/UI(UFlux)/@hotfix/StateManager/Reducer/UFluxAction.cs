using System;

namespace BDFramework.UFlux.Reducer
{
    public class UFluxAction
    {
        /// <summary>
        /// Action的枚举
        /// </summary>
        public Enum ActionTag { get; set; }
        
        public object Params { get; private set; }
        /// <summary>
        /// 设置Params
        /// </summary>
        /// <param name="o"></param>
        public void SetParams(object o)
        {
            this.Params = o;
        }
        
        /// <summary>
        /// 获取参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetParams<T>() 
        {
           return  (T)this.Params;
        }
    }
}