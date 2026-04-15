using System;

namespace BDFramework.Adaptor
{
    /// <summary>
    /// 双参数 Action 的适配器，用于复用统一基类补充多参委托桥接。
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class ActionAdaptor<T1, T2> : AActionAdaptor where T1 : class
    {
        //缓存的action
        public Action<T1, T2> Action { get; private set; }

        public ActionAdaptor(Action<T1, T2> action)
        {
            this.Action = action;
        }
        override public int ParamsNum { get; } = 2;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        public override void Invoke(object o1, object o2)
        {
            T1 t1 = (T1) o1;
            T2 t2 = default(T2);
            if (o2 != null)
            {
                t2 = (T2) o2;
            }
            this.Action.Invoke(t1, t2);
        }

        /// <summary>
        /// 比较
        /// </summary>
        /// <param name="o"></param>
        public override bool Equals(object o)
        {
            return this.Action.Equals(o);
        }
    }
}
