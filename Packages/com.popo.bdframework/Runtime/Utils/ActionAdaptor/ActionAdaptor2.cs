using System;

namespace BDFramework.Adaptor
{
    /// <summary>
    /// action的包装类型，用来保存Action<T>值
    /// </summary>
    abstract public class AActionAdaptor
    {
        virtual public int ParamsNum { get; } = 0;
        /// <summary>
        /// 执行
        /// 1参数
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o)
        {
        }

        /// <summary>
        /// 执行
        /// 2参数
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o1, object o2)
        {
        }

        /// <summary>
        /// 执行
        /// 3参数
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o1, object o2, object o3)
        {
        }

        /// <summary>
        /// 执行
        /// 4参数
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o1, object o2, object o3, object o4)
        {
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        virtual public void TriggerEvent()
        {
        }

        virtual public bool Equals(object o)
        {
            return false;
        }
    }

    /// <summary>
    /// Action的适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionAdaptor<T1> : AActionAdaptor
    {
        //缓存的action
        public Action<T1> Action { get; private set; }

        public ActionAdaptor(Action<T1> action)
        {
            this.Action = action;
        }
        
        override public int ParamsNum { get; } = 1;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        public override void Invoke(object o1)
        {
            T1 t1 = (T1) o1;

            this.Action.Invoke(t1);
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

    /// <summary>
    /// Action的适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
