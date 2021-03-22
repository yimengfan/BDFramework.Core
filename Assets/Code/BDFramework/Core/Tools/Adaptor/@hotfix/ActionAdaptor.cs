using System;

namespace BDFramework.Adaptor
{
    /// <summary>
    /// action的Base
    /// </summary>
    abstract public class AActionAdaptor
    {
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o)
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