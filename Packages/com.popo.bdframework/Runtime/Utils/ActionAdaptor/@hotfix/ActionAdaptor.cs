using System;

namespace BDFramework.Adaptor
{
    /// <summary>
    /// Action 适配器统一基类，用于承载不同参数数量的委托桥接能力。
    /// </summary>
    abstract public class AActionAdaptor
    {
        /// <summary>
        /// 当前适配器支持的参数数量。
        /// </summary>
        virtual public int ParamsNum { get; } = 0;

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o)
        {
        }

        /// <summary>
        /// 执行
        /// 2参数
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        virtual public void Invoke(object o1, object o2)
        {
        }

        /// <summary>
        /// 执行
        /// 3参数
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <param name="o3"></param>
        virtual public void Invoke(object o1, object o2, object o3)
        {
        }

        /// <summary>
        /// 执行
        /// 4参数
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <param name="o3"></param>
        /// <param name="o4"></param>
        virtual public void Invoke(object o1, object o2, object o3, object o4)
        {
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        virtual   public  void TriggerEvent()
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
    public class ActionAdaptor<T> :AActionAdaptor  where T : class
    {
        //缓存的action
        public Action<T> Action { get; private set; }
        public ActionAdaptor(Action<T> action)
        {
            this.Action = action;
        }

        override public int ParamsNum { get; } = 1;

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        public override void Invoke(object o)
        {
            if (o != null)
            {
                var t = (T)o;
                Action.Invoke(t);
            }
            else
            {
               
                Action.Invoke(null);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        public override bool Equals(object o)
        {
            return this.Action.Equals(o);
        }
    }
}